# Raisin.AvalonDock

A WPF docking layout library for building IDE-like interfaces with document and tool window management — similar to Visual Studio, Eclipse, and PhotoShop.

This is a maintained fork of [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock) (forked at v4.72.0), targeting **.NET 8** with bug fixes and enhancements.

## Fork-specific enhancements

- Floating window inline title editing and docking lock toggle
- Floating window transparent flash fix on startup layout restore
- Floating window title persistence with layout serialization
- Navigator window: mouse click selection, hide empty anchorables panel, deferred activation, Shift+Tab backwards cycling
- Snap-to-sibling splitter behavior
- Tab order corruption fix in DocumentPaneTabPanel
- Drag-to-float FilterMessage fix
- Floating window drop reentrancy fix (Dispatcher.BeginInvoke deferral)
- Single-child document pane group collapse with stale DockWidth

## Cherry-picked upstream bug fixes

- DockAsDocument floating window safety (#551) — prevents docking into floating panes
- CloseInternal null checks — prevents NRE when parent already removed
- LayoutAnchorable.Show() bounds checking — prevents index overflow on stale PreviousContainerIndex
- DragService null-safe navigation
- DockingManager OnSizeChanged null checks — prevents NRE during early layout passes
- DockingManager_Loaded collection-safe iteration — prevents modification during enumeration
- TransformToDeviceDPI null safety — prevents NRE when PresentationSource unavailable
- OnClosed null checks in LayoutAnchorableFloatingWindowControl
- Skip mouse events on unloaded LayoutDocumentTabItem (#454)
- Don't double-add floating windows in OnLayoutChanged (#457)
- WindowState.Maximized timing fix inside BeginInvoke (#490)
- Remove CaptureMouse interference before WM_NCLBUTTONDOWN (#459)
- GetSide multi-pane support for multiple anchorable panes on same side (#486)
- Xceed backport (#541): InputBindings transfer to floating windows — keyboard shortcuts work in undocked panels
- Xceed backport (#541): PointToScreenDPI null safety — prevents NRE on disconnected visuals
- Xceed backport (#541): Vertical drag buffer for tab reorder — prevents accidental floating when reordering tabs
- Xceed backport (#541): Soften FixupLayout — skip missing PreviousContainer references instead of throwing
- Null-safety (#554): UpdateDragPosition guard for disconnected floating windows
- Null-safety (#554): SelectedContent null check in pane activation helpers
- Null-safety (#554): OverlayWindow areaElement null guard for unknown drop area types
- Remove invalid IsFloating check in CollectGarbage (#451) — fixes stale PreviousContainer references and pane leaks
- Stable pane insertion order (#556) — prevents wrong position when docking right/bottom
- LayoutDocumentTabItem null-safety (#517) — prevents NRE on disconnected model during tab click/reorder
- Auto-hide anchor double-click dock and right-click context menu (#517) — opt-in DPs on DockingManager
- OnActivated PresentationSource retry (#517) — prevents InvalidOperationException in multi-DPI drag-to-float
- Float/Dock lifecycle events (#545) — cancelable ContentFloating/ContentDocking events on DockingManager
- ResizeBorderThickness property (#544) — programmatic control of floating window resize grip
- AllowMovingFloatingWindowWithKeyboard (#542) — arrow-key movement for floating windows
- Customizable NavigatorWindow labels (#543) — AnchorablesLabel/DocumentsLabel DPs for localization
- AnchorableGridStyle for auto-hide window (#498) — custom style for inner Grid + InvalidateMeasure fix

## Installation

```
dotnet add package Raisin.AvalonDock
```

Optional theme package:

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
