using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.MVVM.Model.Database;
using Elva.MVVM.Model.Database.Saveable;
using Elva.MVVM.Model.Export;
using Elva.MVVM.Model.Manager;
using Elva.MVVM.ViewModel.CControl.Info;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WebsiteScraper.Downloadable;
using WebsiteScraper.Downloadable.Books;

namespace Elva.MVVM.ViewModel.Model
{
    public partial class ComicVM : ObservableObject
    {
        private static readonly Random _rand = new();
        private Comic _comic;
        private SaveableComic _scomic;
        [ObservableProperty]
        private SolidColorBrush _background = new();
        private bool _canExport;
        private bool _isExporting;
        [ObservableProperty]
        public int _exportProgress;

        public bool IsVisible => _comic?.Title.Equals(_comic.Url) == false;
        public string Url => _comic.Url;
        public string Title => _comic.Title;
        public string Description => _comic.Description;
        public string Website => _comic.HoldingWebsite.Name + _comic.HoldingWebsite.Suffix;
        public string Author => _comic.Author;
        public string? CoverPath => string.IsNullOrWhiteSpace(_comic.CoverPath) ? null : _comic.CoverPath;
        public string Genres => string.Join("; ", _comic.Genres);
        public DateTime LastUpdated => _comic.LastUpdated;
        public Status Status => _comic.Status;
        public float LastChapterNumber => _comic.Chapter.LastOrDefault()?.Number ?? -1;
        public float FirstChapterNumber => _comic.Chapter.FirstOrDefault()?.Number ?? -1;
        public string AlternativeTitles => string.Join("; ", _comic.AlternativeTitles);

        public bool CanExport { get => _canExport && !_isExporting; set => SetProperty(ref _canExport, value); }

        public ChapterVM[] ChapterVMs => _chapterVMs.Value;

        private ComicDatabaseManager _dbManager;
        private Lazy<ChapterVM[]> _chapterVMs;
        private Lazy<InfoVM> _infoVM;

        public ComicVM(Comic comic)
        {
            _comic = comic;
            _chapterVMs = new([]);
            _infoVM = new(App.Current.ServiceProvider.GetRequiredService<InfoVM>);
            _dbManager = App.Current.ServiceProvider.GetRequiredService<ComicDatabaseManager>();
            if (_dbManager.TryGetSaved(comic.Url, out _scomic!))
            {
                if (!File.Exists(_scomic.CoverPath))
                    _scomic.CoverFileName = string.Empty;
                _scomic.UpdateToComic(comic);
                _chapterVMs = new Lazy<ChapterVM[]>(() => _comic.Chapter.Select(x => new ChapterVM(x, this)).ToArray());
                if (_scomic.LastSaveUpdate < DateTime.Now.AddMinutes(-45))
                    _ = comic.UpdateAsync();
            }
            else
            {
                _ = comic.UpdateAsync();
                _scomic = new(comic);
            }
            IOManager.DownloadPathChanged += IOManager_DownloadPathChanged;
            comic.PropertyChanged += Comic_PropertyChanged;
            CreateColor();
            _canExport = ChapterVMs.Any(x => x.DownloadProgress == 100);
        }

        private void IOManager_DownloadPathChanged(object? sender, string e)
        {
            CanExport = false;
            foreach (ChapterVM chapter in ChapterVMs)
                chapter.CheckDonwloaded();
        }

        /// <summary>
        /// builds the path to the Comic destination
        /// </summary>
        /// <returns>Directory path to comic destination</returns>
        public string GetComicDestination()
        {
            string[] words = IOManager.RemoveInvalidFileNameChars(Title).Split(' ');
            StringBuilder destination = new(IOManager.DownloadPath);
            if (words.Length == 1)
                destination.Append(words[0]);
            else
                for (int i = 0; i < words.Length; i++)
                    if (destination.Length + words[i].Length < 210)
                        destination.Append(words[i] + ' ');
            return destination.ToString().Trim();
        }

        [RelayCommand]
        public void OpenChapterInBrowser(float f)
        {
            Process.Start("explorer", _comic.Chapter.Where(x => x.Number == f).First().Url);
        }

        [RelayCommand]
        public void OpenInfo()
        {
            _infoVM.Value.Comic = this;
            _infoVM.Value.Navigation.NavigateTo<InfoVM>();
        }

        private void Comic_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Update")
            {
                UpdateAllPropertys();
                if (CoverPath == null && !string.IsNullOrEmpty(_comic.CoverUrl))
                    _ = _comic.DownloadCoverAsync(IOManager.TempPath);
                _scomic.UpateWithComic(_comic);
                _chapterVMs = new Lazy<ChapterVM[]>(() => _comic.Chapter.Select(x => new ChapterVM(x, this)).ToArray());
            }
            else if (e.PropertyName == nameof(CoverPath))
            {
                OnPropertyChanged(nameof(CoverPath));
                _scomic.UpateWithComic(_comic);
            }
        }

        private void UpdateAllPropertys()
        {
            OnPropertyChanged(nameof(IsVisible));
            OnPropertyChanged(nameof(Title));
            OnPropertyChanged(nameof(Genres));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(Author));
            OnPropertyChanged(nameof(CoverPath));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(AlternativeTitles));
            OnPropertyChanged(nameof(LastUpdated));
            OnPropertyChanged(nameof(FirstChapterNumber));
            OnPropertyChanged(nameof(LastChapterNumber));
            OnPropertyChanged(nameof(ChapterVMs));
        }

        private void CreateColor()
        {
            byte[] values = new byte[] { 105, 235, (byte)_rand.Next(105, 235) };
            int n = values.Length;
            while (n > 1)
            {
                int k = _rand.Next(n--);
                byte temp = values[n];
                values[n] = values[k];
                values[k] = temp;
            }
            Background.Color = Color.FromRgb(values[0], values[1], values[2]);
        }

        internal void Export(int exportAs)
        {
            CanExport = false;
            _isExporting = true;
            ExportProgress = 0;
            if (exportAs == 1)
            {
                Task.Run(() =>
                {
                    PDFExport export = new(this, new Progress<int>(x => ExportProgress = x));
                    export.CreatePDF();
                    _isExporting = false;
                    CanExport = true;
                });
            }
        }
    }
}
