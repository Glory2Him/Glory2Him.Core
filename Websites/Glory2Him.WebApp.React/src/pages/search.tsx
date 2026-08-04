import { ChangeEvent, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { BlogSidebar } from '../components/coreUI/blogSidebar';
import { Pagination } from '../components/coreUI/pagination';
import { PostListItem } from '../components/coreUI/postListItem';
import SearchBarComponent from '../components/coreUI/searchBar';
import { TagInput } from '../components/coreUI/tagInput';
import { PostView } from '../models/coreUI/postView';
import { SamplePost, latest } from './sampleContent';

// The post-list layout without its hero banner: a search box stands where the banner was, and the
// list below it only appears once something has been searched for.
//
// Ported from Search.razor + SearchBase.cs. This is a demo: whatever is typed, the same posts come
// back — the ones the home page lists. The text is not matched against anything, so the flow can
// be shown without a search index behind it. The advanced options do narrow that set, so nothing
// on screen is a control that does nothing. The query lives in the URL (?q=) so the header's
// /Search link and deep links land with the results already showing, exactly as the Blazor page
// did with [SupplyParameterFromQuery].
export const resultsPerPage = 5;

const toPostView = (post: SamplePost): PostView => ({
    id: post.slug,
    title: post.title,
    slug: post.slug,
    excerpt: post.excerpt,
    imageUrl: post.imageUrl,
    category: post.category,
    categoryBadgeCss: post.categoryBadgeCss,
    authorName: post.authorName,
    authorImageUrl: post.authorImageUrl,
    publishedDate: post.publishedDate,
    readMinutes: post.readMinutes,
    isFeatured: post.isFeatured,
    tags: post.tags,
});

const demoPosts: ReadonlyArray<PostView> = latest.map(toPostView);

interface CommittedFilters {
    category: string;
    author: string;
    tags: ReadonlyArray<string>;
    matchAllTags: boolean;
}

// Query deliberately absent: the demo always returns the set, whatever was typed.
const matches = (post: PostView, filters: CommittedFilters): boolean => {
    const matchesCategory =
        filters.category.trim().length === 0
            || post.category.toLowerCase() === filters.category.toLowerCase();

    // Contains, not equals: the author box is free text, so a surname or a first name has
    // to be enough to find someone.
    const matchesAuthor =
        filters.author.trim().length === 0
            || post.authorName.toLowerCase().includes(filters.author.trim().toLowerCase());

    return matchesCategory && matchesAuthor && matchesTags(post, filters);
};

const matchesTags = (post: PostView, filters: CommittedFilters): boolean => {
    if (filters.tags.length === 0) {
        return true;
    }

    const carries = (tag: string) =>
        post.tags.some((posted) => posted.toLowerCase() === tag.toLowerCase());

    return filters.matchAllTags
        ? filters.tags.every(carries)
        : filters.tags.some(carries);
};

const categories = [...new Map(
    demoPosts.map((post) => [post.category.toLowerCase(), post.category]))
    .values()]
    .sort((left, right) => left.localeCompare(right));

const trending = demoPosts.slice(0, 4);

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
        tags: [],
        matchAllTags: false,
    });

    // The box follows the URL when something else navigates here (the header's search link,
    // the tag pills on the Bible-reference pages).
    useEffect(() => {
        setQuery(committedQuery ?? '');
    }, [committedQuery]);

    useEffect(() => {
        document.title = 'Search — Glory 2 Him';
    }, []);

    const results = useMemo(
        () => demoPosts.filter((post) => matches(post, committedFilters)),
        [committedFilters]);

    const totalCount = results.length;
    const totalPages = Math.max(1, Math.ceil(totalCount / resultsPerPage));

    const pageOfResults = results.slice(
        (currentPage - 1) * resultsPerPage,
        currentPage * resultsPerPage);

    const search = () => {
        setCurrentPage(1);

        setCommittedFilters({
            category: selectedCategory,
            author: selectedAuthor,
            tags,
            matchAllTags,
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
                        {totalCount === 0 ? (
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
