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

using Glory2Him.Core.Brokers.Storages.Sql;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Microsoft.EntityFrameworkCore;

namespace Glory2Him.WebApp.Data
{
    // Idempotent seed for the stored tiers of the approval policy (design §8.4): the GLOBAL
    // default, one entity-type default per EntityType member, and the one narrowing row the
    // house policy needs — personal associations, which open and close their round on submit
    // (§8.5 rules 1 and 6) so a user's own reaction never waits on a review. Every approvable
    // thing then resolves a stated policy rather than falling through to the fail-closed
    // system default. The sibling of ContentItemSettingSeedData, and shaped like it.
    //
    // THE ENUM IS THE SET. Every row carries the same policy, so there is nothing per member to
    // hand-write and the seed walks Enum.GetValues<EntityType>() — the argument SeedData makes for
    // roles. A member added later is seeded on the next start rather than silently resolving a
    // looser policy than its siblings while everyone believes the house policy is in force.
    // That includes Attachment, whose approval path throws NotSupported today: the row governs
    // the path the moment it exists, and a policy that appears with the feature is one nobody
    // remembers to add.
    //
    // REPAIR BY SCOPE, not by id. The probe is the row's own scope — (EntityType, ContentType,
    // IsPersonal) with IsDeleted false — the exact term the tier's unique index constrains, so
    // a soft-deleted default is REPLACED rather than counted as present: §14.5 hides a deleted
    // row from every caller and §8.4 excludes it from resolution, so counting it would leave
    // that scope without a live policy forever and nothing would say so. A LIVE row in the
    // scope is left exactly as it
    // is, whatever its values: administrators are meant to edit these through the admin surface,
    // and a seed that overwrote would revert a deliberate decision on every restart. The seed
    // says so when it finds one that differs, at Information — divergence is legitimate, but an
    // environment quietly running a different policy than the one shipped should never be a
    // surprise.
    //
    // WRITTEN DIRECTLY THROUGH IStorageBroker, bypassing ApprovalSettingService, for the reason
    // ContentItemSettingSeedData states at length: the foundation enforces its Administrators
    // gate by reading the SecurityContext an inbound EventEnvelope carries, and there is no
    // HttpContext at host startup to populate one. These rows are system-authored configuration,
    // not a user action.
    public static class ApprovalSettingSeedData
    {
        private const string SeededBy = "system-seed";

        // The house policy, stated once. RequiredNumberOfApprovals is the one field that differs
        // from ApprovalPolicyDefaults.SystemDefaultFor — the fail-closed fallback §8.4 rule 2
        // applies when NO row exists — which stays at 1. With two required and self-approval
        // off, nothing is approvable until two reviewers who are not the author have approved.
        internal const bool RequireApprovals = true;
        internal const int RequiredNumberOfApprovals = 2;
        internal const bool AutoApproveIfAllApprovalRequirementsMet = false;
        internal const bool AllowSelfApproval = false;
        internal const bool BlockOnReject = true;
        internal const bool BlockOnZeroApprovalScore = true;
        internal const bool RequireReapprovalOnChange = true;
        internal const bool RequireReviewCommentResolutionBeforeApprovals = true;
        internal const bool DoNotAllowBypassingSettings = false;

        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            IServiceProvider services = scope.ServiceProvider;
            var storageBroker = services.GetRequiredService<IStorageBroker>();

            ILogger logger = services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(nameof(ApprovalSettingSeedData));

            IQueryable<ApprovalSetting> existingApprovalSettings =
                await storageBroker.SelectAllApprovalSettingsAsync();

            foreach (ApprovalSetting defaultApprovalSetting in
                BuildDefaultApprovalSettings(DateTimeOffset.UtcNow))
            {
                ApprovalSetting liveDefault = await existingApprovalSettings.FirstOrDefaultAsync(
                    approvalSetting =>
                        approvalSetting.EntityType == defaultApprovalSetting.EntityType
                        && approvalSetting.ContentType == null
                        && approvalSetting.IsPersonal == defaultApprovalSetting.IsPersonal
                        && approvalSetting.IsDeleted == false);

                if (liveDefault is null)
                {
                    await storageBroker.InsertApprovalSettingAsync(defaultApprovalSetting);

                    continue;
                }

                string[] divergingFields = DescribeDivergence(liveDefault, defaultApprovalSetting);

                if (divergingFields.Length > 0)
                {
                    // Logged, never thrown: InitializeCoreAsync swallows after its last attempt,
                    // so a throwing check would silence itself and take the Core endpoints down
                    // with it.
                    logger.LogInformation(
                        "The live approval setting for {Scope} differs from the shipped policy "
                            + "on {Fields}; leaving it as an administrator set it.",
                        DescribeScope(defaultApprovalSetting),
                        string.Join(", ", divergingFields));
                }
            }
        }

