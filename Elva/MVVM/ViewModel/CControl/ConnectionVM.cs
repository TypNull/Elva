using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
                ConnectionManager.DisableConnection();
            else ConnectionManager.EnabledConnection();

        }

        [ObservableProperty]
        private TypeOfConnection _connectionType;
    }
}

