// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://john.bible/john-14-6
// If Jesus is who He said He is, what does that mean for you, today?
// ────────────────────────────────────────────────────────────────────────────────

using Glory2Him.WebApp.Components.CoreUI;

namespace Glory2Him.WebApp.Components.Pages.SamplePages
{
    // Sample passage for the Bible-reference layout demos.
    //
    // IMPORTANT: the chapter text below is PLACEHOLDER sample data for laying out the page. It has
    // not been verified word for word against any published translation, and no translation is
    // licensed for redistribution here. Before this ships, the chapter body must come from a
    // proper source — a licensed Bible API, or a public-domain text you have checked — rather than
    // from this file. The section headings follow the usual divisions of John 14.
    public static class SampleScripture
    {
        public const string Reference = "John 14:6";

        public const string ChapterReference = "John 14";

        public const string ChapterBook = "John";

        public const int ChapterNumber = 14;

        // John's own chapter count — the reader lists them all so the step buttons and the chapter
        // picker work, even though only the one below has text.
        public const int ChapterCount = 21;

        // The wording already used in this repo's own file headers and footer.
        public const string SingleVerseText =
            "Jesus answered, \"I am the way and the truth and the life. No one comes to the "
                + "Father except through me.\"";

        public static IReadOnlyList<string> Tags =>
            new[] { "Jesus", "Salvation" };

        // One entry, because one body of text exists — and it is the unverified placeholder the
        // note at the top of this file describes, so it is not claimed as any published
        // translation. Add an entry per translation once real text is loaded and both dropdowns in
        // the reader fill out on their own; parallel mode already works with one, showing the same
        // text either side (bible.com does the same when both sides are set alike).
        public static IReadOnlyList<ScriptureTranslation> Translations =>
            new[] { new ScriptureTranslation("SAMPLE", "Sample text (unverified)") };

        // Null for every chapter but the one transcribed below — the reader shows that as a note
        // rather than an empty column.
        public static IReadOnlyList<BibleSection>? ChapterFor(int chapter, string translationCode) =>
            chapter == ChapterNumber
                && Translations.Any(translation => translation.Code == translationCode)
                    ? Chapter
                    : null;

        public static IReadOnlyList<BibleSection> Chapter =>
            new[]
            {
                new BibleSection("Jesus Comforts His Disciples", new[]
                {
                    new BibleVerse(1, "\"Do not let your hearts be troubled. You believe in God; "
                        + "believe also in me."),
                    new BibleVerse(2, "My Father's house has many rooms; if that were not so, "
                        + "would I have told you that I am going there to prepare a place for you?"),
                    new BibleVerse(3, "And if I go and prepare a place for you, I will come back "
                        + "and take you to be with me that you also may be where I am."),
                    new BibleVerse(4, "You know the way to the place where I am going.\""),
                }),

                new BibleSection("Jesus the Way to the Father", new[]
                {
                    new BibleVerse(5, "Thomas said to him, \"Lord, we don't know where you are "
                        + "going, so how can we know the way?\""),
                    new BibleVerse(6, "Jesus answered, \"I am the way and the truth and the life. "
                        + "No one comes to the Father except through me."),
                    new BibleVerse(7, "If you really know me, you will know my Father as well. "
                        + "From now on, you do know him and have seen him.\""),
                    new BibleVerse(8, "Philip said, \"Lord, show us the Father and that will be "
                        + "enough for us.\""),
                    new BibleVerse(9, "Jesus answered: \"Don't you know me, Philip, even after I "
                        + "have been among you such a long time? Anyone who has seen me has seen "
                        + "the Father. How can you say, 'Show us the Father'?"),
                    new BibleVerse(10, "Don't you believe that I am in the Father, and that the "
                        + "Father is in me? The words I say to you I do not speak on my own "
                        + "authority. Rather, it is the Father, living in me, who is doing his work."),
                    new BibleVerse(11, "Believe me when I say that I am in the Father and the "
                        + "Father is in me; or at least believe on the evidence of the works "
                        + "themselves."),
                    new BibleVerse(12, "Very truly I tell you, whoever believes in me will do the "
                        + "works I have been doing, and they will do even greater things than "
                        + "these, because I am going to the Father."),
                    new BibleVerse(13, "And I will do whatever you ask in my name, so that the "
                        + "Father may be glorified in the Son."),
                    new BibleVerse(14, "You may ask me for anything in my name, and I will do it.\""),
                }),

                new BibleSection("Jesus Promises the Holy Spirit", new[]
                {
                    new BibleVerse(15, "\"If you love me, keep my commands."),
                    new BibleVerse(16, "And I will ask the Father, and he will give you another "
                        + "advocate to help you and be with you forever —"),
                    new BibleVerse(17, "the Spirit of truth. The world cannot accept him, because "
                        + "it neither sees him nor knows him. But you know him, for he lives with "
                        + "you and will be in you."),
                    new BibleVerse(18, "I will not leave you as orphans; I will come to you."),
                    new BibleVerse(19, "Before long, the world will not see me anymore, but you "
                        + "will see me. Because I live, you also will live."),
                    new BibleVerse(20, "On that day you will realize that I am in my Father, and "
                        + "you are in me, and I am in you."),
                    new BibleVerse(21, "Whoever has my commands and keeps them is the one who "
                        + "loves me. The one who loves me will be loved by my Father, and I too "
                        + "will love them and show myself to them.\""),
                    new BibleVerse(22, "Then Judas (not Judas Iscariot) said, \"But, Lord, why do "
                        + "you intend to show yourself to us and not to the world?\""),
                    new BibleVerse(23, "Jesus replied, \"Anyone who loves me will obey my "
                        + "teaching. My Father will love them, and we will come to them and make "
                        + "our home with them."),
                    new BibleVerse(24, "Anyone who does not love me will not obey my teaching. "
                        + "These words you hear are not my own; they belong to the Father who sent "
                        + "me."),
                    new BibleVerse(25, "All this I have spoken while still with you."),
                    new BibleVerse(26, "But the Advocate, the Holy Spirit, whom the Father will "
                        + "send in my name, will teach you all things and will remind you of "
                        + "everything I have said to you."),
                    new BibleVerse(27, "Peace I leave with you; my peace I give you. I do not give "
                        + "to you as the world gives. Do not let your hearts be troubled and do "
                        + "not be afraid."),
                    new BibleVerse(28, "You heard me say, 'I am going away and I am coming back to "
                        + "you.' If you loved me, you would be glad that I am going to the Father, "
                        + "for the Father is greater than I."),
                    new BibleVerse(29, "I have told you now before it happens, so that when it "
                        + "does happen you will believe."),
                    new BibleVerse(30, "I will not say much more to you, for the prince of this "
                        + "world is coming. He has no hold over me,"),
                    new BibleVerse(31, "but he comes so that the world may learn that I love the "
                        + "Father and that I do exactly what my Father has commanded me. Come now; "
                        + "let us leave.\""),
                }),
            };
    }
}
