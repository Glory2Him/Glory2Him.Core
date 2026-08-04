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

using System.Collections.Generic;
using System.Linq;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.CoreUI;

namespace Glory2Him.WebApp.Tests.Unit.Components.CoreUI
{
    public class BibleChapterComponentTests : BunitContext
    {
        private static IReadOnlyList<BibleSection> CreateChapter() =>
            new[]
            {
                new BibleSection("Jesus Comforts His Disciples", new[]
                {
                    new BibleVerse(1, "Do not let your hearts be troubled."),
                    new BibleVerse(2, "My Father's house has many rooms."),
                }),
                new BibleSection("Jesus the Way to the Father", new[]
                {
                    new BibleVerse(6, "I am the way and the truth and the life."),
                }),
            };

        [Fact]
        public void ShouldRenderEverySectionHeadingAndVerse()
        {
            // when
            IRenderedComponent<BibleChapterComponent> rendered =
                Render<BibleChapterComponent>(parameters => parameters
                    .Add(chapter => chapter.Reference, "John 14")
                    .Add(chapter => chapter.Sections, CreateChapter()));

            // then
            rendered.Find("h2").TextContent.Trim().Should().Be("John 14");

            rendered.FindAll("h3").Select(h => h.TextContent.Trim()).Should().Equal(
                "Jesus Comforts His Disciples",
                "Jesus the Way to the Father");

            rendered.FindAll("sup.g2h-verse-number").Select(s => s.TextContent.Trim())
                .Should().Equal("1", "2", "6");
        }

        [Fact]
        public void ShouldSetEveryVerseAsPlainText()
        {
            // when
            IRenderedComponent<BibleChapterComponent> rendered =
                Render<BibleChapterComponent>(parameters => parameters
                    .Add(chapter => chapter.Reference, "John 14")
                    .Add(chapter => chapter.Sections, CreateChapter()));

            // then (the chapter reads as one passage — no verse is singled out with a tint)
            List<IElement> verses = rendered.FindAll("span.g2h-verse").ToList();

            verses.Should().HaveCount(3);
            verses.Should().OnlyContain(verse => verse.ClassList.Length == 1);
        }

        [Fact]
        public void ShouldHideTheShareLinksWhenNotWanted()
        {
            // when
            IRenderedComponent<BibleChapterComponent> rendered =
                Render<BibleChapterComponent>(parameters => parameters
                    .Add(chapter => chapter.Reference, "John 14")
                    .Add(chapter => chapter.Sections, CreateChapter())
                    .Add(chapter => chapter.ShowShareLinks, false));

            // then
            rendered.FindAll("a.bg-facebook").Should().BeEmpty();
        }

        [Fact]
        public void ShouldRenderAnUnheadedSectionWithoutABlankHeading()
        {
            // given
            var sections = new[]
            {
                new BibleSection(null, new[] { new BibleVerse(1, "In the beginning…") }),
            };

            // when
            IRenderedComponent<BibleChapterComponent> rendered =
                Render<BibleChapterComponent>(parameters => parameters
                    .Add(chapter => chapter.Reference, "Genesis 1")
                    .Add(chapter => chapter.Sections, sections));

            // then
            rendered.FindAll("h3").Should().BeEmpty();
            rendered.FindAll("sup.g2h-verse-number").Should().ContainSingle();
        }
    }
}
