import { useMemo, useState } from 'react';
import emojiGroups from 'unicode-emoji-json/data-by-group.json';
import { useDocumentTitle } from '../../useDocumentTitle';
import { CodeSample, ComponentDoc, DocSection, LiveDemo } from '../components/shared/componentDoc';

// unicode-emoji-json ships the full Unicode emoji catalogue (~1,900 entries, no skin-tone or
// flag-country variants) as a bundled JSON file — data from unicode.org, no network request,
// no icon library. Emoji are plain text characters, so the whole catalogue is just as
// enumerable and searchable as the Bootstrap Icons manifest the Icons page reads.
const allEmoji = emojiGroups.flatMap((group) =>
    group.emojis.map((entry) => ({ glyph: entry.emoji, name: entry.name, group: group.name })));

const MAX_UNFILTERED_RESULTS = 150;

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

    const filtered = useMemo(() => {
        const term = search.trim().toLowerCase();
        return term === '' ? allEmoji : allEmoji.filter((entry) => entry.name.includes(term));
    }, [search]);

    const visible = search.trim() === '' ? filtered.slice(0, MAX_UNFILTERED_RESULTS) : filtered;

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
                lead={`The full set — ${allEmoji.length} emoji from unicode.org. Search narrows the grid; without a search term only the first ${MAX_UNFILTERED_RESULTS} are shown.`}>
                <LiveDemo>
                    <input
                        type="search"
                        className="form-control mb-3"
                        placeholder="Search emoji names, e.g. &quot;heart&quot;"
                        value={search}
                        onChange={(event) => setSearch(event.target.value)} />

                    <div className="d-flex flex-wrap gap-3">
                        {visible.map((entry) => (
                            <div
                                key={entry.glyph}
                                className="text-center border rounded p-2"
                                style={{ width: '6.5rem' }}
                                title={entry.group}>
                                <div className="fs-2" aria-hidden="true">{entry.glyph}</div>
                                <div className="small text-body-secondary text-truncate">
                                    {entry.name}
                                </div>
                            </div>
                        ))}
                    </div>

                    {filtered.length > visible.length && (
                        <p className="small text-body-secondary mt-2 mb-0">
                            {filtered.length - visible.length} more — refine the search to see
                            them.
                        </p>
                    )}

                    {filtered.length === 0 && (
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
