import { useMemo } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ContentItemDetailPanel } from '../components/contentItems/contentItemDetailPanel';
import { Spinner } from '../components/coreUI/spinner';
import { contentItemService } from '../services/foundations/contentItemService';
import { contentItemSettingService } from '../services/foundations/contentItemSettingService';
import { contributorService } from '../services/foundations/contributorService';
import { readingTimeMinutesOf } from '../services/views/contentItems/readingTimeMinutesOf';
import { toContentItemFormItem } from '../services/views/contentItems/toContentItemFormItem';

import {
    contentTypeNameOf,
    resolveContentItemSetting
} from '../services/views/contentItems/resolveContentItemSetting';
import { useDocumentTitle } from './useDocumentTitle';

// One content item, read. Where a contribution lands after it is submitted, and the permanent
// address of the item afterwards.
//
// EDITING IS OFF HERE. isEditingAllowed is left at its default, which is the surface switch
// ContentItemDetailPanel puts ahead of every role check: no Edit, no Delete, no route into the edit
// mode, however the reader's roles fall. A public page that could never be turned into an edit
// surface by a role change elsewhere is the point of that switch — an editing surface is a
// separate page's decision, not this one's.
export function PostDetail() {
    const { contentItemId = '' } = useParams();

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

    // WHO SUBMITTED IT. The item carries CreatedBy — an account id — so the byline needs a second
    // read to turn that into a name and a face. Anonymous, so a signed-out reader gets the byline
    // too, and a 404 resolves to null rather than throwing: an account that has gone leaves the
    // article intact and the byline absent, which is the right shape for both.
    //
    // The panel is rendered before this resolves, and deliberately: an article must not wait on
    // its byline. The block simply appears when the name arrives.
    const { data: contributor } = contributorService.useGetContributorById(
        contentItem?.createdBy ?? '');

    // Memoized because the panel seeds its editor from the item's identity: a fresh projection
    // object on every render would be a fresh item as far as any consumer of it is concerned.
    const formItem = useMemo(
        () => contentItem == null
            ? undefined
            : toContentItemFormItem(contentItem, contentItemSettings ?? []),
        [contentItem, contentItemSettings]);

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

    // THE PANEL RENDERS THE VISIBLE HEADING, as an h1, because the design puts the title UNDER
    // the type chip and a page cannot render a heading above a chip the panel owns.
    //
    // So this page states an h1 only in the case the panel will NOT — the same condition the
    // panel's read surface applies, restated here rather than guessed at: a type whose effective
    // setting has no title, or a row that simply carries none, leaves the panel nothing to head
    // and the document would start at the article with no h1 at all.
    //
    // That heading is visually hidden, because what it would say is the type's name and the chip
    // directly beneath it already says exactly that. Hidden rather than dropped: the outline is
    // for a screen reader and a search engine, and neither of them is reading the chip.
    const panelRendersHeading =
        contentItem != null && showsTitle && (contentItem.title ?? '').length > 0;

    const rendersOwnHeading = contentItem != null && panelRendersHeading === false;

    // A pure function of the content, so it is computed here rather than stored or fetched. The
    // three engagement counts are NOT passed: there is no comment, reaction or view client in
    // this app yet, and a zero would assert an empty conversation rather than an absent one.
    const readingTimeMinutes = readingTimeMinutesOf(contentItem?.content);

    useDocumentTitle(
        contentItem == null ? 'Glory 2 Him' : `${pageHeading} — Glory 2 Him`);

    return (
        <section className="pt-4 pb-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-xl-9">
                        {isLoading ? (
                            <div className="text-center py-5"><Spinner /></div>
                        ) : isError || formItem == null ? (
                            <>
                                <div className="alert alert-danger" role="alert">
                                    We could not load this contribution right now. It may have been
                                    removed, or it may not be yours to read.
                                </div>

                                <Link to="/" className="btn btn-outline-primary mb-0">
                                    <i className="bi bi-arrow-left me-1" aria-hidden="true"></i>
                                    Back to the journal
                                </Link>
                            </>
                        ) : (
                            <>
                                {rendersOwnHeading && (
                                    <h1 className="visually-hidden">{pageHeading}</h1>
                                )}

                                <div className="card card-body border p-4 p-lg-5">
                                    <ContentItemDetailPanel
                                        ariaLabel="Contribution"
                                        contentItem={formItem}
                                        titleHeadingLevel="h1"
                                        contentItemSettingCollection={contentItemSettings ?? []}
                                        submittedByDisplayName={contributor?.displayName}
                                        submittedByImageUrl={contributor?.imageUrl ?? undefined}
                                        readingTimeMinutes={
                                            readingTimeMinutes > 0
                                                ? readingTimeMinutes
                                                : undefined} />
                                </div>
                            </>
                        )}
                    </div>
                </div>
            </div>
        </section>
    );
}
