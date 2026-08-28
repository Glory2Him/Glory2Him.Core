import { useMemo, useState } from 'react';
import { useDocumentTitle } from '../../useDocumentTitle';
import { CodeSample, ComponentDoc, DocSection, LiveDemo } from '../components/shared/componentDoc';

// Plain Unicode characters — no font, no icon library, no network request. Every modern
// browser and OS ships its own emoji glyphs, so these render everywhere without a dependency.
//
// This is a broad hand-picked selection (~350), grouped the way the Unicode emoji standard
// itself groups them — not the full ~3,800-glyph catalogue, since that has no equivalent to
// the Bootstrap Icons manifest the Icons page can fetch and enumerate live. Search narrows it.
const emojiCategories: ReadonlyArray<{ title: string; glyphs: ReadonlyArray<[string, string]> }> = [
    {
        title: 'Smileys & emotion',
        glyphs: [
            ['😀', 'grinning face'], ['😃', 'grinning face with big eyes'],
            ['😄', 'grinning face with smiling eyes'], ['😁', 'beaming face'],
            ['😆', 'grinning squinting face'], ['😅', 'grinning face with sweat'],
            ['🤣', 'rolling on the floor laughing'], ['😂', 'face with tears of joy'],
            ['🙂', 'slightly smiling face'], ['🙃', 'upside-down face'],
            ['😉', 'winking face'], ['😊', 'smiling face with smiling eyes'],
            ['😇', 'smiling face with halo'], ['🥰', 'smiling face with hearts'],
            ['😍', 'heart eyes'], ['🤩', 'star-struck'], ['😘', 'face blowing a kiss'],
            ['😗', 'kissing face'], ['☺️', 'smiling face'], ['😚', 'kissing face with closed eyes'],
            ['😋', 'face savoring food'], ['😛', 'face with tongue'],
            ['😜', 'winking face with tongue'], ['🤪', 'zany face'],
            ['😝', 'squinting face with tongue'], ['🤑', 'money-mouth face'],
            ['🤗', 'hugging face'], ['🤭', 'face with hand over mouth'],
            ['🤫', 'shushing face'], ['🤔', 'thinking face'], ['🤐', 'zipper-mouth face'],
            ['🤨', 'face with raised eyebrow'], ['😐', 'neutral face'],
            ['😑', 'expressionless face'], ['😶', 'face without mouth'],
            ['😏', 'smirking face'], ['😒', 'unamused face'], ['🙄', 'face with rolling eyes'],
            ['😬', 'grimacing face'], ['🤥', 'lying face'], ['😌', 'relieved face'],
            ['😔', 'pensive face'], ['😪', 'sleepy face'], ['🤤', 'drooling face'],
            ['😴', 'sleeping face'], ['😷', 'face with medical mask'], ['🤒', 'face with thermometer'],
            ['🤕', 'face with head-bandage'], ['🤢', 'nauseated face'], ['🤮', 'vomiting face'],
            ['🥵', 'hot face'], ['🥶', 'cold face'], ['😵', 'dizzy face'], ['🤯', 'exploding head'],
            ['🤠', 'cowboy hat face'], ['🥳', 'partying face'], ['😎', 'smiling face with sunglasses'],
            ['🤓', 'nerd face'], ['🧐', 'face with monocle'], ['😕', 'confused face'],
            ['😟', 'worried face'], ['🙁', 'slightly frowning face'], ['☹️', 'frowning face'],
            ['😮', 'face with open mouth'], ['😯', 'hushed face'], ['😲', 'astonished face'],
            ['😳', 'flushed face'], ['🥺', 'pleading face'], ['😦', 'frowning face with open mouth'],
            ['😧', 'anguished face'], ['😨', 'fearful face'], ['😰', 'anxious face with sweat'],
            ['😥', 'sad but relieved face'], ['😢', 'crying face'], ['😭', 'loudly crying face'],
            ['😱', 'face screaming in fear'], ['😖', 'confounded face'], ['😣', 'persevering face'],
            ['😞', 'disappointed face'], ['😓', 'downcast face with sweat'], ['😩', 'weary face'],
            ['😫', 'tired face'], ['😤', 'face with steam from nose'], ['😡', 'pouting face'],
            ['😠', 'angry face'], ['🤬', 'face with symbols on mouth'], ['😈', 'smiling face with horns'],
            ['👿', 'angry face with horns'], ['💀', 'skull'], ['💩', 'pile of poo'],
            ['🤡', 'clown face'], ['👻', 'ghost'], ['👽', 'alien'], ['🤖', 'robot'],
        ]
    },
    {
        title: 'People & body',
        glyphs: [
            ['👋', 'waving hand'], ['🤚', 'raised back of hand'], ['🖐️', 'hand with fingers splayed'],
            ['✋', 'raised hand'], ['🖖', 'vulcan salute'], ['👌', 'OK hand'],
            ['🤏', 'pinching hand'], ['✌️', 'victory hand'], ['🤞', 'crossed fingers'],
            ['🤟', 'love-you gesture'], ['🤘', 'sign of the horns'], ['🤙', 'call me hand'],
            ['👈', 'backhand index pointing left'], ['👉', 'backhand index pointing right'],
            ['👆', 'backhand index pointing up'], ['👇', 'backhand index pointing down'],
            ['☝️', 'index pointing up'], ['👍', 'thumbs up'], ['👎', 'thumbs down'],
            ['✊', 'raised fist'], ['👊', 'oncoming fist'], ['🤛', 'left-facing fist'],
            ['🤜', 'right-facing fist'], ['👏', 'clapping hands'], ['🙌', 'raising hands'],
            ['👐', 'open hands'], ['🤲', 'palms up together'], ['🤝', 'handshake'],
            ['🙏', 'folded hands'], ['✍️', 'writing hand'], ['💅', 'nail polish'],
            ['🤳', 'selfie'], ['💪', 'flexed biceps'], ['🦾', 'mechanical arm'],
            ['🦵', 'leg'], ['🦶', 'foot'], ['👂', 'ear'], ['👃', 'nose'], ['🧠', 'brain'],
            ['🦷', 'tooth'], ['👀', 'eyes'], ['👁️', 'eye'], ['👅', 'tongue'], ['👄', 'mouth'],
            ['👶', 'baby'], ['🧒', 'child'], ['👦', 'boy'], ['👧', 'girl'], ['🧑', 'person'],
            ['👨', 'man'], ['👩', 'woman'], ['🧓', 'older person'], ['👴', 'old man'],
            ['👵', 'old woman'], ['🙋', 'person raising hand'], ['🙇', 'person bowing'],
            ['🤦', 'person facepalming'], ['🤷', 'person shrugging'], ['🧑‍💻', 'technologist'],
            ['🧑‍🏫', 'teacher'], ['🧑‍⚕️', 'health worker'], ['🧑‍🌾', 'farmer'],
            ['🧑‍🎨', 'artist'], ['👨‍👩‍👧‍👦', 'family'], ['💑', 'couple with heart'],
        ]
    },
    {
        title: 'Animals & nature',
        glyphs: [
            ['🐶', 'dog face'], ['🐱', 'cat face'], ['🐭', 'mouse face'], ['🐹', 'hamster'],
            ['🐰', 'rabbit face'], ['🦊', 'fox'], ['🐻', 'bear'], ['🐼', 'panda'],
            ['🐨', 'koala'], ['🐯', 'tiger face'], ['🦁', 'lion'], ['🐮', 'cow face'],
            ['🐷', 'pig face'], ['🐸', 'frog'], ['🐵', 'monkey face'], ['🐔', 'chicken'],
            ['🐧', 'penguin'], ['🐦', 'bird'], ['🕊️', 'dove'], ['🦅', 'eagle'], ['🦉', 'owl'],
            ['🦇', 'bat'], ['🐺', 'wolf'], ['🐴', 'horse face'], ['🦄', 'unicorn'],
            ['🐝', 'honeybee'], ['🦋', 'butterfly'], ['🐌', 'snail'], ['🐞', 'lady beetle'],
            ['🐢', 'turtle'], ['🐍', 'snake'], ['🦎', 'lizard'], ['🐙', 'octopus'],
            ['🐬', 'dolphin'], ['🐳', 'whale'], ['🐟', 'fish'], ['🦈', 'shark'],
            ['🐊', 'crocodile'], ['🐅', 'tiger'], ['🦓', 'zebra'], ['🦒', 'giraffe'],
            ['🐘', 'elephant'], ['🦏', 'rhinoceros'], ['🐫', 'camel'], ['🐄', 'cow'],
            ['🐑', 'ewe'], ['🐐', 'goat'], ['🐓', 'rooster'], ['🦃', 'turkey'],
            ['🕷️', 'spider'], ['🦂', 'scorpion'], ['🐚', 'spiral shell'],
            ['🌸', 'cherry blossom'], ['💐', 'bouquet'], ['🌷', 'tulip'], ['🌹', 'rose'],
            ['🌻', 'sunflower'], ['🌼', 'blossom'], ['🌱', 'seedling'], ['🌲', 'evergreen tree'],
            ['🌳', 'deciduous tree'], ['🌴', 'palm tree'], ['🌵', 'cactus'], ['🍀', 'four leaf clover'],
            ['🍁', 'maple leaf'], ['🍂', 'fallen leaf'], ['🍃', 'leaf fluttering in wind'],
            ['🌅', 'sunrise'], ['🌄', 'sunrise over mountains'], ['🌈', 'rainbow'],
            ['☀️', 'sun'], ['⭐', 'star'], ['🌙', 'crescent moon'], ['⚡', 'high voltage'],
            ['🔥', 'fire'], ['💧', 'droplet'], ['🌊', 'water wave'], ['☁️', 'cloud'],
        ]
    },
    {
        title: 'Food & drink',
        glyphs: [
            ['🍏', 'green apple'], ['🍎', 'red apple'], ['🍐', 'pear'], ['🍊', 'tangerine'],
            ['🍋', 'lemon'], ['🍌', 'banana'], ['🍉', 'watermelon'], ['🍇', 'grapes'],
            ['🍓', 'strawberry'], ['🫐', 'blueberries'], ['🍈', 'melon'], ['🍒', 'cherries'],
            ['🍑', 'peach'], ['🥭', 'mango'], ['🍍', 'pineapple'], ['🥥', 'coconut'],
            ['🥝', 'kiwi fruit'], ['🍅', 'tomato'], ['🥑', 'avocado'], ['🥦', 'broccoli'],
            ['🥕', 'carrot'], ['🌽', 'ear of corn'], ['🌶️', 'hot pepper'], ['🥔', 'potato'],
            ['🍞', 'bread'], ['🥐', 'croissant'], ['🥯', 'bagel'], ['🧀', 'cheese wedge'],
            ['🥚', 'egg'], ['🍳', 'cooking'], ['🥞', 'pancakes'], ['🥓', 'bacon'],
            ['🍔', 'hamburger'], ['🍟', 'french fries'], ['🍕', 'pizza'], ['🌭', 'hot dog'],
            ['🥪', 'sandwich'], ['🌮', 'taco'], ['🌯', 'burrito'], ['🥗', 'green salad'],
            ['🍝', 'spaghetti'], ['🍜', 'steaming bowl'], ['🍲', 'pot of food'], ['🍣', 'sushi'],
            ['🍱', 'bento box'], ['🍤', 'fried shrimp'], ['🍰', 'shortcake'], ['🎂', 'birthday cake'],
            ['🍦', 'soft ice cream'], ['🍩', 'doughnut'], ['🍪', 'cookie'], ['🍫', 'chocolate bar'],
            ['🍬', 'candy'], ['🍭', 'lollipop'], ['☕', 'hot beverage'], ['🍵', 'teacup without handle'],
            ['🧃', 'beverage box'], ['🥤', 'cup with straw'], ['🍺', 'beer mug'], ['🍷', 'wine glass'],
            ['🍽️', 'fork and knife with plate'], ['🍴', 'fork and knife'],
        ]
    },
    {
        title: 'Travel & places',
        glyphs: [
            ['🚗', 'automobile'], ['🚕', 'taxi'], ['🚌', 'bus'], ['🚓', 'police car'],
            ['🚑', 'ambulance'], ['🚒', 'fire engine'], ['🚚', 'delivery truck'],
            ['🚲', 'bicycle'], ['🛵', 'motor scooter'], ['🏍️', 'motorcycle'],
            ['✈️', 'airplane'], ['🚀', 'rocket'], ['🚁', 'helicopter'], ['⛵', 'sailboat'],
            ['🚢', 'ship'], ['🚆', 'train'], ['🚇', 'metro'], ['🚏', 'bus stop'],
            ['⛽', 'fuel pump'], ['🚦', 'vertical traffic light'], ['🗺️', 'world map'],
            ['🗽', 'Statue of Liberty'], ['🗼', 'tower'], ['🏰', 'castle'], ['⛪', 'church'],
            ['🕌', 'mosque'], ['🕍', 'synagogue'], ['⛩️', 'shinto shrine'], ['🕋', 'kaaba'],
            ['🏠', 'house'], ['🏡', 'house with garden'], ['🏢', 'office building'],
            ['🏥', 'hospital'], ['🏫', 'school'], ['🏨', 'hotel'], ['⛰️', 'mountain'],
            ['🏔️', 'snow-capped mountain'], ['🌋', 'volcano'], ['🏖️', 'beach with umbrella'],
            ['🏜️', 'desert'], ['🏕️', 'camping'], ['🌉', 'bridge at night'],
        ]
    },
    {
        title: 'Activities',
        glyphs: [
            ['⚽', 'soccer ball'], ['🏀', 'basketball'], ['🏈', 'american football'],
            ['⚾', 'baseball'], ['🎾', 'tennis'], ['🏐', 'volleyball'], ['🏉', 'rugby football'],
            ['🎱', 'pool 8 ball'], ['🏓', 'ping pong'], ['🏸', 'badminton'], ['🥊', 'boxing glove'],
            ['🥋', 'martial arts uniform'], ['⛳', 'flag in hole'], ['⛸️', 'ice skate'],
            ['🎣', 'fishing pole'], ['🎽', 'running shirt'], ['🎿', 'skis'], ['🛹', 'skateboard'],
            ['🏆', 'trophy'], ['🥇', 'gold medal'], ['🎮', 'video game'], ['🎲', 'game die'],
            ['🧩', 'puzzle piece'], ['🎯', 'direct hit'], ['🎳', 'bowling'], ['🎨', 'artist palette'],
            ['🎭', 'performing arts'], ['🎬', 'clapper board'], ['🎤', 'microphone'],
            ['🎧', 'headphone'], ['🎼', 'musical score'], ['🎹', 'musical keyboard'],
            ['🥁', 'drum'], ['🎸', 'guitar'], ['🎺', 'trumpet'], ['🎻', 'violin'],
            ['🎵', 'musical note'], ['🎶', 'musical notes'], ['🎊', 'confetti ball'],
            ['🎉', 'party popper'], ['🎁', 'wrapped gift'], ['🎈', 'balloon'],
        ]
    },
    {
        title: 'Objects',
        glyphs: [
            ['⌚', 'watch'], ['📱', 'mobile phone'], ['💻', 'laptop'], ['🖥️', 'desktop computer'],
            ['🖨️', 'printer'], ['⌨️', 'keyboard'], ['🖱️', 'computer mouse'], ['💾', 'floppy disk'],
            ['📷', 'camera'], ['🔦', 'flashlight'], ['💡', 'light bulb'], ['🔋', 'battery'],
            ['🔌', 'electric plug'], ['📖', 'open book'], ['📚', 'books'], ['📝', 'memo'],
            ['✏️', 'pencil'], ['🖊️', 'pen'], ['📌', 'pushpin'], ['📎', 'paperclip'],
            ['📏', 'straight ruler'], ['🔍', 'magnifying glass tilted left'], ['🔒', 'locked'],
            ['🔑', 'key'], ['🔨', 'hammer'], ['🧰', 'toolbox'], ['⚙️', 'gear'], ['🧲', 'magnet'],
            ['💊', 'pill'], ['🩹', 'adhesive bandage'], ['🧭', 'compass'], ['📅', 'calendar'],
            ['📆', 'tear-off calendar'], ['⏰', 'alarm clock'], ['⏳', 'hourglass'],
            ['🔔', 'bell'], ['📢', 'loudspeaker'], ['💰', 'money bag'], ['💳', 'credit card'],
            ['✉️', 'envelope'], ['📦', 'package'], ['🗑️', 'wastebasket'],
        ]
    },
    {
        title: 'Symbols',
        glyphs: [
            ['❤️', 'red heart'], ['🧡', 'orange heart'], ['💛', 'yellow heart'],
            ['💚', 'green heart'], ['💙', 'blue heart'], ['💜', 'purple heart'],
            ['🤎', 'brown heart'], ['🖤', 'black heart'], ['🤍', 'white heart'],
            ['💔', 'broken heart'], ['❣️', 'heart exclamation'], ['💕', 'two hearts'],
            ['✝️', 'latin cross'], ['☦️', 'orthodox cross'], ['✡️', 'star of David'],
            ['☪️', 'star and crescent'], ['🕉️', 'om'], ['☸️', 'wheel of dharma'],
            ['✅', 'check mark button'], ['✔️', 'check mark'], ['❌', 'cross mark'],
            ['❎', 'cross mark button'], ['➕', 'plus'], ['➖', 'minus'], ['➗', 'divide'],
            ['✖️', 'multiply'], ['♾️', 'infinity'], ['❗', 'exclamation mark'],
            ['❓', 'question mark'], ['⚠️', 'warning'], ['🚫', 'prohibited'],
            ['♻️', 'recycling symbol'], ['🔞', 'no one under eighteen'], ['📵', 'no mobile phones'],
            ['💯', 'hundred points'], ['🔟', 'keycap 10'], ['🔢', 'input numbers'],
            ['🔀', 'shuffle tracks'], ['🔁', 'repeat'], ['▶️', 'play button'],
            ['⏸️', 'pause button'], ['⏹️', 'stop button'], ['⏭️', 'next track'],
            ['🔊', 'speaker high volume'], ['🔇', 'muted speaker'], ['🆕', 'new button'],
            ['🆗', 'OK button'], ['🔝', 'top arrow'], ['🏁', 'chequered flag'],
        ]
    },
    {
        title: 'Flags',
        glyphs: [
            ['🏳️', 'white flag'], ['🏴', 'black flag'], ['🏁', 'chequered flag'],
            ['🚩', 'triangular flag'], ['🏳️‍🌈', 'rainbow flag'], ['🇬🇧', 'flag: United Kingdom'],
            ['🇺🇸', 'flag: United States'], ['🇮🇪', 'flag: Ireland'], ['🇿🇦', 'flag: South Africa'],
            ['🇦🇺', 'flag: Australia'], ['🇨🇦', 'flag: Canada'], ['🇫🇷', 'flag: France'],
            ['🇩🇪', 'flag: Germany'], ['🇮🇹', 'flag: Italy'], ['🇪🇸', 'flag: Spain'],
            ['🇳🇱', 'flag: Netherlands'], ['🇮🇱', 'flag: Israel'], ['🇮🇳', 'flag: India'],
            ['🇯🇵', 'flag: Japan'], ['🇰🇷', 'flag: South Korea'], ['🇧🇷', 'flag: Brazil'],
            ['🇰🇪', 'flag: Kenya'], ['🇳🇬', 'flag: Nigeria'], ['🇬🇭', 'flag: Ghana'],
        ]
    },
];

