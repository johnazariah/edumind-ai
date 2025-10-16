# Azure AD B2C Setup Checklist

Quick reference checklist for setting up Azure AD B2C with Google OAuth 2.0.

## ☑️ Part 1: Google Cloud Console

- [ ] Create Google Cloud project: `EduMind-AI-Auth`
- [ ] Configure OAuth consent screen:
  - [ ] App name: `EduMind.AI`
  - [ ] Authorized domains: `edumindai.b2clogin.com`, `edumindai.onmicrosoft.com`
  - [ ] Scopes: email, profile, openid
- [ ] Create OAuth 2.0 credentials:
  - [ ] Type: Web application
  - [ ] Name: `EduMind.AI Azure AD B2C`
  - [ ] Redirect URI: `https://edumindai.b2clogin.com/edumindai.onmicrosoft.com/oauth2/authresp`
- [ ] **Save Client ID**: _______________________________________________
- [ ] **Save Client Secret**: _______________________________________________

## ☑️ Part 2: Azure AD B2C Tenant

- [ ] Create B2C tenant:
  - [ ] Organization name: `EduMind.AI`
  - [ ] Domain: `edumindai.onmicrosoft.com`
  - [ ] Resource group: `rg-edumind-b2c`
- [ ] **Record Tenant ID**: _______________________________________________
- [ ] Switch to B2C directory

## ☑️ Part 3: Identity Provider

- [ ] Add Google identity provider:
  - [ ] Name: `Google`
  - [ ] Metadata URL: `https://accounts.google.com/.well-known/openid-configuration`
  - [ ] Client ID: (from Part 1)
  - [ ] Client secret: (from Part 1)
  - [ ] Scope: `openid email profile`
  - [ ] Claims mapping configured

## ☑️ Part 4: Custom Attributes

- [ ] Create `SchoolId` attribute (String)
- [ ] Create `ClassIds` attribute (String)

## ☑️ Part 5: User Flow

- [ ] Create sign-up/sign-in flow:
  - [ ] Name: `susi_google` (becomes B2C_1_susi_google)
  - [ ] Identity provider: Google selected
  - [ ] Collect attributes: Display Name, Email, Given Name, Surname, SchoolId, ClassIds
  - [ ] Return claims: All above + User Object ID
- [ ] Test user flow works

## ☑️ Part 6: App Registration

- [ ] Register application:
  - [ ] Name: `EduMind.AI API`
  - [ ] Redirect URI: `https://localhost:5001/signin-oidc`
- [ ] **Record Application (client) ID**: _______________________________________________
- [ ] Configure authentication (ID tokens enabled)
- [ ] Create client secret:
  - [ ] **Save secret value**: _______________________________________________
- [ ] Expose API:
  - [ ] URI: `https://edumindai.onmicrosoft.com/api`
  - [ ] Scope: `user_impersonation`

## ☑️ Part 7: Application Configuration

- [ ] Update appsettings.json with values
- [ ] OR set user secrets:

  ```bash
  dotnet user-secrets set "AzureAdB2C:TenantId" "<value>"
  dotnet user-secrets set "AzureAdB2C:ClientId" "<value>"
  dotnet user-secrets set "AzureAdB2C:ClientSecret" "<value>"
  ```

- [ ] Enable authentication in appsettings.Development.json

## ☑️ Part 8: Testing

- [ ] Run application
- [ ] Access Swagger UI
- [ ] Sign in with Google
- [ ] Verify JWT token contains expected claims
- [ ] Test API endpoint with token
- [ ] Verify authorization works

## ☑️ Part 9: Production Readiness (Optional)

- [ ] Submit Google app for verification
- [ ] Configure custom domain
- [ ] Enable Application Insights
- [ ] Set up monitoring alerts
- [ ] Configure MFA for admin roles
- [ ] Document all configuration

---

## Quick Values Reference

Fill in as you complete the setup:

```
Google OAuth:
  Client ID:     ________________________________
  Client Secret: ________________________________

Azure AD B2C:
  Tenant ID:     ________________________________
  Tenant Name:   edumindai
  Domain:        edumindai.onmicrosoft.com
  
  Client ID:     ________________________________
  Client Secret: ________________________________
  
  User Flow:     B2C_1_susi_google
  
  Instance:      https://edumindai.b2clogin.com/
  Authority:     https://edumindai.b2clogin.com/edumindai.onmicrosoft.com/B2C_1_susi_google
```

---

## Need Help?

See detailed instructions in [AZURE_AD_B2C_SETUP_GUIDE.md](./AZURE_AD_B2C_SETUP_GUIDE.md)
