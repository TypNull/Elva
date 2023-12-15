using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Elva.MVVM.View.CControl
{
    /// <summary>
    /// Interaktionslogik für ControlView.xaml
    /// </summary>
    public partial class ControlView : UserControl
    {
        private Window Window { get; init; }
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
        private void MaximizeWindow() { Window.WindowState = Window.WindowState != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal; }
        [RelayCommand]
        private void MinimizeWindow() => Window.WindowState = WindowState.Minimized;



        private readonly SolidColorBrush _hoverColor = new((Color)ColorConverter.ConvertFromString("#FF969696"));
        private readonly SolidColorBrush _transparentColor = new((Color)ColorConverter.ConvertFromString("#FFB1B1B1"));



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
            _ = lparam.ToInt32();
            int x = lparam.ToInt32() & 0xffff;
            int y = lparam.ToInt32() >> 16;
            Rect rect = new(MaxButton.PointToScreen(new Point()), new Size(MaxButton.Width, MaxButton.Height));
            if (rect.Contains(new Point(x, y)))
            {
                MaxButton.Background = _hoverColor;
                handled = true;
            }
            else
                MaxButton.Background = _transparentColor;
        }
    }
}

