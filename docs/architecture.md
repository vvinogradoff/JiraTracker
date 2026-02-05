# UpworkJiraTracker Architecture Document

## Overview

UpworkJiraTracker is a WPF desktop application built on .NET 9.0 that integrates time tracking across Upwork, Jira, and Deel. The application follows the MVVM (Model-View-ViewModel) architectural pattern with a service layer for external integrations.

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Presentation Layer                          │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                    Views (XAML + Code-Behind)               │    │
│  │  MainWindow | SettingsWindow | DeelBrowserWindow | Dialogs  │    │
│  └─────────────────────────────────────────────────────────────┘    │
│                              │ DataBinding                          │
│  ┌─────────────────────────────────────────────────────────────┐    │
│  │                      ViewModels                             │    │
│  │  MainWindowViewModel | SettingsViewModel | TimeLogViewModel │    │
│  └─────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
                               │
                               │ Orchestration
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                         Service Layer                               │
│  ┌──────────────┐ ┌──────────────┐ ┌────────────────────────────┐   │
│  │   Upwork     │ │    Jira      │ │          Deel              │   │
│  │  Services    │ │  Services    │ │        Services            │   │
│  │              │ │              │ │                            │   │
│  │ FlaUI/UIA3   │ │ OAuth + API  │ │ WebView2 + CDP + API       │   │
│  └──────────────┘ └──────────────┘ └────────────────────────────┘   │
│                                                                     │
│  ┌──────────────┐ ┌──────────────┐ ┌────────────────────────────┐   │
│  │TimeTracking  │ │  Settings    │ │      Notifications         │   │
│  │  Service     │ │  Service     │ │        Service             │   │
│  └──────────────┘ └──────────────┘ └────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
                               │
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│                        External Systems                             │
│                                                                     │
│  Upwork Desktop App │ Atlassian Jira Cloud │ Deel.com │ Windows OS  │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Project Structure

```
UpworkJiraTracker/
├── View/                    # XAML Windows and UserControls
│   ├── MainWindow.xaml      # Primary taskbar overlay
│   ├── SettingsWindow.xaml  # Settings and integrations
│   ├── DeelBrowserWindow.xaml
│   ├── TimeLogWindow.xaml
│   ├── TimeLogControl.xaml
│   ├── TimeLogPopup.xaml
│   └── WorklogInputWindow.xaml
│
├── ViewModel/               # MVVM ViewModels
│   ├── MainWindowViewModel.cs
│   ├── SettingsViewModel.cs
│   ├── TimeLogViewModel.cs
│   ├── DeelBrowserViewModel.cs
│   ├── WorklogInputViewModel.cs
│   ├── JiraCredentialsOverlayViewModel.cs
│   ├── ConfirmationOverlayViewModel.cs
│   └── TimezonePickerViewModel.cs
│
├── Service/                 # Business logic and integrations
│   ├── UpworkIntegrationFlaUI.cs
│   ├── UpworkWindowWatcherService.cs
│   ├── JiraOAuthService.cs
│   ├── JiraIssuesService.cs
│   ├── JiraIssueCacheService.cs
│   ├── DeelAutomationService.cs
│   ├── DeelEmbeddedBrowserService.cs
│   ├── DeelApiClient.cs
│   ├── TimeTrackingService.cs
│   ├── TimeLogService.cs
│   ├── WindowSettingsService.cs
│   ├── TaskbarMonitor.cs
│   └── NotificationService.cs
│
├── Model/                   # Data models
│   ├── JiraIssue.cs
│   ├── IssueDetails.cs
│   ├── UpworkState.cs
│   ├── UpworkTimeStats.cs
│   ├── WindowSettings.cs
│   ├── TimezoneEntry.cs
│   ├── TimeLogData.cs
│   ├── WorklogResult.cs
│   ├── WindowInfo.cs
│   ├── DeelApiModels.cs
│   └── EventArgs/
│
├── Helper/                  # Utilities
│   ├── RelayCommand.cs
│   ├── ThemeHelper.cs
│   ├── NativeMethods.cs
│   └── TaskExtensions.cs
│
├── XAML/                    # Custom controls and converters
│   ├── AutocompleteTextBox.xaml
│   ├── ConfirmationOverlay.xaml
│   ├── JiraCredentialsOverlay.xaml
│   ├── TimezonePickerDialog.xaml
│   └── Converter/
│
├── Extensions/
├── Properties/
├── Resources/               # Icons and images
├── App.xaml
├── Constants.cs
└── AssemblyInfo.cs
```

---

## Architectural Principles

### 1. MVVM Pattern (Strict Adherence)

The application strictly follows MVVM to separate concerns:

**Views (Windows, UserControls)**
- Only handle UI concerns: positioning, focus, drag behavior
- No business logic in code-behind
- All data displayed via bindings to ViewModel properties
- All user actions routed through Commands

