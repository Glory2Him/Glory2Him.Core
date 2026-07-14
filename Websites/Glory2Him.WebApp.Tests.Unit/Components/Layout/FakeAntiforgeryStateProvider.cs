// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// John 14:6 (NIV) "Jesus answered, ‘I am the way and the truth and the life.
//                  No one comes to the Father except through me.’"
// https://mark.bible/mark-16-15
// https://john.bible/john-14-6
// ────────────────────────────────────────────────────────────────────────────────

using Microsoft.AspNetCore.Components.Forms;

namespace Glory2Him.WebApp.Tests.Unit.Components.Layout
{
    // Minimal antiforgery provider so components that render <AntiforgeryToken /> (e.g. the
    // header's logout form) can be rendered under bUnit without the ASP.NET Core host.
    public class FakeAntiforgeryStateProvider : AntiforgeryStateProvider
    {
        public override AntiforgeryRequestToken GetAntiforgeryToken() =>
            new AntiforgeryRequestToken(value: "test-token", formFieldName: "__RequestVerificationToken");
    }
}
