# AvalonDock: Fork vs. Upstream Delta

Upstream: [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock) v5.0.0
Fork: Raisin.AvalonDock v4.72.6 (branched from upstream ~v4.72.0)

## What the fork uses

| Component | Fork TFM | Upstream TFM |
|-----------|----------|--------------|
| AvalonDock (main library) | net8.0-windows | net9.0/net10.0-windows + net48 |
| Themes: Aero, Expression, Metro, VS2010, VS2013 | net8.0-windows | net9.0/net10.0-windows + net48 |
| Layout/Serialization (XmlLayoutSerializer) | Inline in main project | Extracted to AvalonDock.Serializer.Xml |
| Test project (NUnit 4.6.1) | net8.0-windows | multi-TFM + FlaUI |

## What the fork drops

These upstream v5 projects are not used by the fork and not relevant to adopt.

| Upstream project | Purpose | Why we skip it |
|------------------|---------|----------------|
| AvalonDock.Core (netstandard2.0) | UI-agnostic interfaces extracted from main | We only target WPF — no cross-platform need |
| AvalonDock.Mvvm | MVVM support library | We use our own MVVM infrastructure |
| AvalonDock.DependencyInjection | MS.Extensions.DI integration | We have our own DI |
| AvalonDock.Mvvm.CommunityToolkit | CommunityToolkit.Mvvm bindings | Not used |
| AvalonDock.Serializer.Json | JSON layout serialization (Newtonsoft) | We use XML serialization |
| AvalonDock.Themes.Arc | New theme | Not using it |
| AvalonDock.Themes.VS | VS2022 theme with .vstheme support | Not using it |
| ToggleLayoutEngine / ToggleDockingManager | Layout engine abstraction layer | Not needed |
| LayoutDtoMapper | DTO-based serialization (replaced IXmlSerializable) | Would break our XmlLayoutSerializer |

## Fork-specific customizations

Changes in our fork that are not in upstream and must be preserved during any merge.

1. **Floating window InternalClose deferral** — `Dispatcher.BeginInvoke` to prevent ObservableCollection reentrancy during drag-drop (LayoutDocumentFloatingWindowControl, LayoutAnchorableFloatingWindowControl)
2. **Floating window inline title editing** — PART_TitleEditor TextBox for renaming floating windows
3. **Floating window docking lock** — IsDockingLocked property and toggle button on floating windows
4. **Floating window title persistence** — titles saved/restored with layout serialization
5. **Floating window transparent flash fix** — prevents transparent flash on startup layout restore
6. **DropTarget null-safe ActiveContent** — null-conditional on `root?.ActiveContent`
7. **Navigator window mouse click selection** — added mouse click to select items in navigator
8. **Navigator window hide empty anchorables panel** — hides panel when no tool windows visible
9. **Navigator window deferred activation** — `Dispatcher.BeginInvoke` for activation after ShowDialog
10. **Navigator window Shift+Tab** — backwards cycling in navigator
11. **Snap-to-sibling splitter behavior** — splitter snaps to adjacent sibling edges
12. **Tab order corruption fix** — DocumentPaneTabPanel tab ordering
13. **Drag-to-float FilterMessage fix** — FilterMessage handling during drag-to-float
14. **Single-child pane group collapse** — collapses groups with stale DockWidth
15. **NuGet rebranding** — package IDs renamed to Raisin.AvalonDock
