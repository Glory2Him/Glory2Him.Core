import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { Card } from '../../components/coreUI/card';
import { formatDate } from '../../components/coreUI/dateFormats';
import { Spinner } from '../../components/coreUI/spinner';
import { BreadcrumbItem } from '../../models/coreUI/breadcrumbItem';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';

import {
    ContentItemSettingQuery,
    ContentItemSettingScope
} from '../../models/foundations/contentItemSettings/contentItemSettingQuery';

import {
    ContentType,
    contentTypeLabels,
    contentTypeMembers
} from '../../models/foundations/contentItemSettings/contentType';

import { contentItemSettingService } from '../../services/foundations/contentItemSettingService';
import { useDocumentTitle } from '../useDocumentTitle';

// The master list of ContentItemSetting rows. Searching, filtering and paging all run
// server-side over [EnableQuery] rather than in the DataTable component the other admin lists
// use: the host caps a collection read at OData:PageSize rows, so an in-memory table would go
// quietly blind past that cap once per-item overrides outnumber the eight type defaults.

const settingsRoute = '/Admin/ContentItemSettings';
const pageSize = 10;
const searchDebounceMilliseconds = 300;

const crumbs: BreadcrumbItem[] = [
    { title: 'Admin' },
    { title: 'Content Item Settings', href: settingsRoute, isActive: true },
];

const scopeOptions: ReadonlyArray<{ value: ContentItemSettingScope; text: string }> = [
    { value: 'All', text: 'All scopes' },
    { value: 'Default', text: 'Type defaults' },
    { value: 'Override', text: 'Item overrides' },
];

// The feature pairs design §6.10 resolves, in the order the detail page edits them. "Allowed"
// governs whether something new can be created; "shown" governs whether it renders at all.
const featureColumns: ReadonlyArray<{
    title: string;
    allowed: (setting: ContentItemSetting) => boolean;
    shown: (setting: ContentItemSetting) => boolean;
}> = [
    { title: 'Tags', allowed: (s) => s.tagsAllowed, shown: (s) => s.showTags },
    { title: 'Reactions', allowed: (s) => s.reactionsAllowed, shown: (s) => s.showReactions },
    { title: 'Links', allowed: (s) => s.linksAllowed, shown: (s) => s.showLinks },
    { title: 'Attachments', allowed: (s) => s.attachmentsAllowed, shown: (s) => s.showAttachments },
    { title: 'Comments', allowed: (s) => s.commentsAllowed, shown: (s) => s.showComments },
    { title: 'Bible refs', allowed: (s) => s.bibleReferenceAllowed, shown: (s) => s.showBibleReferences },
];

