using System;
using System.Linq;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.MVVM.Model.Manager
{
    internal class WebsiteManager
    {
        public Website[] Websites { get; private set; } = Array.Empty<Website>();


        public event EventHandler? WebsiteAdded;
        public WebsiteManager(params Website[] websites) { AddWebsite(websites); }

        public void AddWebsite(params Website[] websites)
        {
            Websites = Websites.Concat(websites).ToArray();
            WebsiteAdded?.Invoke(this, EventArgs.Empty);
        }

        public Website? GetWebsite(string wholeName)
        {
            if (wholeName.Length == 0)
                return null;
            return Array.Find(Websites, x => x.Name.ToLower() == wholeName.Split('.')[0].ToLower().Trim()
            || wholeName.ToLower().Trim().Contains((x.Name + x.Suffix).ToLower()));
        }
    }
}
