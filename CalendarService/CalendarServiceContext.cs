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
        public DbSet<StoredToken> Tokens { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(v => v.Id);
            modelBuilder.Entity<User>()
                .HasMany(v => v.Tokens)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId);
            modelBuilder.Entity<User>()
                .HasMany(v => v.ConfigStates)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId);

            modelBuilder.Entity<StoredConfigState>()
                .HasKey(v => v.State);
            modelBuilder.Entity<StoredConfigState>()
                .Property(v => v.StoredTime).IsRequired();

            modelBuilder.Entity<StoredToken>()
                .HasKey(v => v.Id);
        }
    }
}