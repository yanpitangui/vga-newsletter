using Microsoft.EntityFrameworkCore;
using TelegramBot.Entities;

namespace TelegramBot.Infrastructure
{
    public class LocalDbContext : DbContext
    {
        public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options) { }

        public DbSet<MessageTracking> MessageTrackings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new MessageTrackingConfiguration());
        }
    }
}
