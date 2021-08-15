using NoXaml.Framework.Extensions.WPF;
using NoXaml.Model.Components;
using NoXaml.Model.DOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace NoXaml.Framework.Services
{
    public class UIBuilder
    {
        public static void UpdateUI(INoXaml view)
        {
            var oldVDOM = view.VDOM;
            var newVDOM = view.BuildVDOM();

            var oldContent = (FrameworkElement)view.Content;

            view.Content = UpdateVDOM(oldContent, oldVDOM, newVDOM);

            view.VDOM = newVDOM;
        }

        private static FrameworkElement UpdateVDOM(FrameworkElement content, Element oldVDOM, Element newVDOM)
        {
            // TODO: compare old to new, and make changes as appropriate
            if (newVDOM == null && oldVDOM == null)
            {
                // nothing to do
            }
            if (newVDOM == null && oldVDOM != null)
            {
                RemoveElement(content);
                return null;
            }
            else if (oldVDOM == null && newVDOM != null)
                return CreateElement(newVDOM);
            else if (newVDOM.Type != oldVDOM.Type)
            {
                RemoveElement(content);
                return CreateElement(newVDOM);
            }
            else // the types are equal; update properties & children
            {
                UpdateElementProperties(content, newVDOM.Properties);

                // update children
                if(content is Panel panel)
                {
                    int newLength = newVDOM.Children.Count;
                    int oldLength = oldVDOM.Children.Count;

                    List<int> remove = new();
                    List<FrameworkElement> add = new();

                    for (int i = 0; i < newLength || i < oldLength; i++)
                    {
                        var newChild = UpdateVDOM(
                            i < oldLength ? panel.Children[i] as FrameworkElement : null,
                            i < oldLength ? oldVDOM.Children[i] : null,
                            i < newLength ? newVDOM.Children[i] : null
                        );

                        if (newChild == null)
                        {
                            if (i < oldLength)
                                remove.Add(i);
                        }
                        else if (i >= oldLength)
                        {
                            add.Add(newChild);
                        }
                        else if (newChild != panel.Children[i])
                        {
                            remove.Add(i);

                            add.Add(newChild);
                        }
                    }

                    // remove in reverse order, so that indicies don't get screwed up
                    for(int i = remove.Count - 1; i >= 0; i--)
                        panel.Children.RemoveAt(remove[i]);

                    foreach(var fe in add)
                        panel.Children.Add(fe);
                }

                return content;
            }
        }

        private static void RemoveElement(FrameworkElement e)
        {
            // TODO: call OnDestroy-type handlers?
        }

        private static FrameworkElement CreateElement(Element e)
        {
            var element = (FrameworkElement)Activator.CreateInstance(e.Type);

            SetElementProperties(element, e.Properties);

            if(element is Panel panel)
            {
                foreach (var child in e.Children)
                    panel.Add(CreateElement(child));
            }

            return element;
        }

        private static void UpdateElementProperties(FrameworkElement element, Dictionary<string, object> properties)
        {
            foreach (var property in properties)
            {
                if (property.Key == "click")
                {
                    // event handlers can't change; do nothing
                }
                else
                {
                    element.SetProperty(property.Key, property.Value);
                }
            }
        }

        private static void SetElementProperties(FrameworkElement element, Dictionary<string, object> properties)
        {
            foreach (var property in properties)
            {
                if (property.Key == "click")
                {
                    if (element is Button button)
                        button.Click += (RoutedEventHandler)property.Value;
                }
                else
                {
                    element.SetProperty(property.Key, property.Value);
                }
            }
        }
    }
}
