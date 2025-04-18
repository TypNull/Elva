using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Pages.Shared.Models;
using Elva.Pages.Shared.ViewModels;
using Elva.Services.Database;
using Elva.Services.Database.Saveable;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebsiteScraper.Downloadable.Books;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.Pages.Home.ViewModels
{
    internal partial class HomeWebsiteVM : ObservableObject
    {
        [ObservableProperty]
        private bool _visible = false;
        [ObservableProperty]
        private bool _newVisible = false;
        [ObservableProperty]
        private bool _recommendedVisible = false;
        [ObservableProperty]
        private ObservableCollection<ComicVM> _newItems = new();
        [ObservableProperty]
        private ObservableCollection<ComicVM> _recommendedItems = new();
        [ObservableProperty]
        private Website _websiteObject = null!;
        [ObservableProperty]
        private string _websiteLogo;
        private SettingsManager _settingsManager;

        public string WebsiteName => WebsiteObject.Name + WebsiteObject.Suffix;

        public HomeWebsiteVM(Website website)
        {
            _websiteObject = website;
            _websiteLogo = _websiteObject.GetLogoPath();
            _settingsManager = App.Current.ServiceProvider.GetRequiredService<SettingsManager>();
            LoadItemsFromDatabase();
            _ = LoadNewItems();
        }

        private void LoadItemsFromDatabase()
        {
            (string url, ReferenceType typ)[]? references = _settingsManager.GetHomeComics(WebsiteObject);
            if (references == null)
                return;
            NewItems = ReferencesToCollection(references, ReferenceType.New);
            RecommendedItems = ReferencesToCollection(references, ReferenceType.Recommended);
            ChangeVisibility();
        }

        private ObservableCollection<ComicVM> ReferencesToCollection((string url, ReferenceType typ)[] values, ReferenceType typ) => new(values.Where(x => x.typ == typ).Select(c => new ComicVM(new(c.url, c.url, WebsiteObject))));

        private void ChangeVisibility()
        {
            RecommendedVisible = RecommendedItems.Count > 0;
            NewVisible = NewItems.Count > 0;
            Visible = RecommendedVisible || NewVisible;
        }

        private async Task LoadNewItems()
        {
            Task<Comic[]> newTask = WebsiteObject.LoadNewAsync<Comic>();
            Task<Comic[]> recoTask = WebsiteObject.LoadExtraAsync<Comic>("Recommended");
            Comic[] newComic = await newTask;
            Comic[] recoComic = await recoTask;
            ResizeArray(ref newComic, 8);
            ResizeArray(ref recoComic, 8);
            NewItems = new(newComic.Select(c => new ComicVM(c)));
            RecommendedItems = new(recoComic.Select(c => new ComicVM(c)));

            ChangeVisibility();

            List<(string url, ReferenceType typ)> list = new();
            foreach (ComicVM value in RecommendedItems)
                list.Add((value.Url, ReferenceType.Recommended));
            foreach (ComicVM value in NewItems)
                list.Add((value.Url, ReferenceType.New));
            _settingsManager.SetHomeComics(WebsiteObject, list.ToArray());
            _settingsManager.SaveSettings();
        }

        private void ResizeArray(ref Comic[] comics, int length)
        {
            if (comics.Length > length)
                Array.Resize(ref comics, length);
        }


        [RelayCommand]
        public void OpenWebsiteInfo()
        {
            Debug.WriteLine("OpenWebsite");
        }
    }


}