const allEmoji = emojiCategories.flatMap((category) =>
    category.glyphs.map(([glyph, name]) => ({ glyph, name, category: category.title })));

const MAX_UNFILTERED_PER_CATEGORY = 24;

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
    const term = search.trim().toLowerCase();

    const matches = useMemo(
        () => (term === '' ? null : allEmoji.filter((entry) => entry.name.includes(term))),
        [term]);

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
                    Roughly {allEmoji.length} are catalogued below, grouped the way the Unicode
                    standard itself groups them; search narrows across every group at once.
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
                lead={`${allEmoji.length} emoji across ${emojiCategories.length} categories. Without a search term, each category shows its first ${MAX_UNFILTERED_PER_CATEGORY}.`}>
                <LiveDemo>
                    <input
                        type="search"
                        className="form-control mb-3"
                        placeholder="Search emoji names, e.g. &quot;heart&quot;"
                        value={search}
                        onChange={(event) => setSearch(event.target.value)} />

                    {matches != null && (
                        matches.length === 0 ? (
                            <p className="text-body-secondary small mb-0">No matches.</p>
                        ) : (
                            <div className="d-flex flex-wrap gap-3">
                                {matches.map((entry) => (
                                    <div
                                        key={entry.category + entry.name}
                                        className="text-center border rounded p-2"
                                        style={{ width: '5.5rem' }}
                                        title={entry.category}>
                                        <div className="fs-2" aria-hidden="true">{entry.glyph}</div>
                                        <div className="small text-body-secondary text-truncate">
                                            {entry.name}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )
                    )}

                    {matches == null && emojiCategories.map((category) => {
                        const shown = category.glyphs.slice(0, MAX_UNFILTERED_PER_CATEGORY);
                        const remaining = category.glyphs.length - shown.length;

                        return (
                            <div key={category.title} className="mb-4">
                                <h3 className="h6">{category.title}</h3>
                                <div className="d-flex flex-wrap gap-3">
                                    {shown.map(([glyph, name]) => (
                                        <div
                                            key={name}
                                            className="text-center border rounded p-2"
                                            style={{ width: '5.5rem' }}>
                                            <div className="fs-2" aria-hidden="true">{glyph}</div>
                                            <div className="small text-body-secondary text-truncate">
                                                {name}
                                            </div>
                                        </div>
                                    ))}
                                </div>
                                {remaining > 0 && (
                                    <p className="small text-body-secondary mt-2 mb-0">
                                        {remaining} more in this category — search to see them.
                                    </p>
                                )}
                            </div>
                        );
                    })}
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
