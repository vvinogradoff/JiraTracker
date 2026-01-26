using System.Windows;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using FormsColorDialog = System.Windows.Forms.ColorDialog;
using FormsDialogResult = System.Windows.Forms.DialogResult;
using FormsFolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;
using DrawingColor = System.Drawing.Color;
using WpfApplication = System.Windows.Application;
using UpworkJiraTracker.ViewModel;
using UpworkJiraTracker.Service;
using UpworkJiraTracker.Model;
using UpworkJiraTracker.Helper;

namespace UpworkJiraTracker.View;

public partial class SettingsWindow : Window
{
    private readonly MainWindow _mainWindow;
    private readonly MainWindowViewModel _mainViewModel;
    private readonly SettingsViewModel _viewModel;
    private readonly UpworkIntegrationFlaUI _upworkIntegration;
    private readonly JiraOAuthService _jiraService;
    private bool _isShowingDialog = false;

    public SettingsWindow(MainWindow mainWindow, UpworkIntegrationFlaUI upworkIntegration, JiraOAuthService jiraService)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _mainViewModel = mainWindow.ViewModel;
        _upworkIntegration = upworkIntegration;
        _jiraService = jiraService;

        _jiraService.AuthenticationCompleted += JiraService_AuthenticationCompleted;
        _jiraService.AuthenticationFailed += JiraService_AuthenticationFailed;
        _jiraService.Disconnected += JiraService_Disconnected;

        _viewModel = new SettingsViewModel
        {
            CustomBackgroundColor = _mainViewModel.CustomBackgroundColor,
            MainWindowWidth = _mainWindow.Width,
            MainWindowHeight = _mainWindow.Height,
            LogDirectory = Properties.Settings.Default.LogDirectory ?? "."
        };

        // Populate ViewModel timezones from MainWindowViewModel
        foreach (var tz in _mainViewModel.Timezones)
        {
            _viewModel.Timezones.Add(tz);
        }

        DataContext = _viewModel;

        // Subscribe to ViewModel command events
        _viewModel.PickColorRequested += (s, e) => PickColor();
        _viewModel.ResetColorRequested += (s, e) => ResetColor();
        _viewModel.CloseRequested += (s, e) => ConfirmationOverlay.Visibility = Visibility.Visible;
        _viewModel.AddTimezoneRequested += (s, e) => AddTimezone();
        _viewModel.RemoveTimezoneRequested += (s, entry) => RemoveTimezone(entry);
        _viewModel.ConnectJiraRequested += (s, e) => ConnectJira();
        _viewModel.DisconnectJiraRequested += (s, e) => DisconnectJira();
        _viewModel.BrowseLogDirectoryRequested += (s, e) => BrowseLogDirectory();

        // Update Jira UI state
        UpdateJiraUIState();

        // Update Upwork UI state
        UpdateUpworkUIState();

        // Show current background color
        UpdateColorPreview();

        // Subscribe to ViewModel changes
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        ConfirmationOverlay.Confirmed += ConfirmationOverlay_Confirmed;
        ConfirmationOverlay.Cancelled += ConfirmationOverlay_Cancelled;

        JiraCredentialsOverlay.CredentialsSubmitted += JiraCredentialsOverlay_CredentialsSubmitted;
        JiraCredentialsOverlay.Cancelled += JiraCredentialsOverlay_Cancelled;

