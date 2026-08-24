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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using CoreBibleReference = Glory2Him.Core.Models.Foundations.BibleReferences.BibleReference;

namespace Glory2Him.WebApp.Tests.Acceptance.Brokers
{
    /// <summary>
    /// BibleReference rows arranged and torn down beneath HTTP, for state no endpoint can
    /// produce — the sibling of <c>ApiBroker.TagArrangements.cs</c> and
    /// <c>ApiBroker.ReactionArrangements.cs</c>, and the same shape for the same reasons.
    /// </summary>
    public partial class ApiBroker
    {
        public async ValueTask<CoreBibleReference> InsertSubmittedBibleReferenceAsync(string authorUserId)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;

            var bibleReference = new CoreBibleReference
            {
                Id = Guid.NewGuid(),

                // UX_BibleReferences_USFM is unique but IS filtered on IsDeleted, unlike the
                // Tag and Reaction name indexes (#201) — so a soft-deleted key is genuinely
                // released. A fresh guid per arrangement is still used so concurrent runs cannot
                // collide on a live key.
                USFM = $"{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}.3.16.NIV",

                // Required, capped at 255 and 50 (BibleReferenceService.Validations). Scripture
                // is optional and is left unset.
                Reference = $"Book {Guid.NewGuid().ToString("N").Substring(0, 8)} 3:16",
                Translation = Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant(),

                ApprovalStatus = ApprovalStatus.Submitted,
                IsPublished = false,
                IsDeleted = false,
                CreatedBy = authorUserId,
                CreatedWhen = now,
                UpdatedBy = authorUserId,
                UpdatedWhen = now
            };

            return await this.storageBroker.InsertBibleReferenceAsync(bibleReference);
        }

        public async ValueTask<CoreBibleReference> GetCoreBibleReferenceByIdAsync(Guid bibleReferenceId) =>
            await this.storageBroker.SelectBibleReferenceByIdAsync(bibleReferenceId);

        public async ValueTask RemoveCoreBibleReferenceAsync(CoreBibleReference bibleReference) =>
            await this.storageBroker.DeleteBibleReferenceAsync(bibleReference);

        /// <summary>
        /// Physically removes a bibleReference if it is still there, whatever state it is in. Every
        /// acceptance test finishes with this so the database is left as it was found: the API's
        /// own delete is a SOFT delete, so a test that tore down through the endpoint still left
        /// its row behind, and a test whose assertion threw left a live one.
        /// </summary>
        public async ValueTask RemoveCoreBibleReferenceByIdAsync(Guid bibleReferenceId)
        {
            CoreBibleReference storedBibleReference =
                await this.storageBroker.SelectBibleReferenceByIdAsync(bibleReferenceId);

            if (storedBibleReference is not null)
            {
                await this.storageBroker.DeleteBibleReferenceAsync(storedBibleReference);
            }
        }
    }
}
