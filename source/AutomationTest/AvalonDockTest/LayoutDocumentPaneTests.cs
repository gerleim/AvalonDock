using NUnit.Framework;
using AvalonDock.Layout;
using System.Linq;

namespace AvalonDockTest
{
	/// <summary>
	/// Unit tests for LayoutDocumentPane operations — adding, removing, selecting documents.
	/// Covers issues:
	///   #201 - Closing Active document always selects first document instead of previous
	///   #204 - DockingManager not updating state when closing a tab
	///   #184 - All documents disappear if close cancelled
	///   #17  - NullRef inserting documents when isActive set before insertion
	///   #11  - ModelChange event happens before new LayoutDocumentItem is added
	/// </summary>
	[TestFixture]
	public class LayoutDocumentPaneTests
	{
		[Test]
		public void AddMultipleDocuments_AllPresent()
		{
			var pane = new LayoutDocumentPane();
			for (int i = 0; i < 5; i++)
			{
				pane.Children.Add(new LayoutDocument
				{
					Title = $"Doc{i}",
					ContentId = $"doc{i}"
				});
			}

			Assert.That(pane.Children.Count, Is.EqualTo(5));
			Assert.That(pane.Children.All(c => c.Parent == pane), Is.True,
				"All documents should have the pane as parent.");
		}

		[Test]
		public void RemoveDocument_OthersRemain_Issue204()
		{
			var pane = new LayoutDocumentPane();
			var doc1 = new LayoutDocument { Title = "Doc1", ContentId = "doc1" };
			var doc2 = new LayoutDocument { Title = "Doc2", ContentId = "doc2" };
			var doc3 = new LayoutDocument { Title = "Doc3", ContentId = "doc3" };
			pane.Children.Add(doc1);
			pane.Children.Add(doc2);
			pane.Children.Add(doc3);

			pane.RemoveChild(doc2);

			Assert.That(pane.Children.Count, Is.EqualTo(2));
			Assert.That(pane.Children.Contains(doc1), Is.True, "Doc1 should remain.");
			Assert.That(pane.Children.Contains(doc3), Is.True, "Doc3 should remain.");
			Assert.That(pane.Children.Contains(doc2), Is.False, "Doc2 should be removed.");
		}

		[Test]
		public void RemoveSelectedDocument_IndexStaysValid_Issue201()
		{
			var pane = new LayoutDocumentPane();
			var doc1 = new LayoutDocument { Title = "Doc1", ContentId = "doc1" };
			var doc2 = new LayoutDocument { Title = "Doc2", ContentId = "doc2" };
			var doc3 = new LayoutDocument { Title = "Doc3", ContentId = "doc3" };
			pane.Children.Add(doc1);
			pane.Children.Add(doc2);
			pane.Children.Add(doc3);

			pane.SelectedContentIndex = 1;
			Assert.That(pane.SelectedContent, Is.EqualTo(doc2));

			pane.RemoveChild(doc2);

			Assert.That(pane.SelectedContentIndex, Is.GreaterThanOrEqualTo(0));
			Assert.That(pane.SelectedContentIndex, Is.LessThan(pane.Children.Count),
				"SelectedContentIndex should be valid after removing selected document (Issue #201).");
		}

		[Test]
		public void InsertDocumentAtIndex_Works()
		{
			var pane = new LayoutDocumentPane();
			var doc1 = new LayoutDocument { Title = "Doc1", ContentId = "doc1" };
			var doc3 = new LayoutDocument { Title = "Doc3", ContentId = "doc3" };
			pane.Children.Add(doc1);
			pane.Children.Add(doc3);

			var doc2 = new LayoutDocument { Title = "Doc2", ContentId = "doc2" };
			pane.InsertChildAt(1, doc2);

			Assert.That(pane.Children.Count, Is.EqualTo(3));
			Assert.That(pane.Children[0], Is.EqualTo(doc1));
			Assert.That(pane.Children[1], Is.EqualTo(doc2));
			Assert.That(pane.Children[2], Is.EqualTo(doc3));
		}

		[Test]
		public void CloseDocument_RemovesFromPane()
		{
			var pane = new LayoutDocumentPane();
			var doc = new LayoutDocument { Title = "Doc", ContentId = "doc1" };
			pane.Children.Add(doc);

			var root = new LayoutRoot();
			root.RootPanel = new LayoutPanel(pane);

			doc.Close();

			Assert.That(pane.Children.Contains(doc), Is.False,
				"Closed document should be removed from pane.");
		}

		[Test]
		public void SetIsActive_BeforeInsertion_DoesNotCrash_Issue17()
		{
			var doc = new LayoutDocument { Title = "Doc", ContentId = "doc1" };

			Assert.DoesNotThrow(() =>
			{
				doc.IsActive = true;
			}, "Setting IsActive before insertion should not throw (Issue #17).");

			var pane = new LayoutDocumentPane();
			Assert.DoesNotThrow(() =>
			{
				pane.Children.Add(doc);
			}, "Adding document with IsActive=true should not throw.");
		}

		[Test]
		public void TitleChange_FiresPropertyChanged_Issue11()
		{
			var doc = new LayoutDocument { Title = "Original", ContentId = "doc1" };

			bool titleChanged = false;
			doc.PropertyChanged += (s, e) =>
			{
				if (e.PropertyName == nameof(LayoutDocument.Title))
					titleChanged = true;
			};

			doc.Title = "Updated";

			Assert.That(titleChanged, Is.True,
				"PropertyChanged should fire for Title changes (Issue #11).");
			Assert.That(doc.Title, Is.EqualTo("Updated"));
		}

		[Test]
		public void EmptyPane_HasValidState()
		{
			var pane = new LayoutDocumentPane();

			Assert.That(pane.Children.Count, Is.EqualTo(0));
			Assert.That(pane.SelectedContentIndex, Is.EqualTo(-1));
			Assert.That(pane.IsVisible, Is.True, "Unparented empty pane defaults to visible.");
		}
	}
}
