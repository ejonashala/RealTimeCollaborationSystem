using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Models;

namespace RealTimeCollaborationSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users => Set<User>();
        public DbSet<TopicBatch> TopicBatches { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<CourseTask> CourseTasks { get; set; }
        public DbSet<TaskSubmission> TaskSubmissions { get; set; }
        public DbSet<SystemNotification> SystemNotifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TaskSubmission>()
                .HasIndex(submission => new { submission.CourseTaskId, submission.StudentId })
                .IsUnique();

            modelBuilder.Entity<TaskSubmission>()
                .Property(submission => submission.Grade)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<CourseTask>()
                .HasOne(task => task.TopicBatch)
                .WithMany()
                .HasForeignKey(task => task.TopicBatchId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<TaskSubmission>()
                .HasOne(submission => submission.Student)
                .WithMany()
                .HasForeignKey(submission => submission.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SystemNotification>()
                .HasOne(notification => notification.RecipientUser)
                .WithMany()
                .HasForeignKey(notification => notification.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SystemNotification>()
                .HasOne(notification => notification.ActorUser)
                .WithMany()
                .HasForeignKey(notification => notification.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SystemNotification>()
                .HasOne(notification => notification.Topic)
                .WithMany()
                .HasForeignKey(notification => notification.TopicId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<SystemNotification>()
                .HasOne(notification => notification.CourseTask)
                .WithMany()
                .HasForeignKey(notification => notification.CourseTaskId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            modelBuilder.Entity<SystemNotification>()
                .HasOne(notification => notification.TaskSubmission)
                .WithMany()
                .HasForeignKey(notification => notification.TaskSubmissionId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }

}
