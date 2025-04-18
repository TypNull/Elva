using Elva.Pages.Search.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

namespace Elva.Pages.Search.Views
{
    /// <summary>
    /// Interaction logic for SearchBarView.xaml
    /// </summary>
    public partial class SearchBarView : UserControl
    {
        private SearchBarVM ViewModel => (SearchBarVM)DataContext;

        public SearchBarView()
        {
            DataContext = App.Current.ServiceProvider.GetRequiredService<SearchBarVM>();
            InitializeComponent();

            // Subscribe to key events
            SearchTextBox.PreviewKeyDown += SearchTextBox_PreviewKeyDown;
        }

        private void SearchTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine($"Key pressed: {e.Key}, Modifiers: {Keyboard.Modifiers}");

            // Handle Up arrow
            if (e.Key == Key.Up)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    Debug.WriteLine("Executing ToggleFullHistory with Up");
                    ViewModel.ToggleFullHistoryCommand.Execute("Up");
                }
                else
                {
                    Debug.WriteLine("Executing NavigateHistoryUp");
                    ViewModel.NavigateHistoryUpCommand.Execute(null);
                }

                e.Handled = true;
            }
            // Handle Down arrow
            else if (e.Key == Key.Down)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    Debug.WriteLine("Executing ToggleFullHistory with Down");
                    ViewModel.ToggleFullHistoryCommand.Execute("Down");
                }
                else
                {
                    Debug.WriteLine("Executing NavigateHistoryDown");
                    ViewModel.NavigateHistoryDownCommand.Execute(null);
                }

                e.Handled = true;
            }
            // Handle Escape key
            else if (e.Key == Key.Escape)
            {
                Debug.WriteLine("Executing ClearInput");
                ViewModel.ClearInputCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}