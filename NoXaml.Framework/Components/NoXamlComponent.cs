using System.Windows.Controls;
using NoXaml.Interfaces.Components;

namespace NoXaml.Framework.Components
{
    public abstract class NoXamlComponent: UserControl, INoXaml
    {
        public virtual void BuildUI()
        {

        }
    }
}
