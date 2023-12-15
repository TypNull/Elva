using CommunityToolkit.Mvvm.ComponentModel;
using Elva.MVVM.ViewModel.Model;
using System.Collections.ObjectModel;

namespace Elva.MVVM.ViewModel.CControl.Info
{
    internal partial class ChapterInfoVM : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ChapterVM> _chapterVMs;

        public ChapterInfoVM(ChapterVM[] chapterVMs)
        {
            _chapterVMs = new(chapterVMs);
        }

        public void UpdateChapter(ChapterVM[] chapterVMs) { ChapterVMs = new(chapterVMs); }

    }
}
