/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using AvalonDock.Layout;
using AvalonDock.Themes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace AvalonDock.Controls
{
	/// <inheritdoc />
	/// <summary>
	/// Implements a floating window for navigating between documents and toolwindows in AvalonDock.
	/// The floating navigator window can be invoked with CTRL+TAB.
	/// </summary>
	/// <seealso cref="Window"/>
	[TemplatePart(Name = PART_AnchorableListBox, Type = typeof(ListBox))]
	[TemplatePart(Name = PART_DocumentListBox, Type = typeof(ListBox))]
	public class NavigatorWindow : Window
	{
		#region fields
		private ResourceDictionary currentThemeResourceDictionary; // = null

		private const string PART_AnchorableListBox = "PART_AnchorableListBox";
		private const string PART_DocumentListBox = "PART_DocumentListBox";

		private DockingManager _manager;
		private bool _isSelectingDocument;
		private bool _hasMultipleWindows;
		private ListBox _anchorableListBox;
		private ListBox _documentListBox;
		private bool _internalSetSelectedDocument = false;
		private bool _internalSetSelectedAnchorable = false;

		#endregion fields

		#region Constructors

		static NavigatorWindow()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigatorWindow), new FrameworkPropertyMetadata(typeof(NavigatorWindow)));
			ShowActivatedProperty.OverrideMetadata(typeof(NavigatorWindow), new FrameworkPropertyMetadata(false));
			ShowInTaskbarProperty.OverrideMetadata(typeof(NavigatorWindow), new FrameworkPropertyMetadata(false));
			WindowStyleProperty.OverrideMetadata(typeof(NavigatorWindow), new FrameworkPropertyMetadata(WindowStyle.None));
		}

		internal NavigatorWindow(DockingManager manager)
		{
			_manager = manager;
			_internalSetSelectedDocument = true;
			SetAnchorables(_manager.Layout.Descendents()
				.OfType<LayoutAnchorable>()
				.Where(a => a.IsVisible)
				.OrderByDescending(d => d.LastActivationTimeStamp.GetValueOrDefault())
				.Select(d => (LayoutAnchorableItem)_manager.GetLayoutItemFromModel(d))
				.ToArray());
			SetValue(HasAnchorablesPropertyKey, Anchorables?.Any() == true);

			var allDocs = _manager.Layout.Descendents()
				.OfType<LayoutDocument>()
				.OrderByDescending(d => d.LastActivationTimeStamp.GetValueOrDefault())
				.ToArray();

			var secondMru = allDocs.Length > 1 ? allDocs[1] : allDocs.FirstOrDefault();

			_hasMultipleWindows = allDocs.Any(d => d.IsFloating) && allDocs.Any(d => !d.IsFloating);
			if (_hasMultipleWindows)
			{
				allDocs = allDocs
					.OrderBy(d => d.IsFloating ? 1 : 0)
					.ThenBy(d => d.FindParent<LayoutDocumentFloatingWindow>()?.GetHashCode() ?? 0)
					.ThenByDescending(d => d.LastActivationTimeStamp.GetValueOrDefault())
					.ToArray();
			}

			SetDocuments(allDocs
				.Select(d => (LayoutDocumentItem)_manager.GetLayoutItemFromModel(d))
				.ToArray());
			_internalSetSelectedDocument = false;

			if (Documents.Length > 1)
			{
				var preselect = secondMru != null
					? Documents.FirstOrDefault(d => d.LayoutElement == secondMru)
					: null;
				InternalSetSelectedDocument(preselect ?? Documents[1]);
				_isSelectingDocument = true;
			}
			else if (Documents.Length == 1)
			{
				InternalSetSelectedDocument(Documents[0]);
				_isSelectingDocument = true;
			}
			else
			{
				var anchorable = Anchorables.FirstOrDefault();
				if (anchorable != null)
				{
					InternalSetSelectedAnchorable(anchorable);
					_isSelectingDocument = false;
				}
			}

			DataContext = this;
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
			UpdateThemeResources();
		}

		#endregion Constructors

		#region Properties

		#region HasAnchorables

		/// <summary><see cref="HasAnchorables"/> read-only dependency property.</summary>
		private static readonly DependencyPropertyKey HasAnchorablesPropertyKey =
			DependencyProperty.RegisterReadOnly(nameof(HasAnchorables), typeof(bool), typeof(NavigatorWindow),
				new FrameworkPropertyMetadata(false));

		public static readonly DependencyProperty HasAnchorablesProperty = HasAnchorablesPropertyKey.DependencyProperty;

		/// <summary>Gets whether there are any visible anchorables.</summary>
		public bool HasAnchorables => (bool)GetValue(HasAnchorablesProperty);

		#endregion HasAnchorables

		#region Documents

		/// <summary><see cref="Documents"/> read-only dependency property.</summary>
		private static readonly DependencyPropertyKey DocumentsPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Documents), typeof(IEnumerable<LayoutDocumentItem>), typeof(NavigatorWindow),
				new FrameworkPropertyMetadata(null));

		public static readonly DependencyProperty DocumentsProperty = DocumentsPropertyKey.DependencyProperty;

		/// <summary>Gets the list of documents managed in this framework.</summary>
		[Bindable(true), Description("Gets the list of documents managed in this framework."), Category("Document")]
		public LayoutDocumentItem[] Documents => (LayoutDocumentItem[])GetValue(DocumentsProperty);

		#endregion Documents

		#region Anchorables

		/// <summary><see cref="Anchorables"/> read-only dependency property.</summary>
		private static readonly DependencyPropertyKey AnchorablesPropertyKey = DependencyProperty.RegisterReadOnly(nameof(Anchorables), typeof(IEnumerable<LayoutAnchorableItem>), typeof(NavigatorWindow),
				new FrameworkPropertyMetadata((IEnumerable<LayoutAnchorableItem>)null));

		public static readonly DependencyProperty AnchorablesProperty = AnchorablesPropertyKey.DependencyProperty;

		/// <summary>Gets the list of anchorables managed in the framework.</summary>
		[Bindable(true), Description("Gets the list of anchorables managed in the framework."), Category("Anchorable")]
		public IEnumerable<LayoutAnchorableItem> Anchorables => (IEnumerable<LayoutAnchorableItem>)GetValue(AnchorablesProperty);

		#endregion Anchorables

		#region AnchorablesLabel

		/// <summary><see cref="AnchorablesLabel"/> dependency property.</summary>
		public static readonly DependencyProperty AnchorablesLabelProperty = DependencyProperty.Register(nameof(AnchorablesLabel), typeof(string), typeof(NavigatorWindow),
				new FrameworkPropertyMetadata(Properties.Resources.Active_ToolWindows));

		/// <summary>Gets/sets the label displayed above the anchorables list in the navigator.</summary>
		[Bindable(true), Description("Gets/sets the label displayed above the anchorables list in the navigator."), Category("Navigator")]
		public string AnchorablesLabel
		{
			get => (string)GetValue(AnchorablesLabelProperty);
			set => SetValue(AnchorablesLabelProperty, value);
		}

		#endregion AnchorablesLabel

		#region DocumentsLabel

		/// <summary><see cref="DocumentsLabel"/> dependency property.</summary>
		public static readonly DependencyProperty DocumentsLabelProperty = DependencyProperty.Register(nameof(DocumentsLabel), typeof(string), typeof(NavigatorWindow),
				new FrameworkPropertyMetadata(Properties.Resources.Active_Files));

		/// <summary>Gets/sets the label displayed above the documents list in the navigator.</summary>
		[Bindable(true), Description("Gets/sets the label displayed above the documents list in the navigator."), Category("Navigator")]
		public string DocumentsLabel
		{
			get => (string)GetValue(DocumentsLabelProperty);
			set => SetValue(DocumentsLabelProperty, value);
		}

		#endregion DocumentsLabel

		#region SelectedDocument

		/// <summary><see cref="SelectedDocument"/> dependency property.</summary>
		public static readonly DependencyProperty SelectedDocumentProperty = DependencyProperty.Register(nameof(SelectedDocument), typeof(LayoutDocumentItem), typeof(NavigatorWindow),
				new FrameworkPropertyMetadata(null, OnSelectedDocumentChanged));

		/// <summary>Gets/sets the currently selected document.</summary>
		[Bindable(true), Description("Gets/sets the currently selected document."), Category("Document")]
		public LayoutDocumentItem SelectedDocument
		{
			get => (LayoutDocumentItem)GetValue(SelectedDocumentProperty);
			set => SetValue(SelectedDocumentProperty, value);
		}

		/// <summary>Handles changes to the <see cref="SelectedDocument"/> property.</summary>
		private static void OnSelectedDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((NavigatorWindow)d).OnSelectedDocumentChanged(e);

		/// <summary>Provides derived classes an opportunity to handle changes to the <see cref="SelectedDocument"/> property.</summary>
		protected virtual void OnSelectedDocumentChanged(DependencyPropertyChangedEventArgs e)
		{
			if (_internalSetSelectedDocument || SelectedDocument == null)
			{
				return;
			}

			if (!SelectedDocument.ActivateCommand.CanExecute(null))
			{
				return;
			}

			Close();
		}

		#endregion SelectedDocument

		#region SelectedAnchorable

		/// <summary><see cref="SelectedAnchorable"/> dependency property.</summary>
		public static readonly DependencyProperty SelectedAnchorableProperty = DependencyProperty.Register(nameof(SelectedAnchorable), typeof(LayoutAnchorableItem), typeof(NavigatorWindow),
				new FrameworkPropertyMetadata(null, OnSelectedAnchorableChanged));

		/// <summary>Gets/sets the currently selected anchorable.</summary>
		[Bindable(true), Description("Gets/sets the currently selected anchorable."), Category("Anchorable")]
		public LayoutAnchorableItem SelectedAnchorable
		{
			get => (LayoutAnchorableItem)GetValue(SelectedAnchorableProperty);
			set => SetValue(SelectedAnchorableProperty, value);
		}

		/// <summary>Handles changes to the <see cref="SelectedAnchorable"/> property.</summary>
		private static void OnSelectedAnchorableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((NavigatorWindow)d).OnSelectedAnchorableChanged(e);

		/// <summary>Provides derived classes an opportunity to handle changes to the <see cref="SelectedAnchorable"/> property.</summary>
		protected virtual void OnSelectedAnchorableChanged(DependencyPropertyChangedEventArgs e)
		{
			if (_internalSetSelectedAnchorable) return;
			if (SelectedAnchorable != null && SelectedAnchorable.ActivateCommand.CanExecute(null))
			{
				Close();
			}
		}

		#endregion SelectedAnchorable

		#endregion Properties

		#region Overrides

		/// <inheritdoc />
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			_anchorableListBox = GetTemplateChild(PART_AnchorableListBox) as ListBox;
			_documentListBox = GetTemplateChild(PART_DocumentListBox) as ListBox;

			if (_anchorableListBox != null)
			{
				_anchorableListBox.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
			}

			if (_documentListBox != null)
			{
				_documentListBox.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
				if (_hasMultipleWindows)
					_documentListBox.Items.GroupDescriptions.Add(new PropertyGroupDescription(null, new DocumentWindowGroupConverter()));
			}
		}

		private void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
		{
			bool isListOfDocuments = sender == _documentListBox.ItemContainerGenerator;
			var itemsCollection = isListOfDocuments ? (IEnumerable)Documents : Anchorables.ToArray();
			ItemContainerGenerator generator = (ItemContainerGenerator)sender;
			switch (generator.Status)
			{
				case GeneratorStatus.ContainersGenerated:
					foreach (object item in itemsCollection)
					{
						ListBoxItem container = (ListBoxItem)generator.ContainerFromItem(item);
						if (container != null)
						{
							container.PreviewMouseLeftButtonDown += Container_PreviewMouseLeftButtonDown;
							if (isListOfDocuments)
							{
								container.IsKeyboardFocusedChanged += DocumentsItemContainer_IsKeyboardFocusedChanged;
							}
							else
							{
								container.IsKeyboardFocusedChanged += AnchorablesItemContainer_IsKeyboardFocusedChanged;
							}
						}
					}
					break;
			}
		}

		private void Container_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			var item = (ListBoxItem)sender;
			if (item.DataContext is LayoutDocumentItem document)
			{
				_internalSetSelectedDocument = true;
				SelectedDocument = document;
				_internalSetSelectedDocument = false;
				_isSelectingDocument = true;
			}
			else if (item.DataContext is LayoutAnchorableItem anchorable)
			{
				_internalSetSelectedAnchorable = true;
				SelectedAnchorable = anchorable;
				_internalSetSelectedAnchorable = false;
				_isSelectingDocument = false;
			}
			Close();
		}

		private void AnchorablesItemContainer_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			ListBoxItem item = (ListBoxItem)sender;
			if (item.IsKeyboardFocused)
			{
				_internalSetSelectedAnchorable = true;
				item.IsSelected = true;
				_internalSetSelectedAnchorable = false;
			}
		}

		private void DocumentsItemContainer_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			ListBoxItem item = (ListBoxItem)sender;
			if (item.IsKeyboardFocused)
			{
				_internalSetSelectedDocument = true;
				item.IsSelected = true;
				_internalSetSelectedDocument = false;
			}
		}

		internal void HandleKeyDown(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					Close();
					e.Handled = true;
					break;
				case Key.Escape:
					InternalSetSelectedDocument(null);
					InternalSetSelectedAnchorable(null);
					Close();
					e.Handled = true;
					break;
				case Key.Tab:
					SetNextLayoutContent(!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift));
					e.Handled = true;
					break;
				case Key.Left:
				case Key.Right:
					if (_isSelectingDocument)
					{
						var anchorable = Anchorables.ElementAtOrDefault(Documents.IndexOf(SelectedDocument))
							?? Anchorables.LastOrDefault();
						if (anchorable != null)
						{
							_isSelectingDocument = false;
							InternalSetSelectedDocument(null);
							InternalSetSelectedAnchorable(anchorable);
						}
					}
					else
					{
						int index = _anchorableListBox?.SelectedIndex
							?? Anchorables.ToArray().IndexOf(SelectedAnchorable);
						var document = Documents.ElementAtOrDefault(index)
							?? Documents.LastOrDefault();
						if (document != null)
						{
							_isSelectingDocument = true;
							InternalSetSelectedAnchorable(null);
							InternalSetSelectedDocument(document);
						}
					}
					e.Handled = true;
					break;
				case Key.Up:
					SetNextLayoutContent(false);
					e.Handled = true;
					break;
				case Key.Down:
					SetNextLayoutContent(true);
					e.Handled = true;
					break;
			}

			void SetNextLayoutContent(bool next)
			{
				if (_isSelectingDocument)
				{
					if (SelectedDocument != null)
					{
						if (next)
							SelectNextDocument();
						else
							SelectPreviousDocument();
					}
					else if (Documents.Length > 0)
					{
						InternalSetSelectedDocument(Documents[0]);
					}
				}
				else
				{
					if (SelectedAnchorable != null)
					{
						if (next)
							SelectNextAnchorable();
						else
							SelectPreviousAnchorable();
					}
					else
					{
						var anchorableItem = Anchorables.FirstOrDefault();
						if (anchorableItem != null)
							InternalSetSelectedAnchorable(anchorableItem);
					}
				}
			}
		}

		internal void HandleKeyUp(KeyEventArgs e)
		{
			if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
			{
				Close();
				e.Handled = true;
			}
		}

		#endregion Overrides

		#region Internal Methods

		/// <summary>
		/// Provides a secure method for setting the Anchorables property.
		/// This dependency property indicates the list of anchorables.
		/// </summary>
		/// <param name="value">The new value for the property.</param>
		protected void SetAnchorables(IEnumerable<LayoutAnchorableItem> value) => SetValue(AnchorablesPropertyKey, value);

		/// <summary>
		/// Provides a secure method for setting the Documents property.
		/// This dependency property indicates the list of documents.
		/// </summary>
		/// <param name="value">The new value for the property.</param>
		protected void SetDocuments(LayoutDocumentItem[] value) => SetValue(DocumentsPropertyKey, value);

		/// <summary>Is Invoked when AvalonDock's WPF Theme changes via the <see cref="DockingManager.OnThemeChanged()"/> method.</summary>
		/// <param name="oldTheme"></param>
		internal void UpdateThemeResources(Theme oldTheme = null)
		{
			if (oldTheme != null) // Remove the old theme if present
			{
				if (oldTheme is DictionaryTheme)
				{
					if (currentThemeResourceDictionary != null)
					{
						Resources.MergedDictionaries.Remove(currentThemeResourceDictionary);
						currentThemeResourceDictionary = null;
					}
				}
				else
				{
					var resourceDictionaryToRemove = Resources.MergedDictionaries.FirstOrDefault(r => r.Source == oldTheme.GetResourceUri());
					if (resourceDictionaryToRemove != null) Resources.MergedDictionaries.Remove(resourceDictionaryToRemove);
				}
			}

			// Implicit parameter to this method is the new theme already set here
			if (_manager.Theme == null) return;
			if (_manager.Theme is DictionaryTheme dictionaryTheme)
			{
				currentThemeResourceDictionary = dictionaryTheme.ThemeResourceDictionary;
				Resources.MergedDictionaries.Add(currentThemeResourceDictionary);
			}
			else
				Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = _manager.Theme.GetResourceUri() });
		}

		internal void SelectNextDocument()
		{
			if (SelectedDocument == null) return;
			var docIndex = Documents.IndexOf(SelectedDocument);
			docIndex++;
			if (docIndex == Documents.Length) docIndex = 0;
			InternalSetSelectedDocument(Documents[docIndex]);
		}

		internal void SelectNextAnchorable()
		{
			if (SelectedAnchorable == null) return;
			var anchorablesArray = Anchorables.ToArray();
			var anchorableIndex = anchorablesArray.IndexOf(SelectedAnchorable);
			anchorableIndex++;
			if (anchorableIndex == anchorablesArray.Length) anchorableIndex = 0;
			InternalSetSelectedAnchorable(anchorablesArray[anchorableIndex]);
		}

		internal void SelectPreviousDocument()
		{
			if (SelectedDocument == null) return;
			var docIndex = Documents.IndexOf(SelectedDocument);
			docIndex--;
			if (docIndex < 0) docIndex = Documents.Length - 1;
			InternalSetSelectedDocument(Documents[docIndex]);
		}

		internal void SelectPreviousAnchorable()
		{
			if (SelectedAnchorable == null) return;
			var anchorablesArray = Anchorables.ToArray();
			var anchorableIndex = anchorablesArray.IndexOf(SelectedAnchorable);
			anchorableIndex--;
			if (anchorableIndex < 0) anchorableIndex = anchorablesArray.Length - 1;
			InternalSetSelectedAnchorable(anchorablesArray[anchorableIndex]);
		}

		#endregion Internal Methods

		#region Private Methods

		private void InternalSetSelectedAnchorable(LayoutAnchorableItem anchorableToSelect)
		{
			_internalSetSelectedAnchorable = true;
			SelectedAnchorable = anchorableToSelect;
			_internalSetSelectedAnchorable = false;
			if (anchorableToSelect != null)
				_anchorableListBox?.ScrollIntoView(anchorableToSelect);
		}

		private void InternalSetSelectedDocument(LayoutDocumentItem documentToSelect)
		{
			_internalSetSelectedDocument = true;
			SelectedDocument = documentToSelect;
			_internalSetSelectedDocument = false;
			if (documentToSelect != null)
				_documentListBox?.ScrollIntoView(documentToSelect);
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			Loaded -= OnLoaded;
		}

		private void OnUnloaded(object sender, RoutedEventArgs e) => Unloaded -= OnUnloaded;

		#endregion Private Methods

		private class DocumentWindowGroupConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				if (value is LayoutDocumentItem item && item.LayoutElement is LayoutDocument doc && doc.IsFloating)
				{
					var fw = doc.FindParent<LayoutDocumentFloatingWindow>();
					return fw?.RootPanel?.Descendents().OfType<LayoutDocument>()
						.FirstOrDefault(d => d.IsSelected || d.IsActive)?.Title
						?? fw?.RootPanel?.Descendents().OfType<LayoutDocument>().FirstOrDefault()?.Title
						?? "Floating Window";
				}
				return "Main Window";
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
				=> throw new NotSupportedException();
		}
	}
}