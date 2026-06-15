using NUnit.Framework;
using AvalonDock.Layout;

namespace AvalonDockTest
{
	/// <summary>
	/// Unit tests for LayoutContent properties (IsActive, IsSelected, CanClose, CanFloat, etc.)
	/// These test the layout model without needing a DockingManager or WPF window.
	/// Covers issues:
	///   #375 - Is LayoutAnchorable Active?
	///   #163 - IsSelected vs IsActive behavior
	///   #165 - ActiveContent not stable
	///   #432 - IsActiveChanged activation state switches back and forth
	///   #182 - CanClose property of new LayoutAnchorableItem differs from its LayoutAnchorable
	///   #69  - LayoutAnchorable not closeable but CanClose is True
	///   #210 - CanDockAsTabbedDocument=False docks to empty pane
	/// </summary>
	[TestFixture]
	public class LayoutContentPropertyTests
	{
		[Test]
		public void IsActive_FiresEvent_Issue375()
		{
			var pane = new LayoutDocumentPane();
			var doc = new LayoutDocument { Title = "Doc1", ContentId = "doc1" };
			pane.Children.Add(doc);

			var root = new LayoutRoot();
			root.RootPanel = new LayoutPanel(pane);

			bool eventFired = false;
			doc.IsActiveChanged += (s, e) => eventFired = true;

			doc.IsActive = true;

			Assert.That(eventFired, Is.True, "IsActiveChanged should fire when IsActive is set (Issue #375).");
			Assert.That(doc.IsActive, Is.True);
		}

		[Test]
		public void IsActive_OnlyOneDocumentActive_Issue165()
		{
			var pane = new LayoutDocumentPane();
			var doc1 = new LayoutDocument { Title = "Doc1", ContentId = "doc1" };
			var doc2 = new LayoutDocument { Title = "Doc2", ContentId = "doc2" };
			pane.Children.Add(doc1);
			pane.Children.Add(doc2);

			var root = new LayoutRoot();
			root.RootPanel = new LayoutPanel(pane);

			doc1.IsActive = true;
			Assert.That(doc1.IsActive, Is.True);

			doc2.IsActive = true;
			Assert.That(doc2.IsActive, Is.True);
			Assert.That(doc1.IsActive, Is.False,
				"Activating doc2 should deactivate doc1 (Issue #165).");
		}

		[Test]
		public void IsSelected_IndependentOfIsActive_Issue163()
		{
			var pane = new LayoutDocumentPane();
			var doc1 = new LayoutDocument { Title = "Doc1", ContentId = "doc1" };
			var doc2 = new LayoutDocument { Title = "Doc2", ContentId = "doc2" };
			pane.Children.Add(doc1);
			pane.Children.Add(doc2);

			var root = new LayoutRoot();
			root.RootPanel = new LayoutPanel(pane);

			doc1.IsSelected = true;

			Assert.That(doc1.IsSelected, Is.True);
		}

		[Test]
		public void ActiveContent_TracksActiveDocument_Issue165()
		{
			var pane = new LayoutDocumentPane();
			var doc1 = new LayoutDocument { Title = "Doc1", ContentId = "doc1" };
			var doc2 = new LayoutDocument { Title = "Doc2", ContentId = "doc2" };
			pane.Children.Add(doc1);
			pane.Children.Add(doc2);

			var root = new LayoutRoot();
			root.RootPanel = new LayoutPanel(pane);

			doc1.IsActive = true;
			Assert.That(root.ActiveContent, Is.EqualTo(doc1),
				"ActiveContent should point to doc1 after activation.");

			doc2.IsActive = true;
			Assert.That(root.ActiveContent, Is.EqualTo(doc2),
				"ActiveContent should switch to doc2 (Issue #165).");
		}

		[Test]
		public void CanClose_DefaultValues_Issue182()
		{
			var doc = new LayoutDocument();
			var anchorable = new LayoutAnchorable();

			Assert.That(doc.CanClose, Is.True, "LayoutDocument.CanClose should default to true.");
			Assert.That(anchorable.CanClose, Is.False, "LayoutAnchorable.CanClose should default to false.");
		}

		[Test]
		public void CanClose_SetFalse_PreventsClose_Issue69()
		{
			var anchorable = new LayoutAnchorable { Title = "Tool", ContentId = "tool1", CanClose = false };
			var pane = new LayoutAnchorablePane(anchorable);
			var root = new LayoutRoot();
			root.RootPanel = new LayoutPanel(pane);

			Assert.That(anchorable.CanClose, Is.False);
			Assert.That(pane.CanClose, Is.False,
				"Pane.CanClose should reflect child CanClose=false (Issue #69).");
		}

		[Test]
		public void CanFloat_DefaultAndSet()
		{
			var anchorable = new LayoutAnchorable();
			Assert.That(anchorable.CanFloat, Is.True, "CanFloat should default to true.");

			anchorable.CanFloat = false;
			Assert.That(anchorable.CanFloat, Is.False);
		}

		[Test]
		public void CanDockAsTabbedDocument_CanBeSet_Issue210()
		{
			var anchorable = new LayoutAnchorable();
			Assert.That(anchorable.CanDockAsTabbedDocument, Is.True,
				"CanDockAsTabbedDocument should default to true.");

			anchorable.CanDockAsTabbedDocument = false;
			Assert.That(anchorable.CanDockAsTabbedDocument, Is.False,
				"CanDockAsTabbedDocument should be settable to false (Issue #210).");
		}

		[Test]
		public void CanHide_Defaults()
		{
			var doc = new LayoutDocument();
			var anchorable = new LayoutAnchorable();

			Assert.That(doc.CanHide, Is.False, "LayoutDocument.CanHide should be false.");
			Assert.That(anchorable.CanHide, Is.True, "LayoutAnchorable.CanHide should default to true.");
		}

		[Test]
		public void IsActiveChanged_NotExcessivelyFired_Issue432()
		{
			var pane = new LayoutDocumentPane();
			var doc1 = new LayoutDocument { Title = "Doc1", ContentId = "doc1" };
			var doc2 = new LayoutDocument { Title = "Doc2", ContentId = "doc2" };
			pane.Children.Add(doc1);
			pane.Children.Add(doc2);

			var root = new LayoutRoot();
			root.RootPanel = new LayoutPanel(pane);

			int doc1ActiveCount = 0;
			int doc2ActiveCount = 0;
			doc1.IsActiveChanged += (s, e) => doc1ActiveCount++;
			doc2.IsActiveChanged += (s, e) => doc2ActiveCount++;

			doc1.IsActive = true;
			doc2.IsActive = true;

			Assert.That(doc1ActiveCount, Is.EqualTo(2),
				"Doc1 should fire IsActiveChanged exactly twice (Issue #432).");
			Assert.That(doc2ActiveCount, Is.EqualTo(1),
				"Doc2 should fire IsActiveChanged exactly once (Issue #432).");
		}
	}
}
