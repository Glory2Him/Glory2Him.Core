import { ChangeEvent, useEffect, useId, useState } from 'react';
import SearchBarComponent from '../coreUI/searchBar';
import { TagInput } from '../coreUI/tagInput';
import { ContentItemSetting } from '../../models/foundations/contentItemSettings/contentItemSetting';
import { ContentType } from '../../models/foundations/contentItemSettings/contentType';

import {
    ShareabilityBasis,
    shareabilityBasisLabels,
    shareabilityBasisMembers
} from '../../models/components/contentItems/contentItemFormItem';

import {
    ContentItemSearchCriteria,
    ContentItemTagMatchMode
} from '../../models/components/contentItems/contentItemSearchItem';

import './contentItems.css';

// THE RED BLOCK: the generic search bar with the advanced-options fold-out, over content items.
// A pure presentation component — it holds the half-typed drafts and raises onSearch with the
// committed criteria; what a search MEANS is the consumer's decision.
//
// Two kinds of criterion live here, and they behave differently on purpose. The TYPED
// criteria (query, Category, Author, Submitted by, Shareability, the Tags list) commit only
// when Search is pressed, matching the search page this bar came from. The CLICKED criteria
// arrive already committed from the pill hooks upstream — a submitted-by pill carries the
// account id a typed name never can — and the committed filters wear removable chips below
// the bar, so a narrowed list always says why on screen.
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
    submittedByLabelText?: string;
    submittedByPlaceholderText?: string;
    shareabilityLabelText?: string;
    anyShareabilityText?: string;
    tagsLabelText?: string;
    tagPlaceholderText?: string;
    tagMatchAnyText?: string;
    tagMatchAllText?: string;
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
    submittedByLabelText = 'Submitted by',
    submittedByPlaceholderText = 'Anyone',
    shareabilityLabelText = 'Shareability',
    anyShareabilityText = 'Any shareability',
    tagsLabelText = 'Tags',
    tagPlaceholderText = 'Type a tag and press Enter',
    tagMatchAnyText = 'Any',
    tagMatchAllText = 'All',
    submittedByChipText = 'Submitted by',
    tagChipText = 'Tag',
    removeFilterText = 'Remove this filter'
}: ContentItemSearchBarPanelProps) {
    const fieldId = useId();

    const [draftQuery, setDraftQuery] = useState(criteria?.query ?? '');
    const [draftAuthor, setDraftAuthor] = useState(criteria?.author ?? '');

    const [draftContentType, setDraftContentType] =
        useState<ContentType | null>(criteria?.contentType ?? null);

    const [draftSubmittedByName, setDraftSubmittedByName] =
        useState(criteria?.submittedBy?.name ?? '');

    const [draftShareabilityBasis, setDraftShareabilityBasis] =
        useState<ShareabilityBasis | null>(criteria?.shareabilityBasis ?? null);

    const [draftTags, setDraftTags] =
        useState<ReadonlyArray<string>>(criteria?.tags ?? []);

    const [draftTagMatchMode, setDraftTagMatchMode] =
        useState<ContentItemTagMatchMode>(criteria?.tagMatchMode ?? 'any');

    // Keyed on the MEMBERS rather than on the object, so a consumer building the criteria inline
    // — the natural thing when they live in the URL — does not wipe what is being typed on every
    // render.
    const committedTagsKey = (criteria?.tags ?? []).join('\u241f');

    useEffect(() => {
        setDraftQuery(criteria?.query ?? '');
        setDraftAuthor(criteria?.author ?? '');
        setDraftContentType(criteria?.contentType ?? null);
        setDraftSubmittedByName(criteria?.submittedBy?.name ?? '');
        setDraftShareabilityBasis(criteria?.shareabilityBasis ?? null);
        setDraftTags(criteria?.tags ?? []);
        setDraftTagMatchMode(criteria?.tagMatchMode ?? 'any');
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [
        criteria?.query,
        criteria?.author,
        criteria?.contentType,
        criteria?.submittedBy?.name,
        criteria?.shareabilityBasis,
        committedTagsKey,
        criteria?.tagMatchMode
    ]);

    // WHAT A TYPED SUBMITTED-BY MEANS. The box shows the committed criterion's name; while
    // the reader leaves it alone, the id a pill-click carried survives a Search. The moment
    // they retype it the id is gone — the bar has no resolver from a display name to an
    // account — so a typed name travels with an empty id and the page filters on what it can.
    const committedSubmittedBy = () => {
        const typedName = draftSubmittedByName.trim();

        if (typedName.length === 0) {
            return null;
        }

        return typedName === (criteria?.submittedBy?.name ?? '')
            ? criteria?.submittedBy ?? { id: '', name: typedName }
            : { id: '', name: typedName };
    };

    const committed = (): ContentItemSearchCriteria => ({
        query: draftQuery,
        contentType: draftContentType,
        author: draftAuthor,
        submittedBy: committedSubmittedBy(),
        tags: draftTags,
        tagMatchMode: draftTagMatchMode,
        shareabilityBasis: draftShareabilityBasis
    });

    const search = () => onSearch?.(committed());

    const clearSubmittedBy = () => {
        setDraftSubmittedByName('');
        onSearch?.({ ...committed(), submittedBy: null });
    };

    const removeTag = (tag: string) => {
        const remaining = (criteria?.tags ?? []).filter((listed) => listed !== tag);

        setDraftTags(remaining);
        onSearch?.({ ...committed(), tags: remaining });
    };

    const onCategoryChanged = (event: ChangeEvent<HTMLSelectElement>) =>
        setDraftContentType(
            event.target.value.length === 0 ? null : Number(event.target.value) as ContentType);

    const onShareabilityChanged = (event: ChangeEvent<HTMLSelectElement>) =>
        setDraftShareabilityBasis(
            event.target.value.length === 0
                ? null
                : Number(event.target.value) as ShareabilityBasis);

    // A soft-deleted row is excluded from active policy resolution (§6.6).
    const filterableSettings = contentItemSettingCollection
        .filter((setting) => setting.isDeleted !== true && setting.contentItemId == null)
        .sort((first, second) => first.sortOrder - second.sortOrder);

    const submittedBy = criteria?.submittedBy ?? null;
    const committedTags = criteria?.tags ?? [];

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
                            it asks about the AUTHOR OF THE WORDS, not whoever submitted the row. */}
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

                        {/* Whoever SUBMITTED the row — the other person a card names. A typed
                            name travels without an account id (see committedSubmittedBy). */}
                        <div className="col-sm-6">
                            <label className="form-label" htmlFor={`${fieldId}-submitted-by`}>
                                {submittedByLabelText}
                            </label>

                            <input
                                className="form-control"
                                type="text"
                                id={`${fieldId}-submitted-by`}
                                placeholder={submittedByPlaceholderText}
                                value={draftSubmittedByName}
                                onChange={(event) =>
                                    setDraftSubmittedByName(event.target.value)} />
                        </div>

                        {/* The basis is a small closed set, so it is a list — the PICKER
                            labels rather than the read ones, because the read labels collapse
                            the owned and non-owned members into the same words and a filter
                            whose options repeat is a filter nobody can use. */}
                        <div className="col-sm-6">
                            <label className="form-label" htmlFor={`${fieldId}-shareability`}>
                                {shareabilityLabelText}
                            </label>

                            <select
                                className="form-select"
                                id={`${fieldId}-shareability`}
                                value={draftShareabilityBasis == null
                                    ? ''
                                    : String(draftShareabilityBasis)}
                                onChange={onShareabilityChanged}>

                                <option value="">{anyShareabilityText}</option>

                                {shareabilityBasisMembers.map((basis) => (
                                    <option key={basis} value={String(basis)}>
                                        {shareabilityBasisLabels[basis]}
                                    </option>
                                ))}
                            </select>
                        </div>

                        {/* The Tags list, full width, with the Any/All match mode standing
                            beside the label. Enter turns what is typed into a pill; the pills
                            commit with Search like every other typed criterion. */}
                        <div className="col-12">
                            <div className="d-flex justify-content-between align-items-center">
                                <span className="form-label mb-0">{tagsLabelText}</span>

                                <div
                                    className="btn-group btn-group-sm"
                                    role="group"
                                    aria-label={`${tagsLabelText} match mode`}>
                                    <button
                                        type="button"
                                        className={`btn mb-0 ${draftTagMatchMode === 'any'
                                            ? 'btn-primary'
                                            : 'btn-outline-primary'}`}
                                        aria-pressed={draftTagMatchMode === 'any'}
                                        onClick={() => setDraftTagMatchMode('any')}>
                                        {tagMatchAnyText}
                                    </button>

                                    <button
                                        type="button"
                                        className={`btn mb-0 ${draftTagMatchMode === 'all'
                                            ? 'btn-primary'
                                            : 'btn-outline-primary'}`}
                                        aria-pressed={draftTagMatchMode === 'all'}
                                        onClick={() => setDraftTagMatchMode('all')}>
                                        {tagMatchAllText}
                                    </button>
                                </div>
                            </div>

                            <div className="mt-2">
                                <TagInput
                                    tags={draftTags}
                                    onTagsChange={setDraftTags}
                                    placeholder={tagPlaceholderText}
                                    ariaLabel={tagPlaceholderText}
                                    tagPrefix="#" />
                            </div>
                        </div>
                    </div>
                } />

            {/* The clicked criteria, worn where the reader can see and remove them. Without this
                row a pill-click filter would be invisible state — a narrowed list with nothing on
                screen saying why. */}
            {(submittedBy != null || committedTags.length > 0) && (
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

                    {committedTags.map((committedTag) => (
                        <button
                            key={committedTag}
                            type="button"
                            className="btn btn-xs btn-success-soft mb-0"
                            onClick={() => removeTag(committedTag)}
                            title={removeFilterText}>
                            {tagChipText} #{committedTag}
                            <i className="bi bi-x ms-1" aria-hidden="true"></i>
                        </button>
                    ))}
                </div>
            )}
        </div>
    );
}
