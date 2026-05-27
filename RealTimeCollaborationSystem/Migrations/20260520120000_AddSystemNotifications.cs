using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RealTimeCollaborationSystem.Data;

#nullable disable

namespace RealTimeCollaborationSystem.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260520120000_AddSystemNotifications")]
    public partial class AddSystemNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientUserId = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<int>(type: "int", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InvitationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TopicId = table.Column<int>(type: "int", nullable: true),
                    CourseTaskId = table.Column<int>(type: "int", nullable: true),
                    TaskSubmissionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemNotifications_CourseTasks_CourseTaskId",
                        column: x => x.CourseTaskId,
                        principalTable: "CourseTasks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SystemNotifications_TaskSubmissions_TaskSubmissionId",
                        column: x => x.TaskSubmissionId,
                        principalTable: "TaskSubmissions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SystemNotifications_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SystemNotifications_Users_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SystemNotifications_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_ActorUserId",
                table: "SystemNotifications",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_CourseTaskId",
                table: "SystemNotifications",
                column: "CourseTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_RecipientUserId",
                table: "SystemNotifications",
                column: "RecipientUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_TaskSubmissionId",
                table: "SystemNotifications",
                column: "TaskSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_TopicId",
                table: "SystemNotifications",
                column: "TopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemNotifications");
        }
    }
}
