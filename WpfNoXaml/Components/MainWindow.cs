using System;
using System.Collections.Generic;
using NoXaml.Framework.Components;
using NoXaml.Framework.Services;

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
            // "Random" having been registered with Microsoft DI as a singleton service
            RNG = rng;

            Title = "NoXaml Demo";
            Width = 640;
            Height = 360;

            UIBuilder.UpdateUI(this);
        }
        
        public void DoToggle()
        {
            ShowMore = !ShowMore;
            
            UIBuilder.UpdateUI(this);
        }

        public void DoRollDie()
        {
            DieRoll = RNG.Next(6) + 1;
            
            UIBuilder.UpdateUI(this);
        }
    }
}
