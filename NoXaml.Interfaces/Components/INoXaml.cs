using NoXaml.Model.DOM;

namespace NoXaml.Model.Components
{
    public interface INoXaml
    {
        object Content { get; set; }
        Element VDOM { get; set; }
        
        Element BuildVDOM();
    }
}
