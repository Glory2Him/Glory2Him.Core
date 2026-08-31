import { useMemo } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { toastSuccess } from '../brokers/toastBroker.success';
import { BibleReferenceAssociationPanel } from '../components/associations/bibleReferenceAssociationPanel';
import { TagAssociationPanel } from '../components/associations/tagAssociationPanel';
import { ContentItemPanel } from '../components/contentItems/contentItemPanel';
import { SharingPanel } from '../components/contentItems/sharingPanel';
import { Spinner } from '../components/coreUI/spinner';
import { contentItemService } from '../services/foundations/contentItemService';
import { contentItemSettingService } from '../services/foundations/contentItemSettingService';
import { toContentItemSearchItem } from '../services/views/contentItems/toContentItemSearchItem';

import {
    contentTypeNameOf,
    resolveContentItemSetting
} from '../services/views/contentItems/resolveContentItemSetting';

import { useDocumentTitle } from './useDocumentTitle';

// ONE OF MY POSTS, read on its own surface — where /posts/contribute lands a fresh submission
// and where /myposts sends every click into an item. The public /posts/{id} page stays the
// single-column reading surface; this one is the CONTRIBUTOR's view of their own item, so it
// carries the way back to their list and the association surfaces beside the content.
//
// TWO COLUMNS, 7 / 5: the item on the left, and on the right the surfaces that belong BESIDE a
// content item rather than within it (§20.6.2) — its tags, its bible references, and the
// invitation to share something else. Each association panel is a pure renderer; this page owns
// what its events mean, and today that is an honest "coming soon": suggesting an association is
// a ContentItem association write, and associations have no HTTP exposer yet (#318). The panels
// take their collections from THIS page, which holds the item's id off the URL — the wiring
// point where the association read plugs in when it exists.
export function MyPostDetail() {
    const { contentItemId = '' } = useParams();
    const navigate = useNavigate();
    const location = useLocation();

    const { data: contentItem, isLoading, isError } =
        contentItemService.useGetContentItemById(contentItemId, contentItemId.length > 0);

    // Defaults plus THIS item's own override, when one exists — the §6.4 resolution needs
    // the specific row in hand to prefer it.
    const { data: contentItemSettings } =
        contentItemSettingService.useGetEffectiveSettingsFor(
            contentItemId.length > 0 ? [contentItemId] : []);

    // The SAME self-contained element a list surface would carry — one projection for the
    // whole family — except the excerpt is left off: this page is the reading surface for
    // the full item, and a clamp with a read-more that leads here would point at itself.
    const searchItem = useMemo(
        () => contentItem == null
            ? undefined
            : {
                ...toContentItemSearchItem(contentItem, contentItemSettings ?? []),
                excerpt: undefined
            },
        [contentItem, contentItemSettings]);

    // The same resolver and hasTitle rule the panel applies — see postDetail, which this page
    // mirrors: an earlier hand-rolled copy of this logic drifted immediately.
    const pageHeadingSetting = useMemo(
        () => contentItem == null
            ? undefined
            : resolveContentItemSetting(
                contentItemSettings ?? [], contentItem.contentType, contentItem.id),
        [contentItemSettings, contentItem]);

    const showsTitle =
        pageHeadingSetting?.hasTitle ?? (contentItem?.title ?? '').length > 0;

    const pageHeading =
        contentItem == null
            ? ''
            : showsTitle && (contentItem.title ?? '').length > 0
                ? contentItem.title ?? ''
                : contentTypeNameOf(
                    contentItemSettings ?? [], contentItem.contentType, contentItem.id);

    useDocumentTitle(
        contentItem == null ? 'My posts — Glory 2 Him' : `${pageHeading} — Glory 2 Him`);

    // A true way back: the origin a redirect carried in state when there is one, the list
    // otherwise — a deep link or a refresh still has somewhere honest to go.
    const backHref = (location.state as { from?: string } | null)?.from ?? '/myposts';

    const from = `${location.pathname}${location.search}`;

    // The association writes arrive with #318; until then the boxes answer honestly rather
    // than silently dropping what somebody typed.
    const suggestTag = () => toastSuccess('Suggesting tags is coming soon.');

    const suggestBibleReference = () =>
        toastSuccess('Suggesting bible references is coming soon.');

    // The modify write lands with its own service; until then the save answers honestly
    // rather than pretending it stuck.
    const saveChanges = () => toastSuccess('Saving changes is coming soon.');

    return (
        <section className="pt-4 pb-5">
            <div className="container">
                <div className="mb-3">
                    <Link to={backHref} className="btn btn-outline-primary btn-sm mb-0">
                        <i className="bi bi-arrow-left me-1" aria-hidden="true"></i>
                        Back to my posts
                    </Link>
                </div>

                {isLoading ? (
                    <div className="text-center py-5"><Spinner /></div>
                ) : isError || searchItem == null ? (
                    <div className="alert alert-danger" role="alert">
                        We could not load this contribution right now. It may have been removed,
                        or it may not be yours to read.
                    </div>
                ) : (
                    <div className="row g-4">
                        <div className="col-lg-7">
                            {/* The card carries the visible title now — the same face the
                                list shows — so the page states its heading for the outline
                                alone rather than printing it twice. */}
                            <h1 className="visually-hidden">{pageHeading}</h1>

                            {/* Tags and bible references stand in the side panels on the
                                right, so the in-card sections are switched off — the same
                                facts must not appear twice on one screen. */}
                            <ContentItemPanel
                                contentItem={searchItem}
                                isEditingAllowed
                                showApprovalStatusRibbon
                                showApprovalStatus
                                showTagSection={false}
                                showBibleReferenceSection={false}
                                onModified={saveChanges}
                                contentItemSettingCollection={contentItemSettings ?? []} />
                        </div>

                        <div className="col-lg-5">
                            {/* The associations render from what this page holds — the item's id
                                is here off the URL, which is where the association read keys in
                                when #318 gives it an exposer. Until then the collections are
                                honestly empty rather than invented. */}
                            <TagAssociationPanel
                                associationCollection={[]}
                                onAdd={suggestTag}
                                showBorder
                                cssClass="mb-4" />

                            <BibleReferenceAssociationPanel
                                associationCollection={[]}
                                onAdd={suggestBibleReference}
                                showBorder
                                cssClass="mb-4" />

                            <SharingPanel
                                onSubmit={() =>
                                    navigate('/posts/contribute', { state: { from } })} />
                        </div>
                    </div>
                )}
            </div>
        </section>
    );
}
