using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Core;
using Elva.MVVM.Model.Database;
using Elva.MVVM.Model.Database.Saveable;
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
        private SettingsDatabaseManager _settingsManager;
        public InfoVM(INavigationService navigation) : base(navigation)
        {
            _settingsManager = GetService<SettingsDatabaseManager>();
            ComicReference? lastReference = _settingsManager.Context.ComicReferences.FirstOrDefault(x => x.ReferenceType == "Info");
            Website? website = GetService<WebsiteManager>().GetWebsite(lastReference?.WebsiteID ?? "");
            if (website != null && !string.IsNullOrEmpty(lastReference?.Url))
                _comic = new(new(lastReference.Url, lastReference.Url, website));
            else if (string.IsNullOrEmpty(lastReference?.Url))
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
                _settingsManager.Add(new ComicReference()
                {
                    Url = Comic.Url,
                    ReferenceType = "Info",
                    WebsiteID = Comic.Website
                });
                _settingsManager.SaveData();
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
