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

using System;
using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class ScriptureReaderComponentTests : BunitContext
    {
        private static readonly ScriptureTranslation[] TwoTranslations = new[]
        {
            new ScriptureTranslation("AAA", "Translation A"),
            new ScriptureTranslation("BBB", "Translation B"),
        };

        // Chapter 14 has text in both; chapter 15 only in the first, so the "not available" path
        // has something to show.
        private static IReadOnlyList<BibleSection>? Lookup(int chapter, string translationCode)
        {
            if (chapter == 15 && translationCode != "AAA")
            {
                return null;
            }

            if (chapter is not (14 or 15))
            {
                return null;
            }

            return new[]
            {
                new BibleSection($"Heading {translationCode}", new[]
                {
                    new BibleVerse(1, $"Verse one of {chapter} in {translationCode}."),
                }),
            };
        }

        private IRenderedComponent<ScriptureReaderComponent> RenderReader(int chapter = 14) =>
            Render<ScriptureReaderComponent>(parameters => parameters
                .Add(reader => reader.Book, "John")
                .Add(reader => reader.ChapterCount, 21)
                .Add(reader => reader.Chapter, chapter)
                .Add(reader => reader.Translations, TwoTranslations)
                .Add(reader => reader.SectionsFor,
                    (Func<int, string, IReadOnlyList<BibleSection>?>)Lookup));

        [Fact]
        public void ShouldOpenOnOneTranslationWithTheChapterAndTranslationOnTheBar()
        {
            // when
            IRenderedComponent<ScriptureReaderComponent> reader = RenderReader();

            // then
            List<IElement> selects = reader.FindAll("select").ToList();
            selects.Should().HaveCount(2);

            selects[0].GetAttribute("aria-label").Should().Be("Chapter");
            selects[0].QuerySelectorAll("option").Should().HaveCount(21);
            selects[0].QuerySelectorAll("option")[13].TextContent.Trim().Should().Be("John 14");

            selects[1].GetAttribute("aria-label").Should().Be("Translation");

            reader.Find("button[aria-pressed]").TextContent.Trim().Should().Be("Parallel");
        }

        [Fact]
        public void ShouldSplitIntoTwoColumnsOnTheParallelToggle()
        {
            // given
            IRenderedComponent<ScriptureReaderComponent> reader = RenderReader();
            reader.FindAll("div.col-12").Should().ContainSingle();

            // when
            reader.Find("button[aria-pressed]").Click();

            // then (a third select appears for the second translation, and both sides start on the
            // same one — the reader picks the second after splitting)
            reader.FindAll("select").Should().HaveCount(3);
            reader.FindAll("div.col-md-6").Should().HaveCount(2);
            reader.FindAll("div.col-12").Should().BeEmpty();

            reader.Find("button[aria-pressed]").TextContent.Trim()
                .Should().Be("Exit Parallel Mode");

            reader.FindAll("h3").Select(heading => heading.TextContent.Trim())
                .Should().Equal("Heading AAA", "Heading AAA");
        }

        [Fact]
        public void ShouldFoldBackToOneColumnOnExitingParallel()
        {
            // given
            IRenderedComponent<ScriptureReaderComponent> reader = RenderReader();
            reader.Find("button[aria-pressed]").Click();

            // when
            reader.Find("button[aria-pressed]").Click();

            // then
            reader.FindAll("select").Should().HaveCount(2);
            reader.FindAll("div.col-12").Should().ContainSingle();
        }

        [Fact]
        public void ShouldShowTheSecondTranslationBesideTheFirst()
        {
            // given
            IRenderedComponent<ScriptureReaderComponent> reader = RenderReader();
            reader.Find("button[aria-pressed]").Click();

            // when
            reader.FindAll("select")[2].Change("BBB");

            // then
            reader.FindAll("h3").Select(heading => heading.TextContent.Trim())
                .Should().Equal("Heading AAA", "Heading BBB");
        }

        [Fact]
        public void ShouldStepToTheNeighbouringChapters()
        {
            // given
            IRenderedComponent<ScriptureReaderComponent> reader = RenderReader();

            // when
            reader.FindAll("button.g2h-scripture-step")[1].Click();

            // then
            reader.Find("h2").TextContent.Trim().Should().Be("John 15");

            // when (back again)
            reader.FindAll("button.g2h-scripture-step")[0].Click();

            // then
            reader.Find("h2").TextContent.Trim().Should().Be("John 14");
        }

        [Fact]
        public void ShouldStopSteppingAtTheEndsOfTheBook()
        {
            // when
            IRenderedComponent<ScriptureReaderComponent> first = RenderReader(chapter: 1);
            IRenderedComponent<ScriptureReaderComponent> last = RenderReader(chapter: 21);

            // then
            first.FindAll("button.g2h-scripture-step")[0].HasAttribute("disabled")
                .Should().BeTrue();

            first.FindAll("button.g2h-scripture-step")[1].HasAttribute("disabled")
                .Should().BeFalse();

            last.FindAll("button.g2h-scripture-step")[1].HasAttribute("disabled")
                .Should().BeTrue();
        }

        [Fact]
        public void ShouldJumpToTheChapterPickedFromTheList()
        {
            // given
            IRenderedComponent<ScriptureReaderComponent> reader = RenderReader();

            // when
            reader.FindAll("select")[0].Change("15");

            // then
            reader.Find("h2").TextContent.Trim().Should().Be("John 15");
        }

        [Fact]
        public void ShouldRaiseTheChapterSoACallerCanTrackIt()
        {
            // given
            int? raised = null;

            IRenderedComponent<ScriptureReaderComponent> reader =
                Render<ScriptureReaderComponent>(parameters => parameters
                    .Add(read => read.Book, "John")
                    .Add(read => read.ChapterCount, 21)
                    .Add(read => read.Chapter, 14)
                    .Add(read => read.Translations, TwoTranslations)
                    .Add(read => read.SectionsFor,
                        (Func<int, string, IReadOnlyList<BibleSection>?>)Lookup)
                    .Add(read => read.ChapterChanged, chapter => raised = chapter));

            // when
            reader.FindAll("select")[0].Change("15");

            // then
            raised.Should().Be(15);
        }

        [Fact]
        public void ShouldSayWhenAChapterHasNoTextInThatTranslation()
        {
            // given (chapter 15 exists in AAA only)
            IRenderedComponent<ScriptureReaderComponent> reader = RenderReader(chapter: 15);
            reader.Find("button[aria-pressed]").Click();

            // when
            reader.FindAll("select")[2].Change("BBB");

            // then (a note, not an empty column)
            reader.Find("div.alert-info").TextContent.Should()
                .Contain("John 15 is not available in Translation B yet.");
        }

        [Fact]
        public void ShouldLeaveTheShareIconsOffTheChapter()
        {
            // when
            IRenderedComponent<ScriptureReaderComponent> reader = RenderReader();

            // then (the bar above carries the controls now — the share pair belonged to the
            // standalone chapter view)
            reader.FindAll("a.bg-facebook").Should().BeEmpty();
            reader.FindAll("a.bg-twitter").Should().BeEmpty();
        }
    }
}
