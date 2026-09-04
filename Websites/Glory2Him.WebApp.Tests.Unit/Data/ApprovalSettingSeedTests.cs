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
using System.Linq;
using FluentAssertions;
using Glory2Him.Core.Models.Enums;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Glory2Him.WebApp.Data;

namespace Glory2Him.WebApp.Tests.Unit.Data
{
    /// <summary>
    /// Pins the seeded approval policy to the enum it is composed from, and to the reviewed
    /// values.
    ///
    /// <para><b>Why this needs a test at all.</b> An entity type with no live default fails
    /// SILENTLY: §8.4 resolution falls through to the fail-closed system default, which requires
    /// one approval where the house policy requires two, and nothing logs the difference. The
    /// seed is the only thing that puts the row there, so the seeded set is the last place anyone
    /// would look when an entity type turns out to be approvable on one vote.</para>
    ///
    /// <para>Three of the nine values differ from the entity's own C# defaults
    /// (RequiredNumberOfApprovals, BlockOnReject, BlockOnZeroApprovalScore). A builder that
    /// dropped any one of them would still produce a row, so each value is asserted against the
    /// reviewed constant rather than against the entity default it might have fallen back to.</para>
    /// </summary>
    public class ApprovalSettingSeedTests
    {
        private static readonly DateTimeOffset SeededWhen =
            new(2026, 9, 4, 9, 0, 0, TimeSpan.Zero);

        private static IReadOnlyList<ApprovalSetting> BuildSeed() =>
            ApprovalSettingSeedData.BuildDefaultApprovalSettings(SeededWhen);

        [Fact]
        public void ShouldSeedOneDefaultForEveryEntityType()
        {
            // given
            EntityType[] expectedEntityTypes = Enum.GetValues<EntityType>();

            // when
            IReadOnlyList<ApprovalSetting> seededApprovalSettings = BuildSeed();

            // then
            foreach (EntityType entityType in expectedEntityTypes)
            {
                seededApprovalSettings.Should().ContainSingle(
                    approvalSetting =>
                        approvalSetting.EntityType == entityType
                        && approvalSetting.ContentType == null,
                    because:
                        $"{entityType} must resolve a stated policy rather than the "
                            + "fail-closed system default (§8.4)");
            }

            seededApprovalSettings.Should().HaveCount(expectedEntityTypes.Length);
        }

        /// <summary>
        /// The default tier only. A content-type row is a narrower policy an administrator
        /// chooses, not something shipped — and ContentType is legal on ContentItem alone.
        /// </summary>
        [Fact]
        public void ShouldSeedNoContentTypeScopedRow()
        {
            // when
            IReadOnlyList<ApprovalSetting> seededApprovalSettings = BuildSeed();

            // then
            seededApprovalSettings.Should().OnlyContain(
                approvalSetting => approvalSetting.ContentType == null);
        }

        [Fact]
        public void ShouldSeedTheReviewedPolicyOnEveryRow()
        {
            // when
            IReadOnlyList<ApprovalSetting> seededApprovalSettings = BuildSeed();

            // then
            foreach (ApprovalSetting approvalSetting in seededApprovalSettings)
            {
                approvalSetting.RequireApprovals.Should().BeTrue();
                approvalSetting.RequiredNumberOfApprovals.Should().Be(2);
                approvalSetting.AutoApproveIfAllApprovalRequirementsMet.Should().BeFalse();
                approvalSetting.AllowSelfApproval.Should().BeFalse();
                approvalSetting.BlockOnReject.Should().BeTrue();
                approvalSetting.BlockOnZeroApprovalScore.Should().BeTrue();
                approvalSetting.RequireReapprovalOnChange.Should().BeTrue();
                approvalSetting.RequireReviewCommentResolutionBeforeApprovals.Should().BeTrue();
                approvalSetting.DoNotAllowBypassingSettings.Should().BeFalse();
            }
        }

        /// <summary>
        /// Live, and authored by the seed. "system-seed" rather than the runtime system actor:
        /// the distinction is deliberate — one of them predates the row.
        /// </summary>
        [Fact]
        public void ShouldSeedLiveRowsStampedBySeed()
        {
            // when
            IReadOnlyList<ApprovalSetting> seededApprovalSettings = BuildSeed();

            // then
            seededApprovalSettings.Should().OnlyContain(approvalSetting =>
                approvalSetting.IsDeleted == false
                && approvalSetting.DeletedBy == null
                && approvalSetting.DeletedWhen == null
                && approvalSetting.DeletionReason == null
                && approvalSetting.CreatedBy == "system-seed"
                && approvalSetting.UpdatedBy == "system-seed"
                && approvalSetting.CreatedWhen == SeededWhen
                && approvalSetting.UpdatedWhen == SeededWhen);

            seededApprovalSettings.Select(approvalSetting => approvalSetting.Id)
                .Should().OnlyHaveUniqueItems();

            seededApprovalSettings.Should().NotContain(
                approvalSetting => approvalSetting.Id == Guid.Empty);
        }

        /// <summary>
        /// The divergence report names exactly the policy fields that differ, and nothing else —
        /// a live row an administrator has changed is described, not overwritten.
        /// </summary>
        [Fact]
        public void ShouldNamePolicyFieldsThatDivergeAndNothingElse()
        {
            // given
            ApprovalSetting shipped = BuildSeed().First();

            var live = new ApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = shipped.EntityType,
                ContentType = shipped.ContentType,
                RequireApprovals = shipped.RequireApprovals,
                RequiredNumberOfApprovals = 1,
                AutoApproveIfAllApprovalRequirementsMet = shipped.AutoApproveIfAllApprovalRequirementsMet,
                AllowSelfApproval = shipped.AllowSelfApproval,
                BlockOnReject = shipped.BlockOnReject,
                BlockOnZeroApprovalScore = false,
                RequireReapprovalOnChange = shipped.RequireReapprovalOnChange,
                RequireReviewCommentResolutionBeforeApprovals = shipped.RequireReviewCommentResolutionBeforeApprovals,
                DoNotAllowBypassingSettings = shipped.DoNotAllowBypassingSettings,
                CreatedBy = "an-administrator",
                CreatedWhen = SeededWhen.AddDays(3),
                UpdatedBy = "an-administrator",
                UpdatedWhen = SeededWhen.AddDays(4)
            };

            // when
            string[] divergingFields = ApprovalSettingSeedData.DescribeDivergence(live, shipped);

            // then
            divergingFields.Should().BeEquivalentTo(
                nameof(ApprovalSetting.RequiredNumberOfApprovals),
                nameof(ApprovalSetting.BlockOnZeroApprovalScore));
        }

        [Fact]
        public void ShouldReportNoDivergenceForARowMatchingTheShippedPolicy()
        {
            // given
            IReadOnlyList<ApprovalSetting> seededApprovalSettings = BuildSeed();
            ApprovalSetting shipped = seededApprovalSettings[0];
            ApprovalSetting live = seededApprovalSettings[1];

            // when: a different row with the same policy
            string[] divergingFields = ApprovalSettingSeedData.DescribeDivergence(live, shipped);

            // then
            divergingFields.Should().BeEmpty();
        }
    }
}
