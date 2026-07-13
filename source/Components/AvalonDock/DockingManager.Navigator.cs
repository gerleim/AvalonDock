/************************************************************************
   AvalonDock

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at https://opensource.org/licenses/MS-PL
 ************************************************************************/

using AvalonDock.Controls;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace AvalonDock
{
	public partial class DockingManager
	{
		private NavigatorWindow _navigatorWindow = null;

		#region ShowNavigator

		/// <summary><see cref="ShowNavigator"/> dependency property.</summary>
		public static readonly DependencyProperty ShowNavigatorProperty = DependencyProperty.Register(nameof(ShowNavigator), typeof(bool), typeof(DockingManager),
				new FrameworkPropertyMetadata(true));

		/// <summary>Gets/sets whether the navigator window should be shown when the user presses Control + Tab.</summary>
		[Bindable(true), Description("Gets/sets whether floating windows should show the system menu when a custom context menu is not defined."), Category("FloatingWindow")]
		public bool ShowNavigator
		{
			get => (bool)GetValue(ShowNavigatorProperty);
			set => SetValue(ShowNavigatorProperty, value);
		}

		#endregion ShowNavigator

		private bool IsNavigatorWindowActive => _navigatorWindow != null;

		internal bool IsNavigatorCloseInProgress { get; set; }

		/// <summary>
		/// When true, suppresses the next mouse-up activation in LayoutDocumentControl
		/// to allow programmatic content switching without the originating pane
		/// reclaiming activation. Auto-resets after one use.
		/// </summary>
		public bool SuppressMouseUpActivation { get; set; }

		private bool CanShowNavigatorWindow => ShowNavigator && _layoutItems.Any();

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			HandleNavigatorKeyDown(e);
			if (!e.Handled)
				base.OnPreviewKeyDown(e);
		}

		protected override void OnPreviewKeyUp(KeyEventArgs e)
		{
			HandleNavigatorKeyUp(e);
			if (!e.Handled)
				base.OnPreviewKeyUp(e);
		}

		internal void HandleNavigatorKeyDown(KeyEventArgs e)
		{
			if (IsNavigatorWindowActive)
			{
				_navigatorWindow.HandleKeyDown(e);
				if (e.Handled) return;
			}

			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
			{
				if (e.IsDown && e.Key == Key.Tab)
				{
					if (CanShowNavigatorWindow && !IsNavigatorWindowActive)
					{
						ShowNavigatorWindow();
						e.Handled = true;
					}
				}
			}
		}

		internal void HandleNavigatorKeyUp(KeyEventArgs e)
		{
			if (IsNavigatorWindowActive)
			{
				_navigatorWindow.HandleKeyUp(e);
			}
		}

		private void ShowNavigatorWindow()
		{
			if (_navigatorWindow == null)
				_navigatorWindow = new NavigatorWindow(this) { Owner = Window.GetWindow(this), WindowStartupLocation = WindowStartupLocation.CenterOwner };
			_navigatorWindow.Closed += OnNavigatorWindowClosed;
			PreviewMouseDown += OnNavigatorPreviewMouseDown;
			var parentWindow = Window.GetWindow(this);
			if (parentWindow != null)
				parentWindow.Deactivated += OnNavigatorParentDeactivated;
			_navigatorWindow.Show();
			var hwnd = new WindowInteropHelper(_navigatorWindow).Handle;
			if (hwnd != IntPtr.Zero)
			{
				const int GWL_EXSTYLE = -20;
				const int WS_EX_NOACTIVATE = 0x08000000;
				var exStyle = Win32Helper.GetWindowLongPtr(hwnd, GWL_EXSTYLE);
				Win32Helper.SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(exStyle.ToInt64() | WS_EX_NOACTIVATE));
			}
		}

		private void OnNavigatorWindowClosed(object sender, EventArgs e)
		{
			var nav = (NavigatorWindow)sender;
			nav.Closed -= OnNavigatorWindowClosed;
			PreviewMouseDown -= OnNavigatorPreviewMouseDown;
			var parentWindow = Window.GetWindow(this);
			if (parentWindow != null)
				parentWindow.Deactivated -= OnNavigatorParentDeactivated;
			var selectedDoc = nav.SelectedDocument;
			var selectedAnc = nav.SelectedAnchorable;
			_navigatorWindow = null;
			Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Input, () =>
			{
				if (selectedDoc != null && selectedDoc.ActivateCommand.CanExecute(null))
					selectedDoc.ActivateCommand.Execute(null);
				else if (selectedAnc != null && selectedAnc.ActivateCommand.CanExecute(null))
					selectedAnc.ActivateCommand.Execute(null);
				IsNavigatorCloseInProgress = false;
			});
		}

		private void OnNavigatorPreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (_navigatorWindow != null && !_navigatorWindow.IsMouseOver)
				_navigatorWindow.Close();
		}

		private void OnNavigatorParentDeactivated(object sender, EventArgs e)
		{
			if (_navigatorWindow != null && _navigatorWindow.IsMouseOver)
				return;
			_navigatorWindow?.Close();
		}
	}
}
