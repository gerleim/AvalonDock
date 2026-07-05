/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AvalonDock.Controls
{
	/// <summary>
	/// Provides a panel that contains the TabItem Headers of the <see cref="LayoutDocumentPaneControl"/>.
	/// Supports multi-row expansion via mouse wheel: scroll down to show all rows, scroll up to collapse.
	/// </summary>
	public class DocumentPaneTabPanel : Panel
	{
		private bool _isExpanded;
		private double _rowHeight;
		private bool _hasOverflow;

		public DocumentPaneTabPanel()
		{
			FlowDirection = FlowDirection.LeftToRight;
			Background = Brushes.Transparent;
		}

		#region Overrides

		protected override Size MeasureOverride(Size availableSize)
		{
			_rowHeight = 0;
			double totalWidth = 0;

			foreach (FrameworkElement child in Children)
			{
				child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
				totalWidth += child.DesiredSize.Width;
				_rowHeight = Math.Max(_rowHeight, child.DesiredSize.Height);
			}

			if (_rowHeight == 0)
				return new Size(0, 0);

			double panelWidth = double.IsInfinity(availableSize.Width) ? totalWidth : availableSize.Width;
			_hasOverflow = totalWidth > panelWidth;

			if (!_hasOverflow)
				_isExpanded = false;

			if (_isExpanded && _hasOverflow && panelWidth > 0)
			{
				int rowCount = 1;
				double rowWidth = 0;
				foreach (FrameworkElement child in Children)
				{
					if (child.Visibility == Visibility.Collapsed) continue;
					double w = child.DesiredSize.Width;
					if (rowWidth + w > panelWidth && rowWidth > 0)
					{
						rowCount++;
						rowWidth = 0;
					}
					rowWidth += w;
				}
				return new Size(panelWidth, _rowHeight * rowCount);
			}

			return new Size(Math.Min(totalWidth, panelWidth), _rowHeight);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var visibleChildren = Children.Cast<UIElement>()
				.Where(ch => ch.Visibility != Visibility.Collapsed)
				.Cast<TabItem>().ToList();

			if (visibleChildren.Count == 0)
				return finalSize;

			if (_isExpanded && _hasOverflow && finalSize.Width > 0)
				ArrangeMultiRow(visibleChildren, finalSize);
			else
				ArrangeSingleRow(visibleChildren, finalSize);

			return finalSize;
		}

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			if (e.Delta < 0 && !_isExpanded && _hasOverflow)
			{
				_isExpanded = true;
				InvalidateMeasure();
				e.Handled = true;
			}
			else if (e.Delta > 0 && _isExpanded)
			{
				_isExpanded = false;
				InvalidateMeasure();
				e.Handled = true;
			}

			if (!e.Handled)
				base.OnMouseWheel(e);
		}

		protected override void OnMouseLeave(MouseEventArgs e)
		{
			base.OnMouseLeave(e);
		}

		#endregion Overrides

		private void ArrangeMultiRow(List<TabItem> tabs, Size finalSize)
		{
			double x = 0;
			double y = 0;

			foreach (var tab in tabs)
			{
				double w = tab.DesiredSize.Width;
				if (x + w > finalSize.Width && x > 0)
				{
					x = 0;
					y += _rowHeight;
				}
				tab.Visibility = Visibility.Visible;
				tab.Arrange(new Rect(x, y, w, _rowHeight));
				x += tab.ActualWidth + tab.Margin.Left + tab.Margin.Right;
			}
		}

		private void ArrangeSingleRow(List<TabItem> tabs, Size finalSize)
		{
			double offset = 0;
			bool skipAllOthers = false;

			foreach (var doc in tabs)
			{
				if (skipAllOthers || offset + doc.DesiredSize.Width > finalSize.Width)
				{
					doc.Visibility = Visibility.Hidden;
					skipAllOthers = true;
				}
				else
				{
					doc.Visibility = Visibility.Visible;
					doc.Arrange(new Rect(offset, 0.0, doc.DesiredSize.Width, finalSize.Height));
					offset += doc.ActualWidth + doc.Margin.Left + doc.Margin.Right;
				}
			}
		}
	}
}
