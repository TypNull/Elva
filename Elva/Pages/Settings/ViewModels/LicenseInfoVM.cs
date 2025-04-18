using CommunityToolkit.Mvvm.Input;
using Elva.Common;
using Elva.Common.Navigation;
using System.Diagnostics;

namespace Elva.Pages.Settings.ViewModels
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
