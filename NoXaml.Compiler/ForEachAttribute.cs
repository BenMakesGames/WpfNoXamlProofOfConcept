using System;
using System.Collections.Generic;
using System.Text;

namespace NoXaml.Compiler
{
    class ForEachAttribute
    {
        public string Collection { get; set; }
        public string ItemVariableName { get; set; }
    }
}
