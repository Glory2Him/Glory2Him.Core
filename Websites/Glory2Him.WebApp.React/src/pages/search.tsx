import { ChangeEvent, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { BlogSidebar } from '../components/coreUI/blogSidebar';
import { Pagination } from '../components/coreUI/pagination';
import { PostListItem } from '../components/coreUI/postListItem';
import SearchBarComponent from '../components/coreUI/searchBar';
import { TagInput } from '../components/coreUI/tagInput';
import { PostView } from '../models/coreUI/postView';
import { postService } from '../services/foundations/postService';

// The post-list layout without its hero banner: a search box stands where the banner was, and the
// list below it only appears once something has been searched for.
//
// Ported from Search.razor + SearchBase.cs, with the demo's in-memory post set replaced by
// postService.useGetPosts. The query lives in the URL (?q=) so the header's /Search link and
// deep links land with the results already showing, exactly as the Blazor page did with
// [SupplyParameterFromQuery].
export const resultsPerPage = 5;

// The API returns publishedDate as an ISO string; PostListItem formats it as a Date.
const toPostView = (post: PostView): PostView => ({
    ...post,
    publishedDate: new Date(post.publishedDate as unknown as string),
});

interface CommittedFilters {
    category: string;
    author: string;
    tag?: string;
}

export function Search() {
    const [searchParams, setSearchParams] = useSearchParams();

    // Lets a link elsewhere land on /Search?q=anything with the results already showing.
    const committedQuery = searchParams.get('q');
    const hasSearched = committedQuery !== null;

    const [query, setQuery] = useState(committedQuery ?? '');
    const [selectedCategory, setSelectedCategory] = useState('');
    const [selectedAuthor, setSelectedAuthor] = useState('');
    const [tags, setTags] = useState<ReadonlyArray<string>>([]);

    // Any by default: a reader adding a second tag is usually widening the net, not narrowing
    // it to posts that carry both.
    const [matchAllTags, setMatchAllTags] = useState(false);

    const [currentPage, setCurrentPage] = useState(1);

    // The filters as they stood when Search was pressed — changing an advanced option does not
    // re-run the search until the button is pressed again, matching the Blazor page.
    const [committedFilters, setCommittedFilters] = useState<CommittedFilters>({
        category: '',
        author: '',
    });

    // The box follows the URL when something else navigates here (the header's search link,
    // the tag pills on the Bible-reference pages).
    useEffect(() => {
        setQuery(committedQuery ?? '');
    }, [committedQuery]);

    useEffect(() => {
        document.title = 'Search — Glory 2 Him';
    }, []);

    // A small always-on fetch stands in for the Blazor page's DemoPosts: it feeds the category
    // dropdown and the trending sidebar, as the demo set did.
    const basePosts = postService.useGetPosts({ pageSize: resultsPerPage * 4 });

    const results = postService.useGetPosts({
        q: committedQuery || undefined,
        category: committedFilters.category || undefined,
        author: committedFilters.author || undefined,
        tag: committedFilters.tag,
        page: currentPage,
        pageSize: resultsPerPage,
    });

    const categories = useMemo(() => {
        const distinct = new Map<string, string>();

        for (const post of basePosts.data?.items ?? []) {
            distinct.set(post.category.toLowerCase(), post.category);
        }

        return [...distinct.values()].sort((left, right) => left.localeCompare(right));
    }, [basePosts.data]);

    const trending = useMemo(
        () => (basePosts.data?.items ?? []).slice(0, 4).map(toPostView),
        [basePosts.data]);

    const pageOfResults = useMemo(
        () => (results.data?.items ?? []).map(toPostView),
        [results.data]);

    const totalCount = results.data?.totalCount ?? 0;
    const totalPages = Math.max(1, results.data?.totalPages ?? 1);

    const search = () => {
        setCurrentPage(1);

        setCommittedFilters({
            category: selectedCategory,
            author: selectedAuthor,

            // The posts API filters on a single tag; only the first one can be applied here.
            tag: tags.length > 0 ? tags[0] : undefined,
        });

        setSearchParams(query.length > 0 ? { q: query } : { q: '' });
    };

    const onCategoryChanged = (event: ChangeEvent<HTMLSelectElement>) =>
        setSelectedCategory(event.target.value);

    const onAuthorChanged = (event: ChangeEvent<HTMLInputElement>) =>
        setSelectedAuthor(event.target.value);

    return (
        <>
            {/* Centred and given room to breathe while the box is on its own; once results are
                underneath, the bottom padding hands over to the list's own section. */}
            <section className={hasSearched ? 'pt-5 pb-4' : 'py-5 my-lg-5'}>
                <div className="container">
                    <div className="row justify-content-center">
                        <div className="col-lg-8">
                            {!hasSearched && (
                                <div className="text-center mb-4">
                                    <h1 className="mb-2">Search</h1>
                                    <p className="mb-0">
                                        Find a post by its title, its author, or the topic it covers.
                                    </p>
                                </div>
                            )}

                            <SearchBarComponent
                                query={query}
                                onQueryChange={setQuery}
                                onSearch={search}
                                placeholder="Search posts, authors and topics"
                                advanced={
                                    <div className="row g-3">
                                        <div className="col-sm-6">
                                            <label className="form-label" htmlFor="searchCategory">Category</label>
                                            <select className="form-select" id="searchCategory"
                                                value={selectedCategory} onChange={onCategoryChanged}>
                                                <option value="">Any category</option>
                                                {categories.map((category) => (
                                                    <option key={category} value={category}>{category}</option>
                                                ))}
                                            </select>
                                        </div>

                                        {/* Free text rather than a list: there is no useful upper bound on the
                                            number of authors, and a select would grow past being usable. */}
                                        <div className="col-sm-6">
                                            <label className="form-label" htmlFor="searchAuthor">Author</label>
                                            <input className="form-control" type="text" id="searchAuthor"
                                                placeholder="Any author"
                                                value={selectedAuthor} onChange={onAuthorChanged} />
                                        </div>

                                        <div className="col-12">
                                            <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-2">
                                                {/* A span, not a label: the tag box carries its own aria-label,
                                                    and a <label for> pointing at nothing helps no one. */}
                                                <span className="form-label mb-0">Tags</span>

                                                <div className="btn-group btn-group-sm" role="group"
                                                    aria-label="Match any or all of the tags">
                                                    <input type="radio" className="btn-check" name="tagMatch"
                                                        id="tagMatchAny" autoComplete="off"
                                                        checked={!matchAllTags}
                                                        onChange={() => setMatchAllTags(false)} />
                                                    <label className="btn btn-outline-primary mb-0"
                                                        htmlFor="tagMatchAny">Any</label>

                                                    <input type="radio" className="btn-check" name="tagMatch"
                                                        id="tagMatchAll" autoComplete="off"
                                                        checked={matchAllTags}
                                                        onChange={() => setMatchAllTags(true)} />
                                                    <label className="btn btn-outline-primary mb-0"
                                                        htmlFor="tagMatchAll">All</label>
                                                </div>
                                            </div>

                                            <TagInput
                                                tags={tags}
                                                onTagsChange={setTags}
                                                ariaLabel="Add a tag to search for"
                                                placeholder="Type a tag and press Enter" />
                                        </div>
                                    </div>
                                } />
                        </div>
                    </div>
                </div>
            </section>

            {hasSearched && (
                <section className="pb-5">
                    <div className="container">
                        {results.isLoading ? (
                            <div className="text-center py-5">
                                <div className="spinner-border text-primary" role="status">
                                    <span className="visually-hidden">Loading...</span>
                                </div>
                            </div>
                        ) : results.isError ? (
                            <div className="alert alert-danger text-center mb-0" role="alert">
                                We could not run your search right now. Please try again later.
                            </div>
                        ) : totalCount === 0 ? (
                            <div className="alert alert-info text-center mb-0" role="alert">
                                Nothing matched that search. Try clearing the advanced options.
                            </div>
                        ) : (
                            <div className="row g-4">
                                <div className="col-lg-8">
                                    <p className="text-muted">{totalCount} result(s) found.</p>

                                    {pageOfResults.map((post) => (
                                        <PostListItem key={post.id} post={post} />
                                    ))}

                                    {/* Only when there is a second page to go to — a strip of page numbers over
                                        a single page of results would be decoration pretending to be a control. */}
                                    {totalPages > 1 && (
                                        <div className="mt-4">
                                            <Pagination
                                                currentPage={currentPage}
                                                onPageChange={setCurrentPage}
                                                totalPages={totalPages} />
                                        </div>
                                    )}
                                </div>

                                <div className="col-lg-4">
                                    <BlogSidebar trendingPosts={trending} />
                                </div>
                            </div>
                        )}
                    </div>
                </section>
            )}
        </>
    );
}
