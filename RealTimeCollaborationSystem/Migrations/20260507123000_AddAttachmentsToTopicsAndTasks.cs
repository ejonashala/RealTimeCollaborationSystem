using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RealTimeCollaborationSystem.Data;

#nullable disable

namespace RealTimeCollaborationSystem.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260507123000_AddAttachmentsToTopicsAndTasks")]
    public partial class AddAttachmentsToTopicsAndTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "TopicBatches",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "TopicBatches",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentFileName",
                table: "CourseTasks",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPath",
                table: "CourseTasks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "TopicBatches");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "TopicBatches");

            migrationBuilder.DropColumn(
                name: "AttachmentFileName",
                table: "CourseTasks");

            migrationBuilder.DropColumn(
                name: "AttachmentPath",
                table: "CourseTasks");
        }
    }
}
