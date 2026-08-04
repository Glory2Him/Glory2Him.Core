import { Link } from 'react-router-dom';

// Row of hashtag pills, optionally followed by bible-reference pills carrying a book icon. Both
// read as small soft buttons so a card can show its topics without competing with the title.
//
// A tag goes to a search for it; a reference goes to the passage itself. The reference page shows
// one fixed verse for now, so its link carries no query — a reference in the URL that the page
// ignored would be misleading.
export interface TagPillListProps {
    tags?: ReadonlyArray<string>;
    bibleReferences?: ReadonlyArray<string>;
    tagCssClass?: string;
    sizeCssClass?: string;
    cssClass?: string;
}

export function TagPillList({
    tags = [],
    bibleReferences = [],
    tagCssClass = 'btn-success-soft',
    sizeCssClass = 'btn-xs',
    cssClass = 'mb-2',
}: TagPillListProps) {
    return (
        <div className={`d-flex flex-wrap align-items-center gap-2 ${cssClass}`}>
            {tags.map((tag) => (
                <Link
                    key={tag}
                    to={`/Search?q=${encodeURIComponent(tag)}`}
                    className={`btn ${sizeCssClass} ${tagCssClass} mb-0`}>
                    #{tag}
                </Link>
            ))}

            {bibleReferences.map((reference) => (
                <Link
                    key={reference}
                    to="/BibleReferences"
                    className={`btn ${sizeCssClass} btn-primary-soft mb-0`}>
                    <i className="bi bi-book me-1"></i>{reference}
                </Link>
            ))}
        </div>
    );
}
