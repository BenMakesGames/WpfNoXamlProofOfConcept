using System;
using System.Collections.Generic;
using NoXaml.Framework.Components;

namespace WpfNoXaml.Components
{
    public partial class MainWindow : NoXamlWindow
    {
        private Random RNG { get; }

        public bool ShowMore { get; set; } = false;
        public int DieRoll { get; set; }

        public List<string> Labels = new()
        {
            "Apples", "Oranges", "Pears", "Mangoes"
        };

        public MainWindow(Random rng)
        {
            RNG = rng;

            Title = "NoXaml Demo";
            Width = 640;
            Height = 360;

            BuildUI();
        }
        
        public void DoToggle()
        {
            ShowMore = !ShowMore;
            BuildUI();
        }

        public void DoRollDie()
        {
            DieRoll = RNG.Next(6) + 1;
            BuildUI();
        }
    }
}
