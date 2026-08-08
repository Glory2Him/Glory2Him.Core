using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateCommentResolutionSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequireApprovalCommentResolutionBeforeApproval",
                table: "ApprovalSettings",
                newName: "RequireReviewCommentResolutionBeforeApprovals");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RequireReviewCommentResolutionBeforeApprovals",
                table: "ApprovalSettings",
                newName: "RequireApprovalCommentResolutionBeforeApproval");
        }
    }
}
