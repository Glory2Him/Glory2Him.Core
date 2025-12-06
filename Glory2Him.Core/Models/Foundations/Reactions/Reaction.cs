// ────────────────────────────────────────────────────────────────────────────────
// Copyright (c) Glory 2 Him. All rights reserved.
// Licensed under the Glory 2 Him Software License (G2HSL).
// See License.txt in the project root for full license information.
// FREE TO USE TO HELP SHARE THE GOSPEL
// Mark 16:15 (NIV) "Go into all the world and preach the gospel to all creation."
// https://mark.bible/mark-16-15
// ────────────────────────────────────────────────────────────────────────────────

using System;
using Glory2Him.Core.Models.Bases;

namespace Glory2Him.Core.Models.Foundations.Reactions
{
    public class Reaction : IKey, IAudit
    {
        /// <summary>
        /// Primary key identifier for the reaction.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The reaction name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The reaction unicode emoji.
        /// </summary>
        public string UnicodeEmoji { get; set; } = string.Empty;

        /// <summary>
        /// User identifier for who created the reaction.
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the reaction was created.
        /// </summary>
        public DateTimeOffset CreatedWhen { get; set; }

        /// <summary>
        /// User identifier for who last updated the reaction.
        /// </summary>
        public string UpdatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp when the reaction was last updated.
        /// </summary>
        public DateTimeOffset UpdatedWhen { get; set; }
    }
}
