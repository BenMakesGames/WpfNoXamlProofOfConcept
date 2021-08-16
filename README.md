## What

WpfNoXamlProofOfConcept uses [C# Source Generation](https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/) to create a kind of virtual DOM from a custom XML view format. This virtual DOM is used to render the UI, by building WPF components programatically. (There are code samples, and a GIF of the resulting application, below.)

It follows a component-based paradigm more similar to [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor), [Angular](https://angular.io/), or [React](https://reactjs.org/).

The custom XML format allows inline `forEach` loops, `if` blocks, and C# expressions for property values. (Don't write a value converter when a simple ternary operator will do!)

It's currently SUPER-limited, put together in about 8 hours.

## Why

Coming from a web fullstack background, I've been very disappointed with Windows desktop development. XAML is old, and clunky, and the MVVM implementation it forces is ill-suited to dependency injection.

## Why Not

You should not use WpfNoXamlProofOfConcept for a production application. Use [React Native for Windows](https://microsoft.github.io/react-native-windows/), instead.

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

        // "Random" having been registered with Microsoft DI as a singleton service
        public MainWindow(Random rng)
        {
            RNG = rng;

            Title = "NoXaml Demo";
            Width = 640;
            Height = 360;

            UIBuilder.UpdateUI(this); // TODO: remove this line after implementing change-detection
        }
        
        public void DoToggle()
        {
            ShowMore = !ShowMore;
            
            UIBuilder.UpdateUI(this); // TODO: remove this line after implementing change-detection
        }

        public void DoRollDie()
        {
            DieRoll = RNG.Next(6) + 1;
            
            UIBuilder.UpdateUI(this); // TODO: remove this line after implementing change-detection
        }
    }
}
```

A C# Source Generator (https://github.com/BenMakesGames/WpfNoXamlProofOfConcept/blob/main/NoXaml.Compiler/ViewGenerator.cs) generates the following partial class from the view XML at compile-time. The `BuildVDOM` method it generates builds a virtual DOM, which is used by the framework to look for and apply changes to the view. Again: there are improvements that could be made to the efficiency of this logic, and improvements to add automatic change detection.

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
