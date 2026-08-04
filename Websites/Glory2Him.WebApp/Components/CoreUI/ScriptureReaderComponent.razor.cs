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

using Microsoft.AspNetCore.Components;

namespace Glory2Him.WebApp.Components.CoreUI
{
    public partial class ScriptureReaderComponent
    {
        [Parameter]
        [EditorRequired]
        public string Book { get; set; } = string.Empty;

        [Parameter]
        public int ChapterCount { get; set; } = 1;

        [Parameter]
        public int Chapter { get; set; } = 1;

        // Raised so a caller can put the chapter in the URL. The component keeps its own value
        // either way, the same way PaginationComponent does, so a page needs no code of its own.
        [Parameter]
        public EventCallback<int> ChapterChanged { get; set; }

        [Parameter]
        public IReadOnlyList<ScriptureTranslation> Translations { get; set; } =
            new List<ScriptureTranslation>();

        // Returns null when that chapter has no text in that translation, which the reader shows
        // as a plain note rather than an empty column.
        [Parameter]
        [EditorRequired]
        public Func<int, string, IReadOnlyList<BibleSection>?> SectionsFor { get; set; } =
            (_, _) => null;

        private string primaryCode = string.Empty;

        private string secondaryCode = string.Empty;

        private bool isParallel;

        private string PreviousTitle =>
            Chapter <= 1 ? "No earlier chapter" : $"{Book} {Chapter - 1}";

        private string NextTitle =>
            Chapter >= ChapterCount ? "No later chapter" : $"{Book} {Chapter + 1}";

        protected override void OnParametersSet()
        {
            if (Translations.Count == 0)
            {
                return;
            }

            // Both sides start on the same translation, as bible.com does — the reader picks a
            // second one after splitting, rather than the split choosing one for them.
            if (!Translations.Any(translation => translation.Code == this.primaryCode))
            {
                this.primaryCode = Translations[0].Code;
            }

            if (!Translations.Any(translation => translation.Code == this.secondaryCode))
            {
                this.secondaryCode = this.primaryCode;
            }
        }

        private string TranslationName(string code) =>
            Translations.FirstOrDefault(translation => translation.Code == code)?.Name ?? code;

        private async Task GoToChapter(int chapter)
        {
            if (chapter < 1 || chapter > ChapterCount || chapter == Chapter)
            {
                return;
            }

            Chapter = chapter;

            await ChapterChanged.InvokeAsync(chapter);
        }

        private Task OnChapterSelected(ChangeEventArgs args) =>
            int.TryParse(args.Value?.ToString(), out int chapter)
                ? GoToChapter(chapter)
                : Task.CompletedTask;

        private void OnPrimaryTranslationSelected(ChangeEventArgs args) =>
            this.primaryCode = args.Value?.ToString() ?? this.primaryCode;

        private void OnSecondaryTranslationSelected(ChangeEventArgs args) =>
            this.secondaryCode = args.Value?.ToString() ?? this.secondaryCode;

        private void ToggleParallel() =>
            this.isParallel = !this.isParallel;
    }
}
