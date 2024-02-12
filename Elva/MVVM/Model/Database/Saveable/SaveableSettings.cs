using Elva.MVVM.Model.Manager;
using System.Collections.Generic;

namespace Elva.MVVM.Model.Database.Saveable
{
    internal record SaveableSettings
    {
        public string DownloadPath { get; set; } = IOManager.DownloadPath;
        public Dictionary<string, (string url, ReferenceType typ)[]> HomeWebsiteComics { get; set; } = new();
    }
}
