using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.MVVM.Model.Database;
using Elva.MVVM.Model.Database.Saveable;
using Elva.MVVM.Model.Manager;
using Elva.MVVM.ViewModel.Model;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebsiteScraper.Downloadable.Books;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.MVVM.ViewModel.CControl.Home
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
        private SettingsDatabaseManager _settingsManager;

        public string WebsiteName => WebsiteObject.Name + WebsiteObject.Suffix;

        public HomeWebsiteVM(Website website)
        {
            _websiteObject = website;
            _websiteLogo = _websiteObject.GetLogoPath();
            _settingsManager = App.Current.ServiceProvider.GetRequiredService<SettingsDatabaseManager>();
            LoadItemsFromDatabase();
            _ = LoadNewItems();
        }

        private void LoadItemsFromDatabase()
        {
            IEnumerable<WebsiteHomeReference> references = _settingsManager.Context.WebsiteReferences.Where(x => x.WebsiteID == WebsiteName);

            NewItems = ReferencesToCollection(references, ReferenceType.New);
            RecommendedItems = ReferencesToCollection(references, ReferenceType.Recommended);
            ChangeVisibility();
        }

        private ObservableCollection<ComicVM> ReferencesToCollection(IEnumerable<WebsiteHomeReference> references, ReferenceType typ) => new(references.Where(x => x.Type == typ).Select(c => new ComicVM(new(c.Url, c.Url, WebsiteObject))));

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
            AddToDatabase(RecommendedItems, ReferenceType.Recommended);
            AddToDatabase(NewItems, ReferenceType.New);
            _settingsManager.SaveData();
        }

        private void ResizeArray(ref Comic[] comics, int length)
        {
            if (comics.Length > length)
                Array.Resize(ref comics, length);
        }

        private void AddToDatabase(IEnumerable<ComicVM> values, ReferenceType type)
        {
            foreach (ComicVM value in values)
            {
                _settingsManager.Add(new WebsiteHomeReference()
                {
                    Type = type,
                    Url = value.Url,
                    WebsiteID = WebsiteName,
                });
            }
        }

        [RelayCommand]
        public void OpenWebsiteInfo()
        {
            Debug.WriteLine("OpenWebsite");
        }
    }


}
