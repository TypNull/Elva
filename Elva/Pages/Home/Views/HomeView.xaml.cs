using System.Windows.Controls;
using System.Windows.Input;

namespace Elva.Pages.Home.Views
{
    /// <summary>
    /// Interaktionslogik für HomeView.xaml
    /// </summary>
    public partial class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }



        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            MouseWheelEventArgs mouseWheelEventArgs = new(e.MouseDevice, e.Timestamp, e.Delta);
            mouseWheelEventArgs.RoutedEvent = ScrollViewer.MouseWheelEvent;
            mouseWheelEventArgs.Source = sender;
            FistScollViewer.RaiseEvent(mouseWheelEventArgs);
        }
    }
}
