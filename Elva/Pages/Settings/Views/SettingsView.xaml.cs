using Elva.Pages.Settings.ViewModels;
using Elva.Pages.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows.Controls;

namespace Elva.Pages.Settings.Views
{
    /// <summary>
    /// Interaktionslogik für SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        private SettingsVM _vm;
        public SettingsView()
        {
            InitializeComponent();
            _vm = App.Current.ServiceProvider.GetRequiredService<SettingsVM>();
            _vm.OnChangeDownloadFolder += ChangeDownloadFolder;
            _vm.OnAddWebsite += OnAddWebsite;
        }

        private void OnAddWebsite(object? sender, System.EventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = "Add Website",
                Multiselect = false,
                Filter = "Websites | *.wsf"
            };
            if (dialog.ShowDialog() == true)
                _vm.AddWebsite(dialog.FileName);
        }

        private void ChangeDownloadFolder(object? sender, System.EventArgs e)
        {
            OpenFolderDialog dialog = new()
            {
                Title = "Download Folder",
                InitialDirectory = IOManager.DownloadPath,
                Multiselect = false,
            };

            if (dialog.ShowDialog() == true)
                _vm.DownloadFolder = dialog.FolderName;
        }
    }
}
