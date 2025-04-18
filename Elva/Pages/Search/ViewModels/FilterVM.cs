using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Common;
using Elva.Common.Navigation;
using Elva.Pages.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.Pages.Search.ViewModels
{
    internal partial class FilterVM : ViewModelObject
    {

        [ObservableProperty]
        private bool _filterTextVisibility;
        [ObservableProperty]
        private bool _genreVisibility = true;
        [ObservableProperty]
        private bool _otherVisibility = true;
        [ObservableProperty]
        private bool _clearVisibility;
        [ObservableProperty]
        private bool _reloadVisibility;
        [ObservableProperty]
        private string _filterText;

        private string[] _texts = new string[4];
        private SearchManager _searchManager;
        public FilterVM(INavigationService navigation) : base(navigation)
        {
            Navigation.NavigateTo<SourceVM>();
            _filterText = string.Empty;
            _searchManager = GetService<SearchManager>();
            _searchManager.OnSearchChanged += SearchChanged;
            _searchManager.OnSearchStatusChanged += SearchStatusChanged;
        }

        private void SearchStatusChanged(object? sender, SearchStatus e)
        {
            if (e == SearchStatus.Searching)
                ReloadVisibility = false;
            else
                ReloadVisibility = _searchManager.SearchHasChanged;
        }

        private void SearchChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName?.StartsWith("RadioTags") == true)
            {
                IEnumerable<KeyValuePair<string, RadioTag>> radioTags = _searchManager.ActualSearch.RadioTags.Where(x => x.Value.EnabledKey != x.Value.DefaultKey);
                if (radioTags.Any())
                    _texts[3] = string.Join(", ", radioTags.Select(x => $"{x.Value.Title}: {x.Value.EnabledTag?.Title}"));
                else _texts[3] = string.Empty;
            }
            else if (e.PropertyName?.StartsWith("TextTags") == true)
            {
                IEnumerable<KeyValuePair<string, string>> textTags = _searchManager.ActualSearch.TextTags.Where(x => !string.IsNullOrWhiteSpace(x.Value));
                if (textTags.Any())
                    _texts[2] = string.Join(", ", textTags.Select(x => $"{x.Key}: {x.Value}"));
                else _texts[2] = string.Empty;
            }
            else if (e.PropertyName is "DisableAbleTags")
            {
                IEnumerable<KeyValuePair<string, DisableAbleState>> disableTags = _searchManager.ActualSearch.DisableAbleTags.Where(x => x.Value == DisableAbleState.Disabled);
                if (disableTags.Any())
                    _texts[1] = "[" + string.Join(", ", disableTags.Select(x => x.Key)) + "]";
                else _texts[1] = string.Empty;
            }
            if (e.PropertyName is "EnableAbleTags" or "DisableAbleTags")
            {
                List<string> enableTags = _searchManager.ActualSearch.EnableAbleTags.Where(x => x.Value == EnableAbleState.Enabled).Select(x => x.Key).ToList();
                enableTags.AddRange(_searchManager.ActualSearch.DisableAbleTags.Where(x => x.Value == DisableAbleState.Enabled).Select(x => x.Key));
                if (enableTags.Any())
                    _texts[0] = string.Join(", ", enableTags);
                else _texts[0] = string.Empty;
            }
            FilterText = string.Join(";  ", _texts.Where(x => !string.IsNullOrEmpty(x)));
            FilterTextVisibility = FilterText != string.Empty;
            ClearVisibility = !_searchManager.ActualSearch.IsEmpty;
            ReloadVisibility = _searchManager.SearchHasChanged;
        }
        [RelayCommand]
        private void Clear() => _searchManager.Clear();


        [RelayCommand]
        private void Reload()
        {
            _searchManager.StartSearch();
        }

        [RelayCommand]
        private void ChangeFilterView(string filter)
        {
            if (Type.GetType($"Elva.Pages.Search.ViewModels.{filter}VM") is Type viewModel)
                Navigation.NavigateTo(viewModel);
        }
    }
}