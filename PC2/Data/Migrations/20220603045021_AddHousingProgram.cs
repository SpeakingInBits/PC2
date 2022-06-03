using System;
using Microsoft.EntityFrameworkCore.Migrations;
using PC2.Models;

#nullable disable

namespace PC2.Data.Migrations
{
    public partial class AddHousingProgram : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HousingProgram",
                columns: table => new
                {
                    HouseHoldSize = table.Column<int>(type: "int", nullable: false),
                    MaximumIncome = table.Column<double>(type: "float", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HousingProgram", x => x.HouseHoldSize);
                });

            // Insert initial table data
            double[] maxIncomes = new double[] { 50900, 58150, 65400, 72650, 78500, 84300, 90100, 95900 };
            for (int i = 0; i < maxIncomes.Length; i++)
            {
                AddHousingProgramRow(migrationBuilder, i + 1, maxIncomes[i]);
            }

            static void AddHousingProgramRow(MigrationBuilder migrationBuilder, int houseHoldSize, double maxIncome)
            {
                migrationBuilder.InsertData(table: "HousingProgram",
                                columns: new[] { nameof(HousingProgram.HouseHoldSize), nameof(HousingProgram.MaximumIncome), nameof(HousingProgram.LastUpdated) },
                                values: new object[] { houseHoldSize, maxIncome, "9/1/2021" });
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HousingProgram");
        }
    }
}
