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
