## What

WpfNoXamlProofOfConcept uses C# Source Generation - https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/ - to create code-based WPF from a custom XML format.

It follows a component-based paradigm more similar to Blazor, Angular, or React.

The custom XML format allows inline `forEach` loops, `if` blocks, and C# expressions for labels and text values.

It's currently SUPER-limited, put together in less than 8 hours.

## Why

Coming from a web fullstack background, I've been very disappointed with Windows desktop development. XAML is old, and clunky, and the MVVM implementation people use with it is ill-suited to dependency injection.

Blazor Desktop and React Native Windows both look super-cool, but I'm required to use WPF for my particular application, so those solutions aren't available to me. If _you_ can use Blazor Desktop or RNW, you probably should, instead of this.

## Features

* Properties values can be C# expressions by prefixing their name with an underscore, ex: `<TextBlock Text="Hi" _FontSize="SomeModelProperty * 5" />`. `Text`'s value is a string; `_FontSize`'s is an evaluated C# expression.
* `_if` and `_forEach` attributes allow you to conditionally include elements in a component, ex: `<Button _if="SomeModelProperty < 10" Content="Click Me" />`
* The framework generates a virtual DOM from your view XML, and applies changes to the view at run-time as needed, based on component properties. (There are some improvements that could be made here, still, for greater efficiency.)
* Component-based/MVP-y (not MVVM). If you've used Blazor, Angular, or React, it will feel familiar to you. A component's code-behind contains properties and methods which are easily accessible by its XML. Command, and dependency property boilerplate no longer needed, because these things aren't used! (View model property change notification is also not used, but you currently have to do something similar - see code example below - but I think I can eliminate this need with C# Source Generation, as well.)
* DI/IoC container is very easy to achieve, everywhere. No viewmodel locators or factories needed. See `WpfNoXaml/Startup.cs` for application entry point, including config loading and DI setup using `Microsoft.Extensions.*`.

## Limitations

* Code-generation is very primitive: just string concatenation. I have yet to look at the `System.CodeDom` API in detail (I use it for formatting C# strings, but that's it), and I suspect it would help a lot.
* No automatic change detection... yet!
* Some improvements to be made for event handlers, to support them generally (currently, only click is supported, via hard-coded logic).
* No IDE support in view XML. Solving this would require writing a VS extension.
* C# expressions in strings require `&quot;` for double-quotes, which just looks ugly :| Example: `<Button _click="DoToggle()" _text="ShowMore ? &quot;Less&quot; : &quot;More&quot;" />`

## Example

![GIF of running demo](https://github.com/BenMakesGames/WpfNoXamlProofOfConcept/blob/main/no-xaml-demo.gif?raw=true)

The view, `MainWindow.xml`:

```xml
<?Using System?>
<?Using System.Linq?>

<StackPanel>
  <TextBlock Text="Hi" _FontSize="(DieRoll + 5) * 2" _TextAlignment="TextAlignment.Center" />

  <Button _click="DoToggle()" _Content="ShowMore ? &quot;Less&quot; : &quot;More&quot;" />
  <StackPanel _if="ShowMore">
    <TextBlock _forEach="l in Labels" _Text="l" />
  </StackPanel>
  <WrapPanel>
    <Button _click="DoRollDie()" Content="Roll Die" />
    <TextBlock _Text="DieRoll == 0 ? &quot;&quot; : DieRoll.ToString()" />
  </WrapPanel>
</StackPanel>
```

The code-behind, `MainWindow.cs`:

```C#
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

            UIBuilder.UpdateUI(this); // TODO: remove this line after implementing chang-detection
        }
        
        public void DoToggle()
        {
            ShowMore = !ShowMore;
            
            UIBuilder.UpdateUI(this); // TODO: remove this line after implementing chang-detection
        }

        public void DoRollDie()
        {
            DieRoll = RNG.Next(6) + 1;
            
            UIBuilder.UpdateUI(this); // TODO: remove this line after implementing chang-detection
        }
    }
}
```

A C# Source Generator generates the following partial class from the view XML at compile-time. The `BuildVDOM` method it generates builds a virtual DOM, which is used by the framework to look for and apply changes to the view. Again: there are improvements that could be made to the efficient of this logic, and improvements to add automatic change detection.

```C#
using NoXaml.Framework.Extensions.WPF;
using NoXaml.Model.Components;
using NoXaml.Model.DOM;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Linq;


namespace WpfNoXaml.Components
{
    public partial class MainWindow: INoXaml
    {
        public override Element BuildVDOM()
        {
			var _0 = new Element(typeof(StackPanel));
			var _0Child1 = new Element(typeof(TextBlock));
			_0Child1.Properties = new Dictionary<string, object>()
			{
				{ "Text", "Hi" },
				{ "FontSize", (DieRoll + 5) * 2 },
				{ "TextAlignment", TextAlignment.Center },
			};
			_0.Children.Add(_0Child1);
			var _0Child2 = new Element(typeof(Button));
			_0Child2.Properties = new Dictionary<string, object>()
			{
				{ "click", (RoutedEventHandler)((_sender, _args) => DoToggle()) },
				{ "Content", ShowMore ? "Less" : "More" },
			};
			_0.Children.Add(_0Child2);
			if(ShowMore)
			{
				var _0Child3 = new Element(typeof(StackPanel));
				_0Child3.Children.AddRange(Labels.Select(l => {
					var _0Child3Child1ForEach = new Element(typeof(TextBlock));
					_0Child3Child1ForEach.Properties = new Dictionary<string, object>()
					{
						{ "Text", l },
					};
					return _0Child3Child1ForEach;
				}));
				_0.Children.Add(_0Child3);
			}
			var _0Child4 = new Element(typeof(WrapPanel));
			var _0Child4Child1 = new Element(typeof(Button));
			_0Child4Child1.Properties = new Dictionary<string, object>()
			{
				{ "click", (RoutedEventHandler)((_sender, _args) => DoRollDie()) },
				{ "Content", "Roll Die" },
			};
			_0Child4.Children.Add(_0Child4Child1);
			var _0Child4Child2 = new Element(typeof(TextBlock));
			_0Child4Child2.Properties = new Dictionary<string, object>()
			{
				{ "Text", DieRoll == 0 ? "" : DieRoll.ToString() },
			};
			_0Child4.Children.Add(_0Child4Child2);
			_0.Children.Add(_0Child4);

            return _0;
        }
    }
}
```