using iText.Layout.Element;
using System.Collections.Generic;

namespace Elva.MVVM.Model.Export
{
    internal class ExportChapter(string title, int order)
    {
        public int ImageCounter { get; private set; }
        public int Order { get; } = order;
        public string Title { get; } = title;

        private readonly List<Image> contentImages = new();
        public List<Image> ContentImages => contentImages;
        public void AddImage(Image i)
        {
            contentImages.Add(i);
            ImageCounter++;
        }
    }
}
