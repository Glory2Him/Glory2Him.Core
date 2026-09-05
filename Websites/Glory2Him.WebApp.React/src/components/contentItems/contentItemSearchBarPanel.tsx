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
    ApprovalStatus,
    ContentItemSearchCriteria,
    ContentItemTagMatchMode,
    contentItemSearchApprovalStatusMembers,
    defaultContentItemSearchApprovalStatusSelection,
    resolveContentItemSearchApprovalStatuses,
    toContentItemSearchApprovalStatuses
} from '../../models/components/contentItems/contentItemSearchItem';

import {
    approvalStatusRibbonLabels
} from '../../models/components/contentItems/contentItemTemplate';

import './contentItems.css';

// THE RED BLOCK: the generic search bar with the advanced-options fold-out, over content items.
// A pure presentation component — it holds the half-typed drafts and raises onSearch with the
// committed criteria; what a search MEANS is the consumer's decision.
//
// Every criterion has a box, and every box commits on Search — the one register the search
// page this bar came from keeps. The CLICKED criteria (a pill on a card) arrive already
// committed from the hooks upstream and RESEED the boxes — a submitted-by pill carries the
// account id a typed name never can — so opening the advanced options shows every filter
// in play, each removable where it stands and recommitted by Search.
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

    // Whether the advanced options carry the APPROVAL STATUS checkbox group — Draft,
    // Submitted, Approved, Rejected. Off by default, because the public feed's reader has no
    // business being offered a status they can never see; a "my posts" page and the
    // moderation queue turn it on, being the two surfaces where a row's status is the thing
    // a reader is actually looking for.
    //
    // THE ONE OPTION THAT DOES NOT WAIT FOR SEARCH. Ticking a box commits immediately, the
    // way a pill-click on a card does: a checkbox has already said everything it has to say
    // the moment it changes, and a filter that needs a second press to take effect is a
    // filter that reads as broken. Everything else drafted in the fold-out rides along on
    // that commit, exactly as it would have on the Search press.
    showApprovalStatusSearchOptions?: boolean;

    // WHICH BOXES START TICKED — the surface's DEFAULT selection, one flag per status. They
    // rest at the decided rows (Approved and Rejected on, Draft and Submitted off), which
    // is what a journal shows; /myposts and the admin posts list turn all four on. A
    // committed selection in the criteria OVERRIDES them — the reader's choice, once made,
    // is what the boxes show — and unticking the last box hands the surface back to these,
    // because "no status at all" is not a search anybody means.
    //
    // THE READ MUST AGREE. The bar only draws the boxes; the page owns the request, and hands
    // the same four to its search hook (defaultApprovalStatuses) so the results are read with
    // exactly the statuses the boxes show ticked.
    searchApprovalDraftSelected?: boolean;
    searchApprovalSubmittedSelected?: boolean;
    searchApprovalApprovedSelected?: boolean;
    searchApprovalRejectedSelected?: boolean;

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
    bibleReferencesLabelText?: string;
    bibleReferencePlaceholderText?: string;
    approvalStatusLabelText?: string;
}

