using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC2.Data.Migrations
{
    public partial class SimplifyEvents : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvents_CalendarDates_CalendarDateID",
                table: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "CalendarDates");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_CalendarDateID",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "CalendarDateID",
                table: "CalendarEvents");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfEvent",
                table: "CalendarEvents",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfEvent",
                table: "CalendarEvents");

            migrationBuilder.AddColumn<int>(
                name: "CalendarDateID",
                table: "CalendarEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CalendarDates",
                columns: table => new
                {
                    CalendarDateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDates", x => x.CalendarDateID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_CalendarDateID",
                table: "CalendarEvents",
                column: "CalendarDateID");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvents_CalendarDates_CalendarDateID",
                table: "CalendarEvents",
                column: "CalendarDateID",
                principalTable: "CalendarDates",
                principalColumn: "CalendarDateID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
