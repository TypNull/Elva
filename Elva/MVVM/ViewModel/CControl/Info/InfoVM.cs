using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Core;
using Elva.MVVM.Model.Database;
using Elva.MVVM.Model.Manager;
using Elva.MVVM.ViewModel.Model;
using System.Linq;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.MVVM.ViewModel.CControl.Info
{
    internal partial class InfoVM : ViewModelObject
    {
        [ObservableProperty]
        private ComicVM _comic;
        [ObservableProperty]
        private ChapterInfoVM _chapterVM;
        private SettingsManager _settingsManager;
        public InfoVM(INavigationService navigation) : base(navigation)
        {
            _settingsManager = GetService<SettingsManager>();
            (string url, string websiteID)? lastReference = _settingsManager.GetActualComic();
            Website? website = GetService<WebsiteManager>().GetWebsite(lastReference?.websiteID ?? "");
            if (website != null && !string.IsNullOrEmpty(lastReference?.url))
                _comic = new(new(lastReference.Value.url, lastReference.Value.url, website));
            else if (string.IsNullOrEmpty(lastReference?.url))
            {
                Home.HomeWebsiteVM? websiteVM = GetService<Home.HomeVM>().Websites.FirstOrDefault();
                _comic = new(new(websiteVM?.NewItems.FirstOrDefault()?.Url ?? websiteVM?.RecommendedItems.FirstOrDefault()?.Url ?? "", websiteVM?.NewItems.FirstOrDefault()?.Url ?? websiteVM?.RecommendedItems.FirstOrDefault()?.Url ?? "", websiteVM?.WebsiteObject ?? new()));
            }
            else _comic = new(new("", "", new()));

            _chapterVM = new ChapterInfoVM(_comic.ChapterVMs);
            PropertyChanged += InfoVM_PropertyChanged;
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
            foreach (ChapterVM chapter in Comic.ChapterVMs)
                _ = chapter.StartDownloadAsync();
        }
    }
}
