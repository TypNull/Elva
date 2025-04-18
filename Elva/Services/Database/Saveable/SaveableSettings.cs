using Elva.Pages.Shared.Models;
using System.Collections.Generic;

namespace Elva.Services.Database.Saveable
{
    internal record SaveableSettings
    {
        public string DownloadPath { get; set; } = IOManager.DownloadPath;
        public Dictionary<string, (string url, ReferenceType typ)[]> HomeWebsiteComics { get; set; } = new();
        public bool IsKillSwitchEnabled { get; set; } = true;
        public int LastExportIndex { get; set; }
        public List<string> Favorites { get; set; } = new();
        // New property for theme
        public string Theme { get; set; } = "Dark"; // Default to dark theme

        public virtual bool Equals(SaveableSettings? x)
        {
            if (base.Equals(x))
                if (Favorites.Equals(Favorites))
                    return true;
            return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}