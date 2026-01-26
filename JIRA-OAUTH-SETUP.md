# Jira OAuth 2.0 App Setup Guide

This guide walks you through creating a Jira OAuth 2.0 app to use with UpworkJiraTracker.

## Step 1: Create an OAuth 2.0 Integration

1. Go to the [Atlassian Developer Console](https://developer.atlassian.com/console/myapps/)
2. Click **Create** and select **OAuth 2.0 integration**
3. Give your app a name (e.g., "UpworkJiraTracker")

## Step 2: Configure Permissions

1. In your app settings, go to **Permissions**
2. Click **Add** for "Jira API"
3. Click **Configure**
4. Add the following scopes:
   - `read:jira-work` - Read Jira issues and worklogs
   - `write:jira-work` - Create worklogs
5. Click **Save**

## Step 3: Configure Authorization

1. Go to **Authorization** in the app settings
2. Under **OAuth 2.0 (3LO)**, click **Add** for Callback URL
3. Enter: `http://localhost:8080/callback`
4. Click **Save changes**

## Step 4: Get Your Credentials

1. Go to **Settings** in the app settings
2. You'll see your **Client ID** and **Client Secret**
3. Copy these values - you'll need them when connecting to Jira in the app

## Step 5: Connect in UpworkJiraTracker

1. Open UpworkJiraTracker
2. Click the overlay to open Settings
3. Click **Connect to Jira**
4. When prompted, enter:
   - **Client ID** from Step 4
   - **Client Secret** from Step 4
5. Click **Connect**
6. Your browser will open for OAuth authorization
7. Approve the connection
8. You're connected!

## Security Notes

- Your Client ID and Client Secret are stored locally on your device
- They are never transmitted anywhere except to Atlassian's OAuth servers
- The app only has access to Jira issues where you're the assignee or reporter
- You can disconnect and remove all stored credentials at any time

## Publishing as a Public App

### Option 1: User-Specific OAuth Apps (Recommended)

Each user creates their own OAuth app following this guide. This is the most secure approach and doesn't require you to distribute credentials.

**Pros:**
- No shared secrets
- Each installation is isolated
- Users have full control

**Cons:**
- Requires users to create their own OAuth app (5-10 minutes)

### Option 2: Shared OAuth App

You create a single OAuth app and distribute the Client ID and Client Secret.

**Pros:**
- Easier setup for users
- No OAuth app creation required

**Cons:**
- Client Secret must be distributed (though it's only used for OAuth, not direct API access)
- All users share the same OAuth app
- If secret is compromised, all installations are affected

**How to share safely:**
- Document credentials in setup guide (not in source code)
- Distribute via secure channels
- Monitor OAuth app usage in Atlassian console
- Rotate credentials periodically

### Option 3: Backend OAuth Proxy (Enterprise)

Create a backend service that handles OAuth on behalf of users.

**Pros:**
- No credential distribution
- Centralized control

**Cons:**
- Requires hosting infrastructure
- More complex architecture
- Your service becomes a proxy for user data

## Recommended Approach for Open Source

For an open-source project published on GitHub:

1. **Keep credentials out of source code** ✓ (Already done)
2. **Provide this setup guide** ✓
3. **Let users create their own OAuth apps** (Option 1)
4. **Include privacy policy** ✓ (See PRIVACY-POLICY.md)

This way, your code can be fully public while maintaining security and privacy for all users.
