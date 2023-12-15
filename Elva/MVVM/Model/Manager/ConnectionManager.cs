using System;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;

namespace Elva.MVVM.Model.Manager
{
    internal enum TypeOfConnection
    {
        NotAvailable,
        Available,
        Disabled,
        Save
    }

    internal static class ConnectionManager
    {

        public static EventHandler? _connectionChanged;
        public static event EventHandler ConnectionChanged
        {
            add
            {
                _connectionChanged += value; if (_isInitialized) value.Invoke(InternetConnection, EventArgs.Empty);
            }
            remove => _connectionChanged -= value;
        }

        private static TypeOfConnection InternetConnection { get; set; } = TypeOfConnection.NotAvailable;

        public static bool ConnectionIsAvailable => !InternetConnection.Equals(TypeOfConnection.NotAvailable) || !InternetConnection.Equals(TypeOfConnection.Disabled);
        public static bool ConnectionIsEnabled => !InternetConnection.Equals(TypeOfConnection.Disabled);
        public static bool ConnectionIsSave => InternetConnection.Equals(TypeOfConnection.Save);

        private static bool _isInitialized = false;
        public static void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            NetworkChange.NetworkAddressChanged += (obj, eventArgs) =>
            {
                UpdateNetworkConnection();
            };
            UpdateNetworkConnection();
            _update?.Join();
        }

        public static void EnabledConnection()
        {
            Debug.WriteLine("Internet conection enabled");
            InternetConnection = TypeOfConnection.NotAvailable;
            if (!_update?.IsAlive ?? true)
                UpdateNetworkConnection();
        }

        public static void DisableConnection()
        {
            Debug.WriteLine("Internet conection disabled");
            _update?.Join();
            InternetConnection = TypeOfConnection.Disabled;
            _connectionChanged?.Invoke(InternetConnection, EventArgs.Empty);
        }
        private static Thread? _update;

        private static void UpdateNetworkConnection()
        {
            if (InternetConnection == TypeOfConnection.Disabled)
                return;
            else if (!NetworkInterface.GetIsNetworkAvailable())
            {
                InternetConnection = TypeOfConnection.NotAvailable;
                _connectionChanged?.Invoke(InternetConnection, EventArgs.Empty);
                Debug.WriteLine("Internet conection not available");
                return;
            }

            Debug.WriteLine("Update Connection...");

            _update = new Thread(CheckVPN());
            _update.Start();
        }

        private static ThreadStart CheckVPN()
        {
            return () =>
            {
                NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
                bool connectionIsSave = false;
                foreach (NetworkInterface iface in interfaces)
                    if (iface.OperationalStatus == OperationalStatus.Up && iface.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                      (iface.NetworkInterfaceType == NetworkInterfaceType.Ppp ||
                                      iface.NetworkInterfaceType.ToString() == "53" && (iface.Description.Contains("VPN") || iface.Name.Contains("VPN"))))
                    {
                        InternetConnection = TypeOfConnection.Save;
                        connectionIsSave = true;
                        Debug.WriteLine("Internet conection with VPN detected");
                        break;
                    }

                if (!connectionIsSave)
                {
                    InternetConnection = TypeOfConnection.Available;
                    Debug.WriteLine("Internet conection available");
                }
                _connectionChanged?.Invoke(InternetConnection, EventArgs.Empty);
            };
        }

    }
}
