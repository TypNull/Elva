using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebsiteScraper.Downloadable.Books;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.Pages.Search.Models
{
    internal partial class WebsiteSearch : ObservableObject, IEquatable<WebsiteSearch>
    {
        // Collection of websites to search
        public ObservableCollection<Website> Websites { get; private set; } = new();

        // Search parameters collections
        private Dictionary<string, string> _textTags = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, EnableAbleState> _enableAbleTags = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, DisableAbleState> _disableAbleTags = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, RadioTag> _radioTags = new(StringComparer.OrdinalIgnoreCase);

        // Exposed read-only collections for view binding
        public IReadOnlyDictionary<string, string> TextTags => _textTags;
        public IReadOnlyDictionary<string, EnableAbleState> EnableAbleTags => _enableAbleTags;
        public IReadOnlyDictionary<string, DisableAbleState> DisableAbleTags => _disableAbleTags;
        public IReadOnlyDictionary<string, RadioTag> RadioTags => _radioTags;

        [ObservableProperty]
        private string _searchInput = string.Empty;

        [ObservableProperty]
        private bool _canSearchNext;

        /// <summary>
        /// Checks if the search is empty (no search parameters set)
        /// </summary>
        public bool IsEmpty =>
            string.IsNullOrEmpty(SearchInput) &&
            TextTags.All(x => string.IsNullOrEmpty(x.Value)) &&
            EnableAbleTags.All(x => x.Value == EnableAbleState.NotSet) &&
            DisableAbleTags.All(x => x.Value == DisableAbleState.NotSet) &&
            RadioTags.All(x => x.Value.EnabledKey == x.Value.DefaultKey);

        public WebsiteSearch(params Website[] websites)
        {
            _searchInput = string.Empty;
            Websites = new();
            AddWebsite(websites);
        }

        /// <summary>
        /// Sets a text tag value and notifies of the change
        /// </summary>
        public void SetTextTag(string key, string input)
        {
            if (!Websites.Any() || !_textTags.ContainsKey(key))
                return;

            _textTags[key] = input;
            OnPropertyChanged($"{nameof(TextTags)}_{key}");
        }

        /// <summary>
        /// Sets a radio tag value and notifies of the change
        /// </summary>
        public void SetRadioTag(string key, string enabledKey)
        {
            if (!Websites.Any() || !_radioTags.ContainsKey(key))
                return;

            _radioTags[key].EnabledKey = enabledKey;
            OnPropertyChanged($"{nameof(RadioTags)}_{key}");
        }

        /// <summary>
        /// Adds or toggles tag states based on the input
        /// </summary>
        public void AddTag(params string[] newTags)
        {
            if (!Websites.Any() || newTags == null || newTags.Length == 0)
                return;

            foreach (string rawTag in newTags)
            {
                string tag = rawTag.Trim().ToLower();

                // Skip empty tags
                if (string.IsNullOrWhiteSpace(tag))
                    continue;

                // Handle exclusion tags in [tag] format
                if (tag.StartsWith("[") && tag.EndsWith("]"))
                {
                    string innerTag = tag.Substring(1, tag.Length - 2);
                    if (_disableAbleTags.ContainsKey(innerTag))
                    {
                        _disableAbleTags[innerTag] = DisableAbleState.Disabled;
                        OnPropertyChanged(nameof(DisableAbleTags));
                    }
                    continue;
                }

                // Handle enable-able tags
                if (_enableAbleTags.ContainsKey(tag))
                {
                    _enableAbleTags[tag] = _enableAbleTags[tag] == EnableAbleState.Enabled
                        ? EnableAbleState.NotSet
                        : EnableAbleState.Enabled;

                    OnPropertyChanged(nameof(EnableAbleTags));
                    continue;
                }

                // Handle disable-able tags
                if (_disableAbleTags.ContainsKey(tag))
                {
                    // Cycle through states: NotSet -> Enabled -> Disabled -> NotSet
                    _disableAbleTags[tag] = _disableAbleTags[tag] switch
                    {
                        DisableAbleState.NotSet => DisableAbleState.Enabled,
                        DisableAbleState.Enabled => DisableAbleState.Disabled,
                        DisableAbleState.Disabled => DisableAbleState.NotSet,
                        _ => DisableAbleState.NotSet
                    };

                    OnPropertyChanged(nameof(DisableAbleTags));
                }
            }
        }

        /// <summary>
        /// Adds websites to the search and refreshes available tags
        /// </summary>
        public void AddWebsite(params Website[] websites)
        {
            if (websites == null || !websites.Any())
                return;

            foreach (Website website in websites)
                if (!Websites.Contains(website))
                    Websites.Add(website);

            // Refresh available tags based on common tags across all websites
            RefreshAvailableTags();

            CanSearchNext = false;
        }

        /// <summary>
        /// Refreshes the available tags based on the current websites
        /// </summary>
        private void RefreshAvailableTags()
        {
            // Save current tag states for restoration after refresh
            Dictionary<string, EnableAbleState> savedEnableStates = new(_enableAbleTags, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, DisableAbleState> savedDisableStates = new(_disableAbleTags, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> savedTextValues = new(_textTags, StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> savedRadioValues = new();

            foreach (KeyValuePair<string, RadioTag> pair in _radioTags)
                savedRadioValues[pair.Key] = pair.Value.EnabledKey;

            // Find common tags across all websites
            _disableAbleTags = Websites
                .SelectMany(x => x.DisableTags ?? Array.Empty<DisableAbleTag>())
                .GroupBy(x => x.Title)
                .Where(g => g.Count() == Websites.Count)
                .ToDictionary(
                    x => x.Key,
                    x => savedDisableStates.TryGetValue(x.Key, out DisableAbleState state) ? state : DisableAbleState.NotSet,
                    StringComparer.OrdinalIgnoreCase);

            _enableAbleTags = Websites
                .SelectMany(x => x.EnableTags ?? Array.Empty<EnableAbleTag>())
                .GroupBy(x => x.Title)
                .Where(g => g.Count() == Websites.Count)
                .ToDictionary(
                    x => x.Key,
                    x => savedEnableStates.TryGetValue(x.Key, out EnableAbleState state) ? state : EnableAbleState.NotSet,
                    StringComparer.OrdinalIgnoreCase);

            _textTags = Websites
                .SelectMany(x => x.TextTags ?? Array.Empty<TextTag>())
                .GroupBy(x => x.Title)
                .Where(g => g.Count() == Websites.Count)
                .ToDictionary(
                    x => x.Key,
                    x => savedTextValues.TryGetValue(x.Key, out string? value) ? value : string.Empty,
                    StringComparer.OrdinalIgnoreCase);

            _radioTags = Websites
                .SelectMany(x => x.RadioTags ?? Array.Empty<RadioTag>())
                .GroupBy(x => x)
                .Where(g => g.Count() == Websites.Count)
                .ToDictionary(
                    x => x.Key.Key,
                    x =>
                    {
                        RadioTag tag = x.Key with { }; // Clone the tag
                        if (savedRadioValues.TryGetValue(x.Key.Key, out string? value))
                            tag.EnabledKey = value;
                        return tag;
                    },
                    StringComparer.OrdinalIgnoreCase);

            // Notify property changes
            OnPropertyChanged(nameof(DisableAbleTags));
            OnPropertyChanged(nameof(EnableAbleTags));
            OnPropertyChanged(nameof(RadioTags));
            OnPropertyChanged(nameof(TextTags));
            OnPropertyChanged(nameof(Websites));
        }

        /// <summary>
        /// Creates a clone of this search for tracking changes
        /// </summary>
        internal WebsiteSearch Clone()
        {
            WebsiteSearch newSearch = new(Websites.ToArray())
            {
                SearchInput = SearchInput,
            };

            // Copy the dictionaries
            foreach (KeyValuePair<string, DisableAbleState> entry in _disableAbleTags)
                newSearch._disableAbleTags[entry.Key] = entry.Value;

            foreach (KeyValuePair<string, EnableAbleState> entry in _enableAbleTags)
                newSearch._enableAbleTags[entry.Key] = entry.Value;

            foreach (KeyValuePair<string, string> entry in _textTags)
                newSearch._textTags[entry.Key] = entry.Value;

            foreach (KeyValuePair<string, RadioTag> entry in _radioTags)
                newSearch._radioTags[entry.Key] = entry.Value with { }; // Clone the RadioTag

            newSearch.CanSearchNext = CanSearchNext;
            return newSearch;
        }

        /// <summary>
        /// Performs a search across all websites
        /// </summary>
        public async Task<Comic[]> SearchAsync()
        {
            Debug.WriteLine("Search");
            List<Task<Comic[]>> comicTasks = new();

            foreach (Website website in Websites)
                comicTasks.Add(website.SearchAsync<Comic>(CreateSearchInfo(website)));

            Comic[] comics = MergeResults(await Task.WhenAll(comicTasks));
            CanSearchNext = Websites.Any(x => x.CanSearchNext);

            Debug.WriteLine("Found: " + comics.Length);
            return comics;
        }

        /// <summary>
        /// Performs a "load more" search for next page of results
        /// </summary>
        public async Task<Comic[]> SearchNextAsync()
        {
            Debug.WriteLine("SearchNext");
            if (!CanSearchNext)
                return Array.Empty<Comic>();

            List<Task<Comic[]>> comicTasks = new();

            foreach (Website website in Websites)
                if (website.CanSearchNext)
                    comicTasks.Add(website.SearchNextAsync<Comic>());

            Comic[] comics = MergeResults(await Task.WhenAll(comicTasks));
            CanSearchNext = Websites.Any(x => x.CanSearchNext);

            Debug.WriteLine("Found: " + comics.Length);
            return comics;
        }

        /// <summary>
        /// Creates a search info object for a specific website
        /// </summary>
        private SearchInfo CreateSearchInfo(Website website)
        {
            SearchInfo searchInfo = new(SearchInput, website);

            // Apply tag states to the search info
            foreach (KeyValuePair<string, EnableAbleState> tag in _enableAbleTags)
                if (searchInfo.EnableAbleTags.TryGetValue(tag.Key.ToLower(), out EnableAbleTag? searchTag))
                    searchTag.State = tag.Value;

            foreach (KeyValuePair<string, DisableAbleState> tag in _disableAbleTags)
                if (searchInfo.DisableAbleTags.TryGetValue(tag.Key.ToLower(), out DisableAbleTag? searchTag))
                    searchTag.State = tag.Value;

            foreach (KeyValuePair<string, RadioTag> tag in _radioTags)
                if (searchInfo.RadioTags.TryGetValue(tag.Key.ToLower(), out RadioTag? searchTag))
                    searchTag.EnabledKey = tag.Value.EnabledKey;

            foreach (KeyValuePair<string, string> tag in _textTags)
                if (searchInfo.TextTags.TryGetValue(tag.Key, out TextTag? searchTag))
                    searchTag.Input = tag.Value;

            return searchInfo;
        }

        /// <summary>
        /// Clears all search parameters
        /// </summary>
        internal void Clear()
        {
            Website? firstWebsite = Websites.FirstOrDefault();

            _disableAbleTags.Clear();
            _enableAbleTags.Clear();
            _textTags.Clear();
            _radioTags.Clear();
            SearchInput = string.Empty;
            Websites.Clear();

            // If there was a website, add it back
            if (firstWebsite != null)
                AddWebsite(firstWebsite);

            OnPropertyChanged(nameof(Websites));
            CanSearchNext = false;
        }

        /// <summary>
        /// Intelligently merges results from multiple sources
        /// </summary>
        private static Comic[] MergeResults(params Comic[][] comics)
        {
            if (comics.Length == 0)
                return Array.Empty<Comic>();

            if (comics.Length == 1)
                return comics[0];

            // Calculate the total number of comics
            int totalComics = comics.Sum(arr => arr.Length);
            Comic[] result = new Comic[totalComics];

            // Find the arrays with maximum and second maximum lengths
            Comic[] maxLengthArray = comics.OrderByDescending(arr => arr.Length).First();
            Comic[] secondMaxArray = comics.OrderByDescending(arr => arr.Length).Skip(1).FirstOrDefault() ?? Array.Empty<Comic>();

            int resultIndex = 0;

            // Interleave results from all sources up to the length of the second longest array
            for (int i = 0; i < secondMaxArray.Length; i++)
            {
                foreach (Comic[] comicArray in comics)
                {
                    if (i < comicArray.Length)
                        result[resultIndex++] = comicArray[i];
                }
            }

            // Add any remaining comics from the longest array
            if (maxLengthArray.Length > secondMaxArray.Length)
            {
                int remainingCount = maxLengthArray.Length - secondMaxArray.Length;
                Array.Copy(
                    maxLengthArray,
                    secondMaxArray.Length,
                    result,
                    resultIndex,
                    remainingCount);
            }

            return result;
        }

        /// <summary>
        /// Determines if this search is equal to another
        /// </summary>
        public bool Equals(WebsiteSearch? other)
        {
            if (other == null)
                return false;

            // Compare search input and websites
            if (!SearchInput.Equals(other.SearchInput) ||
                !Websites.SequenceEqual(other.Websites))
                return false;

            // Compare dictionaries
            if (!CompareDictionaries(_enableAbleTags, other._enableAbleTags) ||
                !CompareDictionaries(_disableAbleTags, other._disableAbleTags) ||
                !CompareDictionaries(_textTags, other._textTags) ||
                !CompareDictionaries(_radioTags, other._radioTags))
                return false;

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as WebsiteSearch);

        /// <summary>
        /// Compares two dictionaries for equality
        /// </summary>
        private static bool CompareDictionaries<TKey, TValue>(
            IDictionary<TKey, TValue> x,
            IDictionary<TKey, TValue>? y)
        {
            // Quick checks
            if (y == null)
                return x == null;
            if (x == null)
                return false;
            if (ReferenceEquals(x, y))
                return true;
            if (x.Count != y.Count)
                return false;

            // Check all keys are the same
            foreach (TKey key in x.Keys)
                if (!y.ContainsKey(key))
                    return false;

            // Check values are equal
            foreach (TKey key in x.Keys)
            {
                if (!EqualityComparer<TValue>.Default.Equals(x[key], y[key]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                SearchInput,
                Websites,
                _enableAbleTags.Count,
                _disableAbleTags.Count,
                _textTags.Count,
                _radioTags.Count);
        }
    }
}