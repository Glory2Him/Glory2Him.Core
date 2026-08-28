import { useDocumentTitle } from '../../useDocumentTitle';
import { CodeSample, ComponentDoc, DocSection, LiveDemo } from '../components/shared/componentDoc';

// Plain Unicode characters — no font, no icon library, no network request. Every modern
// browser and OS ships its own emoji glyphs, so these render everywhere without a dependency.
const emojiCategories: ReadonlyArray<{ title: string; glyphs: ReadonlyArray<[string, string]> }> = [
    {
        title: 'Faces',
        glyphs: [
            ['😀', 'grinning face'], ['😂', 'face with tears of joy'], ['😉', 'winking face'],
            ['😍', 'heart eyes'], ['🤔', 'thinking face'], ['😢', 'crying face'],
            ['😴', 'sleeping face'], ['🙏', 'folded hands'],
        ]
    },
    {
        title: 'Gestures & people',
        glyphs: [
            ['👍', 'thumbs up'], ['👎', 'thumbs down'], ['👋', 'waving hand'],
            ['✍️', 'writing hand'], ['🙌', 'raising hands'], ['👀', 'eyes'],
        ]
    },
    {
        title: 'Hearts & symbols',
        glyphs: [
            ['❤️', 'red heart'], ['✝️', 'latin cross'], ['⭐', 'star'],
            ['✅', 'check mark'], ['❌', 'cross mark'], ['⚠️', 'warning'],
        ]
    },
    {
        title: 'Nature & objects',
        glyphs: [
            ['🕊️', 'dove'], ['📖', 'open book'], ['🔥', 'fire'],
            ['🌅', 'sunrise'], ['🎵', 'musical note'], ['📅', 'calendar'],
        ]
    },
];

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

    return (
        <ComponentDoc
            name="Unicode Emoji"
            filePath="src/pages/samplePages/icons/unicodeEmojiDoc.tsx"
            sectionTitle="Icons"
            summary={
                <>
                    Emoji are ordinary Unicode text characters, not an icon library — no CSS
                    file, font, or package is loaded to use them. Anywhere a string can go, an
                    emoji can go, and it scales with whatever font-size class surrounds it.
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

            {emojiCategories.map((category) => (
                <DocSection key={category.title} title={category.title}>
                    <div className="d-flex flex-wrap gap-3">
                        {category.glyphs.map(([glyph, label]) => (
                            <div
                                key={label}
                                className="text-center border rounded p-2"
                                style={{ width: '5.5rem' }}>
                                <div className="fs-2" aria-hidden="true">{glyph}</div>
                                <div className="small text-body-secondary text-truncate">
                                    {label}
                                </div>
                            </div>
                        ))}
                    </div>
                </DocSection>
            ))}

            <DocSection
                title="Accessibility"
                lead="Emoji read aloud by default — hide decorative ones and name the ones that carry the whole message.">
                <CodeSample code={accessibleSample} />
            </DocSection>
        </ComponentDoc>
    );
};
