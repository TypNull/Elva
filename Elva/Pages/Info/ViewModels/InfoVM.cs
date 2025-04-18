using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Common;
using Elva.Common.Navigation;
using Elva.Pages.Home.ViewModels;
using Elva.Pages.Shared.Models;
using Elva.Pages.Shared.ViewModels;
using Elva.Services.Database;
using System.Collections.Generic;
using System.Linq;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.Pages.Info.ViewModels
{
    internal partial class InfoVM : ViewModelObject
    {
        [ObservableProperty]
        private int _selectedIndex = 0;
        [ObservableProperty]
        private ComicVM _comic;
        [ObservableProperty]
        private ChapterInfoVM _chapterVM;
        private SettingsManager _settingsManager;

        public InfoVM(INavigationService navigation) : base(navigation)
        {
            _settingsManager = GetService<SettingsManager>();
            _selectedIndex = _settingsManager.LastExportIndex;
            GetLastComic();
            _chapterVM = new ChapterInfoVM(_comic!.ChapterVMs);
            PropertyChanged += InfoVM_PropertyChanged;
        }

        private void GetLastComic()
        {
            (string url, string websiteID)? lastReference = _settingsManager.GetActualComic();

            Website? website = GetService<WebsiteManager>().GetWebsite(lastReference?.websiteID ?? "");
            if (website != null && !string.IsNullOrEmpty(lastReference?.url))
                Comic = new(new(lastReference.Value.url, lastReference.Value.url, website));
            else if (string.IsNullOrEmpty(lastReference?.url))
            {
                HomeWebsiteVM? websiteVM = GetService<HomeVM>().Websites.FirstOrDefault();
                Comic = new(new(websiteVM?.NewItems.FirstOrDefault()?.Url ?? websiteVM?.RecommendedItems.FirstOrDefault()?.Url ?? "", websiteVM?.NewItems.FirstOrDefault()?.Url ?? websiteVM?.RecommendedItems.FirstOrDefault()?.Url ?? "", websiteVM?.WebsiteObject ?? new()));
            }
            else Comic = new(new(string.Empty, string.Empty, new()));
        }

        private void InfoVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Comic))
            {
                ChapterVM.UpdateChapter(Comic.ChapterVMs);
                _settingsManager.SetActualComic(Comic.Website, Comic.Url);
                _settingsManager.SaveSettings();
            }
        }

        [RelayCommand]
        private void StartDownloadAll()
        {
            if (Comic.ChapterVMs.Length == 0) return;

            List<string> chapterTitles = new();

            foreach (ChapterVM chapter in Comic.ChapterVMs)
            {
                chapterTitles.Add(chapter.Title);
                _ = chapter.StartDownloadAsync();

            }
            if (chapterTitles.Count > 0)
            {
                string message = chapterTitles.Count == 1
                    ? $"Downloading chapter: {chapterTitles[0]}"
                    : $"Downloading {chapterTitles.Count} chapters";

                ToastNotification.Show(message, ToastType.Info);
            }
        }

        [RelayCommand]
        private void Export() => Comic.Export(SelectedIndex);


        [RelayCommand]
        private void ExportChanged(int s) =>
            _settingsManager.LastExportIndex = SelectedIndex;

        [RelayCommand]
        private void Favorite() => Comic!.IsFavorite = !Comic.IsFavorite;

    }
}
