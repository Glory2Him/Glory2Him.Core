using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalCommentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // WHAT A COMMENT IS, beside where it stands. §7.8 already draws the line — an
            // observation asks for nothing and is born settled, an ask holds the approval shut —
            // but it draws it in IsResolved, which MOVES. The moment a question is settled its
            // flag is true, which is exactly what an informational comment looks like at birth,
            // and the two become indistinguishable. This column is what keeps them apart.
            //
            // THE BACKFILL IS THE DEFAULT. A non-nullable column added with a default value is
            // stamped onto every existing row as the column is created, so no UPDATE follows and
            // there is no second statement for the script path to split across batches. Comment
            // is the honest value for those rows: nothing before this column existed could have
            // been recorded as an ask.
            //
            // Stored BY NAME like every other enum here, so a hand-written query reads "Question"
            // rather than 1.
            migrationBuilder.AddColumn<string>(
                name: "CommentType",
                table: "ApprovalComments",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Comment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Dropping the column loses which comments were questions, and there is nothing to
            // recover it from — IsResolved cannot answer it, which is why the column exists. That
            // is a genuine loss on a down migration rather than a reversible change, and it is
            // the only honest reversal available.
            migrationBuilder.DropColumn(
                name: "CommentType",
                table: "ApprovalComments");
        }
    }
}