**ViewModels**
- Contain all business logic and state
- Implement `INotifyPropertyChanged` for data binding
- Expose `ICommand` properties for user actions
- Orchestrate Service layer calls
- Never reference UI elements directly

**Models**
- Plain data objects (POCOs)
- No behavior, just data containers
- Used for API responses, settings, transfer objects

**Example - Correct Pattern:**
```csharp
// ViewModel
public class MainWindowViewModel : INotifyPropertyChanged
{
    public ICommand PlayPauseCommand { get; }
    public string DisplayText { get => _displayText; set { _displayText = value; OnPropertyChanged(); } }

    public MainWindowViewModel()
    {
        PlayPauseCommand = new RelayCommand(OnPlayPauseClick, () => IsUpworkReady);
    }
}

// View (XAML)
<Button Command="{Binding PlayPauseCommand}" />
<TextBlock Text="{Binding DisplayText}" />
```

**Anti-Pattern (Never Do This):**
```csharp
// Code-behind - WRONG!
private void Button_Click(object sender, RoutedEventArgs e)
{
    // Business logic in code-behind
    _upworkService.ToggleTracking();
}
```

### 2. Service Layer Pattern

External integrations and business logic are encapsulated in services:

| Service | Responsibility |
|---------|----------------|
| UpworkIntegrationFlaUI | Upwork UI automation via FlaUI/UIA3 |
| JiraOAuthService | OAuth 2.0 authentication |
| JiraIssuesService | Jira API calls (search, worklog) |
| JiraIssueCacheService | Background issue caching |
| DeelAutomationService | Deel integration orchestration |
| DeelEmbeddedBrowserService | WebView2 browser management |
| DeelApiClient | Deel HTTP API calls |
| TimeTrackingService | Time tracking logic coordination |
| TimeLogService | Excel file operations |
| WindowSettingsService | Settings persistence |
| NotificationService | Toast notifications |

**Service Guidelines:**
- Services are injected/instantiated in ViewModels
- Services do not reference UI elements
- Services expose events for state changes
- Services handle their own error logging

### 3. Singleton Pattern (Where Appropriate)

Some services use singleton pattern for state persistence:

```csharp
// DeelEmbeddedBrowserService - singleton for browser instance
public static DeelEmbeddedBrowserService Instance { get; private set; }

public static DeelEmbeddedBrowserService GetOrCreate()
{
    return Instance ??= new DeelEmbeddedBrowserService();
}
```

**When to Use Singleton:**
- Browser instances that must persist
- Shared state across multiple ViewModels
- Resource-intensive objects (single instance sufficient)

### 4. Event-Driven Communication

Services communicate with ViewModels via events:

```csharp
// Service
public event EventHandler<AuthenticationCompletedEventArgs> AuthenticationCompleted;

// ViewModel subscribes
_jiraOAuthService.AuthenticationCompleted += OnJiraAuthCompleted;
```

### 5. Command Pattern

All user actions use `ICommand` via `RelayCommand`:

```csharp
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool> _canExecute;

    public RelayCommand(Action execute, Func<bool> canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
    public void Execute(object parameter) => _execute();
}
```

---

## Module Breakdown

### Upwork Integration Module

**Components:**
- `UpworkIntegrationFlaUI` - Core automation
- `UpworkWindowWatcherService` - Window monitoring
- `UpworkState` enum - Connection states

**Integration Technique:**
Uses FlaUI.UIA3 (UI Automation) to interact with Upwork desktop app:

```csharp
// Finding Upwork window
var app = Application.Attach("Upwork");
var mainWindow = app.GetMainWindow(automation);

// Reading time display
var timeElement = mainWindow.FindFirstDescendant(cf =>
    cf.ByAutomationId("timeDisplay"));
var timeText = timeElement.AsLabel().Text; // "4h 23m"
```

**Critical Requirement:**
Upwork must be launched with `--enable-features=UiaProvider` flag for automation to work.

**State Machine:**
```
NoProcess → ProcessFoundButCannotAutomate → FullyAutomated
    ↑                    ↑                        ↑
    └────────────────────┴────────────────────────┘
                   (Process exits)
```

### Jira Integration Module

**Components:**
- `JiraOAuthService` - OAuth 2.0 flow
- `JiraIssuesService` - API client
- `JiraIssueCacheService` - Background caching

**OAuth 2.0 Flow:**
1. User enters Client ID + Client Secret
2. App opens browser to Atlassian authorization
3. User grants access
4. Atlassian redirects to `localhost:8080/callback`
5. App exchanges code for tokens
6. Refresh token used for subsequent sessions

**API Endpoints Used:**
- `GET /rest/api/3/search` - JQL issue search
- `GET /rest/api/3/issue/{key}` - Issue details
- `POST /rest/api/3/issue/{key}/worklog` - Log work

