using System.Windows;
using System.Windows.Controls;

namespace NoXaml.Framework.Extensions.WPF
{
    public static class PanelExtensions
    {
        public static T Add<T>(this T panel, UIElement element) where T: Panel
        {
            panel.Children.Add(element);
            return panel;
        }
    }
}
