using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.MVVM.ViewModel.Model;
using System;
using System.Collections.ObjectModel;

namespace Elva.MVVM.ViewModel.CControl.Info
{
    internal partial class ChapterInfoVM : ObservableObject
    {
        private bool _collapsed = true;
        public ObservableCollection<ChapterVM> ChapterList { get; set; }
        private ObservableCollection<ChapterVM> _chapterShortVMs;
        private ObservableCollection<ChapterVM> _chapterVMs;

        public ChapterInfoVM(ChapterVM[] chapterVMs)
        {
            _chapterVMs = new();
            _chapterShortVMs = new();
            ChapterList = _chapterShortVMs;
        }

        public void UpdateChapter(ChapterVM[] chapterVMs)
        {
            _chapterVMs = new(chapterVMs);
            if (chapterVMs.Length > 50)
                Array.Resize(ref chapterVMs, 50);
            _chapterShortVMs = new(chapterVMs);
            _collapsed = true;
            ChapterList = _chapterShortVMs;
            OnPropertyChanged(nameof(ChapterList));
        }

        [RelayCommand]
        private void ExpandChapters()
        {
            _collapsed = !_collapsed;
            if (_collapsed)
                ChapterList = _chapterShortVMs;
            else
                ChapterList = _chapterVMs;
            OnPropertyChanged(nameof(ChapterList));
        }

    }
}
