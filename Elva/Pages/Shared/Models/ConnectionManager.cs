using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;

namespace Elva.Pages.Shared.Models
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
        private static readonly string[] VpnProviders = ["Surfshark", "CyberGhost", "Mullvad", "Private Internet Access", "Windscribe", "TunnelBear", "Hotspot Shield", "HideMyAss", "ZenMate", "IPVanish", "Buffered", "TorGuard", "GhostPath", "Trust.Zone", "NordVPN", "ExpressVPN", "ProtonVPN", "VyprVPN", "Ivacy", "PureVPN", "Norton Secure VPN", "Avast SecureLine", "Opera VPN", "Kaspersky VPN"];

        private static readonly string[] VpnKeywords = ["VPN", "Virtual", "Tunnel", "TAP-", "TUN-", "OpenVPN", "SSTP", "L2TP", "PPTP", "Wireguard", "Secure Gateway", "Private Network", "Cisco AnyConnect"];

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
            Debug.WriteLine("Internet connection enabled");
            InternetConnection = TypeOfConnection.NotAvailable;
            if (!_update?.IsAlive ?? true)
                UpdateNetworkConnection();
        }

        public static void DisableConnection()
        {
            Debug.WriteLine("Internet connection disabled");
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
                Debug.WriteLine("Internet connection not available");
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
                {
                    if (iface.OperationalStatus == OperationalStatus.Up && iface.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    {
                        bool isVpn = false;

                        // Check if it's a PPP interface (common for VPNs)
                        if (iface.NetworkInterfaceType == NetworkInterfaceType.Ppp)
                        {
                            isVpn = true;
                        }
                        // Check if it's type 53 (virtual interface often used by VPNs)
                        else if (iface.NetworkInterfaceType.ToString() == "53")
                        {
                            isVpn = true;
                        }
                        // Check for TAP/TUN adapters (commonly used by OpenVPN and other VPNs)
                        else if (iface.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                                (iface.Description.ToUpperInvariant().Contains("TAP") ||
                                 iface.Description.ToUpperInvariant().Contains("TUN") ||
                                 iface.Name.ToUpperInvariant().Contains("TAP") ||
                                 iface.Name.ToUpperInvariant().Contains("TUN")))
                        {
                            isVpn = true;
                        }

                        // Check for VPN keywords in interface description or name
                        if (!isVpn)
                        {
                            foreach (string keyword in VpnKeywords)
                            {
                                if (iface.Description.ToUpperInvariant().Contains(keyword.ToUpperInvariant()) ||
                                    iface.Name.ToUpperInvariant().Contains(keyword.ToUpperInvariant()))
                                {
                                    isVpn = true;
                                    break;
                                }
                            }
                        }

                        // Check for known VPN providers in interface description
                        if (!isVpn)
                        {
                            if (VpnProviders.Any(provider =>
                                iface.Description.ToUpperInvariant().Contains(provider.ToUpperInvariant()) ||
                                iface.Name.ToUpperInvariant().Contains(provider.ToUpperInvariant())))
                            {
                                isVpn = true;
                            }
                        }

                        // Check for unusual MTU size (common in VPNs)
                        if (!isVpn)
                        {
                            try
                            {
                                if (iface.GetIPProperties().GetIPv4Properties() != null)
                                {
                                    int mtu = iface.GetIPProperties().GetIPv4Properties().Mtu;
                                    // Common VPN MTU sizes are often 1300, 1400, 1420, 1450, 1472, etc.
                                    if (mtu > 0 && mtu < 1500 && mtu != 1492) // 1492 is common for regular DSL
                                    {
                                        isVpn = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine("Error checking MTU: " + ex.Message);
                            }
                        }

                        if (isVpn)
                        {
                            InternetConnection = TypeOfConnection.Save;
                            connectionIsSave = true;
                            Debug.WriteLine("Internet connection with VPN detected: " + iface.Name + " (" + iface.Description + ")");
                            break;
                        }
                    }
                }

                if (!connectionIsSave)
                {
                    InternetConnection = TypeOfConnection.Available;
                    Debug.WriteLine("Internet connection available, but no VPN detected");
                }
                _connectionChanged?.Invoke(InternetConnection, EventArgs.Empty);
            };
        }
    }
}