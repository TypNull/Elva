using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Common;
using Elva.Common.Navigation;
using Elva.Pages.Search.Models;
using Elva.Pages.Settings.ViewModels;
using Elva.Pages.Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.Pages.Search.ViewModels
{
    internal partial class SearchBarVM : ViewModelObject
    {
        // UI-bound properties
        [ObservableProperty]
        private string _websiteText = string.Empty;

        [ObservableProperty]
        private string _tagText = string.Empty;

        [ObservableProperty]
        private string _input = string.Empty;

        [ObservableProperty]
        private bool _hasError = false;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        // Foreground property for error state visualization
        public Brush Foreground => HasError
            ? Application.Current.Resources["SearchBarError"] as SolidColorBrush
            : Application.Current.Resources["SearchBarNormal"] as SolidColorBrush;

        // Dependencies
        private readonly SearchManager _searchManager;
        private readonly WebsiteManager _websiteManager;

        // Search history management
        private readonly SearchHistory _searchHistory = new() { MaxHistorySize = 5 };

        // Navigation mode
        private bool _fullHistoryMode = false;

        public SearchBarVM(INavigationService navigation) : base(navigation)
        {
            _searchManager = GetService<SearchManager>();
            _websiteManager = GetService<WebsiteManager>();

            // Subscribe to events
            _searchManager.OnSearchChanged += SearchChanged;
            PropertyChanged += OnPropertyChanged;

            // Initialize website text
            UpdateWebsiteText();

            // Subscribe to theme changes
            GetService<SettingsVM>().OnThemeChanged += (sender, theme) =>
                OnPropertyChanged(nameof(Foreground));
        }

        private void UpdateWebsiteText()
        {
            string websiteNames = string.Join(" + ",
                _searchManager.ActualSearch.Websites.Select(x => x.Name + x.Suffix));

            WebsiteText = string.IsNullOrWhiteSpace(websiteNames)
                ? ">No website found<"
                : websiteNames;
        }

        private void SearchChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Websites")
                UpdateWebsiteText();

            UpdateTagText(e);
        }

        private void OnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Input))
            {
                HasError = false;
                ErrorMessage = string.Empty;
            }

            if (e.PropertyName == nameof(HasError))
                OnPropertyChanged(nameof(Foreground));
        }

        public void UpdateTagText(System.ComponentModel.PropertyChangedEventArgs e)
        {
            StringBuilder tagTextBuilder = new();

            // Add search text if present
            if (!string.IsNullOrWhiteSpace(_searchManager.ActualSearch.SearchInput))
                tagTextBuilder.Append($"Searched: {_searchManager.ActualSearch.SearchInput}   ");

            // Add enabled tags
            IEnumerable<string> enabledTags = _searchManager.ActualSearch.EnableAbleTags
                .Where(x => x.Value == EnableAbleState.Enabled)
                .Select(x => x.Key)
                .Concat(_searchManager.ActualSearch.DisableAbleTags
                    .Where(x => x.Value == DisableAbleState.Enabled)
                    .Select(x => x.Key));

            if (enabledTags.Any())
                tagTextBuilder.Append($"Tags: {string.Join(", ", enabledTags)}   ");

            // Add disabled tags
            IEnumerable<string> disabledTags = _searchManager.ActualSearch.DisableAbleTags
                .Where(x => x.Value == DisableAbleState.Disabled)
                .Select(x => x.Key);

            if (disabledTags.Any())
                tagTextBuilder.Append($"[{string.Join(", ", disabledTags)}]   ");

            // Add text tags
            IEnumerable<string> textTags = _searchManager.ActualSearch.TextTags
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => $"{x.Key}: {x.Value}");

            if (textTags.Any())
                tagTextBuilder.Append(string.Join(", ", textTags));

            // Add radio tags
            IEnumerable<string> radioTags = _searchManager.ActualSearch.RadioTags
                .Where(x => x.Value.EnabledKey != x.Value.DefaultKey)
                .Select(x => $"{x.Value.Title}: {x.Value.EnabledTag?.Title}");

            if (radioTags.Any())
            {
                if (tagTextBuilder.Length > 0)
                    tagTextBuilder.Append("   ");
                tagTextBuilder.Append(string.Join(", ", radioTags));
            }

            TagText = tagTextBuilder.ToString().Trim();
        }

        [RelayCommand]
        private void Enter() => ProcessSearchInput();

        /// <summary>
        /// Command to navigate up in search history (previous searches)
        /// </summary>
        [RelayCommand]
        private void NavigateHistoryUp()
        {
            string historyItem = _searchHistory.NavigateUp(_fullHistoryMode);
            if (!string.IsNullOrEmpty(historyItem))
            {
                Input = historyItem;
            }
            else if (_searchHistory.GetEntries().Count > 0)
            {
                // If navigating up didn't give a result but history exists,
                // try navigating up with the other mode (fallback)
                Input = _searchHistory.NavigateUp(!_fullHistoryMode);
            }
        }

        /// <summary>
        /// Command to navigate down in search history (more recent searches)
        /// </summary>
        [RelayCommand]
        private void NavigateHistoryDown()
        {
            string historyItem = _searchHistory.NavigateDown(_fullHistoryMode);
            if (string.IsNullOrEmpty(historyItem) && _searchHistory.GetEntries().Count > 0)
            {
                // If nothing was retrieved (first time down), try other mode
                historyItem = _searchHistory.NavigateDown(!_fullHistoryMode);
            }
            Input = historyItem; // Will set to empty string if beginning/empty history
        }

        /// <summary>
        /// Command to toggle between full/simple history mode
        /// </summary>
        [RelayCommand]
        private void ToggleFullHistory(string direction)
        {
            _fullHistoryMode = !_fullHistoryMode;

            // After toggling, perform the navigation action
            // Force starting from the beginning to get consistent behavior
            _searchHistory.ResetNavigation();

            if (direction == "Up")
                NavigateHistoryUp();
            else if (direction == "Down")
                NavigateHistoryDown();
        }

        /// <summary>
        /// Clears the input and resets history navigation
        /// </summary>
        [RelayCommand]
        private void ClearInput()
        {
            Input = string.Empty;
            _searchHistory.ResetNavigation();
        }

        /// <summary>
        /// Process the search input and perform search operations
        /// </summary>
        private void ProcessSearchInput()
        {
            // Reset error state
            HasError = false;
            ErrorMessage = string.Empty;
            _searchHistory.ResetNavigation();

            // Save the original input for potential error restoration
            string originalInput = Input.Trim();

            // Validate input length
            if (originalInput.Length < 3)
            {
                HasError = true;
                ErrorMessage = "Search query too short";
                return;
            }

            try
            {
                // Parse the search query
                SearchQuery searchQuery = new(originalInput);

                // Process the query through search manager
                SearchQueryResult result = _searchManager.ProcessQuery(searchQuery);

                if (!result.Success)
                {
                    HasError = true;
                    ErrorMessage = result.ErrorMessage;
                    Input = result.UnrecognizedInput;
                    return;
                }

                // Save the search to history (only successful searches)
                _searchHistory.AddEntry(originalInput, searchQuery.SearchText);

                // Navigate to search view if needed
                if (result.ShouldNavigateToSearch)
                {
                    Navigation.NavigateTo<SearchVM>();

                    // Clear input after successful search
                    if (string.IsNullOrEmpty(result.UnrecognizedInput))
                        Input = string.Empty;
                }
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                HasError = true;
                ErrorMessage = "Invalid search query";
                Debug.WriteLine($"Search processing error: {ex.Message}");
            }
        }
    }
}