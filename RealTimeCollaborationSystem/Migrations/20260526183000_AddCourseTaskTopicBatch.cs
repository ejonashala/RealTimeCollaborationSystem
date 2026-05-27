using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using RealTimeCollaborationSystem.Data;

#nullable disable

namespace RealTimeCollaborationSystem.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260526183000_AddCourseTaskTopicBatch")]
    public partial class AddCourseTaskTopicBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TopicBatchId",
                table: "CourseTasks",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseTasks_TopicBatchId",
                table: "CourseTasks",
                column: "TopicBatchId");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseTasks_TopicBatches_TopicBatchId",
                table: "CourseTasks",
                column: "TopicBatchId",
                principalTable: "TopicBatches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CourseTasks_TopicBatches_TopicBatchId",
                table: "CourseTasks");

            migrationBuilder.DropIndex(
                name: "IX_CourseTasks_TopicBatchId",
                table: "CourseTasks");

            migrationBuilder.DropColumn(
                name: "TopicBatchId",
                table: "CourseTasks");
        }
    }
}
