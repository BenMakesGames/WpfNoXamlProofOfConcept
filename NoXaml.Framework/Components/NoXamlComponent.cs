using NoXaml.Framework.Services;
using NoXaml.Model.Components;
using NoXaml.Model.DOM;
using System.Windows.Controls;

namespace NoXaml.Framework.Components
{
    public abstract class NoXamlComponent: UserControl, INoXaml
    {
        public Element VDOM { get; set; }

        public virtual Element BuildVDOM() => new Element(typeof(Grid));
    }
}
