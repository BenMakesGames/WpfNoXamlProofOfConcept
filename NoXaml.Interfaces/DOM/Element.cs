using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace NoXaml.Model.DOM
{
    public class Element
    {
        public Type Type { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public List<Element> Children { get; set; } = new List<Element>();
        //public FrameworkElement FrameworkElement { get; set; }

        public Element(Type t)
        {
            Type = t;
        }
    }

}
