using System.Windows;

namespace Elva.Core
{
    internal class MarkupExtensions
    {
        public static readonly DependencyProperty IconProperty = DependencyProperty.RegisterAttached("Icon", typeof(string), typeof(MarkupExtensions), new PropertyMetadata(default(string)));

        public static void SetIcon(UIElement element, string value) => element.SetValue(IconProperty, value);
        public static string GetIcon(UIElement element) => (string)element.GetValue(IconProperty);

    }
}
