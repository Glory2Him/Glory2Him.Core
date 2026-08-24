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
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Glory2Him.WebApp.Tests.Acceptance.Models.BibleReferences;
using Tynamix.ObjectFiller;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.BibleReferences
{
    [Collection(nameof(ApiTestCollection))]
    public partial class BibleReferenceApiTests
    {
        private readonly ApiBroker apiBroker;

        public BibleReferenceApiTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;

            // The acting caller is shared client state, so it is reset here rather than left to
            // whichever test ran last.
            this.apiBroker.ActAsSeededAdministrator();
        }

        private int GetRandomNumber() =>
            new IntRange(min: 2, max: 5).GetValue();

        private static DateTimeOffset GetRandomDateTime() =>
            new DateTimeRange(earliestDate: new DateTime()).GetValue();

        private static string GetRandomStringWithLengthOf(int length)
        {
            string result = new MnemonicString(wordCount: 1, wordMinLength: length, wordMaxLength: length).GetValue();

            return result.Length > length ? result.Substring(0, length) : result;
        }

        private static BibleReference UpdateBibleReferenceWithRandomValues(BibleReference inputBibleReference)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var updatedBibleReference = CreateRandomBibleReference();
            updatedBibleReference.Id = inputBibleReference.Id;

            // USFM is carried forward, unlike Tag.Name and Reaction.Name which the equivalent
            // helpers are free to change. It is the canonical passage key and the foundation
            // pins it against storage on modify, so a helper that randomised it would make every
            // PUT in this suite a 400 — and the failure would look like a broken exposer rather
            // than a fixture choosing an immutable field (design §12.3.1 rule 2a).
            updatedBibleReference.USFM = inputBibleReference.USFM;

            updatedBibleReference.CreatedWhen = inputBibleReference.CreatedWhen;
            updatedBibleReference.CreatedBy = inputBibleReference.CreatedBy;
            updatedBibleReference.UpdatedWhen = now;
            updatedBibleReference.IsDeleted = inputBibleReference.IsDeleted;
            updatedBibleReference.DeletionReason = inputBibleReference.DeletionReason;
            updatedBibleReference.IsPublished = inputBibleReference.IsPublished;
            updatedBibleReference.PublishDate = inputBibleReference.PublishDate;
            updatedBibleReference.ApprovalStatus = inputBibleReference.ApprovalStatus;
            updatedBibleReference.IsApprovedByBypass = inputBibleReference.IsApprovedByBypass;
            updatedBibleReference.ApprovedByBypassReason = inputBibleReference.ApprovedByBypassReason;

            return updatedBibleReference;
        }

        private async ValueTask<BibleReference> PostRandomBibleReferenceAsync()
        {
            BibleReference randomBibleReference = CreateRandomBibleReference();
            BibleReference createdBibleReference = await this.apiBroker.PostBibleReferenceAsync(randomBibleReference);

            return createdBibleReference;
        }

        private async ValueTask<List<BibleReference>> PostRandomBibleReferencesAsync()
        {
            int randomNumber = GetRandomNumber();
            var randomBibleReferences = new List<BibleReference>();

            for (int i = 0; i < randomNumber; i++)
            {
                randomBibleReferences.Add(await PostRandomBibleReferenceAsync());
            }

            return randomBibleReferences;
        }

        private static BibleReference CreateRandomBibleReference() =>
            CreateRandomBibleReferenceFiller().Create();

        private static Filler<BibleReference> CreateRandomBibleReferenceFiller()
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var filler = new Filler<BibleReference>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(now)
                .OnType<DateTimeOffset?>().Use(now)

                // USFM is unique across non-deleted rows and capped at 50, and a new reference
                // must arrive unpublished and in Draft with no bypass — the foundation rejects
                // anything else.
                .OnProperty(bibleReference => bibleReference.USFM)
                    .Use(new Func<string>(GetRandomUsfm))

                // Reference and Translation are required and capped at 255 and 50; Scripture is
                // optional and unbounded. Left to the filler, Translation arrives as a sentence
                // well over its cap and every write is refused.
                .OnProperty(bibleReference => bibleReference.Reference)
                    .Use(new Func<string>(GetRandomReference))
                .OnProperty(bibleReference => bibleReference.Translation)
                    .Use(new Func<string>(GetRandomTranslation))
                .OnProperty(bibleReference => bibleReference.IsPublished).Use(false)
                .OnProperty(bibleReference => bibleReference.PublishDate).Use((DateTimeOffset?)null)
                .OnProperty(bibleReference => bibleReference.ApprovalStatus).Use(ApprovalStatus.Draft)
                .OnProperty(bibleReference => bibleReference.IsApprovedByBypass).Use(false)
                .OnProperty(bibleReference => bibleReference.ApprovedByBypassReason).Use((string)null)
                .OnProperty(bibleReference => bibleReference.IsDeleted).Use(false)
                .OnProperty(bibleReference => bibleReference.DeletionReason).Use((string)null)
                .OnProperty(bibleReference => bibleReference.DeletedBy).Use((string)null)
                .OnProperty(bibleReference => bibleReference.DeletedWhen).Use((DateTimeOffset?)null)

                .OnProperty(bibleReference => bibleReference.CreatedWhen).Use(now)
                .OnProperty(bibleReference => bibleReference.CreatedBy).Use(user)
                .OnProperty(bibleReference => bibleReference.UpdatedWhen).Use(now)
                .OnProperty(bibleReference => bibleReference.UpdatedBy).Use(user);

            return filler;
        }

        /// <summary>
        /// Shaped like a real USFM key — <c>JHN.3.16.NIV</c> — but with a random book segment so
        /// two runs cannot collide on the unique index. Nothing validates that the value parses
        /// as USFM; the rules are "present and at most 50 characters". The shape is here for the
        /// reader, not for the validator.
        /// </summary>
        private static string GetRandomUsfm() =>
            $"{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant()}.3.16.NIV";

        private static string GetRandomReference() =>
            $"Book {Guid.NewGuid().ToString("N").Substring(0, 8)} 3:16";

        private static string GetRandomTranslation() =>
            Guid.NewGuid().ToString("N").Substring(0, 10).ToUpperInvariant();
    }
}
