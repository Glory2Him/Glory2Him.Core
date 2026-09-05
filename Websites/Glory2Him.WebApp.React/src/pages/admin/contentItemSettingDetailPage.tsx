import { useEffect, useState } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { Card } from '../../components/coreUI/card';
import { formatDateTime } from '../../components/coreUI/dateFormats';
import { FormSwitch } from '../../components/coreUI/formSwitch';
import { FormText } from '../../components/coreUI/formText';
import { Spinner } from '../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';

import {
    ContentType,
    contentTypeLabels
} from '../../models/foundations/contentItemSettings/contentType';

import {
    contentItemSettingFeatureFields,
    limitReactionsToLoveOnlyDescription,
    limitReactionsToLoveOnlyLabel
} from '../../models/components/contentItemSettings/contentItemSettingFeature';

import { contentItemSettingService } from '../../services/foundations/contentItemSettingService';
import { useDocumentTitle } from '../useDocumentTitle';
import { extractApiErrorMessage } from './apiErrorMessage';

// One ContentItemSetting row, edited in place. The scope — content type and content item — is
// shown but not editable: the unique indexes allow one default per type and one override per
// item, so moving a row to another scope is creating a different policy rather than changing
// this one, and it would collide with whatever already occupies the target.
//
// WHERE THE WAY OUT LEADS. The list hands its own address over in router state when Manage is
// taken, so both exits — Back and a successful save — return to the FILTERED page the
// administrator was working through rather than to an unfiltered first page. Opened directly
// (a pasted link, a refresh) there is no origin to honour, and the bare list is the honest
// fallback.

const settingsRoute = '/Admin/ContentItemSettings';

// Design §12.5.2 caps these in the foundation's validation; stating them here turns a 400 into
// an input that will not accept the 51st character.
const contentTypeNameMaxLength = 50;
const contentTypeDescriptionMaxLength = 500;

// The foundation refuses a negative SortOrder, so the input refuses one too rather than letting
// the save come back a 400. A blank box reads as 0 rather than NaN — there is no "no order".
const sortOrderMinimum = 0;

