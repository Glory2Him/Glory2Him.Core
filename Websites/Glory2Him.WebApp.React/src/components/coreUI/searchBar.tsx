import { FormEvent, ReactElement, ReactNode, useState } from "react";

// One wide box with a green Search button beside it, in the manner of a search engine's home
// page, ported from the Blazor SearchBarComponent. A chevron sits on the end of the button
// group and folds out whatever advanced options the page supplies; it only renders when the
// page actually supplies some. Presentational only — the page decides what a search means
// (ts-ui-001): pages typically pass an onSearch that calls react-router's useNavigate, e.g.
// navigate(`/Search?query=${encodeURIComponent(query)}`).
type SearchBarComponentProps = {
    query: string,
    onQueryChange: (query: string) => void,
    onSearch: () => void,
    placeholder?: string,

    // Left undefined by pages that want the plain box; the chevron only appears when there is
    // something behind it.
    advanced?: ReactNode
}

// A fixed id is safe here: aria-controls only has to be unique on the page, and a page
// carries one search bar.
const advancedPanelId = "advancedSearchOptions";

export default function SearchBarComponent({
    query,
    onQueryChange,
    onSearch,
    placeholder = "Search",
    advanced
}: SearchBarComponentProps): ReactElement {
    const [isAdvancedOpen, setIsAdvancedOpen] = useState(false);

    const onSubmit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        onSearch();
    };

    return (
        <>
            <form onSubmit={onSubmit}>
                <div className="input-group input-group-lg">
                    <input className="form-control border-success" type="search"
                        placeholder={placeholder} value={query}
                        onChange={(event) => onQueryChange(event.target.value)}
                        aria-label={placeholder} />

                    <button className="btn btn-success px-4 m-0" type="submit">
                        <i className="bi bi-search me-2"></i>Search
                    </button>

                    {advanced !== undefined && (
                        <button className="btn btn-success m-0" type="button"
                            onClick={() => setIsAdvancedOpen(!isAdvancedOpen)}
                            aria-expanded={isAdvancedOpen ? "true" : "false"}
                            aria-controls={advancedPanelId}
                            aria-label="Advanced search options"
                            title="Advanced search options">
                            <i className={`bi ${isAdvancedOpen ? "bi-chevron-up" : "bi-chevron-down"}`}></i>
                        </button>
                    )}
                </div>
            </form>

            {/* Outside the form on purpose. The advanced fields raise their own events as
                they change, so they gain nothing from being submitted — and a text box inside
                a form with a submit button runs the search the moment Enter is pressed, which
                would make a tag box impossible to type into. */}
            {advanced !== undefined && isAdvancedOpen && (
                <div id={advancedPanelId} className="border rounded-3 p-3 p-lg-4 mt-3">
                    {advanced}
                </div>
            )}
        </>
    );
}
