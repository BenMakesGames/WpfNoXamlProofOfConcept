using System.Windows;
using NoXaml.Interfaces.Components;

namespace NoXaml.Framework.Components
{
    public abstract class NoXamlWindow: Window, INoXaml
    {
        public virtual void BuildUI()
        {
        }
    }
}
