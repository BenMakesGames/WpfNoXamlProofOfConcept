using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace NoXaml.Framework.Extensions.WPF
{
    public static class ButtonExtensions
    {
        public static T OnClick<T>(this T button, RoutedEventHandler callback) where T : ButtonBase
        {
            button.Click += callback;
            
            return button;
        }
    }
}
