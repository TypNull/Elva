using Elva.Pages.Search.Models;
using Elva.Pages.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebsiteScraper.Downloadable.Books;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.Pages.Shared.Models
{
    /// <summary>
    /// The result of processing a search query
    /// </summary>
    public class SearchQueryResult
    {
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
        public string UnrecognizedInput { get; set; } = string.Empty;
        public bool ShouldNavigateToSearch { get; set; }
    }

    public enum SearchStatus
    {
        None,
        Searching,
        Finished
    }

    internal class SearchManager
    {
        // Current and previous search state
        public WebsiteSearch ActualSearch { get; private set; }
        private WebsiteSearch? _lastSearched;

        // Events
        public event EventHandler<PropertyChangedEventArgs>? OnSearchChanged;
        public event EventHandler<ReadOnlyCollection<ComicVM>>? OnSearchObjectsChanged;
        public event EventHandler<SearchStatus>? OnSearchStatusChanged;

        // Search results and state
        public ReadOnlyCollection<ComicVM> SearchObjects => _searchObjects.AsReadOnly();
        private List<ComicVM> _searchObjects;

        // State properties
        public bool CanSearchNext => _lastSearched?.CanSearchNext == true;
        public bool SearchHasChanged => !ActualSearch.Equals(_lastSearched);

        // Search status
        public SearchStatus SearchStatus
        {
            get => _searchStatus;
            private set
            {
                _searchStatus = value;
                OnSearchStatusChanged?.Invoke(this, SearchStatus);
            }
        }
        private SearchStatus _searchStatus = SearchStatus.None;

        // Services
        private readonly WebsiteManager _websiteManager;

        public SearchManager()
        {
            _websiteManager = App.Current.ServiceProvider.GetRequiredService<WebsiteManager>();
            Website[] websites = _websiteManager.Websites;

            // Initialize with first website if available
            ActualSearch = new WebsiteSearch(websites.Length > 0 ? new[] { websites[0] } : Array.Empty<Website>());
            ActualSearch.PropertyChanged += ActualSearch_PropertyChanged;
            _searchObjects = new();
        }

        private void ActualSearch_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            OnSearchChanged?.Invoke(sender, e);
        }

        /// <summary>
        /// Process a structured search query
        /// </summary>
        public SearchQueryResult ProcessQuery(SearchQuery query)
        {
            SearchQueryResult result = new();
            List<string> unrecognizedSources = new();

            try
            {
                // Process source websites
                if (query.Sources.Count > 0)
                {
                    // Save current websites for potential restoration
                    Website[] currentWebsites = ActualSearch.Websites.ToArray();

                    // Clear website list if not appending
                    if (!query.AppendSources)
                        ActualSearch.Websites.Clear();

                    // Process each requested source
                    foreach (string sourceName in query.Sources)
                    {
                        Website? website = _websiteManager.GetWebsite(sourceName);
                        if (website != null)
                            ActualSearch.AddWebsite(website);
                        else
                            unrecognizedSources.Add(sourceName);
                    }

                    // If no valid websites were found and we cleared the list, restore previous
                    if (ActualSearch.Websites.Count == 0 && !query.AppendSources)
                        foreach (Website website in currentWebsites)
                            ActualSearch.AddWebsite(website);
                }

                // Process tags
                List<string> allTags = new(query.IncludeTags);
                foreach (string tag in query.ExcludeTags)
                    allTags.Add($"[{tag}]");

                if (allTags.Count > 0)
                    ActualSearch.AddTag(allTags.ToArray());

                // Process author search
                if (query.Parameters.TryGetValue("author", out string? authorName) ||
                    query.Parameters.TryGetValue("a", out authorName))
                {
                    // Try to use author text tag if available
                    if (ActualSearch.TextTags.ContainsKey("Author"))
                        ActualSearch.SetTextTag("Author", authorName);
                    else if (!string.IsNullOrWhiteSpace(authorName))
                    {
                        // Include author in search terms instead of modifying query
                        string authorSearch = $"author:{authorName}";
                        ActualSearch.SearchInput = string.IsNullOrWhiteSpace(query.SearchText)
                            ? authorSearch
                            : $"{authorSearch} {query.SearchText}".Trim();
                    }
                    else
                    {
                        // Normal search term
                        ActualSearch.SearchInput = query.SearchText;
                    }
                }
                else
                {
                    // No author parameter - use search text directly
                    ActualSearch.SearchInput = query.SearchText;
                }

                // Process search text
                if (!string.IsNullOrWhiteSpace(query.SearchText))
                {
                    ActualSearch.SearchInput = query.SearchText;
                    result.ShouldNavigateToSearch = true;

                    // Start search if there are no errors and we have search text
                    if (unrecognizedSources.Count == 0)
                        StartSearch();
                }
                else if (query.Sources.Count > 0 || allTags.Count > 0)
                {
                    // Navigate to search even with no search text if other params changed
                    result.ShouldNavigateToSearch = true;
                }

                // Set error info if there were unrecognized sources
                if (unrecognizedSources.Count > 0)
                {
                    result.Success = false;
                    result.UnrecognizedInput = string.Join(" ", unrecognizedSources.Select(s => $"@{s}"));
                    result.ErrorMessage = unrecognizedSources.Count == 1
                        ? "Unknown source"
                        : "Unknown sources";
                }
            }
            catch (Exception ex)
            {
                // Handle unexpected errors gracefully
                result.Success = false;
                result.ErrorMessage = "Error processing search query";
                Debug.WriteLine($"Search query error: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Starts a search operation
        /// </summary>
        public async Task StartSearchAsync()
        {
            SearchStatus = SearchStatus.Searching;
            try
            {
                Task<Comic[]> task;

                if (SearchHasChanged)
                {
                    // New search
                    task = ActualSearch.SearchAsync();
                    _searchObjects.Clear();
                    OnSearchObjectsChanged?.Invoke(this, SearchObjects);
                    _searchObjects.AddRange((await task).Select(x => new ComicVM(x)));
                    _lastSearched = ActualSearch.Clone();
                }
                else
                {
                    // Continue existing search
                    task = _lastSearched!.SearchNextAsync();
                    _searchObjects.AddRange((await task).Select(x => new ComicVM(x)));
                }

                SearchStatus = SearchStatus.Finished;
                OnSearchObjectsChanged?.Invoke(this, SearchObjects);
            }
            catch (Exception ex)
            {
                // Handle search errors
                Debug.WriteLine($"Search error: {ex.Message}");
                SearchStatus = SearchStatus.Finished;
            }
        }

        /// <summary>
        /// Start search asynchronously
        /// </summary>
        public void StartSearch() => Task.Run(StartSearchAsync);

        /// <summary>
        /// Clear search parameters
        /// </summary>
        public void Clear()
        {
            try
            {
                Website? website = ActualSearch.Websites.FirstOrDefault();
                if (website != null)
                {
                    _lastSearched = ActualSearch.Clone();
                    ActualSearch.Clear();
                    ActualSearch.AddWebsite(website);
                    ActualSearch_PropertyChanged(ActualSearch, new PropertyChangedEventArgs(""));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error clearing search: {ex.Message}");
            }
        }

        /// <summary>
        /// Start searching next page asynchronously
        /// </summary>
        internal void StartSearchNext() => Task.Run(StartSearchNextAsync);

        /// <summary>
        /// Search next page
        /// </summary>
        private async Task StartSearchNextAsync()
        {
            Debug.WriteLine("Start SearchNext");

            if (!CanSearchNext)
                return;

            SearchStatus = SearchStatus.Searching;

            try
            {
                Task<Comic[]> task = _lastSearched!.SearchNextAsync();
                _searchObjects.AddRange((await task).Select(x => new ComicVM(x)));
                OnSearchObjectsChanged?.Invoke(this, SearchObjects);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error searching next page: {ex.Message}");
            }
            finally
            {
                SearchStatus = SearchStatus.Finished;
            }
        }
    }
}