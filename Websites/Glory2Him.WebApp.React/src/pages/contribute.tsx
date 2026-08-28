import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Spinner } from '../components/coreUI/spinner';
import { TagInput } from '../components/coreUI/tagInput';
import { contentItemSettingService } from '../services/foundations/contentItemSettingService';
import { useDocumentTitle } from './useDocumentTitle';

// Mirrors Glory2Him.Core.Models.Enums.ShareabilityBasis. Kept local rather than a shared
// foundation model until the submission flow actually posts a ContentItem — at that point this
// needs to agree with the wire's numeric enum value (no JsonStringEnumConverter on the host).
type ShareabilityBasis = 'Owned' | 'PermissionGranted' | 'PublicDomain';

const shareabilityOptions: ReadonlyArray<{ value: ShareabilityBasis; label: string }> = [
    { value: 'Owned', label: "It's my own" },
    { value: 'PermissionGranted', label: 'I have permission from the owner to share it' },
    { value: 'PublicDomain', label: "It's public domain" },
];

// The contribution submission form. "What are you sharing?" is driven by the per-content-type
// ContentItemSetting defaults that are open to general users (IsAvailableAsGeneralUserContribution)
// — one API call on mount, then every type switch below works off that same in-memory list.
// Submit itself is still a no-op: the fields react to the chosen type's settings, but there is no
// submission flow to send them to yet.
export function Contribute() {
    useDocumentTitle('Share what He has done — Glory 2 Him');

    const { data: contentTypeSettings, isLoading, isError } =
        contentItemSettingService.useGetAvailableForContribution();

    const [selectedSettingId, setSelectedSettingId] = useState<string | null>(null);
    const [tags, setTags] = useState<ReadonlyArray<string>>([]);
    const [bibleReferences, setBibleReferences] = useState<ReadonlyArray<string>>([]);
    const [shareabilityBasis, setShareabilityBasis] = useState<ShareabilityBasis>('Owned');
    const [sharePermission, setSharePermission] = useState('');

    // Nothing is selected until the settings arrive, so the first render with data defaults to
    // its first entry — a plain useState initializer can't reach data that isn't fetched yet.
    useEffect(() => {
        if (contentTypeSettings != null && contentTypeSettings.length > 0 && selectedSettingId == null) {
            setSelectedSettingId(contentTypeSettings[0].id);
        }
    }, [contentTypeSettings, selectedSettingId]);

    const selectedType =
        contentTypeSettings?.find((setting) => setting.id === selectedSettingId)
            ?? contentTypeSettings?.[0]
            ?? null;

    const showTagsColumn = selectedType?.tagsAllowed === true;
    const showBibleReferencesColumn = selectedType?.bibleReferenceAllowed === true;
    const associationColumnCssClass = showTagsColumn && showBibleReferencesColumn ? 'col-md-6' : 'col-12';

    return (
        <section className="pt-4 pb-5">
            <div className="container">
                <div className="row justify-content-center">
                    <div className="col-xl-9">
                        <div className="text-center mb-4">
                            <i
                                className="bi bi-pencil-square text-primary display-5"
                                aria-hidden="true"></i>

                            <h1 className="mt-2 mb-2">Share what He has done</h1>
                            <p className="lead mb-0">
                                A story, a testimony, or a verse that carried you through — if it
                                might encourage someone else, we would love to read it. Submissions
                                are reviewed before publishing.
                            </p>
                        </div>

                        {isLoading ? (
                            <div className="text-center py-5"><Spinner /></div>
                        ) : isError ? (
                            <div className="alert alert-danger" role="alert">
                                We could not load the contribution types right now. Please try again later.
                            </div>
                        ) : contentTypeSettings == null || contentTypeSettings.length === 0 || selectedType == null ? (
                            <div className="alert alert-info" role="alert">
                                Contributions are not open for any content type right now.
                            </div>
                        ) : (
                            // Not a <form>: TagInput commits a pill on Enter, and inside a form
                            // with a submit button that Enter would submit the page instead.
                            <div className="card card-body border p-4 p-lg-5">
                                <fieldset className="mb-4">
                                    <legend className="form-label fw-bold fs-6">
                                        What are you sharing?
                                    </legend>

                                    <div className="row g-3 row-cols-2 row-cols-md-3 row-cols-lg-5">
                                        {contentTypeSettings.map((setting) => {
                                            const isSelected = setting.id === selectedType.id;

                                            const selectionCssClass = isSelected
                                                ? 'border-primary bg-primary bg-opacity-10'
                                                : '';

                                            return (
                                                <div key={setting.id} className="col">
                                                    <button
                                                        type="button"
                                                        className={`card h-100 w-100 text-center border p-3 ${selectionCssClass}`}
                                                        aria-pressed={isSelected}
                                                        onClick={() => setSelectedSettingId(setting.id)}>
                                                        <i
                                                            className={`bi ${setting.contentTypeIconCssClass} text-primary fs-4 mx-auto`}
                                                            aria-hidden="true"></i>

                                                        <span className="fw-bold d-block mt-1">
                                                            {setting.contentTypeName}
                                                        </span>

                                                        <small className="text-muted d-block">
                                                            {setting.contentTypeDescription}
                                                        </small>
                                                    </button>
                                                </div>
                                            );
                                        })}
                                    </div>
                                </fieldset>

                                {selectedType.hasTitle && (
                                    <div className="mb-3">
                                        <label className="form-label" htmlFor="contribute-title">
                                            Title <span className="text-danger">*</span>
                                        </label>

                                        <input
                                            type="text"
                                            className="form-control"
                                            id="contribute-title"
                                            placeholder={`e.g. ${selectedType.contentTypeName} title`} />
                                    </div>
                                )}

                                {selectedType.hasAuthor && (
                                    <div className="mb-3">
                                        <label className="form-label" htmlFor="contribute-author">
                                            Author
                                        </label>

                                        <input
                                            type="text"
                                            className="form-control"
                                            id="contribute-author"
                                            placeholder="e.g. Dwight L. Moody — leave blank if it's your own" />
                                    </div>
                                )}

                                <div className="mb-4">
                                    <label className="form-label" htmlFor="contribute-content">
                                        {selectedType.contentTypeName} <span className="text-danger">*</span>
                                    </label>

                                    <textarea
                                        className="form-control"
                                        id="contribute-content"
                                        rows={7}
                                        placeholder={`Share your ${selectedType.contentTypeName.toLowerCase()}…`}></textarea>
                                </div>

                                <div className="mb-3">
                                    <label className="form-label" htmlFor="contribute-shareability-basis">
                                        How are you permitted to share this? <span className="text-danger">*</span>
                                    </label>

                                    <select
                                        className="form-select"
                                        id="contribute-shareability-basis"
                                        value={shareabilityBasis}
                                        onChange={(event) =>
                                            setShareabilityBasis(event.target.value as ShareabilityBasis)}>
                                        {shareabilityOptions.map((option) => (
                                            <option key={option.value} value={option.value}>
                                                {option.label}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                {shareabilityBasis === 'PermissionGranted' && (
                                    <div className="mb-4">
                                        <label className="form-label" htmlFor="contribute-share-permission">
                                            Permission details
                                        </label>

                                        <input
                                            type="text"
                                            className="form-control"
                                            id="contribute-share-permission"
                                            maxLength={500}
                                            value={sharePermission}
                                            onChange={(event) => setSharePermission(event.target.value)}
                                            placeholder="e.g. Permission granted by the author by email, 12 Jan 2026" />
                                    </div>
                                )}

                                {(showTagsColumn || showBibleReferencesColumn) && (
                                    <div className="row g-4 mb-4">
                                        {showTagsColumn && (
                                            <div className={associationColumnCssClass}>
                                                <span className="form-label d-block">Tags</span>

                                                <TagInput
                                                    tags={tags}
                                                    onTagsChange={setTags}
                                                    placeholder="Start typing a tag…"
                                                    ariaLabel="Add a tag"
                                                    tagPrefix="#" />
                                            </div>
                                        )}

                                        {showBibleReferencesColumn && (
                                            <div className={associationColumnCssClass}>
                                                <span className="form-label d-block">Bible references</span>

                                                <TagInput
                                                    tags={bibleReferences}
                                                    onTagsChange={setBibleReferences}
                                                    placeholder="e.g. Romans 3:23…"
                                                    ariaLabel="Add a bible reference"
                                                    tagCssClass="btn-primary-soft"
                                                    tagIconCssClass="bi-book" />
                                            </div>
                                        )}
                                    </div>
                                )}

                                <div className="d-flex align-items-center gap-3">
                                    <button type="button" className="btn btn-primary mb-0">
                                        Submit for review
                                    </button>

                                    <Link to="/" className="btn btn-link text-body p-0 mb-0">
                                        Cancel
                                    </Link>
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </section>
    );
}
