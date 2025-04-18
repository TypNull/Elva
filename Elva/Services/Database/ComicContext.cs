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
            base.OnModelCreating(modelBuilder);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseSqlite($"Data Source={IOManager.LocalDataPath}Comics.db");
        }
    }
}