        // The stored tiers, least specific first: the global default, one default per
        // EntityType member, and the personal-association row. Internal so the unit test can
        // pin the set to the enum and the values to the reviewed policy without a database.
        internal static IReadOnlyList<ApprovalSetting> BuildDefaultApprovalSettings(
            DateTimeOffset seededWhen)
        {
            var approvalSettings = new List<ApprovalSetting>
            {
                BuildHousePolicy(seededWhen, entityType: null, isPersonal: null)
            };

            approvalSettings.AddRange(
                Enum.GetValues<EntityType>()
                    .Select(entityType =>
                        BuildHousePolicy(seededWhen, entityType, isPersonal: null)));

            // A PERSONAL association — a user's own reaction, a tag they keep for themselves
            // (§4.2: UserId set) — is not editorial content and waits on nobody. The round
            // still opens; it closes itself on submission (§8.5 rules 1 and 6), so §9.8 holds
            // and nothing skips the approval record.
            //
            // EVERY GATE OFF, not only the approval count. A gate is a reason to hold a round
            // shut for a reviewer to look at, and this round has no reviewer: a rejection
            // nobody will file, a comment nobody will settle, a re-approval nobody will give
            // and a score nobody will weigh would each leave a user's own reaction stuck on a
            // condition the policy never meant to ask. Bypass stays permitted, as everywhere.
            ApprovalSetting personalAssociations =
                BuildHousePolicy(seededWhen, EntityType.Association, isPersonal: true);

            personalAssociations.RequireApprovals = false;
            personalAssociations.AutoApproveIfAllApprovalRequirementsMet = true;
            personalAssociations.BlockOnReject = false;
            personalAssociations.BlockOnZeroApprovalScore = false;
            personalAssociations.RequireReapprovalOnChange = false;
            personalAssociations.RequireReviewCommentResolutionBeforeApprovals = false;
            approvalSettings.Add(personalAssociations);

            return approvalSettings;
        }

        private static ApprovalSetting BuildHousePolicy(
            DateTimeOffset seededWhen,
            EntityType? entityType,
            bool? isPersonal) =>
            new ApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                ContentType = null,
                IsPersonal = isPersonal,
                RequireApprovals = RequireApprovals,
                RequiredNumberOfApprovals = RequiredNumberOfApprovals,
                AutoApproveIfAllApprovalRequirementsMet = AutoApproveIfAllApprovalRequirementsMet,
                AllowSelfApproval = AllowSelfApproval,
                BlockOnReject = BlockOnReject,
                BlockOnZeroApprovalScore = BlockOnZeroApprovalScore,
                RequireReapprovalOnChange = RequireReapprovalOnChange,

                RequireReviewCommentResolutionBeforeApprovals =
                    RequireReviewCommentResolutionBeforeApprovals,

                DoNotAllowBypassingSettings = DoNotAllowBypassingSettings,
                IsDeleted = false,
                DeletedBy = null,
                DeletedWhen = null,
                DeletionReason = null,
                CreatedBy = SeededBy,
                CreatedWhen = seededWhen,
                UpdatedBy = SeededBy,
                UpdatedWhen = seededWhen
            };

        internal static string DescribeScope(ApprovalSetting approvalSetting) =>
            approvalSetting.EntityType is null
                ? "every entity type"
                : approvalSetting.IsPersonal is null
                    ? approvalSetting.EntityType.Value.ToString()
                    : $"{approvalSetting.EntityType.Value} "
                        + (approvalSetting.IsPersonal.Value ? "(personal)" : "(editorial)");

        // The nine policy fields, by name, where the live row disagrees with the shipped one.
        // Scope and audit fields are not policy and are not compared.
        internal static string[] DescribeDivergence(ApprovalSetting live, ApprovalSetting shipped)
        {
            (string Name, bool Differs)[] comparisons =
            [
                (nameof(ApprovalSetting.RequireApprovals),
                    live.RequireApprovals != shipped.RequireApprovals),

                (nameof(ApprovalSetting.RequiredNumberOfApprovals),
                    live.RequiredNumberOfApprovals != shipped.RequiredNumberOfApprovals),

                (nameof(ApprovalSetting.AutoApproveIfAllApprovalRequirementsMet),
                    live.AutoApproveIfAllApprovalRequirementsMet
                        != shipped.AutoApproveIfAllApprovalRequirementsMet),

                (nameof(ApprovalSetting.AllowSelfApproval),
                    live.AllowSelfApproval != shipped.AllowSelfApproval),

                (nameof(ApprovalSetting.BlockOnReject),
                    live.BlockOnReject != shipped.BlockOnReject),

                (nameof(ApprovalSetting.BlockOnZeroApprovalScore),
                    live.BlockOnZeroApprovalScore != shipped.BlockOnZeroApprovalScore),

                (nameof(ApprovalSetting.RequireReapprovalOnChange),
                    live.RequireReapprovalOnChange != shipped.RequireReapprovalOnChange),

                (nameof(ApprovalSetting.RequireReviewCommentResolutionBeforeApprovals),
                    live.RequireReviewCommentResolutionBeforeApprovals
                        != shipped.RequireReviewCommentResolutionBeforeApprovals),

                (nameof(ApprovalSetting.DoNotAllowBypassingSettings),
                    live.DoNotAllowBypassingSettings != shipped.DoNotAllowBypassingSettings),
            ];

            return comparisons
                .Where(comparison => comparison.Differs)
                .Select(comparison => comparison.Name)
                .ToArray();
        }
    }
}
