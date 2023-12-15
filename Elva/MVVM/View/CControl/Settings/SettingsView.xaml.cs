using Elva.MVVM.Model.Manager;
using Elva.MVVM.ViewModel.CControl.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Windows.Controls;

namespace Elva.MVVM.View.CControl.Settings
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
            _vm.ChangeDownaloadFolder += ChangeDownaloadFolder;
        }

        private void ChangeDownaloadFolder(object? sender, System.EventArgs e)
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
