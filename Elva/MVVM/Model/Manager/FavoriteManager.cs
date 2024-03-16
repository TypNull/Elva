using Elva.MVVM.Model.Database;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Elva.MVVM.Model.Manager
{
    internal class FavoriteManager
    {
        public List<string> Favorites { get; } = new();
        public event EventHandler<string[]>? FavoriteChanged;
        private SettingsManager _settingsManager { get; }
        public FavoriteManager()
        {
            _settingsManager = App.Current.ServiceProvider.GetRequiredService<SettingsManager>();
            Favorites.AddRange(_settingsManager.Favorites);
        }

        public bool IsFavorite(string url)
        {
            if (Favorites.Contains(url))
                return true;
            return false;
        }

        public void AddFavorite(string url)
        {
            if (_settingsManager.Favorites.Contains(url))
                return;
            Favorites.Add(url);
            _settingsManager.AddToFavorite(url);
            FavoriteChanged?.Invoke(this, Favorites.ToArray());
        }

        public void RemoveFavorite(string url)
        {
            if (!_settingsManager.Favorites.Contains(url))
                return;
            Favorites.Remove(url);
            _settingsManager.RemoveFavorite(url);

            FavoriteChanged?.Invoke(this, Favorites.ToArray());
        }

        public void UpdateBrowserBookmarks()
        {

        }
    }
}
