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

using System.Linq;
using Bunit;
using FluentAssertions;
using Glory2Him.WebApp.Components.Pages;
using Glory2Him.WebApp.Components.Pages.SamplePages;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages
{
    public class BibleReferencePageComponentTests : BunitContext
    {
        public BibleReferencePageComponentTests() =>
            JSInterop.Mode = JSRuntimeMode.Loose;

        [Fact]
        public void ShouldShowTheVerseOnItsOwn()
        {
            // when
            IRenderedComponent<BibleReference> page = Render<BibleReference>();

            // then
            page.Find("h1").TextContent.Trim().Should().Be(SampleScripture.Reference);
            page.Find("p.lead").TextContent.Trim().Should().Be(SampleScripture.SingleVerseText);
        }

        [Fact]
        public void ShouldOfferTheFullChapterOnAPublicRoute()
        {
            // when
            IRenderedComponent<BibleReference> page = Render<BibleReference>();

            // then (the sample this clones links to an Administrators-only chapter page, which
            // would bounce a visitor to a login)
            page.Find("a.btn-link").GetAttribute("href")
                .Should().Be("/BibleReferences/Full-Chapter");

            page.Markup.Should().NotContain("SamplePages");
        }

        [Fact]
        public void ShouldSendItsTagsToASearch()
        {
            // when
            IRenderedComponent<BibleReference> page = Render<BibleReference>();

            // then
            page.FindAll("a.btn-outline-secondary")
                .Select(tag => tag.GetAttribute("href")!)
                .Should().OnlyContain(href => href.StartsWith("/Search?q="));
        }

        [Fact]
        public void ShouldShowTheWholeChapterOnTheChapterPage()
        {
            // when
            IRenderedComponent<BibleReferenceChapter> page = Render<BibleReferenceChapter>();

            // then
            page.Find("h2").TextContent.Trim().Should().Be(SampleScripture.ChapterReference);

            page.FindAll("span.g2h-verse").Should().HaveCount(
                SampleScripture.Chapter.Sum(section => section.Verses.Count));

            page.Find("a.btn-link").GetAttribute("href").Should().Be("/BibleReferences");
        }

        [Fact]
        public void ShouldReadTheChapterThroughTheScriptureReader()
        {
            // when
            IRenderedComponent<BibleReferenceChapter> page = Render<BibleReferenceChapter>();

            // then (chapter picker, translation picker, the parallel toggle and a step button
            // either side)
            page.Find("select[aria-label='Chapter']").QuerySelectorAll("option")
                .Should().HaveCount(SampleScripture.ChapterCount);

            page.Find("select[aria-label='Translation']").Should().NotBeNull();
            page.Find("button[aria-pressed]").TextContent.Trim().Should().Be("Parallel");
            page.FindAll("button.g2h-scripture-step").Should().HaveCount(2);
        }

        [Fact]
        public void ShouldSayWhenAChapterHasNoTextYet()
        {
            // given (only John 14 has been transcribed)
            IRenderedComponent<BibleReferenceChapter> page = Render<BibleReferenceChapter>();

            // when
            page.Find("select[aria-label='Chapter']").Change("15");

            // then
            page.Find("div.alert-info").TextContent.Should().Contain("John 15 is not available");
        }
    }
}
