# AvalonDock Upstream Update Plan

## Goal

Cherry-pick bug fixes from upstream Dirkster99/AvalonDock v5.0.0 into our fork (v4.72.6), without adopting the v5 architectural restructuring. We stay on the monolithic AvalonDock project structure with our existing serialization and infrastructure.

## Fork customizations to preserve

See [UPSTREAM-DELTA.md](UPSTREAM-DELTA.md) § "Fork-specific customizations" — 12 items that must survive any merge.

## Changes to apply

### 1. LayoutContent.CloseInternal() — null checks
**File:** `Layout/LayoutContent.cs`
**Upstream fix:** Adds null check for `parentAsContainer` early return, and null check on `parentAsGroup` before calling `IndexOfChild()`.
**Why:** Prevents NullReferenceException when closing content whose parent has already been removed.
**Status:** Done

### 2. LayoutContent.DockAsDocument() — floating window safety (#551)
**File:** `Layout/LayoutContent.cs`
**Upstream fix:** Checks that `PreviousContainer` pane is not inside a `LayoutDocumentFloatingWindow` before reusing it. Adds fallback chain: try `LastFocusedDocument.Parent` (if not floating), then first docked pane, then any pane.
**Why:** Prevents documents from being docked into a floating window's pane instead of the main layout.
**Status:** Done — `[Ignore]` removed from #551 test, now passes

### 3. LayoutAnchorable.Show() — bounds checking
**File:** `Layout/LayoutAnchorable.cs`
**Upstream fix:** Uses pattern-match null check (`PreviousContainer is ILayoutGroup previousContainerAsLayoutGroup`) and adds bounds check (`PreviousContainerIndex >= 0 && PreviousContainerIndex < count`).
**Why:** Prevents index-out-of-range when `PreviousContainerIndex` is stale or -1.
**Status:** Done

### 4. DragService — null-safe navigation
**File:** `Controls/DragService.cs`
**Upstream change:** Uses `?.` safe navigation on layout properties, but removes `IsDockingLocked` check.
**Resolution:** Added `?.` null-safety while keeping our fork-specific `IsDockingLocked` check.
**Status:** Done

### 5. ~~DockingManager.ExecuteCloseAll/CloseAllButThis — floating documents~~
**Status:** Already identical — fork already handles `LayoutDocumentFloatingWindow`. No change needed.

### 6. DockingManager.OnSizeChanged() — null checks
**File:** `DockingManager.cs`
**Upstream fix:** Adds null checks for `LayoutRootPanel`, `RightSidePanel`, `LeftSidePanel`, `TopSidePanel`, `BottomSidePanel` before accessing their properties.
**Why:** Prevents NullReferenceException during early layout passes when panels haven't been initialized yet.
**Status:** Done

### 7. DockingManager_Loaded() — collection iteration safety
**File:** `DockingManager.cs`
**Upstream fix:** Adds `.ToArray()` on `_fwHiddenList` and `Layout.FloatingWindows.Where(...)` before iterating, since the loop body modifies the collections.
**Why:** Prevents collection-modified-during-iteration exceptions.
**Status:** Done

### 8. Xceed backport fixes (#541)
**Upstream commit:** `50671cb`
**What:** Four fixes backported from Xceed's commercial branch (v4.2–v5.0).

**8a. InputBindings transfer to floating windows**
**File:** `Controls/LayoutFloatingWindowControl.cs`
**Fix:** Adds `CopyInputBindingsFromOwner()` that copies keyboard shortcuts from the Owner window to floating windows so hotkeys work in undocked panels.
**Status:** Done

**8b. PointToScreenDPI null safety**
**File:** `Controls/TransformExtentions.cs`
**Fix:** Guards `PointToScreenDPI` against disconnected visuals (PresentationSource null check). Complements the existing `TransformToDeviceDPI` and `TransformFromDeviceDPI` null checks.
**Status:** Done

**8c. Vertical drag buffer for document tab reorder**
**File:** `Controls/LayoutDocumentTabItem.cs`
**Fix:** `_parentDocumentTabPanelScreenArea.Inflate(0, Height/2)` adds vertical tolerance so dragging to reorder tabs doesn't accidentally trigger floating.
**Status:** Done

