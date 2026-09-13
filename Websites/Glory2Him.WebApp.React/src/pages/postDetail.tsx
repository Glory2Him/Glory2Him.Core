import { useMemo } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { toastSuccess } from '../brokers/toastBroker.success';
import { BibleReferenceAssociationPanel } from '../components/associations/bibleReferenceAssociationPanel';
import { TagAssociationPanel } from '../components/associations/tagAssociationPanel';
import { ContentItemPanel } from '../components/contentItems/contentItemPanel';
import { SharingPanel } from '../components/contentItems/sharingPanel';
import { Spinner } from '../components/coreUI/spinner';
import { useContentItemEngagement } from '../hooks/useContentItemEngagement';
import { contentItemService } from '../services/foundations/contentItemService';
import { contentItemSettingService } from '../services/foundations/contentItemSettingService';
import { contributorService } from '../services/foundations/contributorService';
import { toContentItemSearchItem } from '../services/views/contentItems/toContentItemSearchItem';

import {
    contentTypeNameOf,
    resolveContentItemSetting
} from '../services/views/contentItems/resolveContentItemSetting';
import { useDocumentTitle } from './useDocumentTitle';

// One content item, read. Where a contribution lands after it is submitted, and the permanent
// address of the item afterwards.
//
// TWO COLUMNS, 7 / 5, the same split the contributor's own surface keeps: the item on the left,
// and on the right the surfaces that belong BESIDE a content item rather than within it
// (§20.6.2) — its tags, its bible references, and the invitation to share something else. A
// reader arriving at a post meets them there and is invited to suggest one of each; the
// association panels are pure renderers, so this page owns what
// the events mean, and today that is an honest "coming soon": a suggestion is a ContentItem
// association write, and associations have no HTTP exposer yet (#318). The panels take their
// collections from THIS page, which holds the item's id off the URL — the wiring point where the
// association read plugs in when it exists.
//
// EDITING IS OFF HERE. showEditSection is left at its default, the surface switch
// ContentItemPanel puts ahead of every role check: no Edit, no route into the editor,
// however the reader's roles fall. A public page that could never be turned into an edit
// surface by a role change elsewhere is the point of that switch — an editing surface is a
// separate page's decision, not this one's.
export function PostDetail() {
    const { contentItemId = '' } = useParams();
    const navigate = useNavigate();
    const location = useLocation();

    const { data: contentItem, isLoading, isError } =
        contentItemService.useGetContentItemById(contentItemId, contentItemId.length > 0);

    // Rendering an item needs its type's name, icon and field shaping, which is a different
    // question from which types are open to contribution — so the defaults are read rather than
    // the contribution list.
    // Defaults plus THIS item's own override, when one exists — the §6.4 resolution needs
    // the specific row in hand to prefer it.
    const { data: contentItemSettings } =
        contentItemSettingService.useGetEffectiveSettingsFor(
            contentItemId.length > 0 ? [contentItemId] : []);

    // LIKE, SHARE AND SAVE — the same thin wiring every feed card runs on, so a reader who
    // followed a card here meets the controls it offered rather than losing them at the one
    // address the item permanently has. Share is real (it copies this page's address); the
    // reaction lives in page state for the visit and Save answers honestly, because both are
    // ContentItem associations and those have no exposer yet (#318).
    const {
        reactionOptions,
        onReactionSelected,
        onShareClick,
        onSaveClick,
        withViewerReactions
    } = useContentItemEngagement();

    // WHO SUBMITTED IT. The item carries CreatedBy — an account id — so the byline needs a second
    // read to turn that into a name and a face. Anonymous, so a signed-out reader gets the byline
    // too, and a 404 resolves to null rather than throwing: an account that has gone leaves the
    // article intact and the byline absent, which is the right shape for both.
    //
    // The panel is rendered before this resolves, and deliberately: an article must not wait on
    // its byline. The block simply appears when the name arrives.
    const { data: contributor } = contributorService.useGetContributorById(
        contentItem?.createdBy ?? '');

    // The SAME self-contained element the feeds carry — one projection, one face, the whole
    // family — enriched with what this page alone has resolved: the contributor’s name for
    // the meta row.
    const readItem = useMemo(
        () => contentItem == null
            ? undefined
            : {
                ...toContentItemSearchItem(contentItem, contentItemSettings ?? []),
                submittedByName: contributor?.displayName,
                submittedByImageUrl: contributor?.imageUrl ?? undefined
            },
        [contentItem, contentItemSettings, contributor]);

    // The visit's chosen reaction, folded over the projection. OUTSIDE the memo deliberately:
    // withViewerReactions closes over the choices and is rebuilt every render, so memoising on
    // it would hand the panel a new item object on every render and reset the state it owns —
    // the open reaction picker among it. The fold itself returns the SAME object while no
    // reaction is held, so the identity only moves when the reader's choice actually does.
    const searchItem = readItem == null ? undefined : withViewerReactions([readItem])[0];

    // What the page is called, on screen and in the tab.
    //
    // It asks the SAME resolver the panel does, against the same rows - an earlier copy of this
    // logic here drifted immediately, losing the soft-delete filter and the override, and naming
    // a literal when the settings had not arrived. It also has to obey the same hasTitle rule the
    // panel's read surface does: a type whose effective setting carries no title must not have
    // one shouted as the h1 while the panel below deliberately hides it.
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
        contentItem == null ? 'Glory 2 Him' : `${pageHeading} — Glory 2 Him`);

    // Where the contribution surface sends the reader back to — the post they were reading
    // when the invitation caught them, rather than a guess.
    const from = `${location.pathname}${location.search}`;

    // The association writes arrive with #318; until then the boxes answer honestly rather
    // than silently dropping what somebody typed.
    const suggestTag = () => toastSuccess('Suggesting tags is coming soon.');

    const suggestBibleReference = () =>
        toastSuccess('Suggesting bible references is coming soon.');

    return (
        <section className="pt-4 pb-5">
            <div className="container">
                {isLoading ? (
                    <div className="text-center py-5"><Spinner /></div>
                ) : isError || searchItem == null ? (
                    <div className="row justify-content-center">
                        <div className="col-xl-9">
                            <div className="alert alert-danger" role="alert">
                                We could not load this contribution right now. It may have been
                                removed, or it may not be yours to read.
                            </div>

                            <Link to="/" className="btn btn-outline-primary mb-0">
                                <i className="bi bi-arrow-left me-1" aria-hidden="true"></i>
                                Back to the journal
                            </Link>
                        </div>
                    </div>
                ) : (
                    <div className="row g-4">
                        <div className="col-lg-7">
                            {/* The card carries the visible title now — the same face
                                the feeds show — so the page states its heading for the
                                outline alone rather than printing it twice. */}
                            <h1 className="visually-hidden">{pageHeading}</h1>

                            {/* The full reading surface: no cut, no read-more. Tags and bible
                                references stand in the side panels on the right, so the in-card
                                sections are switched off — the same facts must not appear twice
                                on one screen. */}
                            <ContentItemPanel
                                contentItem={searchItem}
                                showContentExpanded
                                showTagSection={false}
                                showBibleReferenceSection={false}
                                reactionOptions={reactionOptions}
                                onReactionSelected={onReactionSelected}
                                onShareClick={onShareClick}
                                onSaveClick={onSaveClick} />
                        </div>

                        <div className="col-lg-5">
                            {/* The associations render from what this page holds — the item's id
                                is here off the URL, which is where the association read keys in
                                when #318 gives it an exposer. Until then the collections are
                                honestly empty rather than invented.

                                showModerationActions is left off, which is the point of a public
                                reading surface: a reader may suggest and withdraw their own
                                suggestion, and nothing here decides anything. */}
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

                            {/* Reading somebody else's contribution is the moment the
                                invitation lands best, so it stands under the two
                                association panels here exactly as it does on the
                                contributor's own surface. */}
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