export function ContentItemSearchBarPanel({
    criteria,
    onSearch,
    contentItemSettingCollection = [],
    showApprovalStatusSearchOptions = false,
    searchApprovalDraftSelected = defaultContentItemSearchApprovalStatusSelection.draft,
    searchApprovalSubmittedSelected = defaultContentItemSearchApprovalStatusSelection.submitted,
    searchApprovalApprovedSelected = defaultContentItemSearchApprovalStatusSelection.approved,
    searchApprovalRejectedSelected = defaultContentItemSearchApprovalStatusSelection.rejected,
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
    bibleReferencesLabelText = 'Bible references',
    bibleReferencePlaceholderText =
    'Type a bible reference and press Enter (e.g. John 3:16)',
    approvalStatusLabelText = 'Approval status',
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

    const [draftBibleReferences, setDraftBibleReferences] =
        useState<ReadonlyArray<string>>(criteria?.bibleReferences ?? []);

    const [draftBibleReferenceMatchMode, setDraftBibleReferenceMatchMode] =
        useState<ContentItemTagMatchMode>(criteria?.bibleReferenceMatchMode ?? 'any');

    // The boxes show the statuses the read was made with: the committed selection where the
    // reader made one, the surface's four flags where they did not.
    const defaultApprovalStatuses = toContentItemSearchApprovalStatuses({
        draft: searchApprovalDraftSelected,
        submitted: searchApprovalSubmittedSelected,
        approved: searchApprovalApprovedSelected,
        rejected: searchApprovalRejectedSelected
    });

    const seededApprovalStatuses = resolveContentItemSearchApprovalStatuses(
        criteria?.approvalStatuses ?? [], defaultApprovalStatuses);

    const [draftApprovalStatuses, setDraftApprovalStatuses] =
        useState<ReadonlyArray<ApprovalStatus>>(seededApprovalStatuses);

    // Keyed on the MEMBERS rather than on the object, so a consumer building the criteria inline
    // — the natural thing when they live in the URL — does not wipe what is being typed on every
    // render.
    const committedTagsKey = (criteria?.tags ?? []).join('\u241f');
    const committedBibleReferencesKey = (criteria?.bibleReferences ?? []).join('\u241f');
    const seededApprovalStatusesKey = seededApprovalStatuses.join('\u241f');

    useEffect(() => {
        setDraftQuery(criteria?.query ?? '');
        setDraftAuthor(criteria?.author ?? '');
        setDraftContentType(criteria?.contentType ?? null);
        setDraftSubmittedByName(criteria?.submittedBy?.name ?? '');
        setDraftShareabilityBasis(criteria?.shareabilityBasis ?? null);
        setDraftTags(criteria?.tags ?? []);
        setDraftTagMatchMode(criteria?.tagMatchMode ?? 'any');
        setDraftBibleReferences(criteria?.bibleReferences ?? []);
        setDraftBibleReferenceMatchMode(criteria?.bibleReferenceMatchMode ?? 'any');
        setDraftApprovalStatuses(seededApprovalStatuses);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [
        criteria?.query,
        criteria?.author,
        criteria?.contentType,
        criteria?.submittedBy?.name,
        criteria?.shareabilityBasis,
        committedTagsKey,
        criteria?.tagMatchMode,
        committedBibleReferencesKey,
        criteria?.bibleReferenceMatchMode,
        seededApprovalStatusesKey
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

    // The statuses are taken as a PARAMETER rather than read off the draft state, because the
    // checkbox group commits in the same breath as it changes and a setState is not visible to
    // the handler that called it — the commit would otherwise carry the tick before last.
    const committed = (
        approvalStatuses: ReadonlyArray<ApprovalStatus>): ContentItemSearchCriteria => ({
        query: draftQuery,
        contentType: draftContentType,
        author: draftAuthor,
        submittedBy: committedSubmittedBy(),
        tags: draftTags,
        tagMatchMode: draftTagMatchMode,
        bibleReferences: draftBibleReferences,
        bibleReferenceMatchMode: draftBibleReferenceMatchMode,
        shareabilityBasis: draftShareabilityBasis,

        // Boxes the surface never drew commit nothing of their own: a journal that hides
        // the group carries whatever its criteria already held, so its defaults stay the
        // read's business and never reach the URL from a bar the reader could not see.
        approvalStatuses: showApprovalStatusSearchOptions
            ? approvalStatuses
            : criteria?.approvalStatuses ?? []
    });

    const search = () => onSearch?.(committed(draftApprovalStatuses));

    // Ticked on, ticked off — and committed there and then, no Search press. The consumer
    // sees one search signal with the box's new state and everything else drafted beside it.
    const approvalStatusToggled = (approvalStatus: ApprovalStatus) => {
        const toggled = draftApprovalStatuses.includes(approvalStatus)
            ? draftApprovalStatuses.filter((listed) => listed !== approvalStatus)
            : [...draftApprovalStatuses, approvalStatus]
                .sort((first, second) => first - second);

        setDraftApprovalStatuses(toggled);
        onSearch?.(committed(toggled));
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

                        {/* Bible references, behaving exactly as the tags do but wearing the
                            association surface's blue and its book icon — one vocabulary for
                            a reference everywhere it appears. */}
                        <div className="col-12">
                            <div className="d-flex justify-content-between align-items-center">
                                <span className="form-label mb-0">
                                    {bibleReferencesLabelText}
                                </span>

                                <div
                                    className="btn-group btn-group-sm"
                                    role="group"
                                    aria-label={`${bibleReferencesLabelText} match mode`}>
                                    <button
                                        type="button"
                                        className={`btn mb-0 ${draftBibleReferenceMatchMode === 'any'
                                            ? 'btn-primary'
                                            : 'btn-outline-primary'}`}
                                        aria-pressed={draftBibleReferenceMatchMode === 'any'}
                                        onClick={() => setDraftBibleReferenceMatchMode('any')}>
                                        {tagMatchAnyText}
                                    </button>

                                    <button
                                        type="button"
                                        className={`btn mb-0 ${draftBibleReferenceMatchMode === 'all'
                                            ? 'btn-primary'
                                            : 'btn-outline-primary'}`}
                                        aria-pressed={draftBibleReferenceMatchMode === 'all'}
                                        onClick={() => setDraftBibleReferenceMatchMode('all')}>
                                        {tagMatchAllText}
                                    </button>
                                </div>
                            </div>

                            <div className="mt-2">
                                <TagInput
                                    tags={draftBibleReferences}
                                    onTagsChange={setDraftBibleReferences}
                                    placeholder={bibleReferencePlaceholderText}
                                    ariaLabel={bibleReferencePlaceholderText}
                                    tagCssClass="btn-primary-soft"
                                    tagIconCssClass="bi-book" />
                            </div>
                        </div>

                        {/* The approval statuses, on the surfaces that asked for them. A
                            checkbox GROUP rather than a list, because these narrow by
                            union — a moderator wants the drafts AND the rejected ones in
                            one pass — and nothing ticked is every status, not none. */}
                        {showApprovalStatusSearchOptions && (
                            <div className="col-12">
                                <fieldset>
                                    <legend className="form-label mb-0 fs-6">
                                        {approvalStatusLabelText}
                                    </legend>

                                    <div
                                        className="d-flex flex-wrap gap-3 mt-2"
                                        role="group"
                                        aria-label={approvalStatusLabelText}>

                                        {contentItemSearchApprovalStatusMembers.map(
                                            (approvalStatus) => (
                                                <div
                                                    className="form-check"
                                                    key={approvalStatus}>

                                                    <input
                                                        className="form-check-input"
                                                        type="checkbox"
                                                        id={`${fieldId}-approval-status-${approvalStatus}`}
                                                        checked={draftApprovalStatuses
                                                            .includes(approvalStatus)}
                                                        onChange={() =>
                                                            approvalStatusToggled(approvalStatus)} />

                                                    <label
                                                        className="form-check-label"
                                                        htmlFor={`${fieldId}-approval-status-${approvalStatus}`}>
                                                        {approvalStatusRibbonLabels[approvalStatus]}
                                                    </label>
                                                </div>
                                            ))}
                                    </div>
                                </fieldset>
                            </div>
                        )}
                    </div>
                } />

        </div>
    );
}
