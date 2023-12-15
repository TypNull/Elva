using AngleSharp.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebsiteScraper.Downloadable.Books;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.MVVM.Model
{
    internal partial class WebsiteSearch : ObservableObject, IEquatable<WebsiteSearch>
    {
        public ObservableCollection<Website> Websites { get; private set; }

        public IReadOnlyDictionary<string, string> TextTags => _textTags;
        private Dictionary<string, string> _textTags = new();
        public IReadOnlyDictionary<string, EnableAbleState> EnableAbleTags => _enableAbleTags;
        public Dictionary<string, EnableAbleState> _enableAbleTags = new();
        public IReadOnlyDictionary<string, DisableAbleState> DisableAbleTags => _disableAbleTags;
        public Dictionary<string, DisableAbleState> _disableAbleTags = new();
        public IReadOnlyDictionary<string, RadioTag> RadioTags => _radioTags;
        public Dictionary<string, RadioTag> _radioTags = new();


        [ObservableProperty]
        private string _searchInput;
        public bool IsEmpty => SearchInput == string.Empty && TextTags.All(x => string.IsNullOrEmpty(x.Value)) && EnableAbleTags.All(x => x.Value == EnableAbleState.NotSet) && DisableAbleTags.All(x => x.Value == DisableAbleState.NotSet) && RadioTags.All(x => x.Value.DefaultKey == x.Value.EnabledKey);

        [ObservableProperty]
        private bool _canSearchNext;

        public WebsiteSearch(params Website[] websites)
        {
            _searchInput = string.Empty;
            Websites = new();
            AddWebsite(websites);
        }

        public void SetTextTag(string key, string input)
        {
            if (!Websites.Any())
                return;

            if (!_textTags.ContainsKey(key))
                return;

            _textTags[key] = input;
            OnPropertyChanged($"{nameof(TextTags)}_{key}");
        }


        public void AddTag(params string[] newTags)
        {
            if (!Websites.Any())
                return;

            foreach (string newTag in Array.ConvertAll(newTags, x => x.Trim().ToLower()))
            {
                if (EnableAbleTags.Any(x => x.Key.ToLower().Equals(newTag)) || EnableAbleTags.Any(x => newTag[1..^1].Equals(x.Key.ToLower())))
                {
                    if (EnableAbleTags.TryGetValue(newTag, out EnableAbleState value))
                    {
                        if (value == EnableAbleState.Enabled)
                            _enableAbleTags[newTag] = EnableAbleState.NotSet;
                        else
                            _enableAbleTags[newTag] = EnableAbleState.Enabled;

                        OnPropertyChanged(nameof(EnableAbleTags));
                    }
                }
                else
                {
                    if (DisableAbleTags.Any(x => newTag[1..^1].Equals(x.Key)))
                    {
                        _disableAbleTags[newTag[1..^1]] = DisableAbleState.Disabled;
                        OnPropertyChanged(nameof(DisableAbleTags));
                    }
                    else
                    if (DisableAbleTags.Any(x => x.Key.Equals(newTag)))
                    {
                        if (DisableAbleTags.TryGetValue(newTag, out DisableAbleState value))
                        {
                            if (value == DisableAbleState.Enabled)
                                _disableAbleTags[newTag] = DisableAbleState.Disabled;
                            else if (value == DisableAbleState.Disabled)
                                _disableAbleTags[newTag] = DisableAbleState.NotSet;
                            else
                                _disableAbleTags[newTag] = DisableAbleState.Enabled;

                            OnPropertyChanged(nameof(DisableAbleTags));
                        }
                    }

                }
            }
        }

        public void AddWebsite(params Website[] websites)
        {
            if (!websites.Any())
                return;

            foreach (Website website in websites)
                if (!Websites.Contains(website))
                    Websites.Add(website);

            _disableAbleTags = Websites.SelectMany(x => x.DisableTags ?? Array.Empty<DisableAbleTag>()).GroupBy(x => x.Title).Where(g => g.Count() == Websites.Count).ToDictionary(x => x.Key, x => DisableAbleTags.ContainsKey(x.Key) ? DisableAbleTags[x.Key] : DisableAbleState.NotSet, StringComparer.OrdinalIgnoreCase);
            _enableAbleTags = Websites.SelectMany(x => x.EnableTags ?? Array.Empty<EnableAbleTag>()).GroupBy(x => x.Title).Where(g => g.Count() == Websites.Count).ToDictionary(x => x.Key, x => EnableAbleTags.ContainsKey(x.Key) ? EnableAbleTags[x.Key] : EnableAbleState.NotSet, StringComparer.OrdinalIgnoreCase);
            _textTags = Websites.SelectMany(x => x.TextTags ?? Array.Empty<TextTag>()).GroupBy(x => x.Title).Where(g => g.Count() == Websites.Count).ToDictionary(x => x.Key, x => TextTags.ContainsKey(x.Key) ? TextTags[x.Key] : string.Empty, StringComparer.OrdinalIgnoreCase);
            _radioTags = Websites.SelectMany(x => x.RadioTags ?? Array.Empty<RadioTag>()).GroupBy(x => x).Where(g => g.Count() == Websites.Count).ToDictionary(x => x.Key.Key, x => x.Key);

            OnPropertyChanged(nameof(DisableAbleTags));
            OnPropertyChanged(nameof(EnableAbleTags));
            OnPropertyChanged(nameof(RadioTags));
            OnPropertyChanged(nameof(TextTags));
            OnPropertyChanged(nameof(Websites));
            CanSearchNext = false;
        }


        public bool Equals(WebsiteSearch? other)
        {
            if (other == null)
                return false;

            if (!SearchInput.Equals(other.SearchInput))
                return false;
            if (!Websites.SequenceEqual(other.Websites))
                return false;
            if (!ProofDictionary(_enableAbleTags, other?._enableAbleTags))
                return false;
            if (!ProofDictionary(_disableAbleTags, other?._disableAbleTags))
                return false;
            if (!ProofDictionary(_textTags, other?._textTags))
                return false;
            if (!ProofDictionary(_radioTags, other?._radioTags))
                return false;
            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as WebsiteSearch);

        public static bool ProofDictionary<TKey, TValue>(IDictionary<TKey, TValue> x, IDictionary<TKey, TValue>? y)
        {
            // early-exit checks
            if (null == y)
                return null == x;
            if (null == x)
                return false;
            if (object.ReferenceEquals(x, y))
                return true;
            if (x.Count != y.Count)
                return false;

            // check keys are the same
            foreach (TKey k in x.Keys)
                if (!y.ContainsKey(k))
                    return false;

            // check values are the same
            foreach (TKey k in x.Keys)
                if (!x[k]?.Equals(y[k]) ?? true)
                    return false;

            return true;
        }

        internal WebsiteSearch Clone()
        {
            WebsiteSearch newSearch = new(Websites.ToArray())
            {
                SearchInput = SearchInput,
            };
            foreach (KeyValuePair<string, DisableAbleState> entry in DisableAbleTags)
                newSearch._disableAbleTags[entry.Key] = entry.Value;
            foreach (KeyValuePair<string, EnableAbleState> entry in EnableAbleTags)
                newSearch._enableAbleTags[entry.Key] = entry.Value;
            foreach (KeyValuePair<string, string> entry in TextTags)
                newSearch._textTags[entry.Key] = entry.Value;
            foreach (KeyValuePair<string, RadioTag> entry in RadioTags)
                newSearch._radioTags[entry.Key] = entry.Value with { };
            newSearch.CanSearchNext = CanSearchNext;
            return newSearch;
        }

        public async Task<Comic[]> SearchAsync()
        {
            Debug.WriteLine("Search");
            List<Task<Comic[]>> comicTasks = new();
            foreach (Website website in Websites)
                comicTasks.Add(website.SearchAsync<Comic>(CreateSearchInfo(website)));

            Comic[] comics = ZipComic(await Task.WhenAll(comicTasks));
            CanSearchNext = Websites.Any(x => x.CanSearchNext);
            Debug.WriteLine("Found: " + comics.Length);
            return comics;
        }

        private static Comic[] ZipComic(params Comic[][] comics)
        {
            if (comics.Length == 0)
                return Array.Empty<Comic>();
            if (comics.Length == 1)
                return comics[0];
            Comic[] result = new Comic[comics.Sum(x => x.Length)];
            int index = 0;
            Comic[] higestComics = comics.OrderByDescending(x => x.Length).First();
            Comic[] secondMaxArray = comics.OrderByDescending(x => x.Length).Skip(1).First();

            for (int i = 0; i < secondMaxArray.Length; i++)
                foreach (Comic[] comicArray in comics)
                    result[index++] = comicArray[i];
            if (higestComics.Length > secondMaxArray.Length)
                Array.Copy(higestComics, secondMaxArray.Length, result, index, higestComics.Length - secondMaxArray.Length);
            return result;
        }

        private SearchInfo CreateSearchInfo(Website website)
        {
            SearchInfo searchInfo = new(SearchInput, website);
            foreach (KeyValuePair<string, EnableAbleState> tag in EnableAbleTags)
                if (searchInfo.EnableAbleTags.TryGetValue(tag.Key.ToLower(), out EnableAbleTag? searchTag))
                    searchTag.State = tag.Value;
            foreach (KeyValuePair<string, DisableAbleState> tag in DisableAbleTags)
                if (searchInfo.DisableAbleTags.TryGetValue(tag.Key.ToLower(), out DisableAbleTag? searchTag))
                    searchTag.State = tag.Value;
            foreach (KeyValuePair<string, RadioTag> tag in RadioTags)
                if (searchInfo.RadioTags.TryGetValue(tag.Key.ToLower(), out RadioTag? searchTag))
                    searchTag.EnabledKey = tag.Value.EnabledKey;
            foreach (KeyValuePair<string, string> tag in TextTags)
                if (searchInfo.TextTags.TryGetValue(tag.Key, out TextTag? searchTag))
                    searchTag.Input = tag.Value;
            return searchInfo;
        }

        public async Task<Comic[]> SearchNextAsync()
        {
            Debug.WriteLine("SearchNext");
            if (!CanSearchNext)
                return Array.Empty<Comic>();
            List<Task<Comic[]>> comicTasks = new();
            foreach (Website website in Websites)
                if (website.CanSearchNext)
                    comicTasks.Add(website.SearchNextAsync<Comic>());

            Comic[] comics = ZipComic(await Task.WhenAll(comicTasks));
            CanSearchNext = Websites.Any(x => x.CanSearchNext);
            Debug.WriteLine("Found: " + comics.Length);
            return comics;
        }

        public override int GetHashCode() => HashCode.Combine(TextTags, SearchInput, EnableAbleTags, DisableAbleTags, RadioTags, Websites);


        internal void Clear()
        {
            _disableAbleTags.Clear();
            _enableAbleTags.Clear();
            _textTags.Clear();
            _radioTags.Clear();
            SearchInput = string.Empty;
            Websites.Clear();
            OnPropertyChanged(nameof(Websites));
            CanSearchNext = false;
        }

        internal void SetRadioTag(string key, string enabledKey)
        {
            if (!Websites.Any())
                return;
            if (!_radioTags.ContainsKey(key))
                return;
            RadioTags[key].EnabledKey = enabledKey;
            OnPropertyChanged($"{nameof(RadioTags)}_{key}");
        }
    }
}
