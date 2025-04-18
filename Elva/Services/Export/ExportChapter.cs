using iText.IO.Image;
using iText.Layout.Element;
using System.Collections.Generic;

namespace Elva.Services.Export
{
    internal class ExportChapter(string title, int order)
    {
        public int ImageCounter { get; private set; }
        public int Order { get; } = order;
        public string Title { get; } = title;
        public List<string> ContentImages { get; } = [];
        public void AddImage(string i)
        {
            ContentImages.Add(i);
            ImageCounter++;
        }

        public IReadOnlyCollection<Image> CreateITextImages()
        {
            List<Image> images = new();
            foreach (string imageFile in ContentImages)
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
