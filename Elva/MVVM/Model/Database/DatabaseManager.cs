using Elva.MVVM.Model.Database.Saveable;
using Elva.MVVM.Model.Manager;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;

namespace Elva.MVVM.Model.Database
{
    public abstract class DatabaseManager<T> where T : DbContext, new()
    {
        private readonly T _context;
        protected readonly object _lockObject;
        private readonly List<SaveableOnlineData> _data;
        private readonly Timer _dbSaveTimer;
        private bool _isChanging;
        public long Version { get; private set; }

        public T Context { get { return _context; } }

        public DatabaseManager(double intervall = 5000)
        {
            _context = new T();
            _data = new List<SaveableOnlineData>();
            _lockObject = new object();
            _dbSaveTimer = new(intervall)
            {
                AutoReset = false
            };
            _dbSaveTimer.Elapsed += DbSaveTimer_Elapsed;
            Directory.CreateDirectory(IOManager.LocalDataPath);
            _context.Database.EnsureCreated();
            // Version = _context.Database.SqlQuery<long>($"PRAGMA user_version").FirstOrDefault();
            Debug.WriteLine(Version);
        }

        private void DbSaveTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (_isChanging)
                return;
            _isChanging = true;
            try
            {
                SaveableOnlineData[] addable = _data.Distinct(new SaveableComparer()).ToArray();
                _data.Clear();
                SaveChanges(addable.ToArray());
                Debug.WriteLine($"{typeof(T).Name} is saved");
            }
            catch (System.InvalidOperationException exeption)
            {
                Debug.WriteLine(exeption);
            }
            finally { _isChanging = false; }

        }

        protected virtual void SaveChanges(params object[] addable)
        {
            lock (_lockObject)
            {
                _context.AddRange(addable);
                _context.SaveChanges();
            }
        }

        public void Add(SaveableOnlineData data) => _data.Add(data);

        public void SaveData()
        {
            _dbSaveTimer.Stop();
            _dbSaveTimer.Start();
        }

        public abstract void LoadData();

        internal bool TryGetSaved<TItem>(string id, out TItem? item) where TItem : SaveableOnlineData
        {
            lock (_lockObject)
            {
                item = default;
                if (_context.Find(typeof(TItem), id) is TItem found)
                {
                    item = found;
                    return true;
                }
                else return false;
            }

        }
    }

    [PrimaryKey(nameof(Url))]
    public class SaveableOnlineData
    {
        public string Url { get; set; } = string.Empty;
    }
}
