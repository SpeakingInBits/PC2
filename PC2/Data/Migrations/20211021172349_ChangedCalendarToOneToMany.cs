using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PC2.Data.Migrations
{
    public partial class ChangedCalendarToOneToMany : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalendarDateCalendarEvent");

            migrationBuilder.AddColumn<int>(
                name: "CalendarDateID",
                table: "CalendarEvents",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalendarEvents_CalendarDates_CalendarDateID",
                table: "CalendarEvents");

            migrationBuilder.DropIndex(
                name: "IX_CalendarEvents_CalendarDateID",
                table: "CalendarEvents");

            migrationBuilder.DropColumn(
                name: "CalendarDateID",
                table: "CalendarEvents");

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
    }
}
