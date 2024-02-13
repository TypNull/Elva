using CommunityToolkit.Mvvm.Input;
using Elva.Core;
using System.Diagnostics;

namespace Elva.MVVM.ViewModel.CControl.Settings
{
    internal partial class LicenseInfoVM : ViewModelObject
    {
        public LicenseInfoVM(INavigationService navigation) : base(navigation)
        {
        }

        [RelayCommand]
        private void BackToSettings() => Navigation.NavigateTo<SettingsVM>();

        [RelayCommand]
        private void OpenWebsite(string url) => Process.Start("explorer", url);

    }
}
