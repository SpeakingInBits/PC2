using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC2.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddJobOpportunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobOpportunities",
                columns: table => new
                {
                    JobOpportunityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosingDate = table.Column<DateTime>(type: "date", nullable: true),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobOpportunities", x => x.JobOpportunityId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobOpportunities");
        }
    }
}
