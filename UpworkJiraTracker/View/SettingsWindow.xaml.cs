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
    private bool _isShowingDialog;
    private bool _isClosing;

    public SettingsWindow(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _mainViewModel = mainWindow.ViewModel;

        _mainViewModel.JiraService.AuthenticationCompleted += JiraService_AuthenticationCompleted;
        _mainViewModel.JiraService.AuthenticationFailed += JiraService_AuthenticationFailed;
        _mainViewModel.JiraService.Disconnected += JiraService_Disconnected;
        _mainViewModel.PropertyChanged += MainViewModel_PropertyChanged;

        _viewModel = new SettingsViewModel(_mainViewModel)
        {
            CustomBackgroundColor = _mainViewModel.CustomBackgroundColor,
            MainWindowWidth = _mainWindow.Width,
            MainWindowHeight = _mainWindow.Height,
            LogDirectory = Properties.Settings.Default.LogDirectory ?? ".",
            TopmostEnforcementIntervalSeconds = Properties.Settings.Default.TopmostEnforcementIntervalSeconds,
            ShowDeelBrowser = Properties.Settings.Default.ShowDeelBrowser,
            PauseOnInactivityMinutes = Properties.Settings.Default.PauseOnInactivityMinutes
        };

        foreach (var tz in _mainViewModel.Timezones)
        {
            _viewModel.Timezones.Add(tz);
        }

        DataContext = _viewModel;

        _viewModel.PickColorRequested += (s, e) => PickColor();
        _viewModel.CloseRequested += (s, e) =>
        {
            ConfirmationOverlay.ResetToDefault();
            ConfirmationOverlay.Tag = null;
            ConfirmationOverlay.Visibility = Visibility.Visible;
        };
        _viewModel.AddTimezoneRequested += (s, e) => AddTimezone();
        _viewModel.ConnectJiraRequested += (s, e) => ConnectJira();
        _viewModel.BrowseLogDirectoryRequested += (s, e) => BrowseLogDirectory();
        _viewModel.OpenTimeLogRequested += (s, e) => OpenTimeLog();
        _viewModel.MinimizeRequested += (s, e) => Minimize();
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;

        UpdateJiraUIState();
        _viewModel.UpworkState = _mainViewModel.UpworkState;
        UpdateColorPreview();
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
                _mainViewModel.TimeTrackingService.TimeLogService.UpdateLogDirectory(_viewModel.LogDirectory);
                SaveSettings();
                break;
            case nameof(SettingsViewModel.TopmostEnforcementIntervalSeconds):
                SaveSettings();
                break;
            case nameof(SettingsViewModel.ShowDeelBrowser):
                SaveSettings();
                break;
            case nameof(SettingsViewModel.PauseOnInactivityMinutes):
                SaveSettings();
                _mainViewModel.ReloadPauseOnInactivitySetting();
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

        if (mainWindowTop - Height >= workArea.Top)
        {
            Top = mainWindowTop - Height;
        }
        else if (mainWindowBottom + Height <= workArea.Bottom)
        {
            Top = mainWindowBottom;
        }
        else
        {
            Top = mainWindowBottom;
        }

        if (mainWindowLeft + Width <= workArea.Right)
        {
            Left = mainWindowLeft;
        }
        else if (mainWindowRight - Width >= workArea.Left)
        {
            Left = mainWindowRight - Width;
        }
        else
        {
            Left = (workArea.Width - Width) / 2 + workArea.Left;
        }

        if (Left < workArea.Left) Left = workArea.Left;
        if (Left + Width > workArea.Right) Left = workArea.Right - Width;
        if (Top < workArea.Top) Top = workArea.Top;
        if (Top + Height > workArea.Bottom) Top = workArea.Bottom - Height;
    }

    private void Minimize()
    {
        if (_isClosing) return;
        _isClosing = true;
        Close();
    }

    private void ConfirmationOverlay_Cancelled(object? sender, EventArgs e)
    {
        ConfirmationOverlay.Visibility = Visibility.Collapsed;
        Activate();
    }

    private void PickColor()
    {
        try
        {
            _isShowingDialog = true;

            using var colorDialog = new FormsColorDialog();

            if (_viewModel.CustomBackgroundColor.HasValue)
            {
                var wpfColor = _viewModel.CustomBackgroundColor.Value;
                colorDialog.Color = DrawingColor.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
            }

            colorDialog.FullOpen = true;

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

    private void OpenTimeLog()
    {
        try
        {
            _isShowingDialog = true;

            var timeLogService = _mainViewModel.TimeTrackingService.TimeLogService;
            var timeLogViewModel = new TimeLogViewModel(timeLogService);
            var timeLogWindow = new TimeLogWindow(timeLogViewModel)
            {
                Owner = this
            };
            timeLogWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open time log: {ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isShowingDialog = false;
            Activate();
        }
    }

    private void SaveSettings()
    {
        try
        {
            var settings = Properties.Settings.Default;

            _mainViewModel.CustomBackgroundColor = _viewModel.CustomBackgroundColor;
            if (_viewModel.CustomBackgroundColor.HasValue)
            {
                var color = _viewModel.CustomBackgroundColor.Value;
                // Save transparent color as "Transparent" to preserve it correctly
                settings.CustomBackgroundColor = color.A == 0 ? "Transparent" : color.ToString();
            }
            else
            {
                settings.CustomBackgroundColor = string.Empty;
            }

            settings.MainWindowWidth = _viewModel.MainWindowWidth;
            settings.MainWindowHeight = _viewModel.MainWindowHeight;
            settings.LogDirectory = _viewModel.LogDirectory ?? ".";
            settings.TopmostEnforcementIntervalSeconds = _viewModel.TopmostEnforcementIntervalSeconds;
            settings.ShowDeelBrowser = _viewModel.ShowDeelBrowser;
            settings.PauseOnInactivityMinutes = _viewModel.PauseOnInactivityMinutes;

            _mainViewModel.Timezones.Clear();
            foreach (var tz in _viewModel.Timezones)
            {
                _mainViewModel.Timezones.Add(tz);
            }

            if (_mainViewModel.Timezones.Count > 0)
            {
                settings.TimezonesJson = System.Text.Json.JsonSerializer.Serialize(_mainViewModel.Timezones.ToList());
            }

            settings.Save();
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

    private void UpdateJiraUIState()
    {
        var isConnected = _mainViewModel.JiraService.IsAuthenticated;
        _viewModel.IsJiraConnected = isConnected;
        _viewModel.JiraStatusText = isConnected ? "Connected to Jira" : "Not connected";
    }

    private async void ConnectJira()
    {
        try
        {
            if (!_mainViewModel.JiraService.HasCredentials)
            {
                var settings = Properties.Settings.Default;
                JiraCredentialsOverlay.SetCredentials(
                    settings.JiraClientId ?? "",
                    settings.JiraClientSecret ?? "");
                JiraCredentialsOverlay.Visibility = Visibility.Visible;
                return;
            }

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

            var success = await _mainViewModel.JiraService.StartAuthenticationFlowAsync();

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
        _mainViewModel.JiraService.SetCredentials(credentials.ClientId, credentials.ClientSecret);
        await StartJiraAuthenticationFlow();
        Activate();
    }

    private void JiraCredentialsOverlay_Cancelled(object? sender, EventArgs e)
    {
        JiraCredentialsOverlay.Visibility = Visibility.Collapsed;
        Activate();
    }

    private void JiraService_AuthenticationCompleted(object? sender, string message)
    {
        Dispatcher.Invoke(() =>
        {
            UpdateJiraUIState();
            Activate();
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
		=> Dispatcher.Invoke(UpdateJiraUIState);

    private void MainViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.UpworkState))
        {
            Dispatcher.Invoke(() =>
            {
                _viewModel.UpworkState = _mainViewModel.UpworkState;
            });
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (_isClosing)
            return;

        if (ConfirmationOverlay.Visibility == Visibility.Visible)
            return;

        if (JiraCredentialsOverlay.Visibility == Visibility.Visible)
            return;

        if (_isShowingDialog)
            return;

        _isClosing = true;
        Close();
    }

    private void JiraIcon_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_viewModel.IsJiraConnected)
        {
            ConfirmationOverlay.SetMessage("Disconnect from Jira?", "You will need to reconnect to log time to Jira issues.", "Disconnect");
            ConfirmationOverlay.Tag = "DisconnectJira";
            ConfirmationOverlay.Visibility = Visibility.Visible;
        }
        else
        {
            ConnectJira();
        }
    }

    private void DeelIcon_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _isShowingDialog = true;

        var service = DeelEmbeddedBrowserService.Instance;
        service.OpenBrowserWindow();

        _viewModel.IsDeelConnected = service.IsAuthenticated;
    }

    private void ConfirmationOverlay_Confirmed(object? sender, EventArgs e)
    {
        var tag = ConfirmationOverlay.Tag as string;
        ConfirmationOverlay.Visibility = Visibility.Collapsed;
        ConfirmationOverlay.ResetToDefault();

        if (tag == "DisconnectJira")
        {
            _ = _mainViewModel.JiraService.DisconnectAsync();
            UpdateJiraUIState();
            ConfirmationOverlay.Tag = null;
        }
        else
        {
            WpfApplication.Current.Shutdown();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _mainViewModel.JiraService.AuthenticationCompleted -= JiraService_AuthenticationCompleted;
        _mainViewModel.JiraService.AuthenticationFailed -= JiraService_AuthenticationFailed;
        _mainViewModel.JiraService.Disconnected -= JiraService_Disconnected;
        _mainViewModel.PropertyChanged -= MainViewModel_PropertyChanged;

        base.OnClosed(e);
    }
}
