# UpworkJiraTracker Requirements Specification

## Overview

UpworkJiraTracker is a Windows desktop application designed for freelancers working on Upwork who need to track time across multiple systems: Upwork Time Tracker, Jira, and Deel. The application provides a compact taskbar overlay that unifies time tracking workflows.

---

## Functional Requirements

### FR-1: Taskbar Overlay Widget

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1.1 | Display a compact overlay window (default 180x48px) positioned over the Windows 11 widgets button area | High |
| FR-1.2 | Keep window always-on-top of other applications | High |
| FR-1.3 | Hide window from taskbar and alt-tab | High |
| FR-1.4 | Match taskbar theme (dark/light mode colors) | Medium |
| FR-1.5 | Allow custom background color override | Low |
| FR-1.6 | Allow window repositioning via drag | Medium |
| FR-1.7 | Allow window resizing via settings | Low |
| FR-1.8 | Enforce topmost position at configurable intervals | Medium |

### FR-2: Upwork Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-2.1 | Detect if Upwork Time Tracker desktop application is running | High |
| FR-2.2 | Detect if Upwork is launched with UI Automation enabled (`--enable-features=UiaProvider`) | High |
| FR-2.3 | Read current session elapsed time from Upwork | High |
| FR-2.4 | Read weekly total hours from Upwork | High |
| FR-2.5 | Detect if time tracking is active in Upwork | High |
| FR-2.6 | Automate clicking Start/Stop buttons in Upwork | High |
| FR-2.7 | Update the "What are you working on?" memo field in Upwork | High |
| FR-2.8 | Display visual indicator of Upwork connection state (No Process / Cannot Automate / Fully Automated) | Medium |
| FR-2.9 | Monitor for new Upwork windows (e.g., screenshot confirmations) | Low |

### FR-3: Jira Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-3.1 | Authenticate with Atlassian Jira using OAuth 2.0 | High |
| FR-3.2 | Support OAuth 2.0 token refresh using refresh tokens | High |
| FR-3.3 | Allow user to enter OAuth client credentials (Client ID, Client Secret) | High |
| FR-3.4 | Search Jira issues by text query | High |
| FR-3.5 | Display autocomplete suggestions for Jira issues | High |
| FR-3.6 | Cache Jira issues in background for fast autocomplete (refresh every 5 minutes) | Medium |
| FR-3.7 | Log work time to Jira issues (worklog creation) | High |
| FR-3.8 | Support optional work description/comment when logging | Medium |
| FR-3.9 | Support optional remaining estimate update when logging | Low |
| FR-3.10 | Round logged time to configurable block size (default 10 minutes) | High |
| FR-3.11 | Display visual indicator of Jira connection state | Medium |
| FR-3.12 | Allow disconnecting from Jira (revoke tokens) | Medium |

### FR-4: Deel Integration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-4.1 | Provide embedded browser for Deel authentication | High |
| FR-4.2 | Detect Deel authentication state | High |
| FR-4.3 | Log hours to Deel time tracking | High |
| FR-4.4 | Support silent (hidden browser) time logging | Medium |
| FR-4.5 | Show browser window only when authentication is required | Medium |
| FR-4.6 | Persist authentication cookies between sessions | Medium |
| FR-4.7 | Display visual indicator of Deel connection state | Medium |
| FR-4.8 | Round logged time to configurable block size (default 10 minutes) | High |

### FR-5: Time Tracking Workflow

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-5.1 | Support Play/Pause/Stop time tracking controls | High |
| FR-5.2 | Display current elapsed time | High |
| FR-5.3 | Support two tracking modes: Upwork-synced and Internal timer | High |
| FR-5.4 | Automatically log time when switching between Jira issues | High |
| FR-5.5 | Automatically log time when stopping tracking | High |
| FR-5.6 | Enforce minimum time thresholds before logging (5 min on stop, 10 min on change) | Medium |
| FR-5.7 | Sync memo text between overlay and Upwork | High |
| FR-5.8 | Support memo-only mode (no Jira issue selected) | Medium |
| FR-5.9 | Auto-pause after configurable inactivity period | Low |
| FR-5.10 | Show worklog input dialog for comments and remaining estimate | Medium |

