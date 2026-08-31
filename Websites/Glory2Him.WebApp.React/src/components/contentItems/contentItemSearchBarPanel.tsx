import { ChangeEvent, useEffect, useId, useState } from 'react';
import SearchBarComponent from '../coreUI/searchBar';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    ContentItemSearchCriteria
} from '../../models/components/contentItems/contentItemSearchItem';

import './contentItems.css';

// THE RED BLOCK: the generic search bar with the advanced-options fold-out, over content items.
// A pure presentation component — it holds the half-typed drafts and raises onSearch with the
// committed criteria; what a search MEANS is the consumer's decision.
//
// Two kinds of criterion live here, and they behave differently on purpose. The TYPED boxes
// (query, Category, Author) commit only when Search is pressed, matching the search page this
// bar came from. The CLICKED criteria (submitted-by, tag) have no box at all — they are set by
// the pill hooks upstream and arrive already committed — so the bar's job for those is to show
// them as chips and let the reader take them off again.
export interface ContentItemSearchBarPanelProps {
    // As last committed. Seeds the boxes and RESEEDS them when it changes, so a page landing from
    // ?q= shows what it searched for and a pill-click upstream is reflected here.
    criteria?: ContentItemSearchCriteria;

    onSearch?: (criteria: ContentItemSearchCriteria) => void;

    // The Category box is built from the per-type DEFAULT rows among these, in the
    // administrator's own SortOrder. Deliberately NOT filtered by
    // IsAvailableAsGeneralUserContribution the way the contribution picker is: searching is not
    // contributing, and a reader must be able to narrow to blog posts whether or not they may
    // write one.
    contentItemSettingCollection?: ReadonlyArray<ContentItemSetting>;

    // ── Text ──────────────────────────────────────────────────────────────────
    placeholderText?: string;
    categoryLabelText?: string;
    anyCategoryText?: string;
    authorLabelText?: string;
    authorPlaceholderText?: string;
    submittedByChipText?: string;
    tagChipText?: string;
    removeFilterText?: string;
}

export function ContentItemSearchBarPanel({
    criteria,
    onSearch,
    contentItemSettingCollection = [],
    placeholderText = 'Search posts, authors and topics',
    categoryLabelText = 'Category',
    anyCategoryText = 'Any category',
    authorLabelText = 'Author',
    authorPlaceholderText = 'Any author',
    submittedByChipText = 'Submitted by',
    tagChipText = 'Tag',
    removeFilterText = 'Remove this filter'
}: ContentItemSearchBarPanelProps) {
    const fieldId = useId();

    const [draftQuery, setDraftQuery] = useState(criteria?.query ?? '');
    const [draftAuthor, setDraftAuthor] = useState(criteria?.author ?? '');

    const [draftContentType, setDraftContentType] =
        useState<ContentType | null>(criteria?.contentType ?? null);

    // Keyed on the MEMBERS rather than on the object, so a consumer building the criteria inline
    // — the natural thing when they live in the URL — does not wipe what is being typed on every
    // render.
    useEffect(() => {
        setDraftQuery(criteria?.query ?? '');
        setDraftAuthor(criteria?.author ?? '');
        setDraftContentType(criteria?.contentType ?? null);
    }, [criteria?.query, criteria?.author, criteria?.contentType]);

    // Committing keeps the clicked criteria as they stand — pressing Search narrows within the
    // person or tag the reader clicked their way into, rather than silently widening back out.
    const committed = (): ContentItemSearchCriteria => ({
        query: draftQuery,
        contentType: draftContentType,
        author: draftAuthor,
        submittedBy: criteria?.submittedBy ?? null,
        tag: criteria?.tag ?? null
    });

    const search = () => onSearch?.(committed());

    const clearSubmittedBy = () => onSearch?.({ ...committed(), submittedBy: null });
    const clearTag = () => onSearch?.({ ...committed(), tag: null });

    const onCategoryChanged = (event: ChangeEvent<HTMLSelectElement>) =>
        setDraftContentType(
            event.target.value.length === 0 ? null : Number(event.target.value) as ContentType);

    // A soft-deleted row is excluded from active policy resolution (§6.6).
    const filterableSettings = contentItemSettingCollection
        .filter((setting) => setting.isDeleted !== true && setting.contentItemId == null)
        .sort((first, second) => first.sortOrder - second.sortOrder);

    const submittedBy = criteria?.submittedBy ?? null;
    const tag = criteria?.tag ?? null;

    return (
        <div className="g2h-content-item-search-bar">
            <SearchBarComponent
                query={draftQuery}
                onQueryChange={setDraftQuery}
                onSearch={search}
                placeholder={placeholderText}
                advanced={
                    <div className="row g-3">
                        <div className="col-sm-6">
                            <label className="form-label" htmlFor={`${fieldId}-category`}>
                                {categoryLabelText}
                            </label>

                            <select
                                className="form-select"
                                id={`${fieldId}-category`}
                                value={draftContentType == null ? '' : String(draftContentType)}
                                onChange={onCategoryChanged}>

                                <option value="">{anyCategoryText}</option>

                                {filterableSettings.map((setting) => (
                                    <option
                                        key={setting.contentType}
                                        value={String(setting.contentType)}>
                                        {setting.contentTypeName}
                                    </option>
                                ))}
                            </select>
                        </div>

                        {/* Free text rather than a list — no useful upper bound on authors — and
                            it asks about the AUTHOR OF THE WORDS, not whoever submitted the row.

                            There is deliberately no Tags BOX. Associations have no HTTP exposer
                            yet (#318), so typing a tag would be a control that does nothing; the
                            tag criterion exists, but it arrives by clicking a pill on a card and
                            shows below as a chip. */}
                        <div className="col-sm-6">
                            <label className="form-label" htmlFor={`${fieldId}-author`}>
                                {authorLabelText}
                            </label>

                            <input
                                className="form-control"
                                type="text"
                                id={`${fieldId}-author`}
                                placeholder={authorPlaceholderText}
                                value={draftAuthor}
                                onChange={(event) => setDraftAuthor(event.target.value)} />
                        </div>
                    </div>
                } />

            {/* The clicked criteria, worn where the reader can see and remove them. Without this
                row a pill-click filter would be invisible state — a narrowed list with nothing on
                screen saying why. */}
            {(submittedBy != null || tag != null) && (
                <div className="d-flex flex-wrap align-items-center gap-2 mt-3">
                    {submittedBy != null && (
                        <button
                            type="button"
                            className="btn btn-xs btn-primary-soft mb-0"
                            onClick={clearSubmittedBy}
                            title={removeFilterText}>
                            {submittedByChipText} {submittedBy.name}
                            <i className="bi bi-x ms-1" aria-hidden="true"></i>
                        </button>
                    )}

                    {tag != null && (
                        <button
                            type="button"
                            className="btn btn-xs btn-success-soft mb-0"
                            onClick={clearTag}
                            title={removeFilterText}>
                            {tagChipText} #{tag}
                            <i className="bi bi-x ms-1" aria-hidden="true"></i>
                        </button>
                    )}
                </div>
            )}
        </div>
    );
}
