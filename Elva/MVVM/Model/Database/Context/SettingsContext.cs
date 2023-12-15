using Elva.MVVM.Model.Database.Saveable;
using Elva.MVVM.Model.Manager;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Elva.MVVM.Model.Database.Context
{
    internal class SettingsContext : DbContext
    {
        public DbSet<WebsiteHomeReference> WebsiteReferences { get; set; }
        public DbSet<ComicReference> ComicReferences { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.UseSqlite($"Data Source={IOManager.LocalDataPath}settings.db");
        }

        public void ClearWebsiteReference() => WebsiteReferences.RemoveRange(WebsiteReferences);

        public void DeleteComicReferences(string referenceType)
        {
            IQueryable<ComicReference> referennces = ComicReferences.Where(x => x.ReferenceType == referenceType);
            ComicReferences.RemoveRange(referennces.ToArray());
        }

    }
}
