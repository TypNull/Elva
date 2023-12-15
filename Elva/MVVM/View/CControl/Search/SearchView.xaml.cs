using Elva.MVVM.View.CElement;
using System.Windows;
using System.Windows.Controls;

namespace Elva.MVVM.View.CControl.Search
{
    /// <summary>
    /// Interaktionslogik für SearchView.xaml
    /// </summary>
    public partial class SearchView : UserControl
    {
        private static int _columns = 1;
        private static int _gridWidth = 1;

        public SearchView()
        {
            InitializeComponent();
        }

        private void UniformGridPanel_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_gridWidth != (int)e.NewSize.Width)
            {
                _gridWidth = (int)e.NewSize.Width;
                int colums = _gridWidth / 400;
                _columns = colums < 1 ? 1 : colums;
            }
            ((UniformGridPanel)sender).Columns = _columns;
        }
    }
}
