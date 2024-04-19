using Elva.MVVM.ViewModel.Model;
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

namespace Elva.MVVM.Model.Manager
{
    public enum SearchStatus
    {
        None,
        Searching,
        Finished
    }
    internal class SearchManager
    {
        public WebsiteSearch ActualSearch { get; private set; }
        private WebsiteSearch? _lastSearched;

        public event EventHandler<PropertyChangedEventArgs>? OnSearchChanged;
        public event EventHandler<ReadOnlyCollection<ComicVM>>? OnSearchObjectsChanged;
        public event EventHandler<SearchStatus>? OnSearchStatusChanged;

        public ReadOnlyCollection<ComicVM> SearchObjects => _searchObjects.AsReadOnly();

        private List<ComicVM> _searchObjects;
        public bool CanSearchNext => _lastSearched?.CanSearchNext == true;
        public bool SearchHasChanged => !ActualSearch.Equals(_lastSearched);

        public SearchStatus SearchStatus
        {
            get => _searchStatus;
            private set
            {
                _searchStatus = value;
                OnSearchStatusChanged?.Invoke(this, SearchStatus);
            }
        }
        private static SearchStatus _searchStatus = SearchStatus.None;

        public SearchManager()
        {
            Website[] websites = App.Current.ServiceProvider.GetRequiredService<WebsiteManager>().Websites;
            ActualSearch = new(websites.Length > 0 ? [websites[0]] : []);
            ActualSearch.PropertyChanged += ActualSearch_PropertyChanged;
            _searchObjects = [];
        }

        private void ActualSearch_PropertyChanged(object? sender, PropertyChangedEventArgs e) => OnSearchChanged?.Invoke(sender, e);

        public async Task StartSearchAsync()
        {
            SearchStatus = SearchStatus.Searching;
            Task<Comic[]> task = null!;
            if (SearchHasChanged)
            {
                task = ActualSearch.SearchAsync();
                _searchObjects.Clear();
                OnSearchObjectsChanged?.Invoke(this, SearchObjects);
                _searchObjects.AddRange((await task).Select(x => new ComicVM(x)));
                _lastSearched = ActualSearch.Clone();
            }
            else
            {
                task = _lastSearched!.SearchNextAsync();
                _searchObjects.AddRange((await task).Select(x => new ComicVM(x)));
            }
            SearchStatus = SearchStatus.Finished;
            OnSearchObjectsChanged?.Invoke(this, SearchObjects);
        }

        public void StartSearch() => Task.Run(StartSearchAsync);


        public void Clear()
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

        internal void StartSearchNext() => Task.Run(StartSearchNextAsync);

        private async Task StartSearchNextAsync()
        {
            Debug.WriteLine("Start SearchNext");
            SearchStatus = SearchStatus.Searching;
            Task<Comic[]> task = null!;
            if (CanSearchNext)
                task = _lastSearched!.SearchNextAsync();
            else return;
            _searchObjects.AddRange((await task).Select(x => new ComicVM(x)));
            OnSearchObjectsChanged?.Invoke(this, SearchObjects);
            SearchStatus = SearchStatus.Finished;
        }
    }
}
