using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RealTimeCollaborationSystem.Models;

namespace RealTimeCollaborationSystem.Data
{
    public static class DatabaseStartupExtensions
    {
        public static async Task RunDatabaseSetupAsync(this WebApplication app)
        {
            var migrateOnStartup = app.Configuration.GetValue<bool>("Database:MigrateOnStartup")
                || app.Configuration.GetValue<bool>("Database:RunSetupOnStartup");
            var seedSampleDataOnStartup = app.Environment.IsDevelopment()
                && app.Configuration.GetValue<bool>("Database:SeedSampleDataOnStartup");

            if (!migrateOnStartup && !seedSampleDataOnStartup)
                return;

            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (migrateOnStartup)
                await db.Database.MigrateAsync();

            if (seedSampleDataOnStartup)
                await SeedProjectTopicsAsync(db);
        }

        private static async Task SeedProjectTopicsAsync(AppDbContext db)
        {
            var professor1 = await EnsureProfessorAsync(db, "Professor 1", "professor1@collabspace.local");
            var professor2 = await EnsureProfessorAsync(db, "Professor 2", "professor2@collabspace.local");

            await EnsureTopicGroupAsync(
                db,
                professor1.Id,
                "Programimi i sistemeve t\u00C3\u00AB shp\u00C3\u00ABrndara",
                new[]
                {
                    "Chat real-time me SignalR",
                    "Sistemi i rezervimit t\u00C3\u00AB temave",
                    "Monitorimi i sh\u00C3\u00ABrbimeve t\u00C3\u00AB shp\u00C3\u00ABrndara",
                    "Menaxhimi i detyrave me API"
                });

            await EnsureTopicGroupAsync(
                db,
                professor2.Id,
                "Kompjutimi Cloud dhe Mobile Cloud",
                new[]
                {
                    "Deploy i aplikacionit n\u00C3\u00AB Azure",
                    "Sinkronizimi i t\u00C3\u00AB dh\u00C3\u00ABnave n\u00C3\u00AB cloud",
                    "Aplikacion mobile p\u00C3\u00ABr menaxhim detyrash",
                    "Njoftime push p\u00C3\u00ABr aplikacione mobile"
                });

            await db.SaveChangesAsync();
        }

        private static async Task<User> EnsureProfessorAsync(AppDbContext db, string name, string email)
        {
            var professor = await db.Users.FirstOrDefaultAsync(user => user.Email == email);

            if (professor != null)
                return professor;

            professor = new User
            {
                Name = name,
                Email = email,
                Role = "Professor",
                PhotoUrl = "/images/users/default-avatar.svg",
                Language = "sq"
            };

            professor.Password = new PasswordHasher<User>().HashPassword(professor, "Professor123!");
            db.Users.Add(professor);
            await db.SaveChangesAsync();

            return professor;
        }

        private static async Task EnsureTopicGroupAsync(AppDbContext db, int professorId, string title, IEnumerable<string> topicTitles)
        {
            var batch = await db.TopicBatches
                .Include(item => item.Topics)
                .FirstOrDefaultAsync(item => item.Title == title);

            if (batch != null)
                return;

            batch = new TopicBatch
            {
                Title = title,
                CreatedByProfessorId = professorId,
                CreatedAt = DateTime.UtcNow,
                IsPublished = true
            };

            db.TopicBatches.Add(batch);

            foreach (var topicTitle in topicTitles)
            {
                if (batch.Topics.Any(topic => topic.Title == topicTitle))
                    continue;

                batch.Topics.Add(new Topic
                {
                    Title = topicTitle,
                    Status = "Available",
                    MaxMembers = 1
                });
            }
        }
    }
}
