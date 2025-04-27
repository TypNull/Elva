using Elva.Pages.Shared.Models;
using Elva.Services.Database.Saveable;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.Services.Database
{
    internal class SettingsManager
    {
        private (string url, string websiteID)? _actualComic;
        private (string url, string websiteID)? _lastActualComic;
        private SaveableSettings _settings;
        private SaveableSettings? _lastSavedSettings;
        private Dictionary<string, (string url, ReferenceType typ)[]> _homeList = new();
        private string _settingsPath = Path.Combine(IOManager.LocalDataPath, "settings.db");
        private string _actualComicPath = Path.Combine(IOManager.LocalDataPath, "actualComic.data");
        private readonly System.Timers.Timer _saveTimer;
        private readonly SemaphoreSlim _settingsLock = new(1, 1);
        private bool _isLoaded = false;
        private readonly TaskCompletionSource<bool> _loadCompletionSource = new();

        public IReadOnlyCollection<string> Favorites => _settings.Favorites;

        public string Theme
        {
            get => _settings.Theme;
            set
            {
                if (Theme == value)
                    return;
                _settings.Theme = value;
                SaveSettings();
            }
        }

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

        public Task SettingsLoadedTask => _loadCompletionSource.Task;

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
                _settingsLock.Wait();
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
                finally
                {
                    _settingsLock.Release();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public async Task LoadSettingsAsync()
        {
            if (_isLoaded)
            {
                return;
            }

            try
            {
                await _settingsLock.WaitAsync();
                try
                {
                    if (_isLoaded)
                    {
                        return;
                    }

                    if (!File.Exists(_settingsPath))
                    {
                        _isLoaded = true;
                        _loadCompletionSource.SetResult(true);
                        return;
                    }

                    await Task.Run(() =>
                    {
                        try
                        {
                            _settings = JsonSerializer.Deserialize<SaveableSettings>(File.ReadAllText(_settingsPath), new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true }) ?? new();

                            if (File.Exists(_actualComicPath))
                            {
                                _actualComic = JsonSerializer.Deserialize<(string url, string websiteID)?>(File.ReadAllText(_actualComicPath), new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true }) ?? new();
                            }

                            IOManager.ChangeDownloadPath(_settings.DownloadPath);
                            _homeList = _settings.HomeWebsiteComics;
                            Debug.WriteLine("Settings loaded");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error loading settings: {ex.Message}");
                            _settings = new SaveableSettings();
                        }
                    });

                    _isLoaded = true;
                    _loadCompletionSource.SetResult(true);
                }
                finally
                {
                    _settingsLock.Release();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadSettingsAsync: {ex.Message}");
                _loadCompletionSource.TrySetException(ex);
            }
        }

        public void LoadSettings()
        {
            _ = LoadSettingsAsync();
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