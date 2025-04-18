using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DownloadAssistant.Request;
using Elva.Pages.Shared.Models;
using Requests;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebsiteScraper.Downloadable.Books;

namespace Elva.Pages.Shared.ViewModels
{
    public partial class ChapterVM : ObservableObject
    {
        private Chapter _chapter;

        public int DownloadProgress
        {
            get => _downloadProgress; private set
            {
                if (DownloadProgress >= 99)
                {
                    SetProperty(ref _downloadProgress, 100);
                    _holdingComic.CanExport = true;
                }
                else
                    SetProperty(ref _downloadProgress, value);
            }
        }
        private int _downloadProgress = -1;

        public string Title => _chapter.Title;
        public float Number => _chapter.Number;
        public string Url => _chapter.Url;
        public string DownloadURL => _chapter.DownloadURL;
        public int Order => _chapter.Order;
        public DateTime UploadDateTime => _chapter.UploadDateTime;

        public ComicVM _holdingComic;

        public ChapterVM(Chapter chapter, ComicVM comicVM)
        {
            _chapter = chapter;
            _holdingComic = comicVM;
            CheckDonwloaded();
        }

        public void CheckDonwloaded()
        {
            string downloadD = Path.Combine(_holdingComic.GetComicDestination(), "Images\\");
            if (Directory.Exists(downloadD) && Directory.GetFiles(downloadD, $"{Order}_0.*").Any())
                DownloadProgress = 100;
            else
                DownloadProgress = -1;
        }

        [RelayCommand]
        public async Task StartDownloadAsync()
        {
            bool silent = false;
            if (DownloadProgress != -1f)
                return;

            if (!silent)
            {
                ToastNotification.Show($"Starting download for {Title}", ToastType.Info);
            }

            DownloadProgress = 0;
            ProgressableContainer<GetRequest> _downloadRequests = await _chapter.DownloadAsync(Path.Combine(_holdingComic.GetComicDestination(), "Images\\"));
            _downloadRequests.Progress.ProgressChanged += (o, f) => DownloadProgress = (int)(100f * f);
            _downloadRequests.StateChanged += (sender, e) => Container_StateChanged(sender, e, silent);
        }

        private void Container_StateChanged(object? sender, Requests.Options.RequestState e, bool silent = false)
        {
            if (e == Requests.Options.RequestState.Compleated)
            {
                DownloadProgress = 100;
                if (!silent)
                {
                    ToastNotification.Show($"Download completed for {Title}", ToastType.Success);
                }
            }
            else if (e == Requests.Options.RequestState.Failed)
            {
                DownloadProgress = -1;
                ToastNotification.Show($"Download failed for {Title}", ToastType.Error);
            }
        }

    }
}
