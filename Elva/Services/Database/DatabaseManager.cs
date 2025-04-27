using Elva.Pages.Shared.Models;
using Elva.Services.Database.Saveable;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace Elva.Services.Database
{
    public abstract class DatabaseManager<T> where T : DbContext, new()
    {
        protected readonly object _lockObject;
        private readonly ConcurrentBag<SaveableOnlineData> _data;
        private readonly Timer _dbSaveTimer;
        private volatile bool _isChanging;
        private volatile bool _isModelReady = false;
        public long Version { get; private set; }

        public T Context { get; }

        protected DatabaseManager(double intervall = 5000)
        {
            Context = new T();
            _data = [];
            _lockObject = new object();
            _dbSaveTimer = new(intervall)
            {
                AutoReset = false
            };
            _dbSaveTimer.Elapsed += DbSaveTimer_Elapsed;

            Directory.CreateDirectory(IOManager.LocalDataPath);

            Task.Run(async () =>
            {
                await Context.Database.EnsureCreatedAsync();
                _isModelReady = true;
            }).ConfigureAwait(false);

            Debug.WriteLine("Database Version: " + Version);
        }

        private async void DbSaveTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (_isChanging)
                return;

            _isChanging = true;
            try
            {
                List<SaveableOnlineData> dataToSave = new();
                while (_data.TryTake(out SaveableOnlineData? item))
                {
                    dataToSave.Add(item);
                }

                SaveableOnlineData[] distinctItems = dataToSave.Distinct(new SaveableComparer()).ToArray();

                if (distinctItems.Length > 0)
                {
                    await Task.Run(() => SaveChanges(distinctItems));
                    Debug.WriteLine($"{typeof(T).Name} saved {distinctItems.Length} items");
                }
            }
            catch (InvalidOperationException exception)
            {
                Debug.WriteLine(exception);
            }
            finally
            {
                _isChanging = false;
            }
        }

        protected virtual void SaveChanges(params object[] addable)
        {
            if (!_isModelReady)
            {
                Debug.WriteLine("Attempting to save before model is ready - operation postponed");
                return;
            }

            lock (_lockObject)
            {
                Context.AddRange(addable);
                Context.SaveChanges();
            }
        }

        public void Add(SaveableOnlineData data) => _data.Add(data);

        public void SaveData()
        {
            _dbSaveTimer.Stop();
            _dbSaveTimer.Start();
        }

        public abstract Task LoadDataAsync();

        internal bool TryGetSaved<TItem>(string id, out TItem? item) where TItem : SaveableOnlineData
        {
            if (!_isModelReady)
            {
                item = default;
                return false;
            }

            lock (_lockObject)
            {
                item = default;
                try
                {
                    if (Context.Find(typeof(TItem), id) is TItem found)
                    {
                        item = found;
                        return true;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine($"Error in TryGetSaved: {ex.Message}");
                }
                return false;
            }
        }

        internal async Task<TItem?> TryGetSavedAsync<TItem>(string id) where TItem : SaveableOnlineData
        {
            if (!_isModelReady)
            {
                return default;
            }

            return await Task.Run(() =>
            {
                lock (_lockObject)
                {
                    try
                    {
                        if (Context.Find(typeof(TItem), id) is TItem found)
                        {
                            return found;
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        Debug.WriteLine($"Error in TryGetSavedAsync: {ex.Message}");
                    }
                    return default;
                }
            });
        }
    }

    [PrimaryKey(nameof(Url))]
    public class SaveableOnlineData
    {
        public string Url { get; set; } = string.Empty;
    }
}