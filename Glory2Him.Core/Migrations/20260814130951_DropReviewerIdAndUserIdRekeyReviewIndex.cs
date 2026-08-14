using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class DropReviewerIdAndUserIdRekeyReviewIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ApprovalReviews_ApprovalId_ReviewerId",
                table: "ApprovalReviews");

            migrationBuilder.DropColumn(
                name: "ReviewerId",
                table: "ApprovalReviews");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ApprovalComments");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalReviews_ApprovalId_CreatedBy",
                table: "ApprovalReviews",
                columns: new[] { "ApprovalId", "CreatedBy" },
                unique: true,
                filter: "[StatusId] <> 4 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ApprovalReviews_ApprovalId_CreatedBy",
                table: "ApprovalReviews");

            migrationBuilder.AddColumn<string>(
                name: "ReviewerId",
                table: "ApprovalReviews",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ApprovalComments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalReviews_ApprovalId_ReviewerId",
                table: "ApprovalReviews",
                columns: new[] { "ApprovalId", "ReviewerId" },
                unique: true,
                filter: "[StatusId] <> 4 AND [IsDeleted] = 0");
        }
    }
}
