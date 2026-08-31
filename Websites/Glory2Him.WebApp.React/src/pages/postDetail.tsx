import { useMemo } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ContentItemPanel } from '../components/contentItems/contentItemPanel';
import { Spinner } from '../components/coreUI/spinner';
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
// EDITING IS OFF HERE. isEditingAllowed is left at its default, the surface switch
// ContentItemPanel puts ahead of every role check: no Edit, no route into the editor,
// however the reader's roles fall. A public page that could never be turned into an edit
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

    // The SAME self-contained element the feeds carry — one projection, one face, the whole
    // family — enriched with what this page alone has resolved: the contributor’s name for
    // the meta row. The excerpt is left off because this page IS the full reading surface.
    const searchItem = useMemo(
        () => contentItem == null
            ? undefined
            : {
                ...toContentItemSearchItem(contentItem, contentItemSettings ?? []),
                excerpt: undefined,
                submittedByName: contributor?.displayName,
                submittedByImageUrl: contributor?.imageUrl ?? undefined
            },
        [contentItem, contentItemSettings, contributor]);

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

    return (
        <section className="pt-4 pb-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-xl-9">
                        {isLoading ? (
                            <div className="text-center py-5"><Spinner /></div>
                        ) : isError || searchItem == null ? (
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
                                {/* The card carries the visible title now — the same face
                                    the feeds show — so the page states its heading for the
                                    outline alone rather than printing it twice. */}
                                <h1 className="visually-hidden">{pageHeading}</h1>

                                <ContentItemPanel contentItem={searchItem} />
                            </>
                        )}
                    </div>
                </div>
            </div>
        </section>
    );
}