**Issue Caching:**
```
Every 5 minutes:
  → Fetch issues where user is assignee/reporter
  → Fetch open issues (status NOT IN done/cancelled/closed)
  → Store in memory for instant search
```

### Deel Integration Module

**Components:**
- `DeelAutomationService` - Orchestration
- `DeelEmbeddedBrowserService` - WebView2 browser
- `DeelApiClient` - HTTP client

**Critical Design Decision:**
Deel pages have strict Content Security Policy (CSP) that blocks JavaScript injection.

**Solution: Chrome DevTools Protocol (CDP)**
```csharp
// JavaScript injection FAILS due to CSP:
await webView.ExecuteScriptAsync("document.querySelector('...')"); // BLOCKED!

// CDP methods work (browser-level, bypasses CSP):
await webView.CoreWebView2.CallDevToolsProtocolMethodAsync(
    "DOM.querySelector",
    JsonSerializer.Serialize(new { nodeId = rootNodeId, selector = "button" })
);
```

**Authentication Flow:**
1. Open hidden WebView2 browser to Deel
2. Check for authentication (profile element exists)
3. If not authenticated, show browser window for login
4. Extract cookies after login
5. Use cookies for API calls

### Time Tracking Module

**Components:**
- `TimeTrackingService` - Central coordinator
- `TimeLogService` - Excel persistence

**Two Tracking Modes:**

| Mode | Source | Use Case |
|------|--------|----------|
| Upwork Mode | Reads time from Upwork app | Primary use when Upwork is running |
| Internal Mode | Local timer | Fallback when Upwork unavailable |

**Logging Rules:**
```
On Stop:
  - Log if accumulated time >= 5 minutes

On Issue Change:
  - Log if accumulated time >= 10 minutes

Always:
  - Round to 10-minute blocks
  - Subtract initial 30-second offset
  - Log to Jira (if connected)
  - Log to Deel (if connected)
  - Record in Excel (if configured)
```

### Settings Module

**Components:**
- `WindowSettingsService` - Persistence
- `WindowSettings` model
- `Properties.Settings` - .NET user settings

**Persisted Settings:**
| Setting | Default | Storage |
|---------|---------|---------|
| Window position | Auto-positioned | User settings |
| Window size | 180x48 | User settings |
| Custom color | null (theme-matched) | User settings |
| Timezones | PST, GMT | JSON in user settings |
| Log directory | Empty (disabled) | User settings |
| Jira credentials | Empty | User settings (encrypted) |
| Jira tokens | Empty | User settings |
| Last memo | Empty | User settings |

---

## Key Design Decisions

### 1. FlaUI over Windows Automation API

**Decision:** Use FlaUI.UIA3 wrapper instead of raw Windows UI Automation.

**Rationale:**
- Cleaner API with fluent syntax
- Better error handling
- Condition builder pattern for finding elements
- Active maintenance and community

### 2. WebView2 over Puppeteer for Deel

**Decision:** Use embedded WebView2 instead of headless Puppeteer.

**Rationale:**
- Visual browser allows user to authenticate
- Cookie persistence through Edge profile
- Lower resource usage than Puppeteer
- CDP still available for automation

### 3. CDP over JavaScript for Deel Automation

**Decision:** Use Chrome DevTools Protocol instead of JavaScript injection.

**Rationale:**
- Deel's CSP blocks inline JavaScript execution
- CDP operates at browser level, bypassing page restrictions
- More reliable for DOM manipulation

### 4. Local OAuth Callback Server

**Decision:** Run temporary HTTP server on localhost:8080 for OAuth callback.

**Rationale:**
- Pretty much the only way to go with Jira OAuth

### 5. Excel for Time Logs

**Decision:** Use ClosedXML to write Excel files instead of CSV or database.

**Rationale:**
- Users can easily open and review
- Week/day structure with worksheets
- No database setup required
- Familiar format for invoicing

### 6. No Dependency Injection Container

**Decision:** Manual dependency management instead of DI container.

**Rationale:**
- Small application scope
- Simple dependency graph
- Reduced complexity
- Services instantiated in ViewModels as needed

---

## Integration Points

### Windows API (P/Invoke)

```csharp
// NativeMethods.cs - Windows API declarations
[DllImport("user32.dll")]
static extern bool SetForegroundWindow(IntPtr hWnd);

[DllImport("user32.dll")]
static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
```

**Used For:**
- Window positioning and focus
- Theme color detection
- Taskbar monitoring

### WebView2 Runtime

**Requirement:** Microsoft Edge WebView2 Runtime must be installed.

**Features Used:**
- `CoreWebView2.NavigateToString()` - Load HTML
- `CoreWebView2.CallDevToolsProtocolMethodAsync()` - CDP calls
- `CoreWebView2.CookieManager` - Cookie access

