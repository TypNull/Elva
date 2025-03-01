using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Elva.MVVM.Model;
using Elva.MVVM.Model.Manager;

namespace Elva.MVVM.ViewModel.CControl
{
    internal partial class ConnectionVM : ObservableObject
    {

        public ConnectionVM() => ConnectionManager.ConnectionChanged += (sender, args) => ConnectionType = (TypeOfConnection)(sender ?? TypeOfConnection.NotAvailable);


        [RelayCommand]
        private void ConnectionPressed()
        {
            if (ConnectionManager.ConnectionIsEnabled)
            {
                ConnectionManager.DisableConnection();
                ToastNotification.Show("Internet connection disabled", ToastType.Info);
            }
            else
            {
                ConnectionManager.EnabledConnection();
                ToastNotification.Show("Internet connection enabled", ToastType.Info);
            }
        }


        [ObservableProperty]
        private TypeOfConnection _connectionType;
    }
}