export const ContentItemSettingsPage = () => {
    const navigate = useNavigate();

    useDocumentTitle('Content Item Settings — Glory 2 Him');

    const [searchInput, setSearchInput] = useState('');
    const [searchTerm, setSearchTerm] = useState('');
    const [contentType, setContentType] = useState<ContentType | undefined>(undefined);
    const [scope, setScope] = useState<ContentItemSettingScope>('All');
    const [page, setPage] = useState(1);

    // Every keystroke would otherwise be its own request and its own cache entry.
    useEffect(() => {
        const timeoutId = window.setTimeout(
            () => setSearchTerm(searchInput),
            searchDebounceMilliseconds);

        return () => window.clearTimeout(timeoutId);
    }, [searchInput]);

    // A narrower filter can leave the current page past the end of the results, which reads as
    // an empty list rather than as a filter that matched something.
    useEffect(() => {
        setPage(1);
    }, [searchTerm, contentType, scope]);

    const query = useMemo<ContentItemSettingQuery>(
        () => ({ searchTerm, contentType, scope, page, pageSize }),
        [searchTerm, contentType, scope, page]);

    const { data: settingsPage, isLoading, isError, isFetching } =
        contentItemSettingService.useGetContentItemSettings(query);

    const settings = settingsPage?.items;
    const hasFilters = searchTerm.length > 0 || contentType != null || scope !== 'All';

    const manageSetting = (contentItemSettingId: string) =>
        navigate(`${settingsRoute}/${contentItemSettingId}`);

    const clearFilters = () => {
        setSearchInput('');
        setSearchTerm('');
        setContentType(undefined);
        setScope('All');
    };

    return (
        <>
            <div className="d-flex flex-wrap justify-content-between align-items-center mb-3">
                <h1 className="h3 mb-0">Content Item Settings</h1>
                <Breadcrumb items={crumbs} />
            </div>
            <hr />

            <p className="text-body-secondary">
                What each content type offers its readers and its contributors. A row is either
                the default for a content type or an override for one content item, and the
                override wins where both exist.
            </p>

            <Card>
                <div className="row g-2 mb-3">
                    <div className="col-md-5">
                        <input
                            type="search"
                            className="form-control"
                            placeholder="Search name or description..."
                            aria-label="Search content item settings"
                            value={searchInput}
                            onChange={(event) => setSearchInput(event.target.value)} />
                    </div>
                    <div className="col-md-3">
                        <select
                            className="form-select"
                            aria-label="Content type"
                            value={contentType == null ? '' : String(contentType)}
                            onChange={(event) =>
                                setContentType(event.target.value === ''
                                    ? undefined
                                    : Number(event.target.value) as ContentType)}>
                            <option value="">All content types</option>
                            {contentTypeMembers.map((member) => (
                                <option key={member} value={member}>{contentTypeLabels[member]}</option>
                            ))}
                        </select>
                    </div>
                    <div className="col-md-3">
                        <select
                            className="form-select"
                            aria-label="Scope"
                            value={scope}
                            onChange={(event) =>
                                setScope(event.target.value as ContentItemSettingScope)}>
                            {scopeOptions.map((option) => (
                                <option key={option.value} value={option.value}>{option.text}</option>
                            ))}
                        </select>
                    </div>
                    <div className="col-md-1 d-grid">
                        <Button color="outline-secondary" disabled={!hasFilters} onClick={clearFilters}>
                            Clear
                        </Button>
                    </div>
                </div>

                {isLoading ? (
                    <div className="text-center py-5">
                        <Spinner />
                    </div>
                ) : isError ? (
                    <div className="alert alert-danger mb-0" role="alert">
                        We could not load content item settings right now. Please try again later.
                    </div>
                ) : (
                    <>
                        <div className={`table-responsive ${isFetching ? 'opacity-75' : ''}`}>
                            <table className="table table-hover align-middle">
                                <thead>
                                    <tr>
                                        <th>Content type</th>
                                        <th>Scope</th>
                                        <th>Contributions</th>
                                        {featureColumns.map((feature) => (
                                            <th key={feature.title} className="text-center">{feature.title}</th>
                                        ))}
                                        <th>Updated</th>
                                        <th className="text-end"></th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {(settings ?? []).map((setting) => (
                                        <tr key={setting.id}>
                                            <td>
                                                <div className="d-flex align-items-center gap-2">
                                                    <i
                                                        className={`bi ${setting.contentTypeIconCssClass} text-primary fs-5`}
                                                        aria-hidden="true"></i>
                                                    <div>
                                                        <div className="fw-semibold">{setting.contentTypeName}</div>
                                                        <div className="small text-body-secondary">
                                                            {contentTypeLabels[setting.contentType]
                                                                ?? ContentType[setting.contentType]}
                                                        </div>
                                                    </div>
                                                </div>
                                            </td>
                                            <td>
                                                {setting.contentItemId == null ? (
                                                    <span className="badge text-bg-secondary">Type default</span>
                                                ) : (
                                                    <>
                                                        <span className="badge text-bg-info">Item override</span>
                                                        <div
                                                            className="small text-body-secondary font-monospace text-truncate"
                                                            style={{ maxWidth: '12rem' }}
                                                            title={setting.contentItemId}>
                                                            {setting.contentItemId}
                                                        </div>
                                                    </>
                                                )}
                                            </td>
                                            <td>
                                                <span className={`badge ${setting.isAvailableAsGeneralUserContribution
                                                    ? 'text-bg-success'
                                                    : 'text-bg-light text-dark border'}`}>
                                                    {setting.isAvailableAsGeneralUserContribution ? 'Open' : 'Closed'}
                                                </span>
                                            </td>
                                            {featureColumns.map((feature) => (
                                                <td key={feature.title} className="text-center text-nowrap">
                                                    <i
                                                        className={`bi ${feature.allowed(setting)
                                                            ? 'bi-plus-circle-fill text-success'
                                                            : 'bi-plus-circle text-body-tertiary'} me-1`}
                                                        title={`${feature.title} ${feature.allowed(setting) ? 'allowed' : 'not allowed'}`}></i>
                                                    <i
                                                        className={`bi ${feature.shown(setting)
                                                            ? 'bi-eye-fill text-primary'
                                                            : 'bi-eye-slash text-body-tertiary'}`}
                                                        title={`${feature.title} ${feature.shown(setting) ? 'shown' : 'hidden'}`}></i>
                                                </td>
                                            ))}
                                            <td className="text-nowrap">
                                                {formatDate(new Date(setting.updatedWhen))}
                                            </td>
                                            <td className="text-end">
                                                <Button
                                                    color="outline-primary"
                                                    cssClass="btn-sm"
                                                    onClick={() => manageSetting(setting.id)}>
                                                    Manage
                                                </Button>
                                            </td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>

                        {(settings == null || settings.length === 0) && (
                            <div className="alert alert-info" role="alert">
                                {hasFilters
                                    ? 'No content item settings match these filters.'
                                    : 'No content item settings found.'}
                            </div>
                        )}

                        {/* The response carries no total, so the pager offers a direction rather
                            than a page count. */}
                        {(page > 1 || settingsPage?.hasNextPage === true) && (
                            <nav className="d-flex justify-content-between align-items-center">
                                <Button
                                    color="outline-secondary"
                                    cssClass="btn-sm"
                                    disabled={page <= 1}
                                    onClick={() => setPage(page - 1)}>
                                    Previous
                                </Button>

                                <span className="small">Page {page}</span>

                                <Button
                                    color="outline-secondary"
                                    cssClass="btn-sm"
                                    disabled={settingsPage?.hasNextPage !== true}
                                    onClick={() => setPage(page + 1)}>
                                    Next
                                </Button>
                            </nav>
                        )}
                    </>
                )}
            </Card>
        </>
    );
};
