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
using Ganss.Xss;

namespace Glory2Him.Core.Services.Foundations.BibleReferences
{
    internal partial class BibleReferenceService
    {
        // Parser-backed (AngleSharp), not regex-based: HTML is not a regular language, and a
        // hand-rolled tag-matching regex either fails closed only for syntax it anticipated (a
        // security bug — malformed/unanticipated tag syntax then ships untouched) or risks
        // catastrophic backtracking on adversarial input. A real parser tokenizes the actual DOM
        // it will be rendered as, so the allow-list can't be bypassed by markup the parser didn't
        // expect, and HTML5 parsing is defined to always terminate in roughly linear time.
        //
        // The small, stable class vocabulary a Bible provider's renderer emits (design/#347):
        // red-letter (words of Jesus) is "wj", deity names are "nd", poetry indentation is
        // "q1"/"q2", and italics are "it". Anything outside this vocabulary is not scripture
        // formatting, so it is dropped rather than escaped or stored verbatim — ScriptureHtml
        // renders straight into UI, and a general-purpose HTML column is a latent XSS surface.
        private static readonly HtmlSanitizer ScriptureHtmlSanitizer = CreateScriptureHtmlSanitizer();

        private static HtmlSanitizer CreateScriptureHtmlSanitizer()
        {
            var sanitizer = new HtmlSanitizer
            {
                // A disallowed wrapper (e.g. unrecognized markup from a provider) drops the tag
                // but keeps its text — scripture words are never silently lost because of an
                // unanticipated wrapper. Script/style are the one exception (below): their body is
                // code/CSS, not scripture text, so it is discarded along with the tag.
                KeepChildNodes = true
            };

            sanitizer.AllowedTags.Clear();
            sanitizer.AllowedTags.Add("p");
            sanitizer.AllowedTags.Add("span");

            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedAttributes.Add("class");

            sanitizer.AllowedClasses.Clear();
            sanitizer.AllowedClasses.Add("wj");
            sanitizer.AllowedClasses.Add("nd");
            sanitizer.AllowedClasses.Add("q1");
            sanitizer.AllowedClasses.Add("q2");
            sanitizer.AllowedClasses.Add("it");

            // No URIs, inline styles, or CSS at-rules are ever legitimate in this vocabulary.
            sanitizer.AllowedSchemes.Clear();
            sanitizer.AllowedCssProperties.Clear();
            sanitizer.AllowedAtRules.Clear();
            sanitizer.UriAttributes.Clear();

            // KeepChildNodes preserves a disallowed tag's text, which for <script>/<style> would
            // otherwise leak their raw code/CSS body into the sanitized output as visible text.
            // Clearing the element before removal empties that text too, so nothing survives.
            sanitizer.RemovingTag += (_, args) =>
            {
                string tagName = args.Tag.TagName;

                if (string.Equals(tagName, "script", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(tagName, "style", StringComparison.OrdinalIgnoreCase))
                {
                    args.Tag.TextContent = string.Empty;
                }
            };

            return sanitizer;
        }

        // Presentation-only, sanitized on write: stored verbatim only where it already matches
        // the allow-list above. Plain text carrying no markup — the common case for a translation
        // with no red-letter edition — round-trips unchanged.
        private static string? SanitizeScriptureHtml(string? scriptureHtml)
        {
            if (string.IsNullOrEmpty(scriptureHtml))
            {
                return scriptureHtml;
            }

            return ScriptureHtmlSanitizer.Sanitize(scriptureHtml);
        }
    }
}
