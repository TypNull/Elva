using Elva.Services.Database.Saveable;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elva.Services.Database
{
    internal class ComicDatabaseManager : DatabaseManager<ComicContext>
    {
        private readonly SemaphoreSlim _loadSemaphore = new(1, 1);
        private bool _basicDataLoaded;
        private bool _fullDataLoaded;
        private readonly List<Action> _fullLoadCallbacks = [];

        public bool IsFullyLoaded => _fullDataLoaded;

        public override async Task LoadDataAsync()
        {
            try
            {
                await _loadSemaphore.WaitAsync();
                if (_basicDataLoaded)
                    return;
                await LoadBasicComicDataAsync();
                _basicDataLoaded = true;
                _ = Task.Run(LoadFullComicDataAsync);
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        private async Task LoadBasicComicDataAsync()
        {
            await Context.Comics
                .Select(c => new { c.Url, c.Title, c.Author, c.CoverFileName, c.Status, c.Description, c.CoverUrl, c.WebsiteID })
                .LoadAsync();

            App.Log<ComicDatabaseManager>("Basic comic data loaded", time: true);
        }

        private async Task LoadFullComicDataAsync()
        {
            try
            {
                await _loadSemaphore.WaitAsync();
                if (_fullDataLoaded)
                {
                    return;
                }

                Stopwatch sw = Stopwatch.StartNew();
                await Context.Comics.Include(x => x.Chapter.OrderBy(chapter => chapter.Order)).LoadAsync();
                sw.Stop();

                _fullDataLoaded = true;
                App.Log<ComicDatabaseManager>($"Full comic data loaded in {sw.ElapsedMilliseconds}ms", time: true);
                NotifyFullDataLoaded();
            }
            catch (Exception ex)
            {
                App.Log<ComicDatabaseManager>($"Error loading full comic data: {ex.Message}", Microsoft.Extensions.Logging.LogLevel.Error);
            }
            finally
            {
                _loadSemaphore.Release();
            }
        }

        public void RegisterForFullLoad(Action callback)
        {
            if (_fullDataLoaded)
            {
                callback();
            }
            else
            {
                lock (_fullLoadCallbacks)
                {
                    _fullLoadCallbacks.Add(callback);
                }
            }
        }

        private void NotifyFullDataLoaded()
        {
            List<Action> callbacks;
            lock (_fullLoadCallbacks)
            {
                callbacks = new List<Action>(_fullLoadCallbacks);
                _fullLoadCallbacks.Clear();
            }

            foreach (Action callback in callbacks)
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    App.Log<ComicDatabaseManager>($"Error in full load callback: {ex.Message}", Microsoft.Extensions.Logging.LogLevel.Error);
                }
            }
        }

        public async Task<SaveableComic?> GetComicByUrlAsync(string url)
        {
            return await Context.Comics
                .AsNoTracking()
                .Include(x => x.Chapter.OrderBy(chapter => chapter.Order))
                .FirstOrDefaultAsync(c => c.Url == url);
        }

        public async Task<List<SaveableComic>> GetBasicComicsAsync()
        {
            return await Context.Comics
                .AsNoTracking()
                .ToListAsync();
        }
    }
}