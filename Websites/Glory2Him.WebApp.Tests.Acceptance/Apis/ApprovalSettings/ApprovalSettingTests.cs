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
using System.Threading;
using System.Threading.Tasks;
using Glory2Him.Core.Models.Enums;
using Glory2Him.WebApp.Tests.Acceptance.Brokers;
using Glory2Him.WebApp.Tests.Acceptance.Models.ApprovalSettings;
using Tynamix.ObjectFiller;
using CoreApprovalSetting = Glory2Him.Core.Models.Foundations.ApprovalSettings.ApprovalSetting;

namespace Glory2Him.WebApp.Tests.Acceptance.Apis.ApprovalSettings
{
    [Collection(nameof(ApiTestCollection))]
    public partial class ApprovalSettingApiTests
    {
        private readonly ApiBroker apiBroker;

        public ApprovalSettingApiTests(ApiBroker apiBroker)
        {
            this.apiBroker = apiBroker;

            // The acting caller is shared client state, so it is reset here rather than left to
            // whichever test ran last. Every write on this exposer is Administrators-only (§14.7 posture
            // C), and the seeded administrator is the only caller who holds it.
            this.apiBroker.ActAsSeededAdministrator();
        }

        /// <summary>
        /// Every scope this suite writes to has to be free, and both unique indexes are keyed on
        /// the scope — so two tests writing the same one collide even when neither means to
        /// exercise the conflict.
        ///
        /// <para><b>The default tier is fully taken.</b> <c>ApprovalSettingSeedData</c> seeds one
        /// live default per <c>EntityType</c> member at startup, so every slot under
        /// <c>UX_ApprovalSettings_EntityTypeDefault</c> is occupied before the first test runs —
        /// the same move that pushed the ContentItemSettings suite off its default tier. The
        /// filler therefore writes the CONTENT-TYPE tier instead: <c>ContentItem</c> paired with
        /// a content type handed out one per call, which is the only pairing
        /// <c>CK_ApprovalSetting_ContentTypeRequiresContentItem</c> admits. Nine slots, and the
        /// suite holds at most four at once under its single collection, so it never competes
        /// with itself. An interlocked counter rather than a random pick because a random one
        /// repeats.</para>
        ///
        /// <para>The test that needs the default tier itself borrows a seeded row and puts it
        /// back — see <c>ShouldAllowPostWhenEntityTypeDefaultIsHeldOnlyByASoftDeletedRowAsync</c>.</para>
        /// </summary>
        private static int scopeCounter = -1;

        private static ContentType GetUnusedContentType()
        {
            ContentType[] contentTypes = Enum.GetValues<ContentType>();
            int next = Interlocked.Increment(ref scopeCounter);

            return contentTypes[next % contentTypes.Length];
        }

        /// <summary>
        /// A soft-deleted predecessor for a seeded default's scope, arranged beneath HTTP so the
        /// index's <c>IsDeleted</c> term can be asserted while the seeded row is lifted out.
        /// </summary>
        private static CoreApprovalSetting CreateSoftDeletedCoreDefaultApprovalSetting(
            EntityType entityType)
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;

