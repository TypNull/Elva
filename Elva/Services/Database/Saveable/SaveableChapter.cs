using System;
using WebsiteScraper.Downloadable.Books;

namespace Elva.Services.Database.Saveable
{
    internal class SaveableChapter : SaveableOnlineData
    {
        public string Title { get; set; } = string.Empty;
        public float Number { get; set; } = 0;
        public string DownloadURL { get; set; } = string.Empty;

        public int Order { get; set; }
        public DateTime UploadDateTime { get; set; }

        public SaveableComic? Comic { get; set; }

        public SaveableChapter() { }

        public SaveableChapter(SaveableComic saveableComic, Chapter chapter)
        {
            Url = chapter.Url;
            UploadDateTime = chapter.UploadDateTime;
            Title = chapter.Title;
            Number = chapter.Number;
            DownloadURL = chapter.DownloadURL;
            Order = chapter.Order;
            Comic = saveableComic;
        }

        public Chapter CreateChapter(Comic comic)
        {
            Chapter chapter = new(comic)
            {
                Url = Url,
                UploadDateTime = UploadDateTime,
                Title = Title,
                Number = Number,
                DownloadURL = DownloadURL
            };
            chapter.SetOrder(Order);
            return chapter;
        }

        internal void UpdateWithChapter(SaveableComic saveableComic, Chapter chapter)
        {
            Url = chapter.Url;
            UploadDateTime = chapter.UploadDateTime;
            Title = chapter.Title;
            Number = chapter.Number;
            DownloadURL = chapter.DownloadURL;
            Order = chapter.Order;
            Comic = saveableComic;
        }
    }
}
