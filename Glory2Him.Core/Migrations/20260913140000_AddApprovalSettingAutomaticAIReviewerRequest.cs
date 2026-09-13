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
    /// Design §8.2 / §8.6.2 / §8.6.2.1: one new column, <c>IsAIReviewerAutomaticallyRequested</c>,
    /// deciding whether Berean is assigned to a round WITHOUT anybody asking, once that round
    /// enters review. One <c>AddColumn</c> and nothing else, so this is a single batch on the
    /// deploy path with no add-then-update and no <c>EXEC</c>.
    ///
    /// <para><b>The default is <c>1</c>, and it is backfilled onto every existing row.</b> This
    /// matches the CLR property initialiser on <c>ApprovalSetting</c> and
    /// <c>ApprovalSettingSeedData</c>'s own constant — <c>Core</c> cannot reference that seed, so
    /// the <c>true</c> literal is duplicated across that boundary on purpose, and the two must
    /// move together: change the seed constant and this default has to follow, or a fresh
    /// database and an upgraded one would disagree about a field nobody set. A deployment that
    /// has ALREADY turned <c>IsAIReviewerOffered</c> on for a scope begins auto-assigning the
    /// moment the consuming workflow ships, because the backfill reads that as the intended
    /// meaning of having turned Berean on rather than as an accident (design §8.6.2.1).</para>
    ///
    /// <para><b>No CHECK constraint pairs this with <c>IsAIReviewerOffered</c></b>, unlike
    /// <c>CK_ApprovalSetting_AIVoteRequiresAIReviewer</c> beside it. A stored <c>true</c> under an
    /// offer of <c>false</c> is a dormant preference — exactly what every shipped row holds — not
    /// a contradiction a constraint would have to refuse; the pairing that matters is asked fresh
    /// on every automatic assignment instead (§8.6.2.1 gate 2). A constraint here would refuse the
    /// seed itself.</para>
    /// </summary>
    /// <inheritdoc />
    public partial class AddApprovalSettingAutomaticAIReviewerRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAIReviewerAutomaticallyRequested",
                table: "ApprovalSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAIReviewerAutomaticallyRequested",
                table: "ApprovalSettings");
        }
    }
}
