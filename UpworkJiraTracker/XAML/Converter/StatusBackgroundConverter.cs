using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace UpworkJiraTracker.XAML.Converter
{

	/// <summary>
	/// Converts a Jira status name to a background brush by looking up a resource.
	/// For status "In Progress", looks for "BackgroundPopup_In_Progress".
	/// Falls back to "BackgroundPopup_Default" if not found.
	/// </summary>
	public class StatusBackgroundConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string status && !string.IsNullOrEmpty(status))
			{
				// Convert status to resource key: replace spaces with underscores
				var resourceKey = $"BackgroundPopup_{status.Replace(" ", "_")}";

				// Try to find the resource
				var brush = System.Windows.Application.Current.TryFindResource(resourceKey);
				if (brush != null)
				{
					return brush;
				}
			}

			// Fall back to default
			return System.Windows.Application.Current.TryFindResource("BackgroundPopup_Default")
				   ?? System.Windows.Media.Brushes.Transparent;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
