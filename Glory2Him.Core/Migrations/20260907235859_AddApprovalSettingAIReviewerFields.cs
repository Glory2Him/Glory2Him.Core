using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalSettingAIReviewerFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AIApprovalConfidenceApprovalThreshold",
                table: "ApprovalSettings",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AIApprovalConfidenceRejectionThreshold",
                table: "ApprovalSettings",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsAIAllowedToVote",
                table: "ApprovalSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsAIReviewerOffered",
                table: "ApprovalSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_ApprovalSetting_AIVoteRequiresAIReviewer",
                table: "ApprovalSettings",
                sql: "(IsAIAllowedToVote = 0 OR IsAIReviewerOffered = 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ApprovalSetting_AIVoteRequiresAIReviewer",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "AIApprovalConfidenceApprovalThreshold",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "AIApprovalConfidenceRejectionThreshold",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "IsAIAllowedToVote",
                table: "ApprovalSettings");

            migrationBuilder.DropColumn(
                name: "IsAIReviewerOffered",
                table: "ApprovalSettings");
        }
    }
}
