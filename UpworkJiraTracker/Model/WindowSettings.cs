using System;

namespace UpworkJiraTracker.Model
{
	public class WindowSettings
	{
		public double WindowLeft { get; set; }
		public double WindowTop { get; set; }
		public double WindowWidth { get; set; }
		public double WindowHeight { get; set; }
		public string? CustomBackgroundColor { get; set; }
		public List<TimezoneEntry> Timezones { get; set; } = new();
		public string LogDirectory { get; set; } = ".";
	}
}