            return new CoreApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                ContentType = null,
                RequireApprovals = true,
                RequiredNumberOfApprovals = 1,
                IsDeleted = true,
                DeletedBy = user,
                DeletedWhen = now,
                DeletionReason = "Arranged by the acceptance suite.",
                CreatedBy = user,
                CreatedWhen = now,
                UpdatedBy = user,
                UpdatedWhen = now
            };
        }

        private int GetRandomNumber() =>
            new IntRange(min: 2, max: 5).GetValue();

        private static ApprovalSetting UpdateApprovalSettingWithRandomValues(
            ApprovalSetting inputApprovalSetting)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var updatedApprovalSetting = CreateRandomApprovalSetting();
            updatedApprovalSetting.Id = inputApprovalSetting.Id;

            // The scope is carried forward. It is not caller-editable in any meaningful sense —
            // moving a row to another scope would be creating a different policy — and randomising
            // it would collide with whatever already occupies the target.
            updatedApprovalSetting.EntityType = inputApprovalSetting.EntityType;
            updatedApprovalSetting.ContentType = inputApprovalSetting.ContentType;

            updatedApprovalSetting.CreatedWhen = inputApprovalSetting.CreatedWhen;
            updatedApprovalSetting.CreatedBy = inputApprovalSetting.CreatedBy;
            updatedApprovalSetting.UpdatedWhen = now;
            updatedApprovalSetting.IsDeleted = inputApprovalSetting.IsDeleted;
            updatedApprovalSetting.DeletionReason = inputApprovalSetting.DeletionReason;

            return updatedApprovalSetting;
        }

        private async ValueTask<ApprovalSetting> PostRandomApprovalSettingAsync()
        {
            ApprovalSetting randomApprovalSetting = CreateRandomApprovalSetting();

            ApprovalSetting createdApprovalSetting =
                await this.apiBroker.PostApprovalSettingAsync(randomApprovalSetting);

            return createdApprovalSetting;
        }

        /// <summary>
        /// Leak-safe: a post that fails part-way tears down what it already posted before it
        /// rethrows. The content-type tier is nine fixed slots shared by the whole run, so a
        /// stranded row does not fail the test that stranded it — it fails whichever later test
        /// draws that slot, and the failure reads as an exposer regression.
        /// </summary>
        private async ValueTask<List<ApprovalSetting>> PostRandomApprovalSettingsAsync()
        {
            int randomNumber = GetRandomNumber();
            var randomApprovalSettings = new List<ApprovalSetting>();

            try
            {
                for (int i = 0; i < randomNumber; i++)
                {
                    randomApprovalSettings.Add(await PostRandomApprovalSettingAsync());
                }
            }
            catch
            {
                foreach (ApprovalSetting postedApprovalSetting in randomApprovalSettings)
                {
                    await this.apiBroker.RemoveCoreApprovalSettingByIdAsync(
                        postedApprovalSetting.Id);
                }

                throw;
            }

            return randomApprovalSettings;
        }

        private static ApprovalSetting CreateRandomApprovalSetting() =>
            CreateRandomApprovalSettingFiller().Create();

        private static Filler<ApprovalSetting> CreateRandomApprovalSettingFiller()
        {
            string user = Guid.NewGuid().ToString();
            DateTimeOffset now = DateTimeOffset.UtcNow;
            var filler = new Filler<ApprovalSetting>();

            filler.Setup()
                .OnType<DateTimeOffset>().Use(now)
                .OnType<DateTimeOffset?>().Use(now)

                // A CONTENT-TYPE row: ContentItem paired with a content type handed out one per
                // call puts it under UX_ApprovalSettings_EntityTypeContentType, the one tier the
                // seed leaves free — see GetUnusedContentType.
                .OnProperty(approvalSetting => approvalSetting.EntityType)
                    .Use(EntityType.ContentItem)
                .OnProperty(approvalSetting => approvalSetting.ContentType)
                    .Use(new Func<ContentType?>(() => GetUnusedContentType()))

                // A plausible policy rather than a random one, so the reader of a failure never
                // has to work out that the fixture was the problem.
                .OnProperty(approvalSetting => approvalSetting.RequireApprovals).Use(true)
                .OnProperty(approvalSetting => approvalSetting.RequiredNumberOfApprovals).Use(1)

                .OnProperty(approvalSetting => approvalSetting.IsDeleted).Use(false)
                .OnProperty(approvalSetting => approvalSetting.DeletionReason).Use((string)null)
                .OnProperty(approvalSetting => approvalSetting.DeletedBy).Use((string)null)
                .OnProperty(approvalSetting => approvalSetting.DeletedWhen).Use((DateTimeOffset?)null)

                .OnProperty(approvalSetting => approvalSetting.CreatedWhen).Use(now)
                .OnProperty(approvalSetting => approvalSetting.CreatedBy).Use(user)
                .OnProperty(approvalSetting => approvalSetting.UpdatedWhen).Use(now)
                .OnProperty(approvalSetting => approvalSetting.UpdatedBy).Use(user);

            return filler;
        }
    }
}