### FR-6: Time Log Export

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-6.1 | Export time entries to Excel file (TimeLog.xlsx) | Medium |
| FR-6.2 | Organize entries by week in Excel worksheets | Medium |
| FR-6.3 | Allow configurable log directory | Medium |
| FR-6.4 | Display time log in a popup window | Low |
| FR-6.5 | Support disabling time log export | Low |

### FR-7: Timezone Display

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-7.1 | Display multiple timezone clocks in overlay | Medium |
| FR-7.2 | Allow adding/removing timezone displays | Medium |
| FR-7.3 | Support custom captions for timezones (e.g., "PST", "Client Time") | Medium |
| FR-7.4 | Persist timezone configuration | Medium |

### FR-8: Settings Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-8.1 | Provide settings window for configuration | Medium |
| FR-8.2 | Persist all settings between application restarts | High |
| FR-8.3 | Confirm before closing settings with unsaved changes | Low |
| FR-8.4 | Allow resetting to default settings | Low |

### FR-9: Notifications

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-9.1 | Display Windows toast notifications for successful operations | Medium |
| FR-9.2 | Display error notifications for failed operations | Medium |

---

## Non-Functional Requirements

### NFR-1: Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-1.1 | Application startup time | not delayed by network calls |
| NFR-1.2 | Timer update latency | < 100ms |
| NFR-1.3 | Autocomplete response time | < 200ms (cached) |

### NFR-2: Reliability

| ID | Requirement |
|----|-------------|
| NFR-2.1 | Handle Upwork application crashes gracefully |
| NFR-2.2 | Handle network disconnections without crashing |
| NFR-2.3 | Recover from OAuth token expiration automatically |
| NFR-2.4 | Persist time data to avoid loss on crash |

### NFR-3: Usability

| ID | Requirement |
|----|-------------|
| NFR-3.1 | Single-click access to primary actions (play/pause) |
| NFR-3.2 | Visual feedback for all connection states |
| NFR-3.3 | Non-intrusive overlay (minimal screen real estate) |
| NFR-3.4 | Keyboard-friendly issue search |
| NFR-3.5 | Clear error messages for failures |

### NFR-4: Security

| ID | Requirement |
|----|-------------|
| NFR-4.1 | Use OAuth 2.0 for Jira (no password storage) |
| NFR-4.2 | Store OAuth tokens securely in user settings |
| NFR-4.3 | Support token refresh without re-authentication |
| NFR-4.4 | Allow explicit token revocation (disconnect) |

---

## Integration Dependencies

### Upwork Time Tracker
- **Type**: Desktop application (Windows only)
- **Integration Method**: UI Automation (FlaUI/UIA3)
- **Requirements**: Application must be launched with `--enable-features=UiaProvider` flag
- **Limitations**: Windows only, requires visible UI elements

### Atlassian Jira Cloud
- **Type**: Cloud SaaS
- **Integration Method**: REST API with OAuth 2.0
- **Requirements**: User must create OAuth 2.0 app in Atlassian Developer Console
- **Scopes**: `read:jira-work`, `write:jira-work`, `offline_access`

### Deel
- **Type**: Cloud SaaS
- **Integration Method**: Embedded WebView2 browser + HTTP API
- **Requirements**: WebView2 runtime installed
- **Limitations**: Browser-based authentication, CSP blocks JavaScript injection

---

## User Stories

### US-1: Freelancer Tracking Time
> As a freelancer working on Upwork, I want to track my time with a single click so that I don't need to switch between multiple applications.

### US-2: Jira Work Logging
> As a developer, I want my time automatically logged to Jira when I switch tasks so that my timesheet is always accurate.

### US-3: Quick Issue Selection
> As a developer, I want to quickly search and select Jira issues from the overlay so that I can start tracking immediately.

### US-4: Multiple Client Timezones
> As a freelancer with international clients, I want to see multiple timezone clocks so that I know when clients are available.

### US-5: Deel Time Logging
> As a contractor paid through Deel, I want my hours automatically synced to Deel so that I don't have to enter them twice.

### US-6: Time Log Review
> As a freelancer, I want to export my time entries to Excel so that I can review my work and create invoices.

---

## Constraints

1. **Windows Only for Upwork**: Upwork integration requires Windows-specific UI Automation
2. **OAuth App Setup**: Users must create their own Atlassian OAuth app
3. **Upwork UIA Flag**: Upwork must be launched with special flag for automation

