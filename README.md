# UpworkJiraTracker

A lightweight WPF desktop application for Windows that provides seamless time tracking integration between Upwork and Jira. The app sits in your taskbar as a compact overlay, allowing you to track time and log work to Jira issues with minimal interruption to your workflow.

## Features

### Core Functionality
- **Taskbar Integration**: Borderless overlay positioned over the Windows 11 widgets button
- **System Theme Matching**: Automatically matches your taskbar's background color
- **Play/Pause Button**: One-click time tracking control
- **Dual Timezone Display**: Shows two configurable timezones simultaneously

### Upwork Integration
- **UI Automation**: Directly controls the Upwork Time Tracker application using FlaUI
- **Visual Status Indicator**: Settings window shows Upwork logo in different colors:
  - Light gray: No Upwork process detected
  - Dark gray: Upwork detected but not automatable (needs `--enable-features=UiaProvider` flag)
  - Full color: Upwork detected and successfully automated
- **Automatic Time Sync**: Reads weekly total hours from Upwork UI
- **Memo Updates**: Automatically updates work description when logging to Jira
- **Smart Detection**: Detects tracking status without interfering with your work

### Jira Integration
- **OAuth 2.0 Authentication**: Secure connection to your Jira Cloud instance
- **Issue Search**: Quick search with autocomplete and recent issues cache
- **Work Logging**: Log time blocks directly to Jira issues
- **Automatic Time Rounding**: Rounds time to configurable block sizes (default: 10 minutes)
- **Bring Your Own Credentials**: Use your own Jira OAuth app credentials (not hardcoded)

### Settings Window
- **Timezone Configuration**: Add/remove multiple timezones with custom labels
- **Window Size Customization**: Adjust overlay dimensions
- **Background Color**: Custom color picker or automatic theme matching
- **Log Directory**: Optional activity logging for debugging
- **Integration Status**: Visual indicators for Upwork and Jira connection status
- **Modal Overlays**: Clean, modern UI without system popups

## Technology Stack

- **.NET 9.0** with WPF
- **FlaUI (UIA3)** for Upwork UI automation
- **Atlassian OAuth 2.0** for Jira authentication
- **Windows API (P/Invoke)** for taskbar detection and monitoring
- **Windows Theme APIs** for background color matching

## Project Structure

### View
- `MainWindow.xaml/cs` - Main overlay window with play/pause button
- `SettingsWindow.xaml/cs` - Settings popup with integrations and preferences
- `ConfirmationOverlay.xaml/cs` - Modal confirmation dialog
- `JiraCredentialsOverlay.xaml/cs` - OAuth credentials input dialog

### ViewModel
- `MainWindowViewModel.cs` - Main window data binding and logic
- `SettingsViewModel.cs` - Settings window data binding

### Service
- `UpworkIntegrationFlaUI.cs` - Upwork UI automation using FlaUI
- `JiraOAuthService.cs` - Jira OAuth 2.0 authentication and token management
- `JiraService.cs` - Jira API client for issue search and worklog creation
- `TimeTrackingService.cs` - Time tracking logic and work logging
- `TaskbarMonitor.cs` - Taskbar and widgets button position monitoring

### Helper
- `ThemeHelper.cs` - System theme color detection
- `NativeMethods.cs` - Windows API P/Invoke declarations

## Prerequisites

- Windows 10/11
- .NET 9.0 SDK or later
- Upwork Desktop App (optional, for Upwork integration)
- Jira Cloud instance (optional, for Jira integration)

## Building and Running

```bash
cd UpworkJiraTracker
dotnet build
dotnet run
```

## Setup

### Upwork Integration

For full automation capabilities, start the Upwork desktop app with UI Automation enabled:

```bash
"C:\Program Files\Upwork\Upwork.exe" --enable-features=UiaProvider
```

You can create a shortcut with this flag to make it permanent.

