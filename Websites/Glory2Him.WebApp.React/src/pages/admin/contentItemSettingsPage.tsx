import { useEffect, useMemo, useState } from 'react';
import { useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { Breadcrumb } from '../../components/coreUI/breadcrumb';
import { Button } from '../../components/coreUI/button';
import { Card } from '../../components/coreUI/card';
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

import {
    toContentItemSettingQuery,
    toContentItemSettingSearchParams
} from '../../services/views/contentItemSettings/contentItemSettingQueryUrl';

import { contentItemSettingService } from '../../services/foundations/contentItemSettingService';
import { useDocumentTitle } from '../useDocumentTitle';

// The master list of ContentItemSetting rows. Searching, filtering and paging all run
// server-side over [EnableQuery] rather than in the DataTable component the other admin lists
// use: the host caps a collection read at OData:PageSize rows, so an in-memory table would go
// quietly blind past that cap once per-item overrides outnumber the eight type defaults.
//
// THE FILTERS LIVE IN THE URL AND NOWHERE ELSE (contentItemSettingQueryUrl), the same place the
// feed pages keep theirs. That is what makes the view an ADDRESS: Manage hands the detail page
// the address it was opened from, and the way back — the detail's Back button and its save —
// lands on the filtered page the administrator was working through rather than on an unfiltered
// first one. Only the search box holds state of its own, because a debounce must hold the
// keystrokes somewhere before it commits them.

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

// The feature pairs design §6.10 resolves, in the order the detail page edits them. "Shown"
// governs whether something renders at all; "allowed" governs whether something new can be
// created. Six pairs were six columns once, which made the table wider than the content area
// and put the row's own Manage button behind a horizontal scrollbar — they now share one cell
// and wrap, so the row action stays where it can be reached.
const featureFields: ReadonlyArray<{
    title: string;
    shown: (setting: ContentItemSetting) => boolean;
    allowed: (setting: ContentItemSetting) => boolean;
}> = [
    { title: 'Tags', shown: (s) => s.showTags, allowed: (s) => s.tagsAllowed },
    { title: 'Reactions', shown: (s) => s.showReactions, allowed: (s) => s.reactionsAllowed },
    { title: 'Links', shown: (s) => s.showLinks, allowed: (s) => s.linksAllowed },
    { title: 'Attachments', shown: (s) => s.showAttachments, allowed: (s) => s.attachmentsAllowed },
    { title: 'Comments', shown: (s) => s.showComments, allowed: (s) => s.commentsAllowed },
    {
        title: 'Bible references',
        shown: (s) => s.showBibleReferences,
        allowed: (s) => s.bibleReferenceAllowed
    },
];

export const ContentItemSettingsPage = () => {
    const navigate = useNavigate();
    const location = useLocation();
    const [searchParams, setSearchParams] = useSearchParams();

    useDocumentTitle('Content Item Settings — Glory 2 Him');

    const query = useMemo<ContentItemSettingQuery>(
        () => toContentItemSettingQuery(searchParams, pageSize),
        [searchParams]);

    const searchTerm = query.searchTerm ?? '';
    const contentType = query.contentType;
    const scope = query.scope ?? 'All';
    const page = query.page;

    // A narrower filter can leave the current page past the end of the results, which reads as
    // an empty list rather than as a filter that matched something — so a filter change carries
    // page 1 with it rather than being followed by a second write that resets it.
    const applyFilters = (
        changes: Partial<ContentItemSettingQuery>,
        options?: { replace?: boolean }) =>
        setSearchParams(toContentItemSettingSearchParams({ ...query, ...changes }), options);

    // The one control that cannot read straight from the URL: every keystroke would otherwise be
    // its own history entry, its own request and its own cache entry.
    const [searchInput, setSearchInput] = useState(searchTerm);

    // The box follows the URL back whenever the URL moves without it — the return from a detail
    // page, a pasted link, the browser's own Back.
    useEffect(() => {
        setSearchInput(searchTerm);
    }, [searchTerm]);

    // Committed by REPLACING rather than pushing: a pause in typing is not a place the reader
    // asked to be able to come back to.
    useEffect(() => {
        if (searchInput === searchTerm) {
            return;
        }

        const timeoutId = window.setTimeout(
            () => setSearchParams(
                toContentItemSettingSearchParams({
                    ...query,
                    searchTerm: searchInput,
                    page: 1
                }),
                { replace: true }),
            searchDebounceMilliseconds);

        return () => window.clearTimeout(timeoutId);
    }, [searchInput, searchTerm, query, setSearchParams]);

    const { data: settingsPage, isLoading, isError, isFetching } =
        contentItemSettingService.useGetContentItemSettings(query);

    const settings = settingsPage?.items;
    const hasFilters = searchTerm.length > 0 || contentType != null || scope !== 'All';

    // The address travels with the navigation, the way every content-item surface hands its
    // origin on: the detail page offers a true way back rather than a guess at one.
    const manageSetting = (contentItemSettingId: string) =>
        navigate(`${settingsRoute}/${contentItemSettingId}`, {
            state: { from: `${location.pathname}${location.search}` }
        });

    const clearFilters = () => {
        setSearchInput('');
        setSearchParams(new URLSearchParams());
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
                                applyFilters({
                                    contentType: event.target.value === ''
                                        ? undefined
                                        : Number(event.target.value) as ContentType,
                                    page: 1
                                })}>
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
                                applyFilters({
                                    scope: event.target.value as ContentItemSettingScope,
                                    page: 1
                                })}>
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
                                        <th></th>
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
                                                <div className="d-flex flex-wrap align-items-center gap-2 mb-2">
                                                    {setting.contentItemId == null ? (
                                                        <span className="badge text-bg-dark">Type default</span>
                                                    ) : (
                                                        <>
                                                            <span className="badge text-bg-info">Item override</span>
                                                            <span
                                                                className="small text-body-secondary font-monospace text-truncate"
                                                                style={{ maxWidth: '12rem' }}
                                                                title={setting.contentItemId}>
                                                                {setting.contentItemId}
                                                            </span>
                                                        </>
                                                    )}

                                                    <span className={`badge ${setting.isAvailableAsGeneralUserContribution
                                                        ? 'text-bg-success'
                                                        : 'text-bg-warning'}`}>
                                                        Public Contributions
                                                        {setting.isAvailableAsGeneralUserContribution ? ' Open' : ' Closed'}
                                                    </span>
                                                </div>

                                                <div className="d-flex flex-wrap gap-2">
                                                    {featureFields.map((feature) => {
                                                        const isShown = feature.shown(setting);
                                                        const isAllowed = feature.allowed(setting);

                                                        return (
                                                            <span
                                                                key={feature.title}
                                                                className={`badge rounded-pill fw-normal ${isShown || isAllowed
                                                                    ? 'bg-primary-subtle text-primary-emphasis'
                                                                    : 'bg-body-secondary text-body-tertiary'}`}>
                                                                {feature.title}
                                                                <i
                                                                    className={`bi ms-2 ${isShown
                                                                        ? 'bi-eye-fill text-primary'
                                                                        : 'bi-eye-slash text-body-tertiary'}`}
                                                                    title={`${feature.title} ${isShown ? 'shown' : 'hidden'}`}></i>
                                                                <i
                                                                    className={`bi ms-1 ${isAllowed
                                                                        ? 'bi-plus-circle-fill text-success'
                                                                        : 'bi-plus-circle text-body-tertiary'}`}
                                                                    title={`${feature.title} can ${isAllowed ? '' : 'not '}be added`}></i>
                                                            </span>
                                                        );
                                                    })}
                                                </div>
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
                                    onClick={() => applyFilters({ page: page - 1 })}>
                                    Previous
                                </Button>

                                <span className="small">Page {page}</span>

                                <Button
                                    color="outline-secondary"
                                    cssClass="btn-sm"
                                    disabled={settingsPage?.hasNextPage !== true}
                                    onClick={() => applyFilters({ page: page + 1 })}>
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
