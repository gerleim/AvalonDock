# Claude Code Guidelines

## Project Overview
This is a fork of [Dirkster99/AvalonDock](https://github.com/Dirkster99/AvalonDock) (forked at v4.72.0), a WPF docking layout library. Published as `Raisin.AvalonDock` on NuGet.

### Key changes from upstream
- Retargeted to .NET 8 (`net8.0-windows`)
- Floating window improvements: inline title editing, docking lock toggle button, fix for transparent flash on startup layout restore
- Navigator window: mouse click selection, hide anchorables panel when no tool windows visible
- Fix tab order corruption in DocumentPaneTabPanel on overflow
- Fix drag-to-float broken by FilterMessage handled flag override
- Snap-to-sibling splitter behavior
- Persist floating window titles with layout serialization

## NuGet
- Packages: `Raisin.AvalonDock`, `Raisin.AvalonDock.Themes.VS2013`
- Build: `dotnet pack -c Release -o nupkgs/`
- Version is set in 6 csproj files under `source/Components/` — keep them in sync when bumping.

## Git
- Do not add Co-Authored-By lines to commit messages.
