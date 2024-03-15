using Elva.MVVM.Model.Database.Saveable;
using Elva.MVVM.Model.Manager;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Timers;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.MVVM.Model.Database
{
    internal class SettingsManager
    {
        //Source={IOManager.LocalDataPath}settings.db
        private (string url, string websiteID)? _actualComic;
        private (string url, string websiteID)? _lastActualComic;
        private SaveableSettings _settings;
        private SaveableSettings? _lastSavedSettings;
        private Dictionary<string, (string url, ReferenceType typ)[]> _homeList = new();
        private string _settingsPath = Path.Combine(IOManager.LocalDataPath, "settings.db");
        private string _actualComicPath = Path.Combine(IOManager.LocalDataPath, "actualComic.data");
        private readonly Timer _saveTimer;

        public IReadOnlyCollection<string> Favorites => _settings.Favorites;

        public int LastExportIndex
        {
            get => _settings.LastExportIndex; set
            {
                if (LastExportIndex == value)
                    return;
                _settings.LastExportIndex = value;
                SaveSettings();
            }
        }

        public bool IsKillSwitchEnabled
        {
            get => _settings.IsKillSwitchEnabled; set
            {
                if (IsKillSwitchEnabled == value)
                    return;
                _settings.IsKillSwitchEnabled = value;
                SaveSettings();
            }
        }

        public SettingsManager()
        {
            _settings = new();
            _saveTimer = new(5000)
            {
                AutoReset = false
            };
            _saveTimer.Elapsed += _saveTimer_Elapsed;
        }

        private void _saveTimer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            SaveSettingsDirect();
        }

        public void SaveSettingsDirect()
        {
            try
            {
                _settings.DownloadPath = IOManager.DownloadPath;
                _settings.HomeWebsiteComics = _homeList;
                if (!_settings.Equals(_lastSavedSettings))
                {
                    File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true }));
                    Debug.WriteLine("Settings saved");
                    _lastSavedSettings = _settings with { };
                }
                if (_lastActualComic != _actualComic)
                {
                    File.WriteAllText(_actualComicPath, JsonSerializer.Serialize(_actualComic, new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true }));
                    _lastActualComic = _actualComic;
                    Debug.WriteLine("Actual comic saved");
                }

            }
            catch (System.Exception)
            {

            }
        }

        public void LoadSettings()
        {
            if (!File.Exists(_settingsPath))
                return;
            try
            {
                _settings = JsonSerializer.Deserialize<SaveableSettings>(File.ReadAllText(_settingsPath), new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true }) ?? new();
                _actualComic = JsonSerializer.Deserialize<(string url, string websiteID)?>(File.ReadAllText(_actualComicPath), new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true }) ?? new();
                IOManager.ChangeDownloadPath(_settings.DownloadPath);
                _homeList = _settings.HomeWebsiteComics;
                Debug.WriteLine("Settings loaded");
            }
            catch (System.Exception ex)
            {
                Debug.Assert(false, ex.Message);
            }
        }

        public void SaveSettings()
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        public void SetHomeComics(Website website, (string url, ReferenceType typ)[] value) => _homeList[website.Name + website.Suffix] = value;
        public (string url, ReferenceType typ)[]? GetHomeComics(Website website) => _homeList.GetValueOrDefault(website.Name + website.Suffix);
        public void SetActualComic(string website, string url) => _actualComic = new(url, website);

        public (string url, string websiteID)? GetActualComic() => _actualComic;

        public void AddToFavorite(string url)
        {
            _settings.Favorites.Add(url);
            SaveSettings();
        }
        public void RemoveFavorite(string url)
        {
            _settings.Favorites.Remove(url);
            SaveSettings();
        }
    }
}
