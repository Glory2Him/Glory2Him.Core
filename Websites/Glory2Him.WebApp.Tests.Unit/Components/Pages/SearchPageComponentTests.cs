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
using Glory2Him.WebApp.Components.Pages;
using Microsoft.AspNetCore.Components.Web;

namespace Glory2Him.WebApp.Tests.Unit.Components.Pages
{
    public partial class SearchPageComponentTests : BunitContext
    {
        public SearchPageComponentTests() =>
            JSInterop.Mode = JSRuntimeMode.Loose;

        // Assertions have to read the result rows specifically, not the whole page: the sidebar's
        // Trending panel lists the same posts, so a NotContain on Markup would pass or fail for
        // the wrong reason.
        private static List<string> ResultTitles(IRenderedComponent<Search> page) =>
            page.FindAll("h3.card-title")
                .Cast<IElement>()
                .Select(title => title.TextContent.Trim())
                .ToList();

        private static IRenderedComponent<Search> SearchFor(
            IRenderedComponent<Search> page,
            string text)
        {
            page.Find("input[type='search']").Input(text);
            page.Find("form").Submit();

            return page;
        }

        // The tag box lives outside the form on purpose, so Enter builds the list instead of
        // running the search.
        private static void AddTag(IRenderedComponent<Search> page, string tag)
        {
            IElement box = page.Find("#advancedSearchOptions input[type='text'][aria-label]");
            box.Input(tag);
            box.KeyDown(new KeyboardEventArgs { Key = "Enter" });
        }
    }
}
