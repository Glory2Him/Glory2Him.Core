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
    /// <para><b>Why this needs a test at all.</b> A scope with no live row fails SILENTLY: §8.4
    /// resolution falls through to the next tier, and past the global default to the
    /// fail-closed system default, which requires one approval where the house policy requires
    /// two — and nothing logs the difference. The seed is the only thing that puts the rows
    /// there, so the seeded set is the last place anyone would look when an entity type turns
    /// out to be approvable on one vote, or a user's own reaction turns out to be waiting on
    /// two.</para>
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

        private static IEnumerable<ApprovalSetting> HousePolicyRows() =>
            BuildSeed().Where(approvalSetting => approvalSetting.IsPersonal != true);

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
                        && approvalSetting.ContentType == null
                        && approvalSetting.IsPersonal == null,
                    because:
                        $"{entityType} must resolve a stated policy rather than fall through "
                            + "the tiers (§8.4)");
            }
        }

        /// <summary>
        /// The tier every entity-type default narrows, and the last stored row before the
        /// fail-closed system default (§8.4). One, and exactly one.
        /// </summary>
        [Fact]
        public void ShouldSeedTheGlobalDefault()
        {
            // when
            IReadOnlyList<ApprovalSetting> seededApprovalSettings = BuildSeed();

            // then
            seededApprovalSettings.Should().ContainSingle(
                approvalSetting =>
                    approvalSetting.EntityType == null
                    && approvalSetting.ContentType == null
                    && approvalSetting.IsPersonal == null);
        }

        /// <summary>
        /// A user's own reaction is not editorial content and waits on nobody (§4.2, §8.4).
        /// The round still opens — it closes itself on submission (§8.5 rules 1 and 6) — so the
        /// row says "no approvals required" AND "approve automatically", never one without the
        /// other: the first alone leaves a round open for a human click nobody will make, and the
        /// second alone auto-approves nothing because the conditions are never met.
        ///
        /// <para>And every gate is off. A gate holds a round shut for a reviewer, and this round
        /// has none — a standing rejection, an unsettled comment, a zero score or an edit would
        /// each leave the reaction stuck on a condition nobody is there to clear.</para>
        /// </summary>
        [Fact]
        public void ShouldSeedPersonalAssociationsAsApprovedOnSubmissionWithNoGate()
        {
            // when
            IReadOnlyList<ApprovalSetting> seededApprovalSettings = BuildSeed();

            // then
            ApprovalSetting personalAssociations = seededApprovalSettings.Should().ContainSingle(
                approvalSetting =>
                    approvalSetting.EntityType == EntityType.Association
                    && approvalSetting.IsPersonal == true)
                .Subject;

            personalAssociations.ContentType.Should().BeNull();
            personalAssociations.RequireApprovals.Should().BeFalse();
            personalAssociations.AutoApproveIfAllApprovalRequirementsMet.Should().BeTrue();
            personalAssociations.BlockOnReject.Should().BeFalse();
            personalAssociations.BlockOnZeroApprovalScore.Should().BeFalse();
            personalAssociations.RequireReapprovalOnChange.Should().BeFalse();
            personalAssociations.RequireReviewCommentResolutionBeforeApprovals.Should().BeFalse();
            personalAssociations.AllowSelfApproval.Should().BeFalse();
            personalAssociations.DoNotAllowBypassingSettings.Should().BeFalse();
        }

        /// <summary>
        /// Editorial associations take the house policy through the Association default; no
        /// separate editorial row is shipped, so an administrator narrowing one later narrows a
        /// row that does not yet exist rather than editing one the seed will fight over.
        /// </summary>
        [Fact]
        public void ShouldSeedNoEditorialAssociationRow()
        {
            // when
            IReadOnlyList<ApprovalSetting> seededApprovalSettings = BuildSeed();

            // then
            seededApprovalSettings.Should().NotContain(
                approvalSetting => approvalSetting.IsPersonal == false);
        }

        /// <summary>
        /// No content-type row, and no personality on anything but Association: a content-type
        /// row is a narrower policy an administrator chooses, and the check constraints refuse
        /// the rest — a seed that tripped one would take Core initialisation down.
        /// </summary>
        [Fact]
        public void ShouldSeedNoScopeTheStoreWouldRefuse()
        {
            // when
            IReadOnlyList<ApprovalSetting> seededApprovalSettings = BuildSeed();

            // then
            seededApprovalSettings.Should().OnlyContain(
                approvalSetting => approvalSetting.ContentType == null);

            seededApprovalSettings.Should().OnlyContain(
                approvalSetting =>
                    approvalSetting.IsPersonal == null
                    || approvalSetting.EntityType == EntityType.Association);

            seededApprovalSettings.Should().HaveCount(Enum.GetValues<EntityType>().Length + 2);
        }

        [Fact]
        public void ShouldSeedTheReviewedPolicyOnEveryHousePolicyRow()
        {
            // when
            IEnumerable<ApprovalSetting> housePolicyRows = HousePolicyRows();

            // then
            foreach (ApprovalSetting approvalSetting in housePolicyRows)
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
                IsPersonal = shipped.IsPersonal,
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
            List<ApprovalSetting> housePolicyRows = HousePolicyRows().ToList();
            ApprovalSetting shipped = housePolicyRows[0];
            ApprovalSetting live = housePolicyRows[1];

            // when: a different row with the same policy
            string[] divergingFields = ApprovalSettingSeedData.DescribeDivergence(live, shipped);

            // then
            divergingFields.Should().BeEmpty();
        }

        /// <summary>
        /// The log line names the scope a reader can find in the admin surface, not a Guid.
        /// </summary>
        [Theory]
        [InlineData(null, null, "every entity type")]
        [InlineData(EntityType.Tag, null, "Tag")]
        [InlineData(EntityType.Association, true, "Association (personal)")]
        [InlineData(EntityType.Association, false, "Association (editorial)")]
        public void ShouldDescribeAScopeByWhatItGoverns(
            EntityType? entityType,
            bool? isPersonal,
            string expectedDescription)
        {
            // given
            var approvalSetting = new ApprovalSetting
            {
                Id = Guid.NewGuid(),
                EntityType = entityType,
                ContentType = null,
                IsPersonal = isPersonal
            };

            // when
            string actualDescription = ApprovalSettingSeedData.DescribeScope(approvalSetting);

            // then
            actualDescription.Should().Be(expectedDescription);
        }
    }
}
