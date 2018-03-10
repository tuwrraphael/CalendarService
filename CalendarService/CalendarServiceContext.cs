using Microsoft.EntityFrameworkCore;

namespace CalendarService
{
    public class CalendarServiceContext : DbContext
    {
        public CalendarServiceContext(DbContextOptions<CalendarServiceContext> contextOptions) : base(contextOptions)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<StoredConfigState> ConfigStates { get; set; }
        public DbSet<StoredConfiguration> Configurations { get; set; }
        public DbSet<StoredFeed> Feeds { get; set; }
        public DbSet<StoredNotification> Notifications { get; set; }
        public DbSet<StoredReminder> Reminders { get; set; }
        public DbSet<ReminderInstance> ReminderInstances { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(v => v.Id);
            modelBuilder.Entity<User>()
                .HasMany(v => v.Configurations)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId);
            modelBuilder.Entity<User>()
                .HasMany(v => v.ConfigStates)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId);
            modelBuilder.Entity<User>()
                .HasMany(v => v.Reminders)
                .WithOne(v => v.User)
                .HasForeignKey(v => v.UserId);

            modelBuilder.Entity<StoredConfigState>()
                .HasKey(v => v.State);
            modelBuilder.Entity<StoredConfigState>()
                .Property(v => v.StoredTime).IsRequired();

            modelBuilder.Entity<StoredConfiguration>()
                .HasKey(v => v.Id);
            modelBuilder.Entity<StoredConfiguration>()
                .HasMany(v => v.SubscribedFeeds)
                .WithOne(v => v.Configuration)
                .HasForeignKey(v => v.ConfigurationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoredFeed>()
                .HasKey(v => v.Id);
            modelBuilder.Entity<StoredFeed>()
                .HasOne(v => v.Notification)
                .WithOne(v => v.Feed)
                .HasForeignKey<StoredNotification>(v => v.StoredFeedId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StoredNotification>().
                HasKey(v => v.NotificationId);

            modelBuilder.Entity<StoredReminder>()
                .HasKey(v => v.Id);
            modelBuilder.Entity<StoredReminder>()
                .HasMany(v => v.Instances)
                .WithOne(v => v.Reminder)
                .HasForeignKey(v => v.ReminderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ReminderInstance>()
                .HasKey(v => v.Id);
        }
    }
}