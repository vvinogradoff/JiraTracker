using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpworkJiraTracker.Model.EventArgs
{
	public class WindowEventArgs : System.EventArgs
	{
		public WindowInfo Window { get; }
		public bool IsOpen { get; }

		public WindowEventArgs(WindowInfo window, bool isOpen)
		{
			Window = window;
			IsOpen = isOpen;
		}
	}
}
