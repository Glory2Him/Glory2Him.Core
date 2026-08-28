import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
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

import { contentItemSettingService } from '../../services/foundations/contentItemSettingService';
import { useDocumentTitle } from '../useDocumentTitle';
import { extractApiErrorMessage } from './apiErrorMessage';

// One ContentItemSetting row, edited in place. The scope — content type and content item — is
// shown but not editable: the unique indexes allow one default per type and one override per
// item, so moving a row to another scope is creating a different policy rather than changing
// this one, and it would collide with whatever already occupies the target.

const settingsRoute = '/Admin/ContentItemSettings';

// Design §12.5.2 caps these in the foundation's validation; stating them here turns a 400 into
// an input that will not accept the 51st character.
const contentTypeNameMaxLength = 50;
const contentTypeDescriptionMaxLength = 500;

// Only the setting's boolean members can be wired to a switch, so a mistyped field name below
// is a compile error rather than a switch that silently never moves.
type ContentItemSettingFlag = {
    [TField in keyof ContentItemSetting]:
    ContentItemSetting[TField] extends boolean ? TField : never
}[keyof ContentItemSetting];

// Each pair is one of design §6.10's resolved features: "allowed" governs whether something new
// can be created against a content item of this type, "shown" whether it renders at all. They
// are independent — a closed comment thread that still displays its history sets allowed off
// and shown on.
type FeatureField = {
    title: string;
    allowedLabel: string;
    shownLabel: string;
    allowed: ContentItemSettingFlag;
    shown: ContentItemSettingFlag;
};

const featureFields: ReadonlyArray<FeatureField> = [
    {
        title: 'Tags',
        allowedLabel: 'Tags can be added',
        shownLabel: 'Tags are shown',
        allowed: 'tagsAllowed',
        shown: 'showTags',
    },
    {
        title: 'Reactions',
        allowedLabel: 'Reactions can be added',
        shownLabel: 'Reactions are shown',
        allowed: 'reactionsAllowed',
        shown: 'showReactions',
    },
    {
        title: 'Links',
        allowedLabel: 'Links can be added',
        shownLabel: 'Links are shown',
        allowed: 'linksAllowed',
        shown: 'showLinks',
    },
    {
        title: 'Attachments',
        allowedLabel: 'Attachments can be added',
        shownLabel: 'Attachments are shown',
        allowed: 'attachmentsAllowed',
        shown: 'showAttachments',
    },
    {
        title: 'Comments',
        allowedLabel: 'Comments can be added',
        shownLabel: 'Comments are shown',
        allowed: 'commentsAllowed',
        shown: 'showComments',
    },
    {
        title: 'Bible references',
        allowedLabel: 'Bible references can be added',
        shownLabel: 'Bible references are shown',
        allowed: 'bibleReferenceAllowed',
        shown: 'showBibleReferences',
    },
];

export const ContentItemSettingDetailPage = () => {
    const { contentItemSettingId = '' } = useParams();
    const navigate = useNavigate();

    const { data: setting, isLoading, isError } =
        contentItemSettingService.useGetContentItemSettingById(
            contentItemSettingId,
            contentItemSettingId.length > 0);

    const updateContentItemSetting = contentItemSettingService.useUpdateContentItemSetting();

    // Edit a copy, so an abandoned edit never leaves the displayed row half-changed.
    const [editModel, setEditModel] = useState<ContentItemSetting | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);
    const [actionMessage, setActionMessage] = useState<string | null>(null);

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

    const goBack = () => navigate(settingsRoute);

    const setField = <TField extends keyof ContentItemSetting>(
        field: TField,
        value: ContentItemSetting[TField]) =>
        setEditModel((current) => current == null ? current : { ...current, [field]: value });

    const saveAsync = async () => {
        if (editModel == null) {
            return;
        }

        setActionError(null);
        setActionMessage(null);

        try {
            await updateContentItemSetting.mutateAsync(editModel);

            setActionMessage('Settings saved.');
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

                    {actionError != null && (
                        <div className="alert alert-danger" role="alert">{actionError}</div>
                    )}
                    {actionMessage != null && (
                        <div className="alert alert-success" role="alert">{actionMessage}</div>
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
                    </Card>

                    <Card cssClass="mb-4" headerContent="Features">
                        {featureFields.map((feature) => (
                            <div className="row align-items-center border-top py-2" key={feature.title}>
                                <div className="col-md-3 fw-semibold">{feature.title}</div>
                                <div className="col-md-4">
                                    <FormSwitch
                                        label={feature.allowedLabel}
                                        value={editModel[feature.allowed]}
                                        onValueChange={(value) => setField(feature.allowed, value)} />
                                </div>
                                <div className="col-md-5">
                                    <FormSwitch
                                        label={feature.shownLabel}
                                        value={editModel[feature.shown]}
                                        onValueChange={(value) => setField(feature.shown, value)} />
                                </div>
                            </div>
                        ))}

                        <div className="border-top pt-3 mt-2">
                            <FormSwitch
                                label="Limit reactions to love only"
                                value={editModel.limitReactionsToLoveOnly}
                                onValueChange={(value) => setField('limitReactionsToLoveOnly', value)} />
                            <p className="text-body-secondary small mb-0">
                                Favourite-style behaviour: only the designated love reaction may be
                                associated with content items of this type.
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
