using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Elva.Pages.Shared.Views
{
    /// <summary>
    /// Interaktionslogik für ControlView.xaml
    /// </summary>
    public partial class ControlView : UserControl
    {
        private Window Window { get; init; }

        // Use ResourceDictionary references for dynamic theme support
        private SolidColorBrush HoverBrush => TryFindResource("BackgroundTertiary") as SolidColorBrush;
        private SolidColorBrush NormalBrush => TryFindResource("BackgroundSecondary") as SolidColorBrush;

        public ControlView()
        {
            InitializeComponent();
            DataContext = this;
            Window = Application.Current.MainWindow;
            Loaded += (o, e) =>
            {
                HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(Window).Handle);
                source.AddHook(new HwndSourceHook(HwndSourceHook));
            };
        }

        [RelayCommand]
        private void CloseWindow() => Window.Close();

        [RelayCommand]
        private void MaximizeWindow()
        {
            Window.WindowState = Window.WindowState != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        }

        [RelayCommand]
        private void MinimizeWindow() => Window.WindowState = WindowState.Minimized;

        private const int WM_NCHITTEST = 0x0084;
        private const int HTMAXBUTTON = 9;

        private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            switch (msg)
            {
                case WM_NCHITTEST:
                    try
                    {
                        SnapLayout(lparam, ref handled);
                        return new IntPtr(HTMAXBUTTON);
                    }
                    catch (OverflowException)
                    {
                        handled = true;
                    }
                    break;
                case 0x00A1:
                    MaxClicked(lparam, ref handled);
                    break;
            }
            return IntPtr.Zero;
        }

        private void MaxClicked(IntPtr lparam, ref bool handled)
        {
            int x = lparam.ToInt32() & 0xffff;
            int y = lparam.ToInt32() >> 16;
            Rect rect = new(MaxButton.PointToScreen(new Point()), new Size(MaxButton.Width, MaxButton.Height));
            if (rect.Contains(new Point(x, y)))
                MaximizeWindow();
        }

        private void SnapLayout(IntPtr lparam, ref bool handled)
        {
            int x = lparam.ToInt32() & 0xffff;
            int y = lparam.ToInt32() >> 16;
            Rect rect = new(MaxButton.PointToScreen(new Point()), new Size(MaxButton.Width, MaxButton.Height));
            if (rect.Contains(new Point(x, y)))
            {
                MaxButton.Background = HoverBrush;
                handled = true;
            }
            else
                MaxButton.Background = NormalBrush;
        }
    }
}