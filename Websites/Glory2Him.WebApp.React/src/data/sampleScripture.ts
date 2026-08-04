import { BibleSection } from '../models/coreUI/bibleSection';
import { ScriptureTranslation } from '../models/coreUI/scriptureTranslation';

// Sample passage for the Bible-reference layout demos, ported from the Blazor
// SamplePages/SampleScripture.cs.
//
// IMPORTANT: the chapter text below is PLACEHOLDER sample data for laying out the page. It has
// not been verified word for word against any published translation, and no translation is
// licensed for redistribution here. Before this ships, the chapter body must come from a
// proper source — a licensed Bible API, or a public-domain text you have checked — rather than
// from this file. The section headings follow the usual divisions of John 14.
export const reference = 'John 14:6';

export const chapterReference = 'John 14';

export const chapterBook = 'John';

export const chapterNumber = 14;

// John's own chapter count — the reader lists them all so the step buttons and the chapter
// picker work, even though only the one below has text.
export const chapterCount = 21;

// The wording already used in this repo's own file headers and footer.
export const singleVerseText =
    'Jesus answered, "I am the way and the truth and the life. No one comes to the '
        + 'Father except through me."';

export const tags: ReadonlyArray<string> = ['Jesus', 'Salvation'];

// Passages that speak to the same truth as John 14:6, written as a person would say them —
// toUsfmReference turns each into the reference the deep-link route addresses.
export const relatedReferences: ReadonlyArray<string> = [
    'John 3:16',
    'John 10:9',
    'Acts 4:12',
    '1 Timothy 2:5',
];

// One entry, because one body of text exists — and it is the unverified placeholder the
// note at the top of this file describes, so it is not claimed as any published
// translation. Add an entry per translation once real text is loaded and both dropdowns in
// the reader fill out on their own; parallel mode already works with one, showing the same
// text either side (bible.com does the same when both sides are set alike).
export const translations: ReadonlyArray<ScriptureTranslation> =
    [{ code: 'SAMPLE', name: 'Sample text (unverified)' }];

export const chapter: ReadonlyArray<BibleSection> = [
    {
        heading: 'Jesus Comforts His Disciples',
        verses: [
            { number: 1, text: '"Do not let your hearts be troubled. You believe in God; '
                + 'believe also in me.' },
            { number: 2, text: 'My Father\'s house has many rooms; if that were not so, '
                + 'would I have told you that I am going there to prepare a place for you?' },
            { number: 3, text: 'And if I go and prepare a place for you, I will come back '
                + 'and take you to be with me that you also may be where I am.' },
            { number: 4, text: 'You know the way to the place where I am going."' },
        ],
    },
    {
        heading: 'Jesus the Way to the Father',
        verses: [
            { number: 5, text: 'Thomas said to him, "Lord, we don\'t know where you are '
                + 'going, so how can we know the way?"' },
            { number: 6, text: 'Jesus answered, "I am the way and the truth and the life. '
                + 'No one comes to the Father except through me.' },
            { number: 7, text: 'If you really know me, you will know my Father as well. '
                + 'From now on, you do know him and have seen him."' },
            { number: 8, text: 'Philip said, "Lord, show us the Father and that will be '
                + 'enough for us."' },
            { number: 9, text: 'Jesus answered: "Don\'t you know me, Philip, even after I '
                + 'have been among you such a long time? Anyone who has seen me has seen '
                + 'the Father. How can you say, \'Show us the Father\'?' },
            { number: 10, text: 'Don\'t you believe that I am in the Father, and that the '
                + 'Father is in me? The words I say to you I do not speak on my own '
                + 'authority. Rather, it is the Father, living in me, who is doing his work.' },
            { number: 11, text: 'Believe me when I say that I am in the Father and the '
                + 'Father is in me; or at least believe on the evidence of the works '
                + 'themselves.' },
            { number: 12, text: 'Very truly I tell you, whoever believes in me will do the '
                + 'works I have been doing, and they will do even greater things than '
                + 'these, because I am going to the Father.' },
            { number: 13, text: 'And I will do whatever you ask in my name, so that the '
                + 'Father may be glorified in the Son.' },
            { number: 14, text: 'You may ask me for anything in my name, and I will do it."' },
        ],
    },
    {
        heading: 'Jesus Promises the Holy Spirit',
        verses: [
            { number: 15, text: '"If you love me, keep my commands.' },
            { number: 16, text: 'And I will ask the Father, and he will give you another '
                + 'advocate to help you and be with you forever —' },
            { number: 17, text: 'the Spirit of truth. The world cannot accept him, because '
                + 'it neither sees him nor knows him. But you know him, for he lives with '
                + 'you and will be in you.' },
            { number: 18, text: 'I will not leave you as orphans; I will come to you.' },
            { number: 19, text: 'Before long, the world will not see me anymore, but you '
                + 'will see me. Because I live, you also will live.' },
            { number: 20, text: 'On that day you will realize that I am in my Father, and '
                + 'you are in me, and I am in you.' },
            { number: 21, text: 'Whoever has my commands and keeps them is the one who '
                + 'loves me. The one who loves me will be loved by my Father, and I too '
                + 'will love them and show myself to them."' },
            { number: 22, text: 'Then Judas (not Judas Iscariot) said, "But, Lord, why do '
                + 'you intend to show yourself to us and not to the world?"' },
            { number: 23, text: 'Jesus replied, "Anyone who loves me will obey my '
                + 'teaching. My Father will love them, and we will come to them and make '
                + 'our home with them.' },
            { number: 24, text: 'Anyone who does not love me will not obey my teaching. '
                + 'These words you hear are not my own; they belong to the Father who sent '
                + 'me.' },
            { number: 25, text: 'All this I have spoken while still with you.' },
            { number: 26, text: 'But the Advocate, the Holy Spirit, whom the Father will '
                + 'send in my name, will teach you all things and will remind you of '
                + 'everything I have said to you.' },
            { number: 27, text: 'Peace I leave with you; my peace I give you. I do not give '
                + 'to you as the world gives. Do not let your hearts be troubled and do '
                + 'not be afraid.' },
            { number: 28, text: 'You heard me say, \'I am going away and I am coming back to '
                + 'you.\' If you loved me, you would be glad that I am going to the Father, '
                + 'for the Father is greater than I.' },
            { number: 29, text: 'I have told you now before it happens, so that when it '
                + 'does happen you will believe.' },
            { number: 30, text: 'I will not say much more to you, for the prince of this '
                + 'world is coming. He has no hold over me,' },
            { number: 31, text: 'but he comes so that the world may learn that I love the '
                + 'Father and that I do exactly what my Father has commanded me. Come now; '
                + 'let us leave."' },
        ],
    },
];

// Null for every chapter but the one transcribed above — the reader shows that as a note
// rather than an empty column.
export function chapterFor(
    requestedChapter: number,
    translationCode: string): ReadonlyArray<BibleSection> | null {
    return requestedChapter === chapterNumber
        && translations.some((translation) => translation.code === translationCode)
            ? chapter
            : null;
}
