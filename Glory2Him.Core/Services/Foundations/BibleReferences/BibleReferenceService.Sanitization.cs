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
using System.Text.RegularExpressions;

namespace Glory2Him.Core.Services.Foundations.BibleReferences
{
    internal partial class BibleReferenceService
    {
        // The small, stable class vocabulary a Bible provider's renderer emits (design/#347):
        // red-letter (words of Jesus) is "wj", deity names are "nd", poetry indentation is
        // "q1"/"q2", and italics are "it". Anything outside this vocabulary is not scripture
        // formatting, so it is dropped rather than escaped or stored verbatim — ScriptureHtml
        // renders straight into UI, and a general-purpose HTML column is a latent XSS surface.
        private static readonly HashSet<string> AllowedScriptureHtmlTags =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "p", "span" };

        private static readonly HashSet<string> AllowedScriptureHtmlClasses =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "wj", "nd", "q1", "q2", "it" };

        private static readonly Regex ScriptureHtmlEmbedPattern = new Regex(
            @"<(script|style)\b[^>]*>.*?</\1\s*>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

        private static readonly Regex ScriptureHtmlCommentPattern = new Regex(
            @"<!--.*?-->",
            RegexOptions.Compiled | RegexOptions.Singleline);

        private static readonly Regex ScriptureHtmlTagPattern = new Regex(
            @"<\s*(/?)\s*([a-zA-Z][a-zA-Z0-9]*)((?:\s+[a-zA-Z][-a-zA-Z0-9]*(?:\s*=\s*(?:""[^""]*""|'[^']*'|[^\s>]+))?)*)\s*/?\s*>",
            RegexOptions.Compiled);

        private static readonly Regex ScriptureHtmlClassAttributePattern = new Regex(
            @"class\s*=\s*(?:""([^""]*)""|'([^']*)')",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Presentation-only, sanitized on write: stored verbatim only where it already matches
        // the allow-list above, everything else is dropped while its inner text is kept. Plain
        // text carrying no markup — the common case for a translation with no red-letter edition,
        // and every value the filler-driven tests draw — has no '<' and returns unchanged.
        private static string? SanitizeScriptureHtml(string? scriptureHtml)
        {
            if (string.IsNullOrEmpty(scriptureHtml) || scriptureHtml.Contains('<') is false)
            {
                return scriptureHtml;
            }

            string withoutEmbeds = ScriptureHtmlEmbedPattern.Replace(scriptureHtml, string.Empty);
            string withoutComments = ScriptureHtmlCommentPattern.Replace(withoutEmbeds, string.Empty);

            return ScriptureHtmlTagPattern.Replace(withoutComments, SanitizeScriptureHtmlTagMatch);
        }

        private static string SanitizeScriptureHtmlTagMatch(Match match)
        {
            bool isClosingTag = match.Groups[1].Value == "/";
            string tagName = match.Groups[2].Value;

            if (AllowedScriptureHtmlTags.Contains(tagName) is false)
            {
                return string.Empty;
            }

            if (isClosingTag)
            {
                return $"</{tagName.ToLowerInvariant()}>";
            }

            string attributes = match.Groups[3].Value;
            Match classMatch = ScriptureHtmlClassAttributePattern.Match(attributes);

            string allowedClass = classMatch.Success
                ? (classMatch.Groups[1].Success ? classMatch.Groups[1].Value : classMatch.Groups[2].Value)
                : string.Empty;

            bool hasAllowedClass =
                string.IsNullOrEmpty(allowedClass) is false
                    && AllowedScriptureHtmlClasses.Contains(allowedClass);

            return hasAllowedClass
                ? $"<{tagName.ToLowerInvariant()} class=\"{allowedClass.ToLowerInvariant()}\">"
                : $"<{tagName.ToLowerInvariant()}>";
        }
    }
}
