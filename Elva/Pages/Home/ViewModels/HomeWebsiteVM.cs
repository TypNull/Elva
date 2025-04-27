using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Pages.Shared.Models;
using Elva.Pages.Shared.ViewModels;
using Elva.Services.Database;
using Elva.Services.Database.Saveable;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using WebsiteScraper.Downloadable.Books;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.Pages.Home.ViewModels
{
    internal partial class HomeWebsiteVM : ObservableObject
    {
        [ObservableProperty]
        private bool _visible = false;

        [ObservableProperty]
        private bool _newVisible = false;

        [ObservableProperty]
        private bool _recommendedVisible = false;

        [ObservableProperty]
        private ObservableCollection<ComicVM> _newItems = new();

        [ObservableProperty]
        private ObservableCollection<ComicVM> _recommendedItems = new();

        [ObservableProperty]
        private Website _websiteObject = null!;

        [ObservableProperty]
        private string _websiteLogo;

        private readonly SettingsManager _settingsManager;
        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private CancellationTokenSource _loadCancellation = new();

        public string WebsiteName => WebsiteObject.Name + WebsiteObject.Suffix;

        public HomeWebsiteVM(Website website)
        {
            _websiteObject = website;
            _websiteLogo = _websiteObject.GetLogoPath();
            _settingsManager = App.Current.ServiceProvider.GetRequiredService<SettingsManager>();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await Task.Run(LoadItemsFromDatabase);
            if (Application.Current != null)
                _ = LoadNewItemsAsync();
        }

        private void LoadItemsFromDatabase()
        {
            (string url, ReferenceType typ)[]? references = _settingsManager.GetHomeComics(WebsiteObject);
            if (references == null)
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    ObservableCollection<ComicVM> newItems = new();
                    ObservableCollection<ComicVM> recommendedItems = new();

                    foreach ((string url, ReferenceType typ) in references.Where(x => x.typ == ReferenceType.New))
                    {
                        Comic comic = new(url, url, WebsiteObject);
                        newItems.Add(new ComicVM(comic));
                    }

                    foreach ((string url, ReferenceType typ) reference in references.Where(x => x.typ == ReferenceType.Recommended))
                    {
                        Comic comic = new(reference.url, reference.url, WebsiteObject);
                        recommendedItems.Add(new ComicVM(comic));
                    }

                    NewItems = newItems;
                    RecommendedItems = recommendedItems;
                    ChangeVisibility();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating ComicVMs: {ex.Message}");
                }
            });
        }

        private void ChangeVisibility()
        {
            RecommendedVisible = RecommendedItems.Count > 0;
            NewVisible = NewItems.Count > 0;
            Visible = RecommendedVisible || NewVisible;
        }

        private async Task LoadNewItemsAsync()
        {
            try
            {
                await _loadSemaphore.WaitAsync();

                _loadCancellation.Cancel();
                _loadCancellation = new CancellationTokenSource();
                CancellationToken cancellationToken = _loadCancellation.Token;

                Task<Comic[]> newTask = WebsiteObject.LoadNewAsync<Comic>();
                Task<Comic[]> recoTask = WebsiteObject.LoadExtraAsync<Comic>("Recommended");

                await Task.WhenAll(newTask, recoTask);

                if (cancellationToken.IsCancellationRequested)
                    return;

                Comic[] newComic = newTask.Result;
                Comic[] recoComic = recoTask.Result;

                ResizeArray(ref newComic, 8);
                ResizeArray(ref recoComic, 8);

                if (cancellationToken.IsCancellationRequested)
                    return;

                Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        ObservableCollection<ComicVM> newItems = [];
                        ObservableCollection<ComicVM> recommendedItems = [];
                        foreach (Comic comic in newComic)
                            newItems.Add(new ComicVM(comic));

                        foreach (Comic comic in recoComic)
                            recommendedItems.Add(new ComicVM(comic));


                        NewItems = newItems;
                        RecommendedItems = recommendedItems;
                        ChangeVisibility();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error updating UI with comic data: {ex.Message}");
                    }
                });

                await Task.Run(() =>
                {
                    try
                    {
                        List<(string url, ReferenceType typ)> list = [];

                        foreach (Comic comic in newComic)
                            list.Add((comic.Url, ReferenceType.New));


                        foreach (Comic comic in recoComic)
                            list.Add((comic.Url, ReferenceType.Recommended));

                        _settingsManager.SetHomeComics(WebsiteObject, [.. list]);
                        _settingsManager.SaveSettings();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error saving comic references: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading website data: {ex.Message}");
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        private static void ResizeArray(ref Comic[] comics, int length)
        {
            if (comics.Length > length)
                Array.Resize(ref comics, length);
        }

        public void RefreshData() => _ = LoadNewItemsAsync();

        [RelayCommand]
        public void OpenWebsiteInfo() => Debug.WriteLine("OpenWebsite");
    }
}