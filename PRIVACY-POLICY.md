# Privacy Policy for UpworkJiraTracker

**Last Updated:** January 26, 2026

## Overview

UpworkJiraTracker is a desktop application that helps users track time across Upwork and Jira. This privacy policy describes how the application handles data.

## Data Collection

**UpworkJiraTracker does NOT collect, transmit, or share any personal information.**

The application operates entirely locally on your device and does not send any data to external servers except when you explicitly authorize it to connect to Jira's official API.

## Data Storage

All data is stored locally on your device in the following locations:

- **Application Settings**: Stored in your Windows user profile (AppData\Local)
  - Window size and position preferences
  - Custom background color
  - Timezone preferences
  - Log directory path
  - Jira OAuth credentials (Client ID and Client Secret)
  - Jira OAuth tokens (access token, refresh token, cloud ID)
  - Recent Jira issue keys (for quick access)

- **Optional Log Files**: If enabled, activity logs are stored in your configured log directory
  - Window position changes
  - Time tracking events
  - No personal information is logged beyond what you explicitly enter into the application

## Third-Party Services

### Jira Integration

When you connect to Jira:
- You provide your own OAuth 2.0 app credentials (Client ID and Client Secret)
- The application uses the official Atlassian OAuth 2.0 flow
- OAuth tokens are stored locally on your device
- The application communicates directly with Atlassian's API servers
- No data is sent to any third party besides Atlassian
- All communication uses HTTPS encryption

For Jira's privacy policy, please visit: https://www.atlassian.com/legal/privacy-policy

### Upwork Integration

When Upwork integration is enabled:
- The application uses UI automation to interact with the Upwork desktop application
- No data is transmitted over the network for Upwork integration
- All automation happens locally on your device
- The application only reads the timer status and weekly total hours displayed in the Upwork UI
- No Upwork credentials or personal data are stored by this application

## Data Security

- All sensitive data (OAuth tokens, credentials) is stored in encrypted Windows user settings
- No telemetry, analytics, or crash reporting is collected
- The application does not connect to any servers other than Atlassian's official Jira API when explicitly authorized by you
- Your Jira OAuth credentials are never transmitted anywhere except Atlassian's OAuth servers

## Your Rights

Since all data is stored locally on your device:
- You have full control over all data
- You can delete all application data by uninstalling the application and removing the settings folder
- Settings location: `%LOCALAPPDATA%\UpworkJiraTracker`

## Open Source

This application is open source. You can review the source code to verify these privacy practices at:
https://github.com/[your-username]/UpworkJiraTracker

## Changes to This Policy

We may update this privacy policy from time to time. Changes will be reflected in the repository and the "Last Updated" date at the top of this document.

## Contact

For questions or concerns about privacy, please open an issue on the GitHub repository.

## Consent

By using UpworkJiraTracker, you consent to this privacy policy.
