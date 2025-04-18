using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Common;
using Elva.Common.Navigation;
using Elva.Pages.Shared.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.Pages.Search.ViewModels
{
    internal partial class GenreVM : ViewModelObject
    {
        [ObservableProperty]
        private ObservableCollection<string> _disabelAbleTags;
        [ObservableProperty]
        private ObservableCollection<string> _enableAbleTags;
        [ObservableProperty]
        private Dictionary<string, bool?> _enableData = new(StringComparer.OrdinalIgnoreCase);
        [ObservableProperty]
        private Dictionary<string, bool?> _disableData = new(StringComparer.OrdinalIgnoreCase);

        private SearchManager _searchManager;
        public GenreVM(INavigationService navigation) : base(navigation)
        {
            _searchManager = GetService<SearchManager>();
            _disabelAbleTags = new(_searchManager.ActualSearch.DisableAbleTags.Select(x => x.Key));
            _enableAbleTags = new(_searchManager.ActualSearch.EnableAbleTags.Select(x => x.Key));
            _searchManager.OnSearchChanged += OnSearchChanged;
        }

        private void OnSearchChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Websites")
            {
                DisabelAbleTags = new(_searchManager.ActualSearch.DisableAbleTags.Select(x => x.Key));
                EnableAbleTags = new(_searchManager.ActualSearch.EnableAbleTags.Select(x => x.Key));
            }
            else if (e.PropertyName == "EnableAbleTags")
            {
                EnableData.Clear();
                foreach (KeyValuePair<string, EnableAbleState> item in _searchManager.ActualSearch.EnableAbleTags)
                    EnableData.Add(item.Key.ToLower(), item.Value == EnableAbleState.NotSet ? false : true);
                OnPropertyChanged(nameof(EnableData));
            }
            else if (e.PropertyName == "DisableAbleTags")
            {
                DisableData.Clear();
                foreach (KeyValuePair<string, DisableAbleState> item in _searchManager.ActualSearch.DisableAbleTags)
                    switch (item.Value)
                    {
                        case DisableAbleState.NotSet:
                            DisableData.Add(item.Key, false);
                            break;
                        case DisableAbleState.Enabled:
                            DisableData.Add(item.Key, true);
                            break;
                        case DisableAbleState.Disabled:
                            DisableData.Add(item.Key, null);
                            break;
                    }
                OnPropertyChanged(nameof(DisableData));
            }

        }


        [RelayCommand]
        private void ChangeGenre(string genre) => _searchManager.ActualSearch.AddTag(genre);

    }
}
