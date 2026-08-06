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

namespace Glory2Him.Core.Models.Bases
{
    /// <summary>
    /// A confidence score, its reason, and the provenance of both (design §9.7.1 rule 5).
    ///
    /// <para><b>All four fields are written together, as one unit.</b> A human correcting a
    /// machine score must clear <see cref="SourceBatchId"/> and <see cref="ModelVersion"/> in
    /// the same write, or the row claims a model produced a score a publisher typed — and
    /// would then be swept up by a retraction targeting that model.</para>
    /// </summary>
    public interface IConfidence
    {
        /// <summary>
        /// Strength of the assertion, 0.00 – 10.00. Persisted as <c>decimal(4,2)</c> so a
        /// process may estimate to two decimal places and fractional thresholds such as 7.5
        /// are expressible (design §13.5). Null means not yet scored — which never blocks
        /// approval; only an explicit zero does (design §8.5 rule 8).
        /// </summary>
        decimal? ConfidenceScore { get; set; }

        /// <summary>
        /// Why the score is what it is. Max 500 characters.
        /// </summary>
        string? ConfidenceReason { get; set; }

        /// <summary>
        /// The producer run that wrote the score. Null when a human set it by hand — which
        /// is what makes a machine-written score distinguishable from a human one, and what
        /// a bulk retraction targets (design §13.4).
        /// </summary>
        Guid? SourceBatchId { get; set; }

        /// <summary>
        /// The model that produced the score, for example
        /// <c>Mistral_7B_Instruct_Q8_0_v0.3</c>. Null when a human set it by hand. Written
        /// from a constant held by the producer, never hand-typed — an inconsistently
        /// spelled value silently drops rows out of the retraction query that exists to
        /// catch them.
        /// </summary>
        string? ModelVersion { get; set; }
    }
}
