using CommunityToolkit.Mvvm.ComponentModel;
using Elva.Common;
using Elva.Common.Navigation;
using Elva.Pages.Shared.Models;
using Elva.Pages.Shared.ViewModels;
using Elva.Services.Database;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WebsiteScraper.Downloadable.Books;

namespace Elva.Pages.Home.ViewModels
{
    internal partial class HomeVM : ViewModelObject
    {
        [ObservableProperty]
        private ObservableCollection<HomeWebsiteVM> _websites = new();

        [ObservableProperty]
        private ObservableCollection<ComicVM>? _favorites = new();

        [ObservableProperty]
        private bool _favoritesVisible = false;

        [ObservableProperty]
        private bool _isLoading = true;

        private readonly WebsiteManager _websiteManager;
        private readonly FavoriteManager _favoriteManager;
        private readonly ComicDatabaseManager _databaseManager;
        private readonly SettingsManager _settingsManager;

        public ObservableCollection<ComicVM>? Favorites1 { get => Favorites; set => Favorites = value; }

        public HomeVM(INavigationService navigation) : base(navigation)
        {
            _websiteManager = App.Current.ServiceProvider.GetRequiredService<WebsiteManager>();
            _favoriteManager = App.Current.ServiceProvider.GetRequiredService<FavoriteManager>();
            _databaseManager = App.Current.ServiceProvider.GetRequiredService<ComicDatabaseManager>();
            _settingsManager = App.Current.ServiceProvider.GetRequiredService<SettingsManager>();

            _favoriteManager.FavoriteChanged += FavoriteChanged;
            _websiteManager.WebsiteAdded += WebsiteManagerOnWebsiteAdded;
            InitializeAsync();
        }

        private void WebsiteManagerOnWebsiteAdded(object? sender, EventArgs e) => Application.Current.Dispatcher.Invoke(SetWebsiteCollection);

        private async void InitializeAsync()
        {
            try
            {
                await _settingsManager.SettingsLoadedTask;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        SetWebsiteCollection();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error setting website collection: {ex.Message}");
                    }
                });
                await Task.Run(LoadFavorites);
                _databaseManager.RegisterForFullLoad(() =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            RefreshWebsiteData();
                            IsLoading = false;
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error refreshing website data: {ex.Message}");
                            IsLoading = false;
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in InitializeAsync: {ex.Message}");
                Application.Current.Dispatcher.Invoke(() => IsLoading = false);
            }
        }

        private void FavoriteChanged(object? sender, string[] e) => LoadFavorites();

        private void LoadFavorites()
        {
            try
            {
                string[] favorites = ResizeArray(_favoriteManager.Favorites.AsEnumerable().Reverse().ToArray(), 9);
                bool needsUpdate = Favorites == null ||
                                  favorites.Length != Favorites.Count ||
                                  !favorites.All(x => Favorites.Select(y => y.Url).Contains(x));

                if (!needsUpdate)
                    return;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        ObservableCollection<ComicVM> newFavorites = new();

                        foreach (string url in favorites)
                        {
                            WebsiteScraper.WebsiteUtilities.Website? website = _websiteManager.GetWebsite(new Uri(url).Host);
                            if (website != null)
                            {
                                Comic comic = new(url, url, website);
                                ComicVM comicVM = new(comic);
                                if (!string.IsNullOrEmpty(comicVM.Website))
                                {
                                    newFavorites.Add(comicVM);
                                }
                            }
                        }

                        Favorites = newFavorites;
                        FavoritesVisible = Favorites.Any();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error creating favorite ComicVMs: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadFavorites: {ex.Message}");
            }
        }

        private static string[] ResizeArray(string[] array, int length)
        {
            if (array.Length > length)
                Array.Resize(ref array, length);
            return array;
        }

        private void SetWebsiteCollection()
        {
            try
            {
                ObservableCollection<HomeWebsiteVM> newWebsites = [];
                foreach (WebsiteScraper.WebsiteUtilities.Website website in _websiteManager.Websites)
                    newWebsites.Add(new HomeWebsiteVM(website));
                Websites = newWebsites;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in SetWebsiteCollection: {ex.Message}");
            }
        }

        private void RefreshWebsiteData()
        {
            try
            {
                foreach (HomeWebsiteVM websiteVM in Websites)
                    websiteVM.RefreshData();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in RefreshWebsiteData: {ex.Message}");
            }
        }
    }
}