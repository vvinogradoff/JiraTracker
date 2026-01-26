using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpworkJiraTracker.Model
{
	public class WindowInfo
	{
		public nint Handle { get; set; }
		public string Name { get; set; } = "";
		public string ClassName { get; set; } = "";
		public string ControlType { get; set; } = "";
		public string AutomationId { get; set; } = "";
		public string BoundingRectangle { get; set; } = "";
		public int ChildCount { get; set; }
		public int ProcessId { get; set; }
		public DateTime FirstDetected { get; set; }
	}
}
