using System.Windows.Controls;

namespace Elva.Pages.Info.Views
{
    /// <summary>
    /// Interaktionslogik für InfoView.xaml
    /// </summary>
    public partial class InfoView : UserControl
    {
        private bool _descriptionIsInFirstRow = true;
        public InfoView()
        {
            InitializeComponent();
        }

        private void Grid_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            if (_descriptionIsInFirstRow && ActualWidth < 700)
            {
                Grid.SetColumnSpan(imageBorder, 2);
                Grid.SetColumn(infoBorder, 0);
                Grid.SetColumnSpan(infoBorder, 2);
                Grid.SetRow(infoBorder, 1);
                _descriptionIsInFirstRow = false;
            }
            else if (!_descriptionIsInFirstRow && ActualWidth > 700)
            {
                Grid.SetColumnSpan(imageBorder, 1);
                Grid.SetColumn(infoBorder, 2);
                Grid.SetColumnSpan(infoBorder, 1);
                Grid.SetRow(infoBorder, 0);
                _descriptionIsInFirstRow = true;
            }
        }
    }
}
