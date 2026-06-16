using System.ComponentModel;
using AvalonDock.Layout;

namespace AvalonDock
{
	public class ContentDockingEventArgs : CancelEventArgs
	{
		public ContentDockingEventArgs(LayoutContent content)
		{
			Content = content;
		}

		public LayoutContent Content { get; }
	}

	public class ContentDockedEventArgs : System.EventArgs
	{
		public ContentDockedEventArgs(LayoutContent content)
		{
			Content = content;
		}

		public LayoutContent Content { get; }
	}
}
