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
            // The new index is created BEFORE the old one is dropped and before ReviewerId is
            // removed. Uniqueness on (ApprovalId, CreatedBy) has never been enforced in this
            // database's history, so this CreateIndex is the one statement here that can fail on
            // existing data. Running it first means that if it does, ReviewerId is still present
            // to identify the offending rows — dropping the discriminating column first would
            // leave a failed deployment with no way to see what collided. The two indexes
            // coexist for the duration.
            migrationBuilder.CreateIndex(
                name: "UX_ApprovalReviews_ApprovalId_CreatedBy",
                table: "ApprovalReviews",
                columns: new[] { "ApprovalId", "CreatedBy" },
                unique: true,
                filter: "[StatusId] <> 4 AND [IsDeleted] = 0");

            migrationBuilder.DropIndex(
                name: "UX_ApprovalReviews_ApprovalId_ReviewerId",
                table: "ApprovalReviews");

            migrationBuilder.DropColumn(
                name: "ReviewerId",
                table: "ApprovalReviews");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ApprovalComments");
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

            // Backfill both columns from CreatedBy before the old unique index is rebuilt.
            // Without this every row carries the same default of '', so two active reviews on one
            // approval collide on (ApprovalId, '') and the CreateIndex below fails — which is the
            // normal state, not an edge case, since the whole point of the filtered index is to
            // allow many active reviews per approval and only one per person. CreatedBy is also
            // the correct historical value: both columns were bound to the acting user on add and
            // pinned against storage on modify, so they never held anything else.
            migrationBuilder.Sql(
                "UPDATE [ApprovalReviews] SET [ReviewerId] = [CreatedBy];");

            migrationBuilder.Sql(
                "UPDATE [ApprovalComments] SET [UserId] = [CreatedBy];");

            migrationBuilder.CreateIndex(
                name: "UX_ApprovalReviews_ApprovalId_ReviewerId",
                table: "ApprovalReviews",
                columns: new[] { "ApprovalId", "ReviewerId" },
                unique: true,
                filter: "[StatusId] <> 4 AND [IsDeleted] = 0");
        }
    }
}
