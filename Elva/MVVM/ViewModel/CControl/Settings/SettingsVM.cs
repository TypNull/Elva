using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.Core;
using Elva.MVVM.Model.Database;
using Elva.MVVM.Model.Manager;
using Microsoft.Extensions.DependencyInjection;
using Requests;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.MVVM.ViewModel.CControl.Settings
{
    internal partial class SettingsVM : ViewModelObject
    {
        private WebsiteManager _websiteManager;
        private SettingsManager _settingsManager;

        [ObservableProperty]
        private string _version;
        [ObservableProperty]
        private bool _isKillSwitchEnabled = true;
        [ObservableProperty]
        private bool _isUpdateingWebsitesEnabled = true;
        public string DownloadFolder
        {
            get => IOManager.DownloadPath;
            set
            {
                IOManager.ChangeDownloadPath(value);
                _settingsManager.SaveSettings();
                OnPropertyChanged(nameof(DownloadFolder));
            }
        }
        public event EventHandler? OnChangeDownloadFolder;
        public event EventHandler? OnAddWebsite;
        public SettingsVM(INavigationService navigation) : base(navigation)
        {
            _websiteManager = App.Current.ServiceProvider.GetRequiredService<WebsiteManager>();
            _settingsManager = App.Current.ServiceProvider.GetRequiredService<SettingsManager>();
            string? versionString = Environment.GetEnvironmentVariable("ClickOnce_CurrentVersion");
            if (string.IsNullOrEmpty(versionString))
                _version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Not found";
            else
                _version = versionString;
            _isKillSwitchEnabled = _settingsManager.IsKillSwitchEnabled;
        }

        [RelayCommand]
        private void ChangeDownloadFolder() => OnChangeDownloadFolder?.Invoke(this, EventArgs.Empty);

        [RelayCommand]
        private void AddWebsite() => OnAddWebsite?.Invoke(this, EventArgs.Empty);

        public void AddWebsite(string path)
        {
            Website website = Website.LoadWebsite(path);
            if (!_websiteManager.Websites.Any(x => (x.Name + x.Suffix) == (website.Name + website.Suffix)))
            {
                IOManager.DeserializeWebsiteImage(IOManager.CopyFileTo(path, IOManager.DataPath));
                _websiteManager.AddWebsite(website);
            }
        }

        [RelayCommand]
        private void KillSwitch()
        {
            IsKillSwitchEnabled = !IsKillSwitchEnabled;
            _settingsManager.IsKillSwitchEnabled = IsKillSwitchEnabled;
            if ((!ConnectionManager.ConnectionIsSave && IsKillSwitchEnabled) || !ConnectionManager.ConnectionIsAvailable)
                RequestHandler.CancelMainCTS();
            else if (ConnectionManager.ConnectionIsAvailable)
                RequestHandler.CreateMainCTS();
        }

        [RelayCommand]
        private void OpenLicenseInfo() => Navigation.NavigateTo<LicenseInfoVM>();


        [RelayCommand]
        private void OpenWebsite(string url) => Process.Start("explorer", url);

        [RelayCommand]
        private void UpdateWebsites()
        {
            IsUpdateingWebsitesEnabled = false;
            Task.Run(async () =>
            {
                await IOManager.DownloadWebsitesFromRepoAsync();
                Website[] websites = IOManager.LoadWebsites();
                foreach (Website website in websites)
                {
                    if (!_websiteManager.Websites.Any(x => (x.Name + x.Suffix) == (website.Name + website.Suffix)))
                        App.Current.Dispatcher.Invoke(() => _websiteManager.AddWebsite(website));
                }
            });
        }

    }
}
