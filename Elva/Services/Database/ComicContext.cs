using Elva.Pages.Shared.Models;
using Elva.Services.Database.Saveable;
using Microsoft.EntityFrameworkCore;

namespace Elva.Services.Database
{
    internal class ComicContext : DbContext
    {
        public DbSet<SaveableComic> Comics { get; set; }
        public DbSet<SaveableChapter> Chapter { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SaveableComic>().HasMany(e => e.Chapter).WithOne(e => e.Comic).HasForeignKey("ComicUrl").IsRequired();
            modelBuilder.Entity<SaveableComic>().HasIndex(e => e.Title);
            modelBuilder.Entity<SaveableComic>().HasIndex(e => e.WebsiteID);
            modelBuilder.Entity<SaveableChapter>().HasIndex(e => e.Order);

            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            optionsBuilder.UseSqlite($"Data Source={IOManager.LocalDataPath}Comics.db",
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
        }
    }
}