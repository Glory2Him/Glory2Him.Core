import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { BibleCard } from '@youversion/platform-react-ui';
import { ReactionBar } from '../components/coreUI/reactionBar';
import { SuggestionPanel } from '../components/coreUI/suggestionPanel';
import { useAuth } from '../components/securitys/authProvider';
import { ReactionOption } from '../models/coreUI/reactionOption';
import * as sampleScripture from '../data/sampleScripture';
import { reactions } from './sampleContent';
import { useYouVersionAvailability } from '../hooks/useYouVersionAvailability';
import { useVersionAbbreviations } from '../hooks/useVersionAbbreviations';
import { bibleReferenceHref } from '../services/views/bibleReferences/toUsfmReference';
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
    const { isAuthenticated } = useAuth();
    const location = useLocation();
    const loginHref = `/Account/Login?returnUrl=${encodeURIComponent(location.pathname)}`;

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
                        <SuggestionPanel
                            heading="Tags"
                            suggestHeading="Suggest a tag"
                            prompt="Think a tag is missing? Suggest one and help others find this passage."
                            placeholder="Start typing a tag…"
                            items={tags}
                            itemCssClass="btn-success-soft"
                            prefixHash={true}
                            hrefFormat="/Search?q={0}"
                            isAuthenticated={isAuthenticated}
                            loginHref={loginHref} />

                        <hr className="my-4" />

                        {/* Each pill reads as a person would say it and addresses as the
                            deep-link route parses it, so a related passage is one click away. */}
                        <SuggestionPanel
                            heading="Related Bible References"
                            suggestHeading="Suggest a bible reference"
                            prompt="Know a matching verse? Suggest it below."
                            placeholder="e.g. Romans 3:23…"
                            items={relatedReferences}
                            itemCssClass="btn-primary-soft"
                            itemIconCssClass="bi-book"
                            hrefFor={bibleReferenceHref}
                            isAuthenticated={isAuthenticated}
                            loginHref={loginHref} />
                    </div>
                </div>
            </div>
        </section>
    );
}
