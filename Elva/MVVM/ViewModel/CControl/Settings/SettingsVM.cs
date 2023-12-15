using CommunityToolkit.Mvvm.Input;
using Elva.Core;
using Elva.MVVM.Model.Manager;
using System;

namespace Elva.MVVM.ViewModel.CControl.Settings
{
    internal partial class SettingsVM : ViewModelObject
    {
        public string DownloadFolder
        {
            get => IOManager.DownloadPath;
            set
            {
                IOManager.ChangeDownloadPath(value);
                OnPropertyChanged(nameof(DownloadFolder));
            }
        }
        public event EventHandler? ChangeDownaloadFolder;
        public SettingsVM(INavigationService navigation) : base(navigation)
        {
        }

        [RelayCommand]
        private void ChangeDownloadFolder() => ChangeDownaloadFolder?.Invoke(this, EventArgs.Empty);
    }
}
