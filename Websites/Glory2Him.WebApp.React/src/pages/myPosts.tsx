import { useMemo } from 'react';
import { Link, useLocation, useNavigate, useSearchParams } from 'react-router-dom';
import { ContentItemSearchPanel } from '../components/contentItems/contentItemSearchPanel';
import { useAuth } from '../components/securitys/authProvider';

import {
    buildContentItemFeedNavigation
} from '../services/views/contentItems/contentItemFeedNavigation';

import {
    toContentItemSearchCriteria,
    toContentItemSearchParams
} from '../services/views/contentItems/contentItemSearchCriteriaUrl';

import { contentItemService } from '../services/foundations/contentItemService';
import { contentItemSettingService } from '../services/foundations/contentItemSettingService';

import {
    ContentItemSearchCriteria
} from '../models/components/contentItems/contentItemSearchItem';

import {
    toContentItemSearchItem
} from '../services/views/contentItems/toContentItemSearchItem';

import { useDocumentTitle } from './useDocumentTitle';

// THE CALLER'S OWN CONTRIBUTIONS, at every status — the same look as the home feed, narrowed to
// one person. The PAGE pins the narrowing (`submittedById` = the signed-in account), which is
// what this surface IS, so a submitted-by pill click cannot widen it back out to somebody else.
//
// The caller-scoped read already answers with the caller's own rows whatever their status
// (§14.5), so the status badges here are the projection doing its honest work: a Draft wears
// Draft, a Submitted wears In review, and nothing the reader owns looks published before it is.
//
// The route sits behind SecuredRoute — there is no "my" for a visitor — and the read is
// additionally gated on the resolved account id, so the page never asks for everybody's rows
// while the identity is still arriving.
export function MyPosts() {
    useDocumentTitle('My posts — Glory 2 Him');

    const navigate = useNavigate();
    const location = useLocation();
    const [searchParams, setSearchParams] = useSearchParams();
    const { user } = useAuth();

    const userId = user?.userId ?? '';

    const criteria = useMemo(
        () => toContentItemSearchCriteria(searchParams),
        [searchParams]);

    const {
        data,
        isLoading,
        isError,
        isFetchingNextPage,
        hasNextPage,
        fetchNextPage
    } = contentItemService.useSearchContentItems(criteria, {
        scope: 'caller',
        submittedById: userId,
        enabled: userId.length > 0
    });

    const { data: contentItemSettings } = contentItemSettingService.useGetDefaults();

    const contentItems = useMemo(
        () => (data?.pages ?? [])
            .flatMap((page) => page.items)
            .map(toContentItemSearchItem),
        [data]);

    const search = (searched: ContentItemSearchCriteria) =>
        setSearchParams(toContentItemSearchParams(searched));

    const feedNavigation = buildContentItemFeedNavigation(navigate, location);

    return (
        <section className="pt-4 pb-5">
            <div className="container">
                <div className="row">
                    <div className="col-12">
                        <div className="d-flex flex-wrap justify-content-between align-items-center gap-2 mb-4">
                            <h1 className="h2 mb-0">My posts</h1>

                            <Link to="/posts/contribute" className="btn btn-primary mb-0">
                                <i className="bi bi-pencil-square me-1" aria-hidden="true"></i>
                                Share what He has done
                            </Link>
                        </div>

                        {isError ? (
                            <div className="alert alert-danger" role="alert">
                                We could not load your posts right now. Please try again later.
                            </div>
                        ) : (
                            <ContentItemSearchPanel
                                ariaLabel="My posts"
                                contentItemCollection={contentItems}
                                contentItemSettingCollection={contentItemSettings ?? []}
                                criteria={criteria}
                                onSearch={search}
                                isLoading={isLoading || userId.length === 0}
                                isLoadingMore={isFetchingNextPage}
                                hasMore={hasNextPage}
                                onLoadMore={fetchNextPage}
                                emptyText={
                                    'You have not contributed anything that matches. Share '
                                    + 'what He has done!'}
                                {...feedNavigation} />
                        )}
                    </div>
                </div>
            </div>
        </section>
    );
}
