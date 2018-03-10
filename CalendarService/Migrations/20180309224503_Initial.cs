using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace CalendarService.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigStates",
                columns: table => new
                {
                    State = table.Column<string>(nullable: false),
                    RedirectUri = table.Column<string>(nullable: true),
                    StoredTime = table.Column<DateTime>(nullable: false),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigStates", x => x.State);
                    table.ForeignKey(
                        name: "FK_ConfigStates_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Configurations",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    AccessToken = table.Column<string>(nullable: true),
                    ExpiresIn = table.Column<DateTime>(nullable: false),
                    RefreshToken = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Configurations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ClientState = table.Column<string>(nullable: true),
                    Expires = table.Column<DateTime>(nullable: false),
                    Minutes = table.Column<uint>(nullable: false),
                    NotificationUri = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reminders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feeds",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ConfigurationId = table.Column<string>(nullable: true),
                    FeedId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feeds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feeds_Configurations_ConfigurationId",
                        column: x => x.ConfigurationId,
                        principalTable: "Configurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReminderInstances",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    EventId = table.Column<string>(nullable: true),
                    FeedId = table.Column<string>(nullable: true),
                    ReminderId = table.Column<string>(nullable: true),
                    Revision = table.Column<int>(nullable: false),
                    Start = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReminderInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReminderInstances_Reminders_ReminderId",
                        column: x => x.ReminderId,
                        principalTable: "Reminders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    NotificationId = table.Column<string>(nullable: false),
                    Expires = table.Column<DateTime>(nullable: false),
                    ProviderNotificationId = table.Column<string>(nullable: true),
                    StoredFeedId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notifications_Feeds_StoredFeedId",
                        column: x => x.StoredFeedId,
                        principalTable: "Feeds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConfigStates_UserId",
                table: "ConfigStates",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Configurations_UserId",
                table: "Configurations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Feeds_ConfigurationId",
                table: "Feeds",
                column: "ConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_StoredFeedId",
                table: "Notifications",
                column: "StoredFeedId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReminderInstances_ReminderId",
                table: "ReminderInstances",
                column: "ReminderId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_UserId",
                table: "Reminders",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigStates");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "ReminderInstances");

            migrationBuilder.DropTable(
                name: "Feeds");

            migrationBuilder.DropTable(
                name: "Reminders");

            migrationBuilder.DropTable(
                name: "Configurations");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
