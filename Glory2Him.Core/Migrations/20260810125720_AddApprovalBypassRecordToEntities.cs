using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Glory2Him.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddApprovalBypassRecordToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedByBypassReason",
                table: "Tags",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByBypass",
                table: "Tags",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByBypassReason",
                table: "Reactions",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByBypass",
                table: "Reactions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByBypassReason",
                table: "Links",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByBypass",
                table: "Links",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByBypassReason",
                table: "ContentItems",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByBypass",
                table: "ContentItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByBypassReason",
                table: "Comments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByBypass",
                table: "Comments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByBypassReason",
                table: "BibleReferences",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByBypass",
                table: "BibleReferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByBypassReason",
                table: "Attachments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByBypass",
                table: "Attachments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ApprovedByBypassReason",
                table: "Associations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedByBypass",
                table: "Associations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedByBypassReason",
                table: "Approvals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedByBypassReason",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "IsApprovedByBypass",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "ApprovedByBypassReason",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "IsApprovedByBypass",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "ApprovedByBypassReason",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "IsApprovedByBypass",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "ApprovedByBypassReason",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "IsApprovedByBypass",
                table: "ContentItems");

            migrationBuilder.DropColumn(
                name: "ApprovedByBypassReason",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "IsApprovedByBypass",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "ApprovedByBypassReason",
                table: "BibleReferences");

            migrationBuilder.DropColumn(
                name: "IsApprovedByBypass",
                table: "BibleReferences");

            migrationBuilder.DropColumn(
                name: "ApprovedByBypassReason",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "IsApprovedByBypass",
                table: "Attachments");

            migrationBuilder.DropColumn(
                name: "ApprovedByBypassReason",
                table: "Associations");

            migrationBuilder.DropColumn(
                name: "IsApprovedByBypass",
                table: "Associations");

            migrationBuilder.AlterColumn<string>(
                name: "ApprovedByBypassReason",
                table: "Approvals",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);
        }
    }
}