### Toast Notifications

**Requirement:** Microsoft.Toolkit.Uwp.Notifications package.

```csharp
new ToastContentBuilder()
    .AddText("Time Logged")
    .AddText("1h 30m logged to PROJ-123")
    .Show();
```

---

## Error Handling Strategy

### Service Layer
- Catch exceptions at service boundaries
- Return result objects with success/error status
- Log errors to diagnostic files where appropriate

### ViewModel Layer
- Handle service errors gracefully
- Update UI state to reflect errors
- Show user-friendly error notifications

### Critical Errors
- Upwork process not found → Update state indicator
- Jira token expired → Auto-refresh or prompt re-auth
- Deel authentication lost → Show browser for re-login
- Network errors → Retry with backoff, then notify user

---

## Threading Model

### UI Thread
- All WPF UI updates
- ViewModel property changes
- Command execution starts

### Background Threads
- Jira cache refresh (timer-based)
- Upwork window monitoring (polling)
- OAuth callback server (HTTP listener)
- WebView2 operations (async)

### Synchronization
```csharp
// Dispatch to UI thread from background
Application.Current.Dispatcher.Invoke(() => {
    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
});
```

---

## Configuration Constants

All configurable values centralized in `Constants.cs`:

```csharp
public static class Constants
{
    public static class Upwork
    {
        public const string ProcessName = "Upwork";
        public const string WindowTitle = "Time Tracker";
    }

    public static class Jira
    {
        public const string CloudInstanceUrl = "https://onestop.atlassian.net";
        public static readonly TimeSpan CacheRefreshInterval = TimeSpan.FromMinutes(5);
    }

    public static class TimeTracking
    {
        public static readonly TimeSpan LoggingBlockSize = TimeSpan.FromMinutes(10);
        public static readonly TimeSpan MinimumTimeToLogOnStop = TimeSpan.FromMinutes(5);
    }

    public static class Window
    {
        public const int DefaultWidth = 180;
        public const int DefaultHeight = 48;
    }
}
```

---

## Future Architecture Considerations

### Cross-Platform Potential

To support non-Windows platforms:

**Shareable Components:**
- All ViewModels (with abstraction)
- Models
- Jira integration (HTTP-based)
- Deel integration (HTTP-based, needs alternative browser)
- TimeLogService (with cross-platform Excel library)
- TimeTrackingService (internal timer mode only)

**Windows-Only Components:**
- MainWindow (WPF-specific taskbar overlay)
- UpworkIntegrationFlaUI (Windows UI Automation)
- UpworkWindowWatcherService (Windows API)
- ThemeHelper (Windows theme detection)
- NativeMethods (P/Invoke)

### Proposed Split Architecture

```
JiraTracker.Shared/           # Cross-platform core
├── ViewModel/
├── Service/
│   ├── JiraOAuthService.cs
│   ├── JiraIssuesService.cs
│   ├── JiraIssueCacheService.cs
│   ├── TimeTrackingService.cs
│   ├── TimeLogService.cs
│   └── Interfaces/
├── Model/
├── Helper/
└── Constants.cs

JiraTracker.Upwork/           # Windows-only with Upwork
├── View/                     # WPF Windows
├── Service/
│   ├── UpworkIntegrationFlaUI.cs
│   └── WindowsSpecific/
├── Resources/
└── App.xaml

JiraTracker.Light/            # Cross-platform (future)
├── (Avalonia/MAUI Views)
└── Platform-specific services
```

---

## Dependencies

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| FlaUI.Core | 4.0.0 | UI Automation core |
| FlaUI.UIA3 | 4.0.0 | UIA3 implementation |
| Microsoft.Web.WebView2 | 1.0.2792.45 | Embedded Chromium browser |
| ClosedXML | 0.104.1 | Excel file operations |
| Microsoft.Toolkit.Uwp.Notifications | 7.1.3 | Toast notifications |

### Runtime Requirements

- .NET 9.0 Runtime
- Windows 10 version 1809+ or Windows 11
- Microsoft Edge WebView2 Runtime
- Upwork Time Tracker (optional, for Upwork integration)

---

## Security Considerations

1. **OAuth Tokens**: Stored in user settings, protected by Windows user profile
2. **No Password Storage**: Only OAuth tokens, never raw passwords
3. **Local Callback Server**: Only listens during OAuth flow, stops after completion
4. **Cookie Handling**: Deel cookies stored in WebView2 profile, user-scoped

---

## Logging and Diagnostics

| Log File | Purpose |
|----------|---------|
| `upwork.window.log` | Upwork window events and automation diagnostics |
| `TimeLog.xlsx` | Time entry history (user-accessible) |

**Diagnostic Commands:**
- `DumpAllUpworkWindowsDiagnostics()` - Logs all Upwork windows to file
