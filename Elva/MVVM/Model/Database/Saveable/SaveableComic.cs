using Elva.MVVM.Model.Manager;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using WebsiteScraper.Downloadable;
using WebsiteScraper.Downloadable.Books;

namespace Elva.MVVM.Model.Database.Saveable
{
    internal class SaveableComic : SaveableOnlineData
    {
        public string Title { get; protected set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string CoverFileName { get; set; } = string.Empty;
        [NotMapped]
        public string CoverPath => Path.Combine(IOManager.TempPath, CoverFileName);
        [NotMapped]
        public string[] Genres { get; set; } = Array.Empty<string>();
        public string GenresAsString
        {
            get { return string.Join(',', Genres); }
            set { Genres = value.Split(','); }
        }
        public DateTime LastUpdated { get; set; }
        public Status Status { get; set; } = Status.None;
        [NotMapped]
        public string[] AlternativeTitles { get; set; } = Array.Empty<string>();

        public string AlternativeTitlesAsString
        {
            get { return string.Join(',', AlternativeTitles); }
            set { AlternativeTitles = value.Split(','); }
        }
        public string WebsiteID { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
        public DateTime LastSaveUpdate { get; set; } = DateTime.Now;

        public List<SaveableChapter> Chapter { get; set; } = new();

        private readonly ComicDatabaseManager _dbManager;

        public SaveableComic() => _dbManager = App.Current.ServiceProvider.GetRequiredService<ComicDatabaseManager>();

        public SaveableComic(Comic comic) : this()
        {
            Url = comic.Url;
            LastUpdated = comic.LastUpdated;
            Status = comic.Status;
            AlternativeTitles = comic.AlternativeTitles;
            Genres = comic.Genres;
            CoverFileName = Path.GetFileName(comic.CoverPath) ?? string.Empty;
            CoverUrl = comic.CoverUrl ?? string.Empty;
            Author = comic.Author;
            Title = comic.Title;
            Description = comic.Description;
            WebsiteID = comic.HoldingWebsite.Name + comic.HoldingWebsite.Suffix;
            if (!string.IsNullOrEmpty(Url))
            {
                AddChapters(comic);
                _dbManager.Add(this);
                _dbManager.SaveData();
            }
        }

        private void AddChapters(Comic comic)
        {
            List<Chapter> newChapter = new();
            foreach (Chapter chapter in comic.Chapter)
            {
                SaveableChapter[] found = Chapter.Where(x => x.Url == chapter.Url).ToArray();
                if (!string.IsNullOrEmpty(chapter.Url))
                    if (!found.Any())
                        newChapter.Add(chapter);
                    else
                        found[0].UpdateWithChapter(this, chapter);
            }
            if (newChapter.Count > 0)
                foreach (Chapter chapter in newChapter)
                    Chapter.Add(new SaveableChapter(this, chapter));

            Chapter = Chapter.Distinct(new SaveableComparer()).Select(x => (SaveableChapter)x).ToList();
        }

        internal void UpdateToComic(Comic comic)
        {
            comic.LastUpdated = LastUpdated;
            if (!string.IsNullOrEmpty(CoverFileName))
                comic.CoverPath = CoverPath;
            comic.CoverUrl = CoverUrl;
            comic.Author = Author;
            comic.Genres = Genres;
            comic.AlternativeTitles = AlternativeTitles;
            comic.Description = Description;
            comic.Status = Status;
            comic.Title = Title;
            comic.Chapter = Chapter.Select(c => c.CreateChapter(comic)).ToArray();
        }

        internal void UpateWithComic(Comic comic)
        {
            LastUpdated = comic.LastUpdated;
            CoverFileName = Path.GetFileName(comic.CoverPath) ?? string.Empty;
            CoverUrl = comic.CoverUrl ?? string.Empty;
            Author = comic.Author;
            Genres = comic.Genres;
            AlternativeTitles = comic.AlternativeTitles;
            Status = comic.Status;
            Title = comic.Title;
            Description = comic.Description;
            LastSaveUpdate = DateTime.Now;
            AddChapters(comic);
            _dbManager.SaveData();
        }
    }
}
