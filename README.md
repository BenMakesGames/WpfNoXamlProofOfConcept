## What

WpfNoXamlProofOfConcept uses C# Source Generation - https://devblogs.microsoft.com/dotnet/introducing-c-source-generators/ - to create code-based WPF from a custom XML format.

It follows a component-based paradigm more similar to Blazor, Angular, or React.

The custom XML format allows inline `forEach` loops, `if` blocks, and C# expressions for labels and text values.

It's currently SUPER-limited, put together in less than 8 hours.

## Why

Coming from a web fullstack background, I've been very disappointed with Windows desktop development. XAML is old, and clunky, and the MVVM implementation people use with it is ill-suited to dependency injection.

Blazor Desktop and React Native Windows both look super-cool, but I'm required to use WPF for my particular application, so those solutions aren't available to me. If _you_ can use Blazor Desktop or RNW, you probably should, instead of this.

## Limitations

* No automatic change detection; I think implementing a sort of virtual DOM - https://medium.com/@deathmood/how-to-write-your-own-virtual-dom-ee74acc13060 - could work here.
* Code-generation is very primitive: just string concatenation. I have yet to look at the `System.CodeDom` API in detail (I use it for formatting C# strings, but that's it), and I suspect it would help a lot.
* Most properties on most built-in controls (`StackPanel` `Orientation`, etc) aren't supported, and properties for custom controls will require modifying the code generator. I think these are related problems, and am not 100% sure how to tackle them at the moment, though I have some early ideas.
* No IDE support in view XML. Solving this would require writing a VS extension.
* C# expressions in strings require `&quot;`, which just looks ugly :| Example: `<Button _click="DoToggle()" _text="ShowMore ? &quot;Less&quot; : &quot;More&quot;" />`

## Features

* NOT MVVM; component-based/MVP-y. If you've used Blazor, Angular, or React, it will feel familiar to you. A component's code-behind contains properties and methods which are easily accessible by its XML.
* DI is very easy to achieve; see `WpfNoXaml/Startup.cs` for application entry point, including config loading and DI setup using `Microsoft.Extensions.*`.

## Example

![GIF of running demo](https://github.com/BenMakesGames/WpfNoXamlProofOfConcept/blob/main/no-xaml-demo.gif?raw=true)

From this `MainWindow.xml`:

```xml
<?Using System?>
<?Using System.Linq?>

<StackPanel>
    <TextBlock text="Hi" />

    <Button _click="DoToggle()" _text="ShowMore ? &quot;Less&quot; : &quot;More&quot;" />
    <StackPanel _if="ShowMore">
        <TextBlock _forEach="l in Labels" _text="l" />
    </StackPanel>

    <WrapPanel>
        <Button _click="DoRollDie()" text="Roll Die" />
        <TextBlock _text="DieRoll == 0 ? &quot;&quot; : DieRoll.ToString()" />
    </WrapPanel>
</StackPanel>
```

Which got compiled into this:

```C#
using NoXaml.Framework.Extensions.WPF;
using NoXaml.Interfaces.Components;
using System.Windows;
using System.Windows.Controls;
using System;
using System.Linq;


namespace WpfNoXaml.Components
{
    public partial class MainWindow: INoXaml
    {
        public override void BuildUI()
        {
			Content = (new StackPanel()
				.Add(new TextBlock()
					.SetText("Hi")
				)
				.Add(new Button()
					.OnClick((_sender, _args) => DoToggle())
					.SetText(ShowMore ? "Less" : "More")
				)
				.If(ShowMore, _v1 =>
					_v1.Add(new StackPanel()
						.ForEach(Labels, (_v2, l) =>
							_v2.Add(new TextBlock()
								.SetText(l)
						))
				))
				.Add(new WrapPanel()
					.Add(new Button()
						.OnClick((_sender, _args) => DoRollDie())
						.SetText("Roll Die")
					)
					.Add(new TextBlock()
						.SetText(DieRoll == 0 ? "" : DieRoll.ToString())
					)
				)
			);

        }
    }
}
```