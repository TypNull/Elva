using iText.IO.Image;
using iText.Layout.Element;
using System.Collections.Generic;

namespace Elva.MVVM.Model.Export
{
    internal class ExportChapter(string title, int order)
    {
        public int ImageCounter { get; private set; }
        public int Order { get; } = order;
        public string Title { get; } = title;

        private readonly List<string> contentImages = new();
        public List<string> ContentImages => contentImages;
        public void AddImage(string i)
        {
            contentImages.Add(i);
            ImageCounter++;
        }

        public IReadOnlyCollection<Image> CreateITextImages()
        {
            List<Image> images = new();
            foreach (string imageFile in contentImages)
            {
                try
                {
                    Image image = new(ImageDataFactory.Create(imageFile));
                    images.Add(image);
                }
                catch { }
            }
            return images;
        }
    }
}