**8d. Soften FixupLayout on stale PreviousContainer references**
**File:** `Layout/Serialization/LayoutSerializer.cs`
**Fix:** Instead of throwing `ArgumentException` when a PreviousContainer pane ID is not found during deserialization, clears the reference and continues. Prevents crash with stale/edited layout files.
**Status:** Done

**8e. CloseInternal null Parent guard** — Already in our fork (item #1). No change needed.

### 9. Null-safety in drag/drop path (#554)
**Upstream commit:** `0d6bd81`
**What:** Null-safety improvements across drag/drop and pane activation code.

**9a. UpdateDragPosition null guard**
**File:** `Controls/LayoutFloatingWindowControl.cs`
**Fix:** Adds `if (Model?.Root?.Manager == null) return;` before creating DragService. Prevents NRE when dragging a floating window disconnected from the layout tree.
**Status:** Done

**9b. SelectedContent null checks in pane activation helpers**
**File:** `Controls/LayoutFloatingWindowControlHelper.cs`
**Fix:** Adds `SelectedContent != null &&` before accessing `SelectedContent.IsActive` in both anchorable and document pane activation.
**Status:** Done

**9c. OverlayWindow areaElement null guard**
**File:** `Controls/OverlayWindow.cs`
**Fix:** Adds `if (areaElement == null) return;` before setting Canvas position on drop area element.
**Status:** Done

**9d–f. DragService, DockingManager, LayoutAnchorable** — Already in our fork (items #4, #7, #3). No change needed.

### 10. Remove invalid IsFloating check in LayoutRoot.CollectGarbage (#451)
**Upstream commit:** `f827146`
**File:** `Layout/LayoutRoot.cs`
**Fix:** Removes `&& !c.IsFloating` from both the PreviousContainer clearing loop and the empty-pane removal check. The old code only cleared stale references for non-floating content, leaving floating content with PreviousContainer references to empty panes — preventing GC of those panes and causing potential layout corruption.
**Status:** Done

### 11. Stable pane insertion order for DocumentPaneDockRight/Bottom (#556)
**Upstream commit:** `269cd28`
**File:** `Controls/DocumentPaneDropTarget.cs`
**Fix:** When dropping a document to the Right or Bottom, `IndexOfChild(targetModel)` could return -1 if the target moved during drag. The old code used that raw index, inserting at position 0 instead of the end. Fix computes `insertToIndex = targetIndex < 0 ? Children.Count : targetIndex + 1` and clamps to `Children.Count`.
**Status:** Done

### 12. LayoutDocumentTabItem null-safety (#517)
**Upstream commit:** `b5957c4`
**File:** `Controls/LayoutDocumentTabItem.cs`
**Fix:** Adds `Model != null` check before `Model.IsActive = true` in `OnMouseLeftButtonDown`, and null-conditional `containerPane?.Parent` and `containerPane?.MoveChild()` during tab reorder. Prevents NRE when model is disconnected.
**Status:** Done

### 14. Auto-hide anchor double-click and right-click (#517)
**Upstream commit:** `b5957c4`
**Files:** `Controls/LayoutAnchorControl.cs`, `DockingManager.cs`
**Feature:** Adds two opt-in DPs on DockingManager (both default `false`):
- `AllowAnchorDoubleClickDock` — double-click auto-hide tab to dock/pin it
- `AllowAnchorRightClickContextMenu` — right-click auto-hide tab for Float/Dock/Hide context menu
**Status:** Done

### 15. OnActivated PresentationSource retry for multi-DPI (#517)
**Upstream commit:** `b5957c4`
**File:** `Controls/LayoutFloatingWindowControl.cs`
**Fix:** When dragging a panel to float, `OnActivated` calls `PointToScreenDPI` which requires a connected `PresentationSource`. In multi-DPI setups the window may not be fully initialized yet, causing `InvalidOperationException`. Fix adds a null check with async retry (up to 5 times, 10ms delay) and also guards the DPI recalculation branch.
**Status:** Done

### 16. Float/Dock lifecycle events (#545)
**Upstream commit:** `d49db72`
**Files:** `DockingManager.cs`, `ContentFloatingEventArgs.cs` (new), `ContentDockedEventArgs.cs` (new)
**Feature:** Adds four events on DockingManager: `ContentFloating` (cancelable), `ContentFloated`, `ContentDocking` (cancelable), `ContentDocked`. Raised from `StartDraggingFloatingWindowForContent`, `StartDraggingFloatingWindowForPane`, `ExecuteFloatCommand`, `ExecuteDockCommand`, and `ExecuteDockAsDocumentCommand`.
**Status:** Done

### 17. ResizeBorderThickness property for floating windows (#544)
**Upstream commit:** `47c4e16`
**File:** `Controls/LayoutFloatingWindowControl.cs`
**Feature:** Adds `ResizeBorderThickness` DP that overrides `WindowChrome.ResizeBorderThickness` at load time and dynamically when changed. Enables programmatic control of the resize grip area without template overrides.
**Status:** Done

### 18. AllowMovingFloatingWindowWithKeyboard (#542)
**Upstream commit:** `655bfa2`
**Files:** `Controls/LayoutFloatingWindowControl.cs`, `DockingManager.cs`
**Feature:** Adds `AllowMovingFloatingWindowWithKeyboard` DP on DockingManager (default `false`). When enabled, arrow keys move floating windows by 10px per keystroke. Improves keyboard accessibility.
**Status:** Done

### 19. Customizable NavigatorWindow labels (#543)
**Upstream commit:** `4a46d5f`
**Files:** `Controls/NavigatorWindow.cs`, `Themes/generic.xaml`
**Feature:** Adds `AnchorablesLabel` and `DocumentsLabel` DPs to NavigatorWindow, defaulting to the existing resource strings. generic.xaml binds to these properties instead of hardcoded `{x:Static}` resource references. Enables localization and customization of Ctrl+Tab navigator section headers.
**Status:** Done

### 20. AnchorableGridStyle for LayoutAutoHideWindowControl (#498)
**Upstream commit:** `b32df6b`
**File:** `Controls/LayoutAutoHideWindowControl.cs`
**Feature:** Adds `AnchorableGridStyle` DP to style the inner Grid hosting the auto-hide pane content. Also hooks `SizeChanged` on the grid to call `InvalidateMeasure()`, fixing potential auto-hide sizing issues.
**Status:** Done

## Decisions made

### Skip upstream v5 architectural projects

These are new abstraction layers for a library serving diverse consumers. We're a single WPF consumer with our own infrastructure — no value in adopting them.

- **AvalonDock.Core** (netstandard2.0) — UI-agnostic interface extraction. We're WPF-only.
- **AvalonDock.Mvvm** — MVVM support library. We have our own.
- **AvalonDock.DependencyInjection** — MS.Extensions.DI integration. We have our own.
- **AvalonDock.Mvvm.CommunityToolkit** — CommunityToolkit.Mvvm bindings. Not used.
- **AvalonDock.Themes.Arc / AvalonDock.Themes.VS** — New themes. Not using them.
- **ToggleLayoutEngine / ToggleDockingManager** — Layout engine abstraction. Not needed.

## Open questions

### 1. DTO-based serialization (upstream PR #580)

Upstream replaced `IXmlSerializable` on layout model classes with a separate DTO layer + `LayoutDtoMapper`. Each model class no longer knows how to serialize itself — instead, a mapper walks the tree and produces plain data objects that any serializer (XML, JSON) can handle.

**Why upstream did it:**
- Decouple layout models from serialization format
- Enable multiple serializers (they added JSON via Newtonsoft alongside XML)
- Remove `IXmlSerializable` boilerplate from every model class
- Cleaner separation of concerns

**Would we gain anything?**
- We only use XML serialization and have no plans for JSON
- Our `XmlLayoutSerializer` works and we've extended it (floating window title persistence)
- Adopting DTOs would be a large diff touching every model class, plus we'd need to port our title persistence into the mapper
- Risk of introducing serialization regressions in a system that currently works

**Tentative answer:** Skip. The cost/risk outweighs the benefit for a single-format consumer. Revisit only if we ever need a second serialization format.
