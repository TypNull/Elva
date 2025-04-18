﻿using CommunityToolkit.Mvvm.ComponentModel;
using Elva.Common;
using Elva.Common.Navigation;
using Elva.Pages.Shared.Models;
using Elva.Pages.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using WebsiteScraper.Downloadable.Books;

namespace Elva.Pages.Home.ViewModels
{
    internal partial class HomeVM : ViewModelObject
    {
        [ObservableProperty]
        private ObservableCollection<HomeWebsiteVM> _websites = null!;
        [ObservableProperty]
        private ObservableCollection<ComicVM>? _favorites = null;
        [ObservableProperty]
        private bool _favoritesVisible = false;
        private WebsiteManager _websiteManagaer;
        private FavoriteManager _favoriteManager;

        public ObservableCollection<ComicVM>? Favorites1 { get => _favorites; set => _favorites = value; }

        public HomeVM(INavigationService navigation) : base(navigation)
        {
            _websiteManagaer = App.Current.ServiceProvider.GetRequiredService<WebsiteManager>();
            _favoriteManager = App.Current.ServiceProvider.GetRequiredService<FavoriteManager>();
            _favoriteManager.FavoriteChanged += FavoriteChanged;
            SetFavoriteCollection();
            SetWebsiteCollection();
            _websiteManagaer.WebsiteAdded += (s, e) => SetWebsiteCollection();
        }

        private void FavoriteChanged(object? sender, string[] e) => SetFavoriteCollection();

        private void SetFavoriteCollection()
        {
            string[] favorites = ResizeArray(_favoriteManager.Favorites.AsEnumerable().Reverse().ToArray(), 9);
            if (favorites.Length == Favorites?.Count && favorites.All(x => Favorites.Select(y => y.Url).Contains(x)))
                return;

            Favorites = new ObservableCollection<ComicVM>(favorites.Select(x => new ComicVM(new Comic(x, x, _websiteManagaer.GetWebsite(new Uri(x).Host) ?? new()))).Where(x => !string.IsNullOrEmpty(x.Website)));
            FavoritesVisible = Favorites.Any();
        }

        private string[] ResizeArray(string[] array, int length)
        {
            if (array.Length > length)
                Array.Resize(ref array, length);
            return array;
        }

        private void SetWebsiteCollection() => Websites = new ObservableCollection<HomeWebsiteVM>(_websiteManagaer.Websites.Select(w => new HomeWebsiteVM(w)));
    }
}
