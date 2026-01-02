# Manual Authentication Testing Procedures

This document describes how to manually test interactive and device code authentication methods that cannot be automated in CI.

## Interactive Browser Authentication

The `InteractiveBrowserCredentialProvider` opens a browser window for the user to sign in.

### Test Procedure

1. **Create a test console app or use the CLI:**
   ```bash
   ppds auth create --name test-interactive
   # Opens browser for sign-in
   ```

2. **Expected behavior:**
   - Browser opens automatically
   - User can sign in with their credentials
   - After successful sign-in, browser shows success message
   - CLI/app receives token and continues

3. **Verification:**
   ```bash
   ppds env list
   # Should list available environments
   ```

4. **Edge cases to test:**
   - [ ] User cancels browser sign-in
   - [ ] Browser is closed before completing sign-in
   - [ ] Network disconnection during sign-in
   - [ ] Multi-factor authentication (MFA) flow
   - [ ] Conditional Access policies

---

## Device Code Authentication

The `DeviceCodeCredentialProvider` displays a code for the user to enter at https://microsoft.com/devicelogin.

### Test Procedure

1. **Create a profile with device code:**
   ```bash
   ppds auth create --name test-device --deviceCode
   ```

2. **Expected behavior:**
   - Console displays a message like:
     ```
     To sign in, use a web browser to open the page https://microsoft.com/devicelogin 
     and enter the code ABC123XYZ to authenticate.
     ```
   - User opens the URL in any browser (can be on different device)
   - User enters the code and signs in
   - After successful sign-in, CLI/app receives token

3. **Verification:**
   ```bash
   ppds env list
   # Should list available environments
   ```

4. **Edge cases to test:**
   - [ ] Code expires (typically 15 minutes)
   - [ ] Wrong code entered
   - [ ] User cancels at device login page
   - [ ] Network disconnection
   - [ ] MFA flow via device code

---

## Test Matrix

| Auth Method | Automated CI | Manual Test | Notes |
|-------------|--------------|-------------|-------|
| Client Secret | ✅ | - | Fully automated |
| Certificate | ✅ | - | Fully automated |
| GitHub OIDC | ✅ | - | Only in GitHub Actions |
| Interactive Browser | ❌ | ✅ | Requires human interaction |
| Device Code | ❌ | ✅ | Requires human interaction |
| Managed Identity | ❌ | ✅ | Only in Azure-hosted environments |
| Azure DevOps OIDC | ❌ | ✅ | Only in Azure Pipelines |

---

## Troubleshooting

### Interactive Browser Issues

**Browser doesn't open:**
- Check default browser settings
- Try with `--forceInteractive` flag
- Check firewall/proxy blocking localhost

**"AADSTS" errors:**
- `AADSTS50011`: Reply URL mismatch - check app registration
- `AADSTS65001`: Consent required - admin must grant consent
- `AADSTS700016`: App not found - check Application ID

### Device Code Issues

**Code not accepted:**
- Ensure code is entered exactly (case-sensitive)
- Check code hasn't expired (15-minute window)
- Verify correct Microsoft account/tenant

**Polling timeout:**
- Default is 15 minutes
- User must complete sign-in within this window

---

## Local Development Setup

For local testing of interactive methods:

1. **App Registration requirements:**
   - Platform: Mobile and desktop applications
   - Redirect URI: `http://localhost` (for interactive)
   - Enable public client flows (for device code)

2. **Environment variables (optional):**
   ```powershell
   $env:DATAVERSE_URL = "https://your-org.crm.dynamics.com"
   ```

3. **Run tests:**
   ```bash
   # Interactive test
   ppds auth create --name local-test
   
   # Device code test  
   ppds auth create --name local-device --deviceCode
   
   # Verify
   ppds env list
   ppds data export --help
   ```
