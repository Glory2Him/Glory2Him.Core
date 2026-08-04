import { FormEvent, useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { PageHeader } from '../components/coreUI/pageHeader';
import { PostListItem } from '../components/coreUI/postListItem';
import { PostView } from '../models/coreUI/postView';
import { postService } from '../services/foundations/postService';

// Ported from SearchResult.razor + SearchResultBase.cs. The Blazor page pulled every post and
// filtered it client-side; here the posts API does the matching through useGetPosts({ q }).
export function SearchResult() {
    const [searchParams, setSearchParams] = useSearchParams();
    const committedQuery = searchParams.get('q') ?? '';

    const [query, setQuery] = useState(committedQuery);

    useEffect(() => {
        setQuery(committedQuery);
    }, [committedQuery]);

    useEffect(() => {
        document.title = 'Search — Glory 2 Him';
    }, []);

    const { data, isLoading, isError } = postService.useGetPosts({
        q: committedQuery || undefined,
    });

    // The API returns publishedDate as an ISO string; PostListItem formats it as a Date.
    const results = useMemo(
        () => (data?.items ?? []).map((post: PostView): PostView => ({
            ...post,
            publishedDate: new Date(post.publishedDate as unknown as string),
        })),
        [data]);

    const onSubmit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setSearchParams({ q: query });
    };

    return (
        <>
            <PageHeader title="Search results" />

            <section className="pt-4 pb-5">
                <div className="container">
                    <div className="row mb-4">
                        <div className="col-lg-8 mx-auto">
                            <form className="input-group" onSubmit={onSubmit}>
                                <input className="form-control" type="search" name="q" value={query}
                                    onChange={(event) => setQuery(event.target.value)}
                                    placeholder="Search posts" aria-label="Search" />
                                <button className="btn btn-primary m-0" type="submit">Search</button>
                            </form>
                        </div>
                    </div>

                    {isLoading ? (
                        <div className="text-center py-5">
                            <div className="spinner-border text-primary" role="status">
                                <span className="visually-hidden">Loading...</span>
                            </div>
                        </div>
                    ) : isError ? (
                        <div className="alert alert-danger text-center mb-0" role="alert">
                            We could not run your search right now. Please try again later.
                        </div>
                    ) : results.length === 0 ? (
                        <div className="alert alert-info text-center mb-0" role="alert">
                            No posts matched <strong>{committedQuery}</strong>. Try a different search.
                        </div>
                    ) : (
                        <div className="row">
                            <div className="col-lg-10 mx-auto">
                                <p className="text-muted">{results.length} result(s) found.</p>
                                {results.map((post) => (
                                    <PostListItem key={post.id} post={post} />
                                ))}
                            </div>
                        </div>
                    )}
                </div>
            </section>
        </>
    );
}
