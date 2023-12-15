using Elva.MVVM.ViewModel.CControl.Search;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace Elva.MVVM.View.CControl.Search
{
    /// <summary>
    /// Interaktionslogik für SearchBarView.xaml
    /// </summary>
    public partial class SearchBarView : UserControl
    {
        public SearchBarView()
        {
            DataContext = App.Current.ServiceProvider.GetRequiredService<SearchBarVM>();
            InitializeComponent();
        }
    }
}
