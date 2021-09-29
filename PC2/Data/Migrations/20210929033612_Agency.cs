using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC2.Data.Migrations
{
    public partial class Agency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Agency",
                columns: table => new
                {
                    AgencyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgencyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AgencyName2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Contact = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    State = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Zip = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MailingAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TollFree = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TTY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TDD = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Crisis_Help_Hotline = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Agency", x => x.AgencyId);
                });

            migrationBuilder.CreateTable(
                name: "AgencyCategory",
                columns: table => new
                {
                    AgencyCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgencyCategoryName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyCategory", x => x.AgencyCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "AgencyAgencyCategory",
                columns: table => new
                {
                    AgencyCategoriesAgencyCategoryId = table.Column<int>(type: "int", nullable: false),
                    agenciesAgencyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgencyAgencyCategory", x => new { x.AgencyCategoriesAgencyCategoryId, x.agenciesAgencyId });
                    table.ForeignKey(
                        name: "FK_AgencyAgencyCategory_Agency_agenciesAgencyId",
                        column: x => x.agenciesAgencyId,
                        principalTable: "Agency",
                        principalColumn: "AgencyId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgencyAgencyCategory_AgencyCategory_AgencyCategoriesAgencyCategoryId",
                        column: x => x.AgencyCategoriesAgencyCategoryId,
                        principalTable: "AgencyCategory",
                        principalColumn: "AgencyCategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgencyAgencyCategory_agenciesAgencyId",
                table: "AgencyAgencyCategory",
                column: "agenciesAgencyId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgencyAgencyCategory");

            migrationBuilder.DropTable(
                name: "Agency");

            migrationBuilder.DropTable(
                name: "AgencyCategory");
        }
    }
}
