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
using System.IO;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// Where this project's own files live at run time.
    ///
    /// <para>The acceptance <c>appsettings.json</c> is read from the project directory rather
    /// than copied to the output, because the host reads it too — <c>TestWebApplicationFactory</c>
    /// layers it over the portal's configuration — and one file in one place cannot drift from
    /// itself. Two readers means the walk up out of <c>bin/Debug/net10.0</c> has to be written
    /// once rather than twice.</para>
    /// </summary>
    internal static class TestProjectPaths
    {
        internal static string ProjectDirectory { get; } =
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
    }
}
