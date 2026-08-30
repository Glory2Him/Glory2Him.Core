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
        /// <c>EntityType</c> — so two tests running against the same one collide even when
        /// neither means to exercise the conflict.
        ///
        /// <para>The seed data and the rest of the suite use the real entity types, so this hands
        /// out a distinct one per call and the suite never competes with itself. It is an
        /// interlocked counter rather than a random pick because a random one repeats.</para>
        /// </summary>
        private static int scopeCounter = -1;

        private static EntityType GetUnusedEntityType()
        {
            EntityType[] entityTypes = Enum.GetValues<EntityType>();
            int next = Interlocked.Increment(ref scopeCounter);

            return entityTypes[next % entityTypes.Length];
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

        private async ValueTask<List<ApprovalSetting>> PostRandomApprovalSettingsAsync()
        {
            int randomNumber = GetRandomNumber();
            var randomApprovalSettings = new List<ApprovalSetting>();

            for (int i = 0; i < randomNumber; i++)
            {
                randomApprovalSettings.Add(await PostRandomApprovalSettingAsync());
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

                // A per-type DEFAULT row: ContentType null puts it under
                // UX_ApprovalSettings_EntityTypeDefault, and the entity type is handed out one
                // per call so the suite cannot collide with itself.
                .OnProperty(approvalSetting => approvalSetting.EntityType)
                    .Use(new Func<EntityType>(GetUnusedEntityType))
                .OnProperty(approvalSetting => approvalSetting.ContentType)
                    .Use((ContentType?)null)

                // A plausible policy rather than a random one. The filler would otherwise pick a
                // negative RequiredNumberOfApprovals, which is refused, and the reader of a
                // failure would have to work out that the fixture was the problem.
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
