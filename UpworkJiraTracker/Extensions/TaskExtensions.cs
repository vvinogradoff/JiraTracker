using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpworkJiraTracker.Extensions
{
	public static class TaskExtensions
	{
		/// <summary>
		/// The construct is a functional equivalent of <code>_ = Task.Run</code> with a try-catch yet allows
		/// for easier usage tracking. Generally speaking any fire and forget activity in aspnet is a bad
		/// practice unless offloaded to a background service. This construct makes it explicit that the 
		/// approach takes place.
		/// </summary>
		/// <param name="task"></param>
		/// <param name="handler"></param>
#pragma warning disable RECS0165 // Asynchronous methods should return a Task instead of void
		public static async void ForgetOnFirstAwait(this Task task, Action<Exception>? handler = null)
#pragma warning restore RECS0165 // Asynchronous methods should return a Task instead of void
		{
			try
			{
				await task;
			}
			catch (Exception ex)
			{
				if (handler == null)
					System.Diagnostics.Debug.WriteLine($"Async task had failed and no exception handler is assigned: " + 
						$"[{ex.GetType().FullName}] {ex.Message}{Environment.NewLine}\t{ex.StackTrace}");
				handler?.Invoke(ex);
			}
		}

		/// <summary>
		/// The construct is a shortcut for <code>_ = Task.Run</code> with a try-catch yet allows
		/// for easier usage tracking. Generally speaking any fire and forget activity in aspnet is a bad
		/// practice unless offloaded to a background service. This construct makes it explicit that the 
		/// approach takes place.
		/// </summary>
		/// <param name="action"></param>
		/// <param name="handler"></param>
		public static void FireNewThread(this Func<Task> action, Action<Exception>? handler = null)
		{
			Task.Run(async () =>
			{
				try
				{
					await action();
				}
				catch (Exception ex)
				{
					if (handler == null)
						System.Diagnostics.Debug.WriteLine($"Async task had failed and no exception handler is assigned: " +
							$"[{ex.GetType().FullName}] {ex.Message}{Environment.NewLine}\t{ex.StackTrace}");
					handler?.Invoke(ex);
				}
			});
		}

		public static T SafeWait<T>(this Task<T> task)
			=> task.ConfigureAwait(false).GetAwaiter().GetResult();
		public static void SafeWait(this Task task)
			=> task.ConfigureAwait(false).GetAwaiter().GetResult();

	}
}
