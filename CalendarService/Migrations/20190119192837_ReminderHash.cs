using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CalendarService.Migrations
{
    public partial class ReminderHash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "new_ReminderInstances",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    EventId = table.Column<string>(nullable: true),
                    FeedId = table.Column<string>(nullable: true),
                    ReminderId = table.Column<string>(nullable: true),
                    RemindRemovalUntil = table.Column<DateTime>(nullable: true),
                    Hash = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_new_ReminderInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_new_ReminderInstances_Reminders_ReminderId",
                        column: x => x.ReminderId,
                        principalTable: "Reminders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
            });
            migrationBuilder.Sql("INSERT INTO new_ReminderInstances SELECT Id, EventId, FeedId, ReminderId, NULL, NULL FROM ReminderInstances");
            migrationBuilder.DropTable("ReminderInstances");
            migrationBuilder.RenameTable(name: "new_ReminderInstances", newName: "ReminderInstances");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hash",
                table: "ReminderInstances");

            migrationBuilder.DropColumn(
                name: "RemindRemovalUntil",
                table: "ReminderInstances");

            migrationBuilder.AddColumn<int>(
                name: "Revision",
                table: "ReminderInstances",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "Start",
                table: "ReminderInstances",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
