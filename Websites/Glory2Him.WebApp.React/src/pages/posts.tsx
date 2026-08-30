import { useMemo } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { ContentItemSearchPanel } from '../components/contentItems/contentItemSearchPanel';
import { ContentType } from '../models/foundations/contentItemSettings/contentType';

import {
    ContentItemSearchCriteria
} from '../models/components/contentItems/contentItemSearchItem';

import { contentItemService } from '../services/foundations/contentItemService';
import { contentItemSettingService } from '../services/foundations/contentItemSettingService';

import {
    toContentItemSearchItem
} from '../services/views/contentItems/toContentItemSearchItem';

import { useDocumentTitle } from './useDocumentTitle';

// EVERY CONTRIBUTION, searched and scrolled — the collection `/posts/{id}` and `/posts/contribute`
// are members of. The list itself is ContentItemSearchPanel; this page's whole job is to decide
// which read feeds it, to page that read, and to project its rows.
//
// THE READ IS THE PAGE'S DECISION, not the panel's. This one uses the CALLER-SCOPED
// GET api/ContentItems, which is [AllowAnonymous] and widens with whoever is asking: the
// canonically visible set for a visitor (§14.1 — approved, published, past its publish date),
// plus the caller's own rows when they are signed in, plus everything a review role covers.
//
// So one page is three surfaces without a switch: a visitor reads the journal, a contributor also
// sees their own drafts wearing a status badge, and a reviewer sees what is waiting for them. The
// foundation decides all of that against the stored row — nothing here filters, and nothing here
// could be made to leak a draft by a role change elsewhere.
//
// THE CRITERIA LIVE IN THE URL, so the header's search, a shared link and the back button all
// land with the results already showing — exactly as the search page does with ?q=.
const queryParameterName = 'q';
const contentTypeParameterName = 'type';
const authorParameterName = 'author';

// The URL carries the member NAME rather than the number: a link reading ?type=Devotional survives
// somebody reading it, and the numbering is a wire contract rather than something to put in front
// of people.
const toContentType = (value: string | null): ContentType | null => {
    if (value == null || value.length === 0) {
        return null;
    }

    const member = ContentType[value as keyof typeof ContentType];

    return typeof member === 'number' ? member : null;
};

export function Posts() {
    useDocumentTitle('The journal — Glory 2 Him');

    const [searchParams, setSearchParams] = useSearchParams();

    // Memoized on the URL's own values: the criteria are part of the query key, and a fresh
    // object literal on every render would restart the scroll on every render.
    const criteria = useMemo<ContentItemSearchCriteria>(() => ({
        query: searchParams.get(queryParameterName) ?? '',
        contentType: toContentType(searchParams.get(contentTypeParameterName)),
        author: searchParams.get(authorParameterName) ?? ''
    }), [searchParams]);

    const {
        data,
        isLoading,
        isError,
        isFetchingNextPage,
        hasNextPage,
        fetchNextPage
    } = contentItemService.useSearchContentItems(criteria);

    // Rendering an item needs its type's name, icon and facet pairs, which is a different question
    // from which types are open to contribution — so the defaults are read rather than the
    // contribution list, the same way /posts/{id} reads them.
    const { data: contentItemSettings } = contentItemSettingService.useGetDefaults();

    // The ACCUMULATED list. react-query keeps the pages, so nothing already fetched is fetched
    // again on the way down.
    const contentItems = useMemo(
        () => (data?.pages ?? [])
            .flatMap((page) => page.items)
            .map(toContentItemSearchItem),
        [data]);

    const search = (searched: ContentItemSearchCriteria) => {
        const parameters = new URLSearchParams();

        if (searched.query.trim().length > 0) {
            parameters.set(queryParameterName, searched.query.trim());
        }

        if (searched.contentType != null) {
            parameters.set(contentTypeParameterName, ContentType[searched.contentType]);
        }

        if (searched.author.trim().length > 0) {
            parameters.set(authorParameterName, searched.author.trim());
        }

        setSearchParams(parameters);
    };

    return (
        <section className="pt-4 pb-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-xl-9">
                        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-4">
                            <h1 className="h2 mb-0">The journal</h1>

                            <Link to="/posts/contribute" className="btn btn-primary mb-0">
                                <i className="bi bi-pencil-square me-1" aria-hidden="true"></i>
                                Share what He has done
                            </Link>
                        </div>

                        {isError ? (
                            <div className="alert alert-danger" role="alert">
                                We could not load the journal right now. Please try again later.
                            </div>
                        ) : (
                            <ContentItemSearchPanel
                                ariaLabel="The journal"
                                contentItemCollection={contentItems}
                                contentItemSettingCollection={contentItemSettings ?? []}
                                criteria={criteria}
                                onSearch={search}
                                isLoading={isLoading}
                                isLoadingMore={isFetchingNextPage}
                                hasMore={hasNextPage}
                                onLoadMore={fetchNextPage}
                                emptyText={
                                    'Nothing matched that search. Try clearing the advanced '
                                    + 'options.'} />
                        )}

                        {/* NO reactionOptions AND NO onReacted, and that is a decision rather
                            than an omission. Giving a reaction is a ContentItem-to-Reaction
                            ASSOCIATION, and associations have no HTTP exposer yet (#318) — so
                            this page cannot persist one. The panel's rule is that a surface which
                            cannot persist a reaction must not appear to accept one, which is why
                            passing the options anyway would be the wrong kind of helpful. It
                            becomes two props when #318 lands. */}
                    </div>
                </div>
            </div>
        </section>
    );
}
