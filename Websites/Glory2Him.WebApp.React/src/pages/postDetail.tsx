import { useMemo } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ContentItemPanel } from '../components/contentItems/contentItemPanel';
import { Spinner } from '../components/coreUI/spinner';
import { contentItemService } from '../services/foundations/contentItemService';
import { contentItemSettingService } from '../services/foundations/contentItemSettingService';
import { toContentItemFormItem } from '../services/views/contentItems/toContentItemFormItem';
import { useDocumentTitle } from './useDocumentTitle';

// One content item, read. Where a contribution lands after it is submitted, and the permanent
// address of the item afterwards.
//
// EDITING IS OFF HERE. isEditingAllowed is left at its default, which is the surface switch
// ContentItemPanel puts ahead of every role check: no Edit, no Delete, no route into the edit
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
    const { data: contentItemSettings } = contentItemSettingService.useGetDefaults();

    // Memoized because the panel seeds its editor from the item's identity: a fresh projection
    // object on every render would be a fresh item as far as any consumer of it is concerned.
    const formItem = useMemo(
        () => contentItem == null ? undefined : toContentItemFormItem(contentItem),
        [contentItem]);

    // What the page is called, on screen and in the tab. The item's title where it has one; the
    // content type's own name where the type does not carry titles at all.
    const pageHeading =
        (contentItem?.title ?? '').length > 0
            ? contentItem?.title ?? ''
            : contentItemSettings?.find(
                (setting) => setting.contentType === contentItem?.contentType
                    && setting.contentItemId == null)?.contentTypeName
            ?? 'Contribution';

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
                                {/* The page's own heading, so the document has an h1 and does not
                                    start its outline at the panel's h3. The panel is told not to
                                    repeat the title underneath it. A type whose setting carries no
                                    title falls back to the type's name, which is never empty. */}
                                <h1 className="h2 mb-4">{pageHeading}</h1>

                                <div className="card card-body border p-4 p-lg-5">
                                    <ContentItemPanel
                                        ariaLabel="Contribution"
                                        contentItem={formItem}
                                        showItemTitle={false}
                                        contentItemSettingCollection={contentItemSettings ?? []} />
                                </div>
                            </>
                        )}
                    </div>
                </div>
            </div>
        </section>
    );
}