        // Position window smartly
        PositionWindow();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SettingsViewModel.CustomBackgroundColor):
                _mainViewModel.CustomBackgroundColor = _viewModel.CustomBackgroundColor;
                UpdateColorPreview();
                break;
            case nameof(SettingsViewModel.MainWindowWidth):
                _mainWindow.Width = _viewModel.MainWindowWidth;
                SaveSettings();
                break;
            case nameof(SettingsViewModel.MainWindowHeight):
                _mainWindow.Height = _viewModel.MainWindowHeight;
                SaveSettings();
                break;
            case nameof(SettingsViewModel.LogDirectory):
                SaveSettings();
                break;
        }
    }

    private void UpdateColorPreview()
    {
        if (_viewModel.CustomBackgroundColor.HasValue)
        {
            ColorPreview.Background = new SolidColorBrush(_viewModel.CustomBackgroundColor.Value);
        }
        else
        {
            ColorPreview.Background = new SolidColorBrush(ThemeHelper.GetTaskbarColor());
        }
    }

    private void PositionWindow()
    {
        var workArea = SystemParameters.WorkArea;
        var mainWindowLeft = _mainWindow.Left;
        var mainWindowTop = _mainWindow.Top;
        var mainWindowBottom = mainWindowTop + _mainWindow.ActualHeight;
        var mainWindowRight = mainWindowLeft + _mainWindow.ActualWidth;

        // Try to position above first
        if (mainWindowTop - Height >= workArea.Top)
        {
            // Fits above
            Top = mainWindowTop - Height;
        }
        else if (mainWindowBottom + Height <= workArea.Bottom)
        {
            // Fits below
            Top = mainWindowBottom;
        }
        else
        {
            // Doesn't fit either way, position below
            Top = mainWindowBottom;
        }

        // Try to align left edge with main window
        if (mainWindowLeft + Width <= workArea.Right)
        {
            // Fits aligned to left edge
            Left = mainWindowLeft;
        }
        else if (mainWindowRight - Width >= workArea.Left)
        {
            // Align to right edge of main window
            Left = mainWindowRight - Width;
        }
        else
        {
            // Center on screen
            Left = (workArea.Width - Width) / 2 + workArea.Left;
        }

        // Ensure window is fully on screen
        if (Left < workArea.Left) Left = workArea.Left;
        if (Left + Width > workArea.Right) Left = workArea.Right - Width;
        if (Top < workArea.Top) Top = workArea.Top;
        if (Top + Height > workArea.Bottom) Top = workArea.Bottom - Height;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ConfirmationOverlay.Visibility = Visibility.Visible;
    }

    private void ConfirmationOverlay_Confirmed(object? sender, EventArgs e)
    {
        WpfApplication.Current.Shutdown();
    }

    private void ConfirmationOverlay_Cancelled(object? sender, EventArgs e)
    {
        ConfirmationOverlay.Visibility = Visibility.Collapsed;
    }

    private void PickColor()
    {
        try
        {
            _isShowingDialog = true;

            // Use Windows Forms ColorDialog for simplicity
            using var colorDialog = new FormsColorDialog();

            // Set current color
            if (_viewModel.CustomBackgroundColor.HasValue)
            {
                var wpfColor = _viewModel.CustomBackgroundColor.Value;
                colorDialog.Color = DrawingColor.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
            }

            colorDialog.FullOpen = true;

            // Create Forms-compatible window handle
            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            var owner = System.Windows.Forms.Control.FromHandle(helper.Handle);

            if (colorDialog.ShowDialog(owner) == FormsDialogResult.OK)
            {
                var drawingColor = colorDialog.Color;
                var wpfColor = WpfColor.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);

                _viewModel.CustomBackgroundColor = wpfColor;
                SaveSettings();
            }
        }
        finally
        {
            _isShowingDialog = false;
        }
    }

    private void ResetColor()
    {
        _viewModel.CustomBackgroundColor = null;
        SaveSettings();
    }

    private void BrowseLogDirectory()
    {
        try
        {
            _isShowingDialog = true;

            using var dialog = new FormsFolderBrowserDialog();
            dialog.Description = "Select Log Directory";
            dialog.ShowNewFolderButton = true;

            if (!string.IsNullOrEmpty(_viewModel.LogDirectory) && _viewModel.LogDirectory != ".")
            {
                dialog.SelectedPath = _viewModel.LogDirectory;
            }

            var helper = new System.Windows.Interop.WindowInteropHelper(this);
            var owner = System.Windows.Forms.Control.FromHandle(helper.Handle);

            if (dialog.ShowDialog(owner) == FormsDialogResult.OK)
            {
                _viewModel.LogDirectory = dialog.SelectedPath;
            }
        }
        finally
        {
            _isShowingDialog = false;
        }
    }

	private void SaveSettings()
	{
		try
		{
			var settings = Properties.Settings.Default;

			// Save custom background color
			_mainViewModel.CustomBackgroundColor = _viewModel.CustomBackgroundColor;
			if (_viewModel.CustomBackgroundColor.HasValue)
			{
				settings.CustomBackgroundColor = _viewModel.CustomBackgroundColor.Value.ToString();
			}
			else
			{
				settings.CustomBackgroundColor = string.Empty;
			}

			// Save window size
			settings.MainWindowWidth = _viewModel.MainWindowWidth;
			settings.MainWindowHeight = _viewModel.MainWindowHeight;

			// Save log directory
			settings.LogDirectory = _viewModel.LogDirectory ?? ".";

			// Sync timezones back to MainWindowViewModel
			_mainViewModel.Timezones.Clear();
			foreach (var tz in _viewModel.Timezones)
			{
				_mainViewModel.Timezones.Add(tz);
			}

			// Save timezones
			if (_mainViewModel.Timezones.Count > 0)
			{
				settings.TimezonesJson = System.Text.Json.JsonSerializer.Serialize(_mainViewModel.Timezones.ToList());
			}

			settings.Save();

			// Update main window display (ViewModel handles this automatically via property changes)
			_mainViewModel.UpdateTimes();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
		}
	}

	private void AddTimezone()
	{
		try
		{
			_isShowingDialog = true;

			var dialog = new XAML.TimezonePickerDialog
			{
				Owner = this
			};

			var result = dialog.ShowDialog();

			if (result == true && dialog.SelectedTimezone != null)
			{
				_viewModel.Timezones.Add(new TimezoneEntry
				{
					Caption = dialog.TimezoneCaption,
					TimeZoneId = dialog.SelectedTimezone.Id
				});

				SaveSettings();
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to add timezone: {ex.Message}");
		}
		finally
		{
			_isShowingDialog = false;
			Activate();
		}
	}

	private void RemoveTimezone(TimezoneEntry entry)
	{
		try
		{
			_viewModel.Timezones.Remove(entry);
			SaveSettings();
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Failed to remove timezone: {ex.Message}");
		}
	}

    private void UpdateJiraUIState()
    {
        _viewModel.IsJiraConnected = _jiraService.IsAuthenticated;
        _viewModel.JiraStatusText = _jiraService.IsAuthenticated
            ? "Connected to Jira"
            : "Not connected";
    }

    private void UpdateUpworkUIState()
    {
        // Check if Upwork process is available
        if (!_upworkIntegration.IsUpworkAvailable())
        {
            _viewModel.UpworkState = Model.UpworkState.NoProcess;
            return;
        }

        // Process is available, check if we can automate it
        var weeklyTotal = _upworkIntegration.ReadWeeklyTotal();
        if (weeklyTotal.HasValue)
        {
            _viewModel.UpworkState = Model.UpworkState.FullyAutomated;
        }
        else
        {
            _viewModel.UpworkState = Model.UpworkState.ProcessFoundButCannotAutomate;
        }
    }

    private async void ConnectJira()
    {
        try
        {
            // Check if credentials are already stored
            if (!_jiraService.HasCredentials)
            {
                // Show credentials overlay
                var settings = Properties.Settings.Default;
                JiraCredentialsOverlay.SetCredentials(
                    settings.JiraClientId ?? "",
                    settings.JiraClientSecret ?? "");
                JiraCredentialsOverlay.Visibility = Visibility.Visible;
                return;
            }

            // Credentials exist, proceed with authentication
            await StartJiraAuthenticationFlow();
        }
        catch (Exception ex)
        {
            _viewModel.JiraStatusText = $"Error: {ex.Message}";
        }
    }

    private async Task StartJiraAuthenticationFlow()
    {
        try
        {
            _isShowingDialog = true;
            _viewModel.JiraStatusText = "Connecting...";

            var success = await _jiraService.StartAuthenticationFlowAsync();

            if (!success)
            {
                _viewModel.JiraStatusText = "Connection failed";
            }
        }
        catch (Exception ex)
        {
            _viewModel.JiraStatusText = $"Error: {ex.Message}";
        }
        finally
        {
            _isShowingDialog = false;
        }
    }

    private async void JiraCredentialsOverlay_CredentialsSubmitted(object? sender, (string ClientId, string ClientSecret) credentials)
    {
        JiraCredentialsOverlay.Visibility = Visibility.Collapsed;

        // Save credentials
        _jiraService.SetCredentials(credentials.ClientId, credentials.ClientSecret);

        // Start authentication flow
        await StartJiraAuthenticationFlow();
    }

    private void JiraCredentialsOverlay_Cancelled(object? sender, EventArgs e)
    {
        JiraCredentialsOverlay.Visibility = Visibility.Collapsed;
    }

    private async void DisconnectJira()
    {
        try
        {
            _viewModel.JiraStatusText = "Disconnecting...";
            await _jiraService.DisconnectAsync();
        }
        catch (Exception ex)
        {
            _viewModel.JiraStatusText = $"Error: {ex.Message}";
        }
    }

    private void JiraService_AuthenticationCompleted(object? sender, string message)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateJiraUIState();
        });
    }

    private void JiraService_AuthenticationFailed(object? sender, string error)
    {
        Dispatcher.Invoke(() =>
        {
            _viewModel.JiraStatusText = $"Failed: {error}";
        });
    }

    private void JiraService_Disconnected(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateJiraUIState();
        });
    }

	private void Window_Deactivated(object? sender, EventArgs e)
    {
        // Don't auto-hide if confirmation overlay is visible
        if (ConfirmationOverlay.Visibility == Visibility.Visible)
            return;

        // Don't auto-hide if showing a dialog (color picker, etc.)
        if (_isShowingDialog)
            return;

        // Auto-hide when clicking outside (context menu behavior)
        Close();
    }
}
