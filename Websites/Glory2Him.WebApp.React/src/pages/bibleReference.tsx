import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { BibleCard } from '@youversion/platform-react-ui';
import { ReactionBar } from '../components/coreUI/reactionBar';
import { BibleReferenceAssociationPanel } from '../components/associations/bibleReferenceAssociationPanel';
import { TagAssociationPanel } from '../components/associations/tagAssociationPanel';
import { useAuth } from '../components/securitys/authProvider';
import { AssociationItem } from '../models/components/associations/associationItem';
import { ReactionOption } from '../models/coreUI/reactionOption';
import {
    asApprovedAssociations,
    asSuggestedAssociation,
    withoutAssociationValue
} from '../services/views/associations/toAssociationItems';
import * as sampleScripture from '../data/sampleScripture';
import { reactions } from './sampleContent';
import { useYouVersionAvailability } from '../hooks/useYouVersionAvailability';
import { useVersionAbbreviations } from '../hooks/useVersionAbbreviations';
import { youVersionVersions } from '../models/youVersion/youVersionVersions';
import { YouVersionUnavailableMessage } from '../components/youVersion/youVersionAppProvider';

// A single verse (or verse range), laid out like the post detail page: the passage and its
// reactions in the left column, tags and related references in the sidebar beside it.
//
// The verse text comes from the YouVersion Platform SDK's BibleCard (licensed scripture, with
// its own title and version picker). Without props this is the /BibleReferences default
// (John 14:6, NIV); the /BibleReferences/:reference route supplies a parsed bible.com-style
// reference instead (JHN.3.16, JHN.3.16-17, ...).
//
// Every passage gets the same page. Only the editorial material differs: John 14:6 is the one
// curated passage so far, so any other reference shows the same panels with nothing in them
// yet — the suggest boxes still work, and the reaction counts start at zero rather than
// borrowing another passage's. Curation moves to the server when there is a store behind it.
//
// When no app key is configured, the card gives way to an inline "unavailable" message rather
// than crashing.
type BibleReferenceParameters = {
    reference?: string,
    versionId?: number,
    chapterHref?: string
}

export function BibleReference({
    reference = 'JHN.14.6',
    versionId = youVersionVersions.niv,
    chapterHref = '/BibleReferences/JHN.14.NIV',
}: BibleReferenceParameters) {
    const { isLoading, isAvailable } = useYouVersionAvailability();
    const { abbreviationFor } = useVersionAbbreviations();
    const navigate = useNavigate();
    const [reactedTo, setReactedTo] = useState<string | null>(null);
    const { user } = useAuth();

    // The panels own no state, so anything suggested here lives on this page and only until it
    // is navigated away from, exactly as it did under SuggestionPanel. Wiring these to the
    // Association API is a separate job.
    const [suggestedTags, setSuggestedTags] = useState<ReadonlyArray<AssociationItem>>([]);

    const [suggestedReferences, setSuggestedReferences] =
        useState<ReadonlyArray<AssociationItem>>([]);

    const asSuggestion = (value: string) => asSuggestedAssociation(value, user?.userId);

    // John 14:6 is the one curated passage so far. Any other reference gets the same page
    // with empty panels rather than another passage's tags, references and reaction counts.
    const isCuratedReference = reference === 'JHN.14.6';

    const tags = isCuratedReference ? sampleScripture.tags : [];

    const relatedReferences = isCuratedReference
        ? sampleScripture.relatedReferences
        : [];

    const passageReactions = isCuratedReference
        ? reactions
        : reactions.map((reaction) => ({ ...reaction, count: 0 }));

    useEffect(() => {
        document.title = isCuratedReference
            ? `${sampleScripture.reference} — Glory 2 Him`
            : `${reference} — Glory 2 Him`;
    }, [reference, isCuratedReference]);

    const onReact = (reaction: ReactionOption) =>
        setReactedTo(reaction.label);

    // The URL owns the passage and its version, so switching translation is a navigation:
    // refresh keeps the translation, and the address bar is always shareable.
    const onVersionChange = (newVersionId: number) => {
        const abbreviation = abbreviationFor(newVersionId);
        const versionSuffix = abbreviation != null ? `.${abbreviation}` : '';

        navigate(`/BibleReferences/${reference}${versionSuffix}`);
    };

    const passage = !isLoading && (
        isAvailable
            ? <BibleCard
                reference={reference}
                versionId={versionId}
                onVersionChange={onVersionChange}
                showVersionPicker />
            : <YouVersionUnavailableMessage />
    );

    const fullChapterLink = (
        <div className="text-center my-4">
            <Link to={chapterHref} className="btn-link">
                Show Full Chapter
            </Link>
        </div>
    );

    return (
        <section className="py-5">
            <div className="container position-relative">
                <div className="row">
                    <div className="col-lg-7 mb-5">
                        {passage}
                        {fullChapterLink}

                        <ReactionBar
                            prompt="How did this passage speak to you?"
                            reactions={passageReactions}
                            onReact={onReact} />

                        {reactedTo != null && (
                            <p className="text-center small text-body-secondary mt-2 mb-0">
                                You reacted with <strong>{reactedTo}</strong>.
                            </p>
                        )}
                    </div>

                    <div className="col-lg-5">
                        {/* A passage rather than a post, so only the prompt is overridden — the
                            rest is the component's own. */}
                        <TagAssociationPanel
                            suggestDescription="Think a tag is missing? Suggest one and help others find this passage."
                            associationCollection={[
                                ...asApprovedAssociations(tags),
                                ...suggestedTags
                            ]}
                            onAdd={(value) =>
                                setSuggestedTags([...suggestedTags, asSuggestion(value)])}
                            onRemove={(item) =>
                                setSuggestedTags(withoutAssociationValue(suggestedTags, item))} />

                        <hr className="my-4" />

                        {/* Each pill reads as a person would say it and addresses as the
                            deep-link route parses it, so a related passage is one click away. */}
                        <BibleReferenceAssociationPanel
                            title="Related Bible References"
                            associationCollection={[
                                ...asApprovedAssociations(relatedReferences),
                                ...suggestedReferences
                            ]}
                            onAdd={(value) =>
                                setSuggestedReferences([
                                    ...suggestedReferences,
                                    asSuggestion(value)
                                ])}
                            onRemove={(item) =>
                                setSuggestedReferences(
                                    withoutAssociationValue(suggestedReferences, item))} />
                    </div>
                </div>
            </div>
        </section>
    );
}
