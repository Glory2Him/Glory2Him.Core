import { ReactElement } from "react";
import { Link } from "react-router-dom";

// Full-width category dropdown (Blogzine's "Lifestyle" mega menu), ported from the Blazor
// MegaMenuComponent: a row of post thumbnails with a topic pill cloud beneath. Bootstrap's
// own dropdown (bootstrap.bundle.min.js, loaded globally) drives the toggle via data-bs
// attributes. Presentational only (ts-ui-001).
export interface MegaMenuPostView {
    title: string;
    slug: string;
    imageUrl: string;
    category: string;
    categoryBadgeCss: string;
}

type MegaMenuComponentProps = {
    title: string,
    posts?: MegaMenuPostView[],
    topics?: string[]
}

// Bootstrap needs a stable id to tie the toggle to its menu; derive it from the title so a
// page with two mega menus still gets two distinct ids.
const toMenuId = (title: string): string =>
    "mega-menu-" + title
        .split("")
        .map((character) => /[a-zA-Z0-9]/.test(character)
            ? character.toLowerCase()
            : "-")
        .join("");

export default function MegaMenuComponent({
    title,
    posts = [],
    topics = []
}: MegaMenuComponentProps): ReactElement {
    const menuId = toMenuId(title);

    return (
        <li className="nav-item dropdown dropdown-fullwidth">
            <a className="nav-link dropdown-toggle" href="#" id={menuId}
                data-bs-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                {title}
            </a>

            <div className="dropdown-menu p-4" aria-labelledby={menuId}>
                <div className="row g-4">
                    {posts.map((post) => (
                        <div className="col-sm-6 col-lg-3" key={post.slug}>
                            <div className="card bg-transparent">
                                <img className="card-img rounded" src={post.imageUrl} alt={post.title} />
                                <div className="card-body px-0 pt-3">
                                    <Link to="/Categories" className={`badge ${post.categoryBadgeCss} mb-2`}>
                                        {post.category}
                                    </Link>
                                    <h6 className="card-title mb-0">
                                        <Link to={`/Post-Single/${post.slug}`}
                                            className="btn-link text-reset stretched-link fw-bold">{post.title}</Link>
                                    </h6>
                                </div>
                            </div>
                        </div>
                    ))}
                </div>

                {topics.length > 0 && (
                    <>
                        <hr />
                        <h6 className="text-uppercase small text-body-secondary mb-3">Browse topics</h6>
                        <ul className="list-inline mb-0">
                            {topics.map((topic) => (
                                <li className="list-inline-item mb-2" key={topic}>
                                    <Link to={`/Tag?name=${encodeURIComponent(topic)}`}
                                        className="btn btn-sm btn-primary-soft">{topic}</Link>
                                </li>
                            ))}
                        </ul>
                    </>
                )}
            </div>
        </li>
    );
}
