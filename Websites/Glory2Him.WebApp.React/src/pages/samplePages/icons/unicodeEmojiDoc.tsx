import { useMemo, useState } from 'react';
import emojiGroups from 'unicode-emoji-json/data-by-group.json';
import { useDocumentTitle } from '../../useDocumentTitle';
import { CodeSample, ComponentDoc, DocSection, LiveDemo } from '../components/shared/componentDoc';

// unicode-emoji-json ships the full Unicode emoji catalogue (~1,900 entries, no skin-tone or
// flag-country variants) as a bundled JSON file — data from unicode.org, no network request,
// no icon library. Emoji are plain text characters, so the whole catalogue is just as
// enumerable and searchable as the Bootstrap Icons manifest the Icons page reads.
//
// The whole catalogue renders at once, under its unicode.org group headings: you cannot search
// for an emoji you do not already know the name of, so browsing has to work without a search
// term. ~1,900 spans of text is a cheap grid — no virtualisation needed.
const catalogue = emojiGroups.map((group) => ({
    name: group.name,
    emojis: group.emojis.map((entry) => ({ glyph: entry.emoji, name: entry.name }))
}));

const totalEmojiCount = catalogue.reduce((total, group) => total + group.emojis.length, 0);

const sizeSample = `
// Emoji are just text, so any font-size utility scales them — no separate asset per size.
<span className="fs-6">🙏</span>   {/* ~1rem   — inline with body copy */}
<span className="fs-3">🙏</span>   {/* ~1.75rem — a card heading */}
<span className="fs-1">🙏</span>   {/* ~2.5rem  — a section lead-in */}
<span className="display-4">🙏</span> {/* ~3.5rem — a hero or empty-state graphic */}
`;

const accessibleSample = `
// A decorative emoji should not be read aloud twice (once as the glyph, once as any
// surrounding label). Mark it aria-hidden and give the real text its own accessible name.
<span aria-hidden="true">🙏</span> Prayer requests

// When the emoji IS the entire message, give it an accessible name explicitly.
<span role="img" aria-label="celebrating">🎉</span>
`;

export const UnicodeEmojiDoc = () => {
    useDocumentTitle('Unicode Emoji — Glory 2 Him');

    const [search, setSearch] = useState('');

    // Groups keep their headings while filtering — a search for "heart" should still say which
    // of them are smileys and which are symbols. Groups with no match drop out entirely.
    const filteredGroups = useMemo(() => {
        const term = search.trim().toLowerCase();

        if (term === '') {
            return catalogue;
        }

        return catalogue
            .map((group) => ({
                name: group.name,
                emojis: group.emojis.filter((entry) =>
                    entry.name.includes(term) || group.name.toLowerCase().includes(term))
            }))
            .filter((group) => group.emojis.length > 0);
    }, [search]);

    const matchCount = filteredGroups.reduce((total, group) => total + group.emojis.length, 0);

    return (
        <ComponentDoc
            name="Unicode Emoji"
            filePath="src/pages/samplePages/icons/unicodeEmojiDoc.tsx"
            sectionTitle="Icons"
            summary={
                <>
                    Emoji are ordinary Unicode text characters, not an icon library — no CSS
                    file, font, or package is loaded to render them. Anywhere a string can go,
                    an emoji can go, and it scales with whatever font-size class surrounds it.
                </>
            }>

            <DocSection
                title="Sizes"
                lead="The same glyph at increasing Bootstrap font-size utilities.">
                <LiveDemo>
                    <div className="d-flex align-items-end gap-4 flex-wrap">
                        <span className="fs-6" title="fs-6">🙏</span>
                        <span className="fs-5" title="fs-5">🙏</span>
                        <span className="fs-4" title="fs-4">🙏</span>
                        <span className="fs-3" title="fs-3">🙏</span>
                        <span className="fs-2" title="fs-2">🙏</span>
                        <span className="fs-1" title="fs-1">🙏</span>
                        <span className="display-4" title="display-4">🙏</span>
                    </div>
                </LiveDemo>
                <CodeSample code={sizeSample} />
            </DocSection>

            <DocSection
                title="Catalogue"
                lead={`Every one of the ${totalEmojiCount} emoji from unicode.org, under its own group heading. Search narrows the grid by emoji name or group name.`}>
                <LiveDemo>
                    <input
                        type="search"
                        className="form-control mb-3"
                        placeholder="Search emoji names, e.g. &quot;heart&quot;"
                        value={search}
                        onChange={(event) => setSearch(event.target.value)} />

                    <p className="small text-body-secondary">
                        Showing {matchCount} of {totalEmojiCount} emoji.
                    </p>

                    {filteredGroups.map((group) => (
                        <section key={group.name} className="mb-4">
                            <h3 className="h6 text-body-secondary border-bottom pb-2 mb-3">
                                {group.name}{' '}
                                <span className="fw-normal">({group.emojis.length})</span>
                            </h3>

                            <div className="d-flex flex-wrap gap-3">
                                {group.emojis.map((entry) => (
                                    <div
                                        key={entry.glyph}
                                        className="text-center border rounded p-2"
                                        style={{ width: '6.5rem' }}
                                        title={entry.name}>
                                        <div className="fs-2" aria-hidden="true">{entry.glyph}</div>
                                        <div className="small text-body-secondary text-truncate">
                                            {entry.name}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </section>
                    ))}

                    {matchCount === 0 && (
                        <p className="text-body-secondary small mb-0">No matches.</p>
                    )}
                </LiveDemo>
            </DocSection>

            <DocSection
                title="Accessibility"
                lead="Emoji read aloud by default — hide decorative ones and name the ones that carry the whole message.">
                <CodeSample code={accessibleSample} />
            </DocSection>
        </ComponentDoc>
    );
};
