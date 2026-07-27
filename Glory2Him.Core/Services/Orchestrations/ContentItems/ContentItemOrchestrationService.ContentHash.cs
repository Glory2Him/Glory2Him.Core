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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Glory2Him.Core.Services.Orchestrations.ContentItems
{
    public partial class ContentItemOrchestrationService
    {
        // The normalization below is a FROZEN CONTRACT (design §3.4.2): trim ends,
        // collapse whitespace/newline runs to a single space, lowercase (invariant
        // culture), then SHA-256 over UTF-8 bytes rendered as lowercase hex (64 chars).
        // Changing any step requires recomputing every stored ContentHash in a migration.
        private static string ComputeContentHash(string content)
        {
            string normalizedContent = NormalizeContent(content);
            byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedContent));

            return Convert.ToHexStringLower(hashBytes);
        }

        private static string NormalizeContent(string content) =>
            Regex.Replace(content.Trim(), pattern: @"\s+", replacement: " ")
                .ToLowerInvariant();
    }
}
