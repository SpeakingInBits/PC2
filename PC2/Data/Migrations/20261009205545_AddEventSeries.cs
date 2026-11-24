using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC2.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventSeries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EventSeriesID",
                table: "CalendarEvents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventSeries",
                columns: table => new
                {
                    EventSeriesID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Frequency = table.Column<int>(type: "int", nullable: false),
                    Interval = table.Column<int>(type: "int", nullable: false),
                    WeeklyDays = table.Column<int>(type: "int", nullable: false),
                    WeekOfMonth = table.Column<int>(type: "int", nullable: true),
                    StartDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndDate = table.Column<DateTime>(type: "date", nullable: true),
                    OccurrenceCount = table.Column<int>(type: "int", nullable: true),
                    GeneratedThrough = table.Column<DateTime>(type: "date", nullable: true),
                    StartingTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EndingTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    EventDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PC2Event = table.Column<bool>(type: "bit", nullable: false),
                    CountyEvent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventSeries", x => x.EventSeriesID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarEvents_EventSeriesID",
                table: "CalendarEvents",
                column: "EventSeriesID");

            migrationBuilder.AddForeignKey(
                name: "FK_CalendarEvents_EventSeries_EventSeriesID",
                table: "CalendarEvents",
                column: "EventSeriesID",
                principalTable: "EventSeries",
                principalColumn: "EventSeriesID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvents_EventSeries_EventSeriesID",
                table: "CalendarEvents");

            migrationBuilder.DropTable(
                name: "EventSeries");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_EventSeriesID",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "EventSeriesID",
                table: "CalendarEvents");
        }
    }
}
