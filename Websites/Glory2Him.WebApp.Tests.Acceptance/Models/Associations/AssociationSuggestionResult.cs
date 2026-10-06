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
using Glory2Him.Core.Models.Orchestrations.Associations;

namespace Glory2Him.WebApp.Tests.Acceptance.Models.Associations
{
    /// <summary>
    /// What the upsert answers: the outcome and the row id, and nothing else (§ARC16.8.1).
    /// </summary>
    public class AssociationSuggestionResult
    {
        public AssociationSuggestionStatus Status { get; set; }
        public Guid AssociationId { get; set; }
    }
}