**Without this flag:** The app will detect Upwork but won't be able to automate it (shown as dark gray icon).

### Jira Integration

1. Create a Jira OAuth 2.0 app in your Atlassian Developer Console:
   - Go to https://developer.atlassian.com/console/myapps/
   - Create a new OAuth 2.0 app
   - Add callback URL: `http://localhost:8080/callback`
   - Add permissions: `read:jira-work` and `write:jira-work`
   - Enable offline access for refresh tokens

2. When you click "Connect to Jira" in the settings, you'll be prompted to enter:
   - Client ID (from your OAuth app)
   - Client Secret (from your OAuth app)

3. These credentials are stored locally on your device and never shared.

## How It Works

### Taskbar Detection

The application uses Windows API calls to:
1. Find the taskbar window (`Shell_TrayWnd`)
2. Locate the widgets button within the taskbar
3. Monitor position changes using `SetWinEventHook`
4. Poll periodically as a fallback mechanism

### Theme Detection

The application attempts to match the taskbar background by:
1. Querying DWM colorization color
2. Falling back to immersive color APIs (`ImmersiveStartBackground`)
3. Using a default dark semi-transparent color if APIs fail

### Overlay Behavior

- Borderless, transparent window
- Topmost (always on top)
- Not shown in taskbar
- Updates position automatically when taskbar moves or widgets button changes

### Settings Window

- Opens on click
- Positioned below the overlay
- Auto-hides when focus is lost (context menu behavior)
- Changes are applied in real-time
- Uses modal overlay for exit confirmation instead of system MessageBox

### Upwork Integration

The application uses FlaUI to directly automate the Upwork Time Tracker window:
1. Detects if Upwork.exe process is running
2. Locates the "Time Tracker" window
3. Reads the Start/Stop button state to determine tracking status
4. Reads weekly total hours from the UI
5. Clicks Start/Stop buttons programmatically
6. Updates memo text when logging time to Jira

This provides seamless control without requiring manual interaction with the Upwork app.

**Note:** For automation to work, Upwork must be started with the `--enable-features=UiaProvider` flag to enable UI Automation.

### Jira Integration

When connected to Jira:
1. Uses OAuth 2.0 for secure authentication (no passwords stored)
2. Searches issues assigned to you or where you're the reporter
3. Caches frequently accessed issues for faster search
4. Logs work time with automatic rounding to configured block size
5. Updates Upwork memo to match the Jira issue key
6. Automatically refreshes expired access tokens using refresh tokens

## Privacy

**This application does NOT collect any personal information.**

All data is stored locally on your device. The only external communication is:
- Direct OAuth authentication with Atlassian (when you connect to Jira)
- Direct API calls to your Jira instance (when logging work)

See [PRIVACY-POLICY.md](PRIVACY-POLICY.md) for complete details.

## Publishing as a Public Jira App

To publish this app as a public Jira integration:

1. **Remove Hardcoded Credentials**: Already done - the app now prompts users for their own OAuth credentials
2. **Privacy Policy**: Use the included `PRIVACY-POLICY.md`
3. **Open Source**: The code can be safely published without exposing credentials
4. **User Setup**: Each user creates their own Jira OAuth app and provides credentials
5. **Cloud Instance URL**: Update `Constants.Jira.CloudInstanceUrl` or make it configurable for multi-tenant use

### Recommended Approach

For a public distribution:
- Keep the code public on GitHub
- Users create their own Jira OAuth apps (ensures each installation is isolated)
- Alternatively, create a shared OAuth app for your distribution and document the credentials in a secure setup guide

## Notes

- Designed for Windows 10/11 with taskbar widgets button
- Background color matching is approximate and may vary based on Windows theme
- Default timezones: Pacific Standard Time (GMT-8) and GMT Standard Time (GMT)
- Upwork automation requires the `--enable-features=UiaProvider` launch flag
- Jira integration requires OAuth 2.0 app setup in Atlassian Developer Console
