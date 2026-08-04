import { Link } from 'react-router-dom';
import { PostView } from '../../../models/coreUI/postView';

// Placeholder article prose shared by the post-single demos, so each of those pages carries
// only the chrome that actually distinguishes it. Ported from the Blazor
// SampleArticleBodyComponent.

export interface SampleArticleBodyProps {
    post: PostView;
    showTags?: boolean;
    tags?: ReadonlyArray<string>;
}

export function SampleArticleBody({
    post,
    showTags = true,
    tags = ['Faith', 'Journey', 'Hope'],
}: SampleArticleBodyProps) {
    return (
        <>
            <p className="lead">{post.excerpt}</p>

            <p>
                Traveling light is a discipline of trust. Every extra thing we carry is one more thing to
                guard, and the road has a way of showing us how little we truly need. The same is true of the
                heart — what we hold onto shapes how freely we can move.
            </p>

            <blockquote className="blockquote border-start border-3 border-primary ps-4 my-4">
                <p className="mb-2">
                    "Cast all your anxiety on him because he cares for you."
                </p>
                <footer className="blockquote-footer mb-0">1 Peter 5:7</footer>
            </blockquote>

            <h3 className="h5 mt-4">What we learned along the way</h3>

            <p>
                The best moments were rarely the ones we planned. They arrived in the gaps — a shared meal, an
                unhurried conversation, a morning where nothing was scheduled and everything mattered.
            </p>

            <ul>
                <li>Leave room in the day for the unplanned.</li>
                <li>Ask more questions than you answer.</li>
                <li>Write down the small things; they become the big ones.</li>
            </ul>

            {showTags && (
                <>
                    <hr className="my-4" />

                    <div className="d-flex flex-wrap align-items-center gap-2">
                        <span className="fw-semibold me-1">Tags:</span>
                        {tags.map((tag) => (
                            <Link
                                key={tag}
                                to={`/Tag?name=${encodeURIComponent(tag)}`}
                                className="btn btn-sm btn-outline-secondary">
                                {tag}
                            </Link>
                        ))}
                    </div>
                </>
            )}
        </>
    );
}
