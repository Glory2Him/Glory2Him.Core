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
    /// <inheritdoc />
    public partial class PinProcessedEventReceiverNameToBinaryCollation : Migration
    {
        // ReceiverName is a key column of IX_ProcessedEvents_EventId_ReceiverName, and SQL
        // Server refuses ALTER COLUMN on a column an index sits on, so the index is dropped
        // and recreated around the alter. EF generates the ALTER alone, which fails with
        // error 5074; the drop and the recreate below are deliberate hand-edits.
        //
        // All three statements are in ONE batch and must stay that way. No column is added
        // here, so the repository's recorded single-batch trap — a column added and then
        // referenced in the same batch needing EXEC — does not bite: nothing below references
        // a name the batch itself creates.

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedEvents_EventId_ReceiverName",
                table: "ProcessedEvents");

            // A receiver name is an identifier and identifiers compare exactly. Pinning the
            // COLUMN, rather than collating an expression, is what makes the probe
            // SelectProcessedEventExistsAsync and the unique index below agree — both inherit
            // the column's collation, and a disagreement would let the probe answer "not
            // processed", the handler run its side effect, and the insert THEN violate the
            // index.
            migrationBuilder.AlterColumn<string>(
                name: "ReceiverName",
                table: "ProcessedEvents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                collation: "Latin1_General_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            // Recreated over the altered column, so the dedup guarantee is keyed under the
            // binary collation the probe now compares under. No existing row can violate it:
            // moving from a case-insensitive key to a binary one only ever SPLITS keys that
            // used to collide, never merges two that did not.
            migrationBuilder.CreateIndex(
                name: "IX_ProcessedEvents_EventId_ReceiverName",
                table: "ProcessedEvents",
                columns: new[] { "EventId", "ReceiverName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The reverse direction is the dangerous one, and knowingly so: restoring the
            // inherited collation MERGES keys, so if two receiver names differing only in case
            // were recorded against one event while this migration was live, the recreated
            // index below refuses them and the down path fails. That window stays empty until
            // a receiver name varies in case, which the EventBrokerIdentifiers constants make
            // a code change rather than an accident.
            migrationBuilder.DropIndex(
                name: "IX_ProcessedEvents_EventId_ReceiverName",
                table: "ProcessedEvents");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiverName",
                table: "ProcessedEvents",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255,
                oldCollation: "Latin1_General_BIN2");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedEvents_EventId_ReceiverName",
                table: "ProcessedEvents",
                columns: new[] { "EventId", "ReceiverName" },
                unique: true);
        }
    }
}