export const ContentItemSettingDetailPage = () => {
    const { contentItemSettingId = '' } = useParams();
    const navigate = useNavigate();
    const location = useLocation();

    const { data: setting, isLoading, isError } =
        contentItemSettingService.useGetContentItemSettingById(
            contentItemSettingId,
            contentItemSettingId.length > 0);

    const updateContentItemSetting = contentItemSettingService.useUpdateContentItemSetting();

    // Edit a copy, so an abandoned edit never leaves the displayed row half-changed.
    const [editModel, setEditModel] = useState<ContentItemSetting | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);

    useDocumentTitle(setting == null
        ? 'Content Item Setting — Glory 2 Him'
        : `${setting.contentTypeName} settings — Glory 2 Him`);

    useEffect(() => {
        if (setting != null) {
            setEditModel({ ...setting });
        }
    }, [setting]);

    const crumbs: BreadcrumbItem[] = [
        { title: 'Admin' },
        { title: 'Content Item Settings', href: settingsRoute },
        { title: setting?.contentTypeName ?? 'Setting', isActive: true },
    ];

    // The view this page was opened from, filters and page and all. Absent when the page was
    // reached without going through the list.
    const backRoute =
        (location.state as { from?: string } | null)?.from ?? settingsRoute;

    const goBack = () => navigate(backRoute);

    const setField = <TField extends keyof ContentItemSetting>(
        field: TField,
        value: ContentItemSetting[TField]) =>
        setEditModel((current) => current == null ? current : { ...current, [field]: value });

    const saveAsync = async () => {
        if (editModel == null) {
            return;
        }

        setActionError(null);

        try {
            await updateContentItemSetting.mutateAsync(editModel);

            // Saved IS finished here, so the save leaves the same way Back does rather than
            // parking the administrator on a form they are done with. The mutation invalidates
            // the list read on its way out, so the row they land on is the row they just wrote.
            goBack();
        } catch (error) {
            setActionError(extractApiErrorMessage(
                error, 'The settings could not be saved. Please try again.'));
        }
    };

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">Content Item Setting</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            {isLoading ? (
                <div className="text-center py-5">
                    <Spinner />
                </div>
            ) : isError || setting == null || editModel == null ? (
                <>
                    <div className="alert alert-danger" role="alert">
                        We could not load this content item setting right now. Please try again later.
                    </div>
                    <Button color="secondary" onClick={goBack}>Back to Content Item Settings</Button>
                </>
            ) : (
                <>
                    <div className="d-flex justify-content-end mb-3">
                        <Button color="secondary" onClick={goBack}>
                            <i className="bi bi-arrow-left me-1"></i>Back to Content Item Settings
                        </Button>
                    </div>

                    {/* A refused save is the one outcome that keeps the reader here, so it is
                        the one that still has something to say on this page. */}
                    {actionError != null && (
                        <div className="alert alert-danger" role="alert">{actionError}</div>
                    )}

                    <Card cssClass="mb-4" headerContent="Scope">
                        <div className="d-flex align-items-center gap-3 mb-4">
                            <i
                                className={`bi ${editModel.contentTypeIconCssClass} text-primary display-6`}
                                aria-hidden="true"></i>
                            <div>
                                <div className="h5 mb-1">{setting.contentTypeName}</div>
                                <div className="text-body-secondary small font-monospace">{setting.id}</div>
                            </div>
                        </div>

                        <div className="d-flex flex-wrap gap-2 mb-4">
                            <span className="badge text-bg-primary">
                                {contentTypeLabels[setting.contentType] ?? ContentType[setting.contentType]}
                            </span>
                            <span className={`badge ${setting.contentItemId == null
                                ? 'text-bg-secondary'
                                : 'text-bg-info'}`}>
                                {setting.contentItemId == null ? 'Type default' : 'Item override'}
                            </span>
                        </div>

                        {setting.contentItemId != null && (
                            <p className="mb-3">
                                <span className="text-body-secondary">Content item </span>
                                <span className="font-monospace">{setting.contentItemId}</span>
                            </p>
                        )}

                        <p className="text-body-secondary mb-0 small">
                            {setting.contentItemId == null
                                ? 'This is the default for every content item of this type that has no override of its own.'
                                : 'This overrides the content type default for one content item.'}
                            {' '}Created by {setting.createdBy} on {formatDateTime(new Date(setting.createdWhen))}.
                            {' '}Last updated by {setting.updatedBy} on {formatDateTime(new Date(setting.updatedWhen))}.
                        </p>
                    </Card>

                    <Card cssClass="mb-4" headerContent="Presentation">
                        <div className="row">
                            <div className="col-md-6">
                                <FormText
                                    label="Name"
                                    value={editModel.contentTypeName ?? ''}
                                    onValueChange={(value) =>
                                        setField('contentTypeName', value.slice(0, contentTypeNameMaxLength))} />
                            </div>
                            <div className="col-md-6">
                                <FormText
                                    label="Icon CSS class"
                                    placeholder="bi-quote"
                                    value={editModel.contentTypeIconCssClass ?? ''}
                                    onValueChange={(value) => setField('contentTypeIconCssClass', value)} />
                            </div>
                        </div>

                        <div className="row">
                            <div className="col-md-6 mb-3">
                                <label className="form-label" htmlFor="sortOrder">Sort order</label>
                                <input
                                    id="sortOrder"
                                    type="number"
                                    min={sortOrderMinimum}
                                    step={1}
                                    className="form-control"
                                    value={editModel.sortOrder}
                                    onChange={(event) => setField(
                                        'sortOrder',
                                        Math.max(
                                            sortOrderMinimum,
                                            Number.parseInt(event.target.value, 10) || 0))} />
                                <div className="form-text">
                                    Where this type sits on the contribute page's type picker.
                                    Lower comes first.
                                </div>
                            </div>
                        </div>

                        <div className="mb-3">
                            <label className="form-label" htmlFor="contentTypeDescription">Description</label>
                            <textarea
                                id="contentTypeDescription"
                                className="form-control"
                                rows={3}
                                maxLength={contentTypeDescriptionMaxLength}
                                value={editModel.contentTypeDescription ?? ''}
                                onChange={(event) =>
                                    setField('contentTypeDescription', event.target.value)}></textarea>
                            <div className="form-text">
                                Shown beside the type on the contribute page.
                                {' '}{(editModel.contentTypeDescription ?? '').length}
                                {' / '}{contentTypeDescriptionMaxLength}
                            </div>
                        </div>
                    </Card>

                    <Card cssClass="mb-4" headerContent="Contributions">
                        <FormSwitch
                            label="Open to general user contributions"
                            value={editModel.isAvailableAsGeneralUserContribution}
                            onValueChange={(value) =>
                                setField('isAvailableAsGeneralUserContribution', value)} />

                        <FormSwitch
                            label="Has a title"
                            value={editModel.hasTitle}
                            onValueChange={(value) => setField('hasTitle', value)} />

                        <FormSwitch
                            label="Has an author"
                            value={editModel.hasAuthor}
                            onValueChange={(value) => setField('hasAuthor', value)} />

                        {/* The field ceilings the contribute and edit forms enforce.
                            Blank is the honest "no limit" — null on the wire — rather
                            than a zero nobody could type under. */}
                        <div className="row mt-3">
                            {([
                                ['maxTitleLength', 'Max title length'],
                                ['maxAuthorLength', 'Max author length'],
                                ['maxContentLength', 'Max content length']
                            ] as const).map(([fieldName, label]) => (
                                <div className="col-md-4 mb-3" key={fieldName}>
                                    <label className="form-label" htmlFor={fieldName}>
                                        {label}
                                    </label>

                                    <input
                                        id={fieldName}
                                        type="number"
                                        min={1}
                                        step={1}
                                        className="form-control"
                                        value={editModel[fieldName] ?? ''}
                                        onChange={(event) => setField(
                                            fieldName,
                                            event.target.value.length === 0
                                                ? null
                                                : Math.max(1, Number.parseInt(
                                                    event.target.value, 10) || 1))} />

                                    <div className="form-text">Blank means no limit.</div>
                                </div>
                            ))}
                        </div>
                    </Card>

                    <Card cssClass="mb-4" headerContent="Features">
                        {contentItemSettingFeatureFields.map((feature) => (
                            <div className="row align-items-center border-top py-2" key={feature.title}>
                                <div className="col-md-3 fw-semibold">{feature.title}</div>
                                <div className="col-md-4">
                                    <FormSwitch
                                        label={feature.shownLabel}
                                        value={editModel[feature.shown]}
                                        onValueChange={(value) => setField(feature.shown, value)} />
                                </div>
                                <div className="col-md-5">
                                    <FormSwitch
                                        label={feature.allowedLabel}
                                        value={editModel[feature.allowed]}
                                        onValueChange={(value) => setField(feature.allowed, value)} />
                                </div>
                            </div>
                        ))}

                        <div className="border-top pt-3 mt-2">
                            <FormSwitch
                                label={limitReactionsToLoveOnlyLabel}
                                value={editModel.limitReactionsToLoveOnly}
                                onValueChange={(value) => setField('limitReactionsToLoveOnly', value)} />
                            <p className="text-body-secondary small mb-0">
                                {limitReactionsToLoveOnlyDescription}
                            </p>
                        </div>
                    </Card>

                    <div className="d-flex gap-2 mb-4">
                        <Button
                            color="primary"
                            disabled={updateContentItemSetting.isPending}
                            onClick={() => void saveAsync()}>
                            {updateContentItemSetting.isPending ? 'Saving...' : 'Save settings'}
                        </Button>

                        <Button
                            color="outline-secondary"
                            disabled={updateContentItemSetting.isPending}
                            onClick={() => setEditModel({ ...setting })}>
                            Reset
                        </Button>
                    </div>
                </>
            )}
        </>
    );
};
