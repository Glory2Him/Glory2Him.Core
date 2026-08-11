using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class FilterApprovalReviewUniqueIndexToActiveReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ApprovalReviews_ApprovalId_ReviewerId",
                table: "ApprovalReviews");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalReviews_ApprovalId_ReviewerId",
                table: "ApprovalReviews",
                columns: new[] { "ApprovalId", "ReviewerId" },
                unique: true,
                filter: "[StatusId] <> 4 AND [IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_ApprovalReviews_ApprovalId_ReviewerId",
                table: "ApprovalReviews");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalReviews_ApprovalId_ReviewerId",
                table: "ApprovalReviews",
                columns: new[] { "ApprovalId", "ReviewerId" },
                unique: true);
        }
    }
}
