﻿using System;
using System.Collections.Generic;
using System.Windows;

namespace NoXaml.Framework.Extensions.WPF
{
    public static class FrameworkElementExtensions
    {
        public static T If<T>(this T element, bool condition, Action<T> method) where T : FrameworkElement
        {
            if (condition)
                method(element);

            return element;
        }

        public static T ForEach<T, U>(this T element, IEnumerable<U> items, Action<T, U> method) where T : FrameworkElement
        {
            foreach (var i in items)
                method(element, i);

            return element;
        }
    }
}
