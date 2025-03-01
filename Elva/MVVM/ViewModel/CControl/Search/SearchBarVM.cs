using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Core;
using Elva.MVVM.Model;
using Elva.MVVM.Model.Manager;
using Elva.MVVM.ViewModel.CControl.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.MVVM.ViewModel.CControl.Search
{
    internal partial class SearchBarVM : ViewModelObject
    {
        [ObservableProperty]
        private string _websiteText = string.Empty;

        [ObservableProperty]
        private string _tagText = string.Empty;

        [ObservableProperty]
        private string _input = string.Empty;

        [ObservableProperty]
        private bool _hasError = false;

        private string[] _texts = new string[4];
        private string _searched = string.Empty;

        private SearchManager _searchManager;
        private WebsiteManager _websiteManager;
        private CommandInterpreter _comInterpreter;

        // Property for the view to bind to
        public Brush Foreground => HasError ? Application.Current.Resources["SearchBarError"] as SolidColorBrush : Application.Current.Resources["SearchBarNormal"] as SolidColorBrush;

        public SearchBarVM(INavigationService navigation) : base(navigation)
        {
            _comInterpreter = new();
            _searchManager = GetService<SearchManager>();
            _websiteManager = GetService<WebsiteManager>();
            _searchManager.OnSearchChanged += SearchChanged;
            PropertyChanged += OnPropertyChanged;
            WebsiteText = string.Join(" + ", _searchManager.ActualSearch.Websites.Select(x => x.Name + x.Suffix).ToArray());
            WebsiteText = string.IsNullOrWhiteSpace(WebsiteText) ? ">No website found<" : WebsiteText;

            // Subscribe to theme changes
            GetService<SettingsVM>().OnThemeChanged += (sender, theme) => OnPropertyChanged(nameof(Foreground));
        }

        private void SearchChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Websites")
                WebsiteText = string.Join(" + ", (sender as WebsiteSearch)!.Websites.Select(x => x.Name + x.Suffix).ToArray());
            SetTagText(e);
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Input))
                HasError = false;

            if (e.PropertyName == nameof(HasError))
                OnPropertyChanged(nameof(Foreground));
        }

        public void SetTagText(System.ComponentModel.PropertyChangedEventArgs e)
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
            else if (e.PropertyName is "SearchInput")
            {
                if (!string.IsNullOrWhiteSpace(_searchManager.ActualSearch.SearchInput))
                    _searched = "   Searched: " + _searchManager.ActualSearch.SearchInput;
                else _searched = string.Empty;
            }
            string tags = string.Join("   ", _texts.Where(x => !string.IsNullOrEmpty(x)));
            if (!string.IsNullOrWhiteSpace(tags))
                tags = "   Tags: " + tags;
            else tags = string.Empty;
            TagText = _searched + tags;
        }

        [RelayCommand]
        private void Enter() => InterpretInput();

        private void InterpretInput()
        {
            if (Input.Trim().Length < 3)
            {
                HasError = true;
                return;
            }
            string[][] parts = SplitInput();
            string back = string.Empty;
            if (parts[0].Length != 0)
            {
                Website[] websites = _searchManager.ActualSearch.Websites.ToArray();
                if (parts[0][0] != "+")
                    _searchManager.ActualSearch.Websites.Clear();
                else parts[0] = parts[0][1..];
                foreach (string websiteString in parts[0])
                {
                    Website? website = _websiteManager.GetWebsite(websiteString);
                    if (website != null)
                        _searchManager.ActualSearch.AddWebsite(website);
                    else
                        back += $"@{websiteString} ";
                }
                if (_searchManager.ActualSearch.Websites.Count == 0)
                    _searchManager.ActualSearch.AddWebsite(websites);
            }
            if (parts[1].Length != 0)
                _searchManager.ActualSearch.AddTag(parts[1]);

            if (parts[2].Length != 0)
            {
                _searchManager.ActualSearch.SearchInput = string.Join(" ", parts[2]);
                if (_searchManager.ActualSearch.SearchInput != string.Empty && back == string.Empty)
                    _searchManager.StartSearch();

                Navigation.NavigateTo<SearchVM>();
            }
            Input = back.Trim();
            HasError = !string.IsNullOrEmpty(back);
        }

        private string[][] SplitInput()
        {
            string[] parts = SpeperatorRegex().Split(Input);
            List<string>[] lists = new List<string>[] { new(), new(), new() };
            int i = 2, j = 2;
            foreach (string part in parts)
            {
                if (part is "@")
                    i = j = 0;
                else if (part is "#")
                    i = j = 1;
                else if (part is ";" or "/")
                    i = j;
                else if (part is "+")
                {
                    if (i != 2)
                        lists[i].Add("+");
                    i = j;
                }
                else if (!string.IsNullOrWhiteSpace(part))
                {
                    lists[i].Add(part.Trim());
                    i = 2;
                }
            }
            return lists.Select(a => a.ToArray()).ToArray();
        }

        [GeneratedRegex("([#@;/+ ]|(r:))")]
        private static partial Regex SpeperatorRegex();
    }
}