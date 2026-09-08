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

using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Glory2Him.Core.Migrations;
using Glory2Him.Core.Models.Foundations.ApprovalSettings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Glory2Him.Core.Tests.Unit.Brokers.Storages
{
    /// <summary>
    /// A guard on the three check constraints that keep an approval policy's confidence
    /// thresholds meaningful (§8.6.2), and on the migration that creates them.
    ///
    /// <para><b>Why the constraints need a guard.</b> Both thresholds are values on
    /// <c>ConfidenceScore</c>'s own 0.00–10.00 scale (§13.5), and the column type is not that
    /// scale — <c>decimal(4,2)</c> admits -99.99 through 99.99. The service refuses an off-scale
    /// or inverted pair first, but the service is not the only writer a database has, so these
    /// are the defence in depth §14.6 rule 2 asks for. Nothing else in the suite would notice if
    /// one were dropped: the storage broker is mocked everywhere above this layer, and
    /// <c>has-pending-model-changes</c> detects a model the migrations do not match, not a model
    /// that is missing a rule.</para>
    ///
    /// <para><b>Why the SQL is pinned as text.</b> The model configuration builds each string by
    /// interpolating <c>nameof</c>, the migration writes the same string as a bare literal, and
    /// EF compares them as text — a rename that updates one side and not the other produces a
    /// migration that is silently pending forever. This asserts the two sides read the same.</para>
    /// </summary>
    public class StorageBrokerApprovalSettingConstraintTests
    {
        private const string ApprovalSettingsTableName = "ApprovalSettings";
        private const string RejectionThresholdRangeName = "CK_ApprovalSetting_AIRejectionThresholdRange";
        private const string ApprovalThresholdRangeName = "CK_ApprovalSetting_AIApprovalThresholdRange";
        private const string ThresholdOrderName = "CK_ApprovalSetting_AIThresholdOrder";

        private const string RejectionThresholdRangeSql =
            "(AIApprovalConfidenceRejectionThreshold BETWEEN 0 AND 10)";

        private const string ApprovalThresholdRangeSql =
            "(AIApprovalConfidenceApprovalThreshold BETWEEN 0 AND 10)";

        // `<=`, not `<`: §8.6.2's rules 1 and 2 are strict comparisons on either side of the
        // pair, so an equal pair only closes the middle band and leaves the two verdicts
        // disjoint. It is also the pair ApprovalPolicyDefaults ships as its fail-closed fallback,
        // which a strict rule would make unstorable.
        private const string ThresholdOrderSql =
            "(AIApprovalConfidenceRejectionThreshold <= AIApprovalConfidenceApprovalThreshold)";

        [Fact]
        public void ShouldConstrainBothThresholdsToTheConfidenceScale()
        {
            // given
            // THE DESIGN-TIME MODEL, not the runtime one the index guards read. EF strips a
            // check constraint out of the read-optimized model — it is nothing query execution
            // needs — and asking the stripped model for one throws rather than answering empty.
            IEntityType approvalSettingEntityType =
                StorageBrokerModelSource.DesignTimeModel.FindEntityType(typeof(ApprovalSetting));

            // when
            Dictionary<string, string> actualCheckConstraints = approvalSettingEntityType
                .GetCheckConstraints()
                .ToDictionary(
                    checkConstraint => checkConstraint.Name,
                    checkConstraint => checkConstraint.Sql);

            // then
            actualCheckConstraints.Should().Contain(
                RejectionThresholdRangeName, RejectionThresholdRangeSql);

            actualCheckConstraints.Should().Contain(
                ApprovalThresholdRangeName, ApprovalThresholdRangeSql);

            actualCheckConstraints.Should().Contain(
                ThresholdOrderName, ThresholdOrderSql);
        }

        /// <summary>
        /// A migration creates the same three constraints, word for word — and it is the LATER
        /// one, not the migration that added the four Berean columns. That one is already recorded
        /// as applied wherever the WebApp has started (<c>Database.Migrate()</c>) or the
        /// Development deployment has run, and a recorded id is never re-executed, so a constraint
        /// added to it would exist on no database that has already seen it.
        ///
        /// <para>One constraint per fault rather than one combined check, because a
        /// check-constraint violation reaches the caller naming no column — the constraint name in
        /// the SQL error is the only diagnostic a raw-SQL writer gets.</para>
        /// </summary>
        [Fact]
        public void ShouldCreateTheThresholdConstraintsInTheCorrectiveMigration()
        {
            // given
            var migration = new AdoptHousePolicyAndConstrainApprovalSettingAIThresholds();

            // when
            Dictionary<string, string> addedCheckConstraints = migration.UpOperations
                .OfType<AddCheckConstraintOperation>()
                .Where(operation => operation.Table == ApprovalSettingsTableName)
                .ToDictionary(
                    operation => operation.Name,
                    operation => operation.Sql);

            // then
            addedCheckConstraints.Should().Contain(
                RejectionThresholdRangeName, RejectionThresholdRangeSql);

            addedCheckConstraints.Should().Contain(
                ApprovalThresholdRangeName, ApprovalThresholdRangeSql);

            addedCheckConstraints.Should().Contain(
                ThresholdOrderName, ThresholdOrderSql);
        }

        /// <summary>
        /// The columns arrived with a backfill of 0.00 — the CLR default, not a policy — and the
        /// corrective migration moves the rows still holding that pair onto the seeded house
        /// policy. It has to: <c>ApprovalSettingSeedData</c> leaves a live row exactly as an
        /// administrator set it and logs only the fields that differ from the shipped policy, so a
        /// row left on 0.00/0.00 reports both thresholds as administrator-diverged on every start
        /// of every upgraded deployment — for values nobody ever chose.
        ///
        /// <para>Narrowed to that pair, which is the assertion with teeth here: a row an
        /// administrator has genuinely set is theirs, and the divergence log exists to report
        /// exactly that. Core cannot reference the WebApp seed, so the literals are duplicated
        /// across that boundary deliberately. This pins the migration's side of the pair;
        /// <c>ApprovalSettingSeedTests</c> pins the seed's. Moving one without the other reddens
        /// rather than drifting quietly.</para>
        /// </summary>
        [Fact]
        public void ShouldMoveTheBackfilledThresholdPairOntoTheSeededHousePolicy()
        {
            // given
            var migration = new AdoptHousePolicyAndConstrainApprovalSettingAIThresholds();

            // when
            string actualRepairSql = migration.UpOperations
                .OfType<SqlOperation>()
                .Single()
                .Sql;

            // then
            actualRepairSql.Should().Contain(
                $"UPDATE [{ApprovalSettingsTableName}]");

            actualRepairSql.Should().Contain(
                $"SET [{nameof(ApprovalSetting.AIApprovalConfidenceRejectionThreshold)}] = 2.50");

            actualRepairSql.Should().Contain(
                $"[{nameof(ApprovalSetting.AIApprovalConfidenceApprovalThreshold)}] = 7.50");

            actualRepairSql.Should().Contain(
                $"WHERE [{nameof(ApprovalSetting.AIApprovalConfidenceRejectionThreshold)}] = 0.00");

            actualRepairSql.Should().Contain(
                $"AND [{nameof(ApprovalSetting.AIApprovalConfidenceApprovalThreshold)}] = 0.00");
        }
    }
}
