using Elva.MVVM.Model;
using System.Windows;

namespace Elva
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Initialize toast notification system
            Loaded += (s, e) => ToastNotification.Initialize(ToastContainer);
        }
    }
}