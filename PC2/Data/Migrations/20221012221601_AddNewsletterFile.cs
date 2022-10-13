using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC2.Data.Migrations
{
    public partial class AddNewsletterFile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NewsletterName",
                table: "NewsletterFile",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "NewsletterFileLocation",
                table: "NewsletterFile",
                newName: "Location");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "NewsletterFile",
                newName: "NewsletterName");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "NewsletterFile",
                newName: "NewsletterFileLocation");
        }
    }
}
