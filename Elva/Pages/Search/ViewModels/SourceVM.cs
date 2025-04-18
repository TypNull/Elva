using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Common;
using Elva.Common.Navigation;
using Elva.Pages.Shared.Models;
using System.Linq;

namespace Elva.Pages.Search.ViewModels
{
    internal partial class SourceVM : ViewModelObject
    {
        [ObservableProperty]
        public string[] _websiteNames;
        [ObservableProperty]
        public string[] _searchWebsiteNames;

        private SearchManager _searchManager;
        private WebsiteManager _websiteManager;

        public SourceVM(INavigationService navigation) : base(navigation)
        {
            _searchManager = GetService<SearchManager>();
            _websiteManager = GetService<WebsiteManager>();
            _websiteNames = _websiteManager.Websites.Select(s => s.Name + s.Suffix).ToArray();
            _searchWebsiteNames = _searchManager.ActualSearch.Websites.Select(s => s.Name + s.Suffix).ToArray();
            _searchManager.OnSearchChanged += OnSearchChanged;

        }

        private void OnSearchChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Websites")
                SearchWebsiteNames = _searchManager.ActualSearch.Websites.Select(s => s.Name + s.Suffix).ToArray();
        }

        [RelayCommand]
        private void ChangeWebsite(string websiteString)
        {
            WebsiteScraper.WebsiteUtilities.Website? website = _websiteManager.GetWebsite(websiteString);
            if (website != null)
            {
                _searchManager.ActualSearch.Websites.Clear();
                _searchManager.ActualSearch.AddWebsite(website);
            }
        }
    }
}
