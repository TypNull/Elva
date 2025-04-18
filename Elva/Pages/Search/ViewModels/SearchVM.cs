using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Common;
using Elva.Common.Navigation;
using Elva.Pages.Shared.Models;
using Elva.Pages.Shared.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;

namespace Elva.Pages.Search.ViewModels
{
    internal partial class SearchVM : ViewModelObject
    {
        [ObservableProperty]
        private FilterVM _filterVM;

        [ObservableProperty]
        private ObservableCollection<ComicVM> _items;

        [ObservableProperty]
        public bool _nothingFoundVisibility;
        [ObservableProperty]
        public bool _loadingVisibility;
        [ObservableProperty]
        public bool _loadMoreVisibility;

        private readonly ServiceProvider _serviceProvider;
        private SearchManager _searchManager;

        public SearchVM(INavigationService navigation) : base(navigation)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<FilterVM>();
            services.AddSingleton<SourceVM>();
            services.AddSingleton<GenreVM>();
            services.AddSingleton<InputVM>();
            services.AddSingleton<OtherVM>();
            services.AddSingleton<INavigationService, NavigationService>();
            services.AddSingleton<Func<Type, ViewModelObject>>(provider => viewModelType => (ViewModelObject)provider.GetRequiredService(viewModelType));
            _serviceProvider = services.BuildServiceProvider();
            _filterVM = _serviceProvider.GetRequiredService<FilterVM>();

            _items = new();
            _searchManager = GetService<SearchManager>();
            _searchManager.OnSearchStatusChanged += OnSearchStatusChanged;
            _searchManager.OnSearchObjectsChanged += SearchObjectsChanged;
            ConnectionManager.ConnectionChanged += ConnectionChanged;
        }

        private void SearchObjectsChanged(object? sender, ReadOnlyCollection<ComicVM> e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (e.Count == 0)
                    Items.Clear();
                else
                    foreach (ComicVM item in _searchManager.SearchObjects)
                    {
                        if (!Items.Contains(item))
                            Items.Add(item);
                    }
            });
        }

        private void OnSearchStatusChanged(object? sender, SearchStatus e)
        {
            if (e == SearchStatus.Searching)
            {
                LoadingVisibility = true;
                NothingFoundVisibility = false;
                LoadMoreVisibility = false;
            }
            else if (e == SearchStatus.Finished)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    LoadingVisibility = false;
                    if (_searchManager.SearchObjects.Count == 0)
                        NothingFoundVisibility = true;
                    else
                        LoadMoreVisibility = _searchManager.CanSearchNext;
                });

            }
        }

        private void ConnectionChanged(object? sender, EventArgs e)
        {
            if (!ConnectionManager.ConnectionIsSave)
            {
                LoadMoreVisibility = false;
            }
            else
            {
                if (_searchManager.SearchStatus == SearchStatus.Searching)
                    LoadingVisibility = true;
                LoadMoreVisibility = _searchManager.CanSearchNext;
            }
        }

        [RelayCommand]
        private void LoadMore()
        {
            NothingFoundVisibility = false;
            LoadMoreVisibility = false;
            LoadingVisibility = true;
            _searchManager.StartSearchNext();
        }
    }
}
