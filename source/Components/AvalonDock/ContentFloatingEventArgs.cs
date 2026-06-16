using System.ComponentModel;
using AvalonDock.Layout;

namespace AvalonDock
{
	public class ContentFloatingEventArgs : CancelEventArgs
	{
		public ContentFloatingEventArgs(LayoutContent content)
		{
			Content = content;
		}

		public LayoutContent Content { get; }
	}

	public class ContentFloatedEventArgs : System.EventArgs
	{
		public ContentFloatedEventArgs(LayoutContent content)
		{
			Content = content;
		}

		public LayoutContent Content { get; }
	}
}
