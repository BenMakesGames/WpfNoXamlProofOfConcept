using NoXaml.Model.Components;
using NoXaml.Model.DOM;
using System.Windows;
using System.Windows.Controls;

namespace NoXaml.Framework.Components
{
    public abstract class NoXamlWindow: Window, INoXaml
    {
        public Element VDOM { get; set; }

        public virtual Element BuildVDOM() => new Element(typeof(Grid));
    }
}
