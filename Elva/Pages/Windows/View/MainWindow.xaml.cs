using Elva.Pages.Settings.ViewModels;
using Elva.Pages.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Elva.Pages.Windows.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += (s, e) => ToastNotification.Initialize(ToastContainer);
            App.Current.ServiceProvider.GetRequiredService<SettingsVM>().OnThemeChanged += MainWindow_OnThemeChanged;
        }

        private void MainWindow_OnThemeChanged(object? sender, string e)
        {
            foreach (RadioButton button in Stack.Children.OfType<RadioButton>())
            {
                string iconName = Common.MarkupExtensions.GetIcon(button);
                Common.MarkupExtensions.SetIcon(button, null);
                Common.MarkupExtensions.SetIcon(button, iconName);
            }

            Common.MarkupExtensions.SetIcon(menuButton, null);
            Common.MarkupExtensions.SetIcon(menuButton, "menu.json");
            menuButton.Reload();

            Stack.InvalidateVisual();
        }
    }
}