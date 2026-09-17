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

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <summary>
    /// Declares in the database what <c>AIReviewerAssignment.ApprovalId</c> has always meant: the
    /// round it names has to exist. A SEPARATE migration rather than an edit to
    /// <c>AddAIReviewerAssignments</c>, because that one has already run — the WebApp calls
    /// <c>Database.Migrate()</c> at startup and Development deploys on every push to an open pull
    /// request, so its id is already recorded on g2h-dev and on every local database, and a
    /// recorded id is never re-executed.
    ///
    /// <para><b>Why a key and not the index that is already there.</b>
    /// <c>UX_AIReviewerAssignments_ApprovalId</c> enforces AT MOST ONE LIVE assignment per round.
    /// This key enforces THE ROUND EXISTS. Neither substitutes for the other, and nothing in C#
    /// was ever going to cover the second: <c>AIReviewerAssignmentService</c> holds no
    /// <c>IAccessBroker</c> and validates <c>ApprovalId</c> with <c>IsInvalid</c> only, so a
    /// fabricated identifier arriving on the <c>AIReviewerAssignment-Adding</c> substrate address
    /// passes every gate between the envelope and the table.</para>
    ///
    /// <para><b>The repair, and why it comes first.</b> <c>AddForeignKey</c> emits a plain
    /// <c>ALTER TABLE</c>, which SQL Server adds <c>WITH CHECK</c> — it validates every EXISTING
    /// row. Two live sources of orphans exist: the substrate write above, and
    /// <c>DoHardRemoveApprovalByIdAsync</c>, which hard-deletes a round with no child cleanup and
    /// is refused today only for the three siblings that already declare a key. One orphan row
    /// would fail the <c>ALTER</c> and fail the migration — and through the startup
    /// <c>Database.Migrate()</c> that is not a loud failure but a silent one: the retry loop in
    /// <c>Program.Configurations</c> logs the fifth failure and swallows it, and the portal comes
    /// up on the previous schema with the seeds and the event substrate registration unrun. So
    /// the migration makes the rows clean rather than asking to be told they are.</para>
    ///
    /// <para><b>The narrowing is the whole of the rule.</b> Orphans and nothing else — no
    /// <c>IsDeleted</c> predicate on either side. <c>AND p.[IsDeleted] = 0</c> on the parent would
    /// delete every assignment against a soft-deleted round, and those rows are legitimate: the
    /// key enforces existence, liveness stays with <c>IAccessBroker</c> (§APR8.6.1).
    /// <c>WHERE a.[IsDeleted] = 0</c> on the child would skip soft-deleted orphans, which the
    /// foreign key still refuses, so the <c>ALTER</c> would fail anyway. Narrowed to orphans, the
    /// statement is a no-op on a healthy database and safe to re-run.</para>
    ///
    /// <para><b>A plain <c>Sql()</c> is correct here and <c>EXEC</c> is not used.</b> The
    /// <c>--idempotent</c> script emits a migration as ONE batch with no <c>GO</c>, and SQL Server
    /// compiles a batch before running any of it — which is why
    /// <c>AddSortOrderToContentItemSettings</c> had to wrap an update against a column the same
    /// batch was still adding. This migration adds no column. <c>AIReviewerAssignments</c> and
    /// <c>Approvals</c> both already exist wherever it runs, so both statements compile.</para>
    ///
    /// <para><b>THE DELETE IS IRREVERSIBLE.</b> <c>Down()</c> drops the key and does NOT bring the
    /// deleted rows back — they are gone, and nothing in the schema recorded them. Their
    /// <c>CreatedBy</c>/<c>CreatedWhen</c> stamps survive only in the orphan survey pasted into
    /// the pull request that shipped this migration. Same posture, and the same reason, as
    /// <c>AdoptHousePolicyAndConstrainApprovalSettingAIThresholds</c> takes for its own
    /// irreversible half.</para>
    /// </summary>
    /// <inheritdoc />
    public partial class DeclareAIReviewerAssignmentToApprovalRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A pure anti-join: rows whose ApprovalId matches NO Approvals row at all. See the
            // summary above for why neither side carries an IsDeleted predicate — both narrowings
            // are tempting and both are wrong, one of them in a way that would contradict the
            // accept-a-soft-deleted-round behaviour this change deliberately keeps.
            //
            // The PRINT is for the sqlcmd deploy path, where somebody is watching. It is
            // discarded on the Database.Migrate() path, so it is neither evidence nor required.
            migrationBuilder.Sql(@"
                DECLARE @orphanCount INT =
                    (SELECT COUNT(*)
                     FROM [AIReviewerAssignments] AS a
                     WHERE NOT EXISTS (
                         SELECT 1 FROM [Approvals] AS p WHERE p.[Id] = a.[ApprovalId]));

                PRINT CONCAT(
                    'DeclareAIReviewerAssignmentToApprovalRelationship: deleting ',
                    @orphanCount,
                    ' orphaned AIReviewerAssignments row(s).');

                DELETE a
                FROM [AIReviewerAssignments] AS a
                WHERE NOT EXISTS (
                    SELECT 1 FROM [Approvals] AS p WHERE p.[Id] = a.[ApprovalId]);");

            // Plain, therefore WITH CHECK, therefore trusted: sys.foreign_keys reports
            // is_not_trusted = 0, which is what says the rows already in the table were actually
            // checked. WITH NOCHECK would create the same-named constraint and prove nothing
            // about them.
            //
            // No onDelete argument, so NO ACTION — matching ApprovalComment, ApprovalReview and
            // ApprovalReviewRequest. Nothing is ever deleted implicitly; the one deletion on this
            // change is the repair above, which runs once.
            migrationBuilder.AddForeignKey(
                name: "FK_AIReviewerAssignments_Approvals_ApprovalId",
                table: "AIReviewerAssignments",
                column: "ApprovalId",
                principalTable: "Approvals",
                principalColumn: "Id");
        }

        /// <summary>
        /// The schema half reverses; the repair deliberately does not — and cannot.
        ///
        /// <para>The deleted rows are gone. Nothing in the schema recorded them, no column on
        /// <c>AIReviewerAssignments</c> other than <c>ApprovalId</c> says which round a row
        /// belonged to, and inventing either the rows or their parents would be worse than the
        /// deletion. A rolled-back schema is a schema without this key, not a reason to
        /// manufacture history.</para>
        /// </summary>
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AIReviewerAssignments_Approvals_ApprovalId",
                table: "AIReviewerAssignments");
        }
    }
}
