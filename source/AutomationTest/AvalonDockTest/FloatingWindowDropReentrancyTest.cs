namespace AvalonDockTest;

using System;
using System.Linq;
using AvalonDock.Layout;
using NUnit.Framework;

/// <summary>
/// Reproduces the reentrancy bug where dragging a document from a floating window
/// with two side-by-side panes causes CollectGarbage to run inside InsertChildAt,
/// modifying the same ObservableCollection that is mid-Insert.
///
/// The control-level fix defers InternalClose via Dispatcher.BeginInvoke so that
/// CollectGarbage runs after InsertChildAt completes, avoiding the reentrancy.
/// </summary>
[TestFixture]
public sealed class FloatingWindowDropReentrancyTest
{
    /// <summary>
    /// Builds the layout state that exists at drop time:
    /// - A "target" floating window with one doc in pane A and an empty pane B
    ///   (empty because the dragged doc was already removed to create the drag window)
    /// - A "drag" floating window with the dragged doc in pane C
    /// Returns (root, targetFw, dragFw, doc1 in target, doc2 in drag).
    /// </summary>
    private static (LayoutRoot root, LayoutDocumentFloatingWindow targetFw,
        LayoutDocumentFloatingWindow dragFw, LayoutDocument doc1, LayoutDocument doc2) BuildDropScenario()
    {
        var doc1 = new LayoutDocument { ContentId = "Chart1" };
        var doc2 = new LayoutDocument { ContentId = "Chart2" };

        // Target floating window: pane A (doc1) + pane B (empty — doc2 was just dragged out)
        var paneA = new LayoutDocumentPane(doc1);
        var paneB = new LayoutDocumentPane(); // empty
        var targetGroup = new LayoutDocumentPaneGroup { Orientation = System.Windows.Controls.Orientation.Horizontal };
        targetGroup.Children.Add(paneA);
        targetGroup.Children.Add(paneB);
        var targetFw = new LayoutDocumentFloatingWindow { RootPanel = targetGroup };

        // Drag floating window: pane C (doc2) — the temp window created for the drag
        var paneC = new LayoutDocumentPane(doc2);
        var dragGroup = new LayoutDocumentPaneGroup();
        dragGroup.Children.Add(paneC);
        var dragFw = new LayoutDocumentFloatingWindow { RootPanel = dragGroup };

        // Wire both into a LayoutRoot (required for CollectGarbage)
        var root = new LayoutRoot
        {
            RootPanel = new LayoutPanel(new LayoutDocumentPaneGroup(new LayoutDocumentPane()))
        };
        root.FloatingWindows.Add(targetFw);
        root.FloatingWindows.Add(dragFw);

        return (root, targetFw, dragFw, doc1, doc2);
    }

    /// <summary>
    /// Proves the bug: synchronous CollectGarbage during InsertChildAt causes
    /// InvalidOperationException (ObservableCollection reentrancy).
    ///
    /// This simulates the unfixed control behavior where Model_PropertyChanged
    /// calls InternalClose → Close → OnClosed → CollectGarbage synchronously.
    /// </summary>
    [Test]
    public void SynchronousGarbageCollectionDuringInsertCausesReentrancy()
    {
        var (root, targetFw, dragFw, _, _) = BuildDropScenario();

        // ObservableCollection.CheckReentrancy only throws when there are 2+ CollectionChanged
        // subscribers. In the real app, LayoutDocumentPaneGroupControl's binding to Children
        // adds the second subscriber. Without it, reentrant modifications silently succeed.
        targetFw.RootPanel.Children.CollectionChanged += (_, _) => { };

        // Simulate the unfixed control: when RootPanel is set to null,
        // call CollectGarbage synchronously (as InternalClose → OnClosed would).
        dragFw.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LayoutDocumentFloatingWindow.RootPanel)
                && dragFw.RootPanel == null)
            {
                root.CollectGarbage();
            }
        };

        // Simulate the drop: insert the drag window's RootPanel into the target's RootPanel.
        // InsertChildAt triggers auto-reparenting → RemoveChild on dragFw → RootPanel = null
        // → PropertyChanged → CollectGarbage → tries to Remove empty paneB from the same
        // Children collection that is mid-Insert → InvalidOperationException.
        Assert.Throws<InvalidOperationException>(
            (Action)(() => targetFw.RootPanel.InsertChildAt(0, dragFw.RootPanel)));
    }

    /// <summary>
    /// Proves the fix: when CollectGarbage is deferred (not called during InsertChildAt),
    /// the drop succeeds and both documents survive in the target floating window.
    ///
    /// This simulates the fixed control behavior where Model_PropertyChanged defers
    /// InternalClose via Dispatcher.BeginInvoke, so CollectGarbage runs after the
    /// insert completes.
    /// </summary>
    [Test]
    public void DeferredGarbageCollectionAllowsDropToSucceed()
    {
        var (root, targetFw, dragFw, doc1, doc2) = BuildDropScenario();

        // No synchronous PropertyChanged → GC hook (simulates the deferred fix).
        // Just perform the insert as DocumentPaneDropTarget.Drop does.
        var dragRootPanel = dragFw.RootPanel;
        targetFw.RootPanel.InsertChildAt(0, dragRootPanel);

        // Now call CollectGarbage separately (as the deferred InternalClose would).
        root.CollectGarbage();

        // Both documents should be reachable in the layout
        var allDocs = root.Descendents().OfType<LayoutDocument>().ToList();
        Assert.AreEqual(2, allDocs.Count, "Both documents should survive the drop");
        Assert.IsTrue(allDocs.Any(d => d.ContentId == "Chart1"), "Chart1 should be in the layout");
        Assert.IsTrue(allDocs.Any(d => d.ContentId == "Chart2"), "Chart2 should be in the layout");

        // Both should be descendants of the target floating window
        var targetDocs = targetFw.Descendents().OfType<LayoutDocument>().ToList();
        Assert.AreEqual(2, targetDocs.Count, "Both documents should be in the target floating window");

        // The empty pane should have been cleaned up by GC
        var emptyPanes = targetFw.Descendents().OfType<LayoutDocumentPane>()
            .Where(p => p.ChildrenCount == 0).ToList();
        Assert.AreEqual(0, emptyPanes.Count, "Empty panes should be cleaned up by GC");
    }
}
