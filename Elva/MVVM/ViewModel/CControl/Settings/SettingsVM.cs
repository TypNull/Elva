using CommunityToolkit.Mvvm.Input;
using Elva.Core;
using Elva.MVVM.Model.Database;
using Elva.MVVM.Model.Manager;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using WebsiteScraper.WebsiteUtilities;

namespace Elva.MVVM.ViewModel.CControl.Settings
{
    internal partial class SettingsVM : ViewModelObject
    {

        private WebsiteManager _websiteManager;
        private SettingsManager _settingsManager;
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
    }
}
