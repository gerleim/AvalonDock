# Raisin.AvalonDock

A WPF docking layout library for building IDE-like interfaces with document and tool window management — similar to Visual Studio, Eclipse, and PhotoShop.

This is a maintained fork of [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock) targeting **.NET 8**, with additional bug fixes and enhancements.

## Features

- Drag-and-drop document and tool window docking
- Floating windows, auto-hide panels, and tabbed documents
- Save/restore layout serialization
- Multiple built-in themes (VS2013, VS2010, Metro, Aero, Expression)
- MVVM-friendly with full data binding support

## Installation

```
dotnet add package Raisin.AvalonDock
```

Optional theme packages:

```
dotnet add package Raisin.AvalonDock.Themes.VS2013
```

## Quick Start

```xml
<avalondock:DockingManager DocumentsSource="{Binding Documents}"
                           AnchorablesSource="{Binding Tools}">
    <avalondock:LayoutRoot>
        <avalondock:LayoutPanel Orientation="Horizontal">
            <avalondock:LayoutDocumentPane />
        </avalondock:LayoutPanel>
    </avalondock:LayoutRoot>
</avalondock:DockingManager>
```

## License

Licensed under the Microsoft Public License (MS-PL). See [LICENSE](LICENSE) for details.
