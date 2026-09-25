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
using FluentAssertions;
using Glory2Him.Core.Infrastructure.Services;

namespace Glory2Him.Core.Tests.Unit.Infrastructure.Services
{
    public class ScriptGenerationServiceTests
    {
        [Fact]
        public void ShouldEmitTheRejectAiAttributionJobFromTheGenerator()
        {
            // given
            var scriptGenerationService = new ScriptGenerationService();

            string prLinterPath = Path.GetFullPath(
                Path.Combine(AppContext.BaseDirectory, "../../../../.github/workflows/prLinter.yml"));

            byte[] committedPrLinterBytes = File.ReadAllBytes(prLinterPath);
            string expectedPrLinter = NormalizeLineEndings(File.ReadAllText(prLinterPath));
            string actualPrLinter;

            // when
            try
            {
                scriptGenerationService.GeneratePrLintScript(branchName: "main");
                actualPrLinter = NormalizeLineEndings(File.ReadAllText(prLinterPath));
            }
            finally
            {
                File.WriteAllBytes(prLinterPath, committedPrLinterBytes);
            }

            // then
            actualPrLinter.Should().Be(expectedPrLinter);

            actualPrLinter.Should().Contain(
                "  rejectAiAttribution:\n    name: Reject AI Identity And Attribution\n");
        }

        private static string NormalizeLineEndings(string text) =>
            text.Replace("\r\n", "\n");
    }
}
