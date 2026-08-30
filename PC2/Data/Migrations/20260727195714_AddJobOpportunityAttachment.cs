using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC2.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobOpportunityAttachment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentLocation",
                table: "JobOpportunities",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentName",
                table: "JobOpportunities",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentLocation",
                table: "JobOpportunities");

            migrationBuilder.DropColumn(
                name: "AttachmentName",
                table: "JobOpportunities");
        }
    }
}
