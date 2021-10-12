using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC2.Data.Migrations
{
    public partial class Calendar : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalendarDates",
                columns: table => new
                {
                    CalendarDateID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDates", x => x.CalendarDateID);
                });

            migrationBuilder.CreateTable(
                name: "CalendarEvents",
                columns: table => new
                {
                    CalendarEventID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StartingTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EndingTime = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PC2Event = table.Column<bool>(type: "bit", nullable: false),
                    CountyEvent = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarEvents", x => x.CalendarEventID);
                });

            migrationBuilder.CreateTable(
                name: "CalendarDateCalendarEvent",
                columns: table => new
                {
                    CalendarDateID = table.Column<int>(type: "int", nullable: false),
                    EventsCalendarEventID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarDateCalendarEvent", x => new { x.CalendarDateID, x.EventsCalendarEventID });
                    table.ForeignKey(
                        name: "FK_CalendarDateCalendarEvent_CalendarDates_CalendarDateID",
                        column: x => x.CalendarDateID,
                        principalTable: "CalendarDates",
                        principalColumn: "CalendarDateID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CalendarDateCalendarEvent_CalendarEvents_EventsCalendarEventID",
                        column: x => x.EventsCalendarEventID,
                        principalTable: "CalendarEvents",
                        principalColumn: "CalendarEventID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CalendarDateCalendarEvent_EventsCalendarEventID",
                table: "CalendarDateCalendarEvent",
                column: "EventsCalendarEventID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarDateCalendarEvent");

            migrationBuilder.DropTable(
                name: "CalendarDates");

            migrationBuilder.DropTable(
                name: "CalendarEvents");
        }
    }
}
