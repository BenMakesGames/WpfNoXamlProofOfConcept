using System;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace NoXaml.Framework.Extensions.WPF
{
    public static class TextBlockExtensions
    {
        public static TextBlock SetText(this TextBlock textBlock, string text)
        {
            textBlock.Text = text;
            
            return textBlock;
        }
    }
}
