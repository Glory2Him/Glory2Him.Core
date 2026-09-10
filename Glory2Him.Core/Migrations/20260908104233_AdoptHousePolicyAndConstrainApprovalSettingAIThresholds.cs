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
    /// The corrective half of <c>AddApprovalSettingAIReviewerFields</c>, and a SEPARATE migration
    /// rather than an edit to that one, because that one has already run. The WebApp calls
    /// <c>Database.Migrate()</c> at startup (<c>Program.Configurations</c>) and the Development
    /// deployment runs on every push to an open pull request, so the earlier id is already
    /// recorded as applied on g2h-dev and on every local database — and a recorded id is never
    /// re-executed. Changing its literals in place would correct nothing on precisely the
    /// databases that need correcting: their rows would keep the 0.00 backfill and their table
    /// would keep no threshold constraint, and nothing anywhere would report either gap.
    ///
    /// <para><b>The data half.</b> Both thresholds arrived with a backfill of 0.00 — the column's
    /// CLR default, not a policy. <c>ApprovalSettingSeedData</c> ships 2.50 and 7.50 (the design's
    /// own suggested band, §8.6.2 and §13.5's "7.5 of 10") and leaves a LIVE row exactly as an
    /// administrator set it, logging only the fields that differ from the shipped policy — so a
    /// row left on the backfilled pair reports both thresholds as administrator-diverged, at
    /// Information, on every start, forever, for values nobody ever chose. It is also the pair
    /// with teeth: under §8.6.2's rules an approval threshold of 0.00 approves anything scoring
    /// above zero, which matters the moment <c>IsAIAllowedToVote</c> is turned on by an
    /// administrator who never visited the thresholds because the drift log had stopped meaning
    /// anything.</para>
    ///
    /// <para><b>The schema half.</b> The three check constraints the model configuration declares
    /// (§14.6 rule 2's defence in depth) exist on no database that has already run the earlier
    /// migration. Their SQL is copied from that configuration character for character: EF compares
    /// a check constraint as text, so a difference of one space leaves a migration silently pending
    /// forever.</para>
    ///
    /// <para><b>And the data half repairs whatever the schema half would refuse</b>, not only the
    /// backfilled pair. <c>AddCheckConstraint</c> emits a plain <c>ALTER TABLE</c>, which SQL Server
    /// adds <c>WITH CHECK</c> — it validates every EXISTING row — and the window between the two
    /// migrations was a window in which an out-of-range or inverted pair really could be stored: the
    /// admin page bounded each field to 0–10 on its own and nothing anywhere ordered the two. One
    /// such row would fail the <c>ALTER</c>, fail the migration, and, through the startup
    /// <c>Database.Migrate()</c>, refuse to start the application on every start until somebody
    /// edited it by hand.</para>
    ///
    /// <para>Both halves converge a database whether or not it has already recorded the earlier id:
    /// one that has not runs the 0.00 backfill and this repair back to back, and one created empty
    /// finds no row to repair and is seeded on the same pair.</para>
    /// </summary>
    /// <inheritdoc />
    public partial class AdoptHousePolicyAndConstrainApprovalSettingAIThresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NARROWED TO THE BACKFILLED PAIR, and that is the whole of the rule for this half: a
            // row an administrator has genuinely set is theirs, and the seed's divergence log exists
            // to report exactly that. The normalisation below does touch such rows, and it is not
            // this rule's exception — it repairs a pair the schema is about to refuse, moving each
            // value as little as the rule it breaks allows rather than replacing the pair with the
            // shipped band.
            //
            // Only a row still holding 0.00/0.00 — the pair AddColumn wrote, and the one
            // ApprovalPolicyDefaults ships as its fail-closed fallback precisely because it is not
            // a claim about where a deployment should set these — is moved onto the shipped house
            // policy. The two literals match ApprovalSettingSeedData; Core cannot reference that
            // class, so the pair is duplicated across the boundary on purpose and
            // ApprovalSettingSeedTests pins it on the other side.
            //
            // Not narrowed to live rows. A soft-deleted policy can be restored, and would come
            // back carrying values nobody chose.
            //
            // A PLAIN Sql() IS SAFE HERE, unlike AddSortOrderToContentItemSettings, which had to
            // wrap its backfill in EXEC. `dotnet ef migrations script --idempotent` — how this repo
            // deploys — emits a migration as ONE batch with no GO, and SQL Server compiles a batch
            // before running any of it, so an UPDATE against a column the same batch is still
            // adding fails to compile (Msg 207). Both threshold columns were added by the earlier
            // migration and already exist wherever this one runs, so there is nothing to defer.
            //
            // FIRST of the two repairs, and the order matters — see the normalisation below.
            migrationBuilder.Sql(@"
                UPDATE [ApprovalSettings]
                SET [AIApprovalConfidenceRejectionThreshold] = 2.50,
                    [AIApprovalConfidenceApprovalThreshold] = 7.50
                WHERE [AIApprovalConfidenceRejectionThreshold] = 0.00
                  AND [AIApprovalConfidenceApprovalThreshold] = 0.00;");

            // AND THEN EVERY OTHER ROW THE THREE CONSTRAINTS BELOW WOULD REFUSE. AddCheckConstraint
            // emits a plain ALTER TABLE, which SQL Server adds WITH CHECK: it validates every
            // EXISTING row, so one row breaking any of the three rules fails the ALTER, fails the
            // migration and — the WebApp calls Database.Migrate() at startup, Program.Configurations
            // — refuses to start the application on every start after that until somebody edits the
            // row by hand. The earlier migration shipped both columns to g2h-dev one deploy before
            // this one, and in that window the admin page bounded each field to 0–10 on its own with
            // NO ordering rule between them, so a stored 8.00/3.00 is not hypothetical.
            //
            // NOT NARROWED TO LIVE ROWS, and here for a harder reason than the update above: a check
            // constraint validates soft-deleted rows too, so skipping them would skip exactly the
            // rows that fail the ALTER.
            //
            // TWO FAULTS, TWO DIFFERENT REPAIRS, because they are not the same kind of mistake.
            //
            // An off-scale value is CLAMPED to the end of the scale it overshot. It is one value
            // that left ConfidenceScore's 0.00–10.00 scale (§8.6.2, §13.5) on its own, and the
            // nearest legal point keeps the direction of what was chosen: 50.00 in the rejection
            // box was somebody asking to reject nearly everything, and 10.00 is the strongest form
            // of that the scale can hold. Nothing about the other column has to be guessed.
            //
            // An inverted pair is SORTED — the two values swap columns. Both numbers were chosen by
            // an administrator, and §8.6.2 says which role each may hold: the rejection threshold is
            // the lower of the two. Sorting keeps both chosen values and gives them the only roles
            // the design allows, where putting the row onto the house band would throw both away —
            // the one thing the update above refuses to do to a row somebody set. It is also the
            // reading with the LEAST automation: stored inverted, rules 1 and 2 fire on the same
            // score and the design defines no behaviour for that; sorted, the span between them is
            // rule 3's band, where a human decides.
            //
            // The swap is one statement because SET reads the row as it stood BEFORE the update, so
            // the two assignments cross rather than chase each other. Clamping first is not an
            // ordering requirement — clamping is monotone and cannot invert a pair — but it means
            // the sort chooses between the two values the row is actually keeping.
            //
            // AFTER the house-policy update above, and that IS an ordering requirement: -5.00/-2.00
            // clamps to 0.00/0.00, and in the other order that administrator-set row would then be
            // handed the shipped band as though it had been the backfill all along.
            //
            // Each statement is narrowed to the rows breaking its own rule, so this is a no-op on a
            // healthy database and safe to run again on any other.
            migrationBuilder.Sql(@"
                UPDATE [ApprovalSettings]
                SET [AIApprovalConfidenceRejectionThreshold] =
                        CASE WHEN [AIApprovalConfidenceRejectionThreshold] < 0.00
                             THEN 0.00 ELSE 10.00 END
                WHERE [AIApprovalConfidenceRejectionThreshold] NOT BETWEEN 0.00 AND 10.00;

                UPDATE [ApprovalSettings]
                SET [AIApprovalConfidenceApprovalThreshold] =
                        CASE WHEN [AIApprovalConfidenceApprovalThreshold] < 0.00
                             THEN 0.00 ELSE 10.00 END
                WHERE [AIApprovalConfidenceApprovalThreshold] NOT BETWEEN 0.00 AND 10.00;

                UPDATE [ApprovalSettings]
                SET [AIApprovalConfidenceRejectionThreshold] = [AIApprovalConfidenceApprovalThreshold],
                    [AIApprovalConfidenceApprovalThreshold] = [AIApprovalConfidenceRejectionThreshold]
                WHERE [AIApprovalConfidenceRejectionThreshold] > [AIApprovalConfidenceApprovalThreshold];");

            // Three constraints rather than one combined check: a check-constraint violation
            // reaches the caller naming no column, so the constraint NAME in the SQL error is the
            // only diagnostic a raw-SQL writer gets. One name per fault is worth two extra ALTER
            // TABLE statements.
            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalSetting_AIRejectionThresholdRange",
                table: "ApprovalSettings",
                sql: "(AIApprovalConfidenceRejectionThreshold BETWEEN 0 AND 10)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalSetting_AIApprovalThresholdRange",
                table: "ApprovalSettings",
                sql: "(AIApprovalConfidenceApprovalThreshold BETWEEN 0 AND 10)");

            // `<=`, not `<`. §8.6.2's rules 1 and 2 are strict comparisons on either side of the
            // pair, so an equal pair only closes the middle band and leaves the two verdicts
            // disjoint — nothing inverts. It is also the shape ApprovalPolicyDefaults ships as its
            // fail-closed fallback, which a strict rule would make unstorable.
            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalSetting_AIThresholdOrder",
                table: "ApprovalSettings",
                sql: "(AIApprovalConfidenceRejectionThreshold <= AIApprovalConfidenceApprovalThreshold)");
        }

        /// <summary>
        /// The schema half reverses; the data half deliberately does not.
        ///
        /// <para>Writing 0.00/0.00 back would not restore a previous state — it would overwrite the
        /// policy of every administrator who has since chosen the house band, and it would leave
        /// them on the one pair that approves anything scoring above zero the moment a vote is
        /// allowed (§8.6.2). A rolled-back schema is a schema without the three guards, not a
        /// reason to put a fail-open pair into rows that hold a deliberate one, and the migration
        /// cannot tell those rows apart from the ones it moved.</para>
        ///
        /// <para>The normalisation is not reversible either, and for a plainer reason: the value a
        /// clamp replaced is recorded nowhere, and restoring an inverted pair would put back a state
        /// §8.6.2 defines no behaviour for.</para>
        /// </summary>
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalSetting_AIRejectionThresholdRange",
                table: "ApprovalSettings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalSetting_AIApprovalThresholdRange",
                table: "ApprovalSettings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalSetting_AIThresholdOrder",
                table: "ApprovalSettings");
        }
    }
}
