#!/bin/bash
# Quick setup script for Google OAuth 2.0 credentials
# This script guides you through the Google Cloud Console setup

set -e

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔐 Google OAuth 2.0 Setup for Azure AD B2C"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

B2C_TENANT_NAME="${1:-edumindai}"
B2C_REDIRECT_URI="https://${B2C_TENANT_NAME}.b2clogin.com/${B2C_TENANT_NAME}.onmicrosoft.com/oauth2/authresp"

echo "B2C Tenant: ${B2C_TENANT_NAME}.onmicrosoft.com"
echo "Redirect URI: $B2C_REDIRECT_URI"
echo ""

echo "📋 Follow these steps in Google Cloud Console:"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Step 1: Create Google Cloud Project"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "1. Open: https://console.cloud.google.com/"
echo "2. Click 'Select a project' → 'New Project'"
echo "3. Project name: EduMind-AI-Auth"
echo "4. Click 'Create'"
echo "5. Wait ~30 seconds for creation"
echo ""
read -p "Press Enter when project is created..."
echo ""

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Step 2: Configure OAuth Consent Screen"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "1. Go to: APIs & Services → OAuth consent screen"
echo "2. User Type: External"
echo "3. Click 'Create'"
echo "4. Fill in App information:"
echo "   App name: EduMind.AI"
echo "   User support email: (your email)"
echo "   Authorized domains:"
echo "     - ${B2C_TENANT_NAME}.b2clogin.com"
echo "     - ${B2C_TENANT_NAME}.onmicrosoft.com"
echo "   Developer contact: (your email)"
echo "5. Click 'Save and Continue'"
echo "6. Scopes: Add these scopes:"
echo "   - .../auth/userinfo.email"
echo "   - .../auth/userinfo.profile"
echo "   - openid"
echo "7. Click 'Save and Continue'"
echo "8. Test users (optional): Add your test email addresses"
echo "9. Click 'Save and Continue'"
echo "10. Review and click 'Back to Dashboard'"
echo ""
read -p "Press Enter when OAuth consent screen is configured..."
echo ""

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Step 3: Create OAuth 2.0 Credentials"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "1. Go to: APIs & Services → Credentials"
echo "2. Click 'Create Credentials' → 'OAuth client ID'"
echo "3. Application type: Web application"
echo "4. Name: EduMind.AI Azure AD B2C"
echo "5. Authorized redirect URIs:"
echo "   Click '+ Add URI' and paste:"
echo ""
echo "   $B2C_REDIRECT_URI"
echo ""
echo "   ⚠️  IMPORTANT: Copy this EXACT URI (no trailing slash)"
echo ""
echo "6. Click 'Create'"
echo "7. Copy the Client ID and Client Secret from the popup"
echo ""
read -p "Press Enter when credentials are created..."
echo ""

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📝 Save Your Credentials"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
read -p "Paste your Google Client ID: " GOOGLE_CLIENT_ID
read -s -p "Paste your Google Client Secret: " GOOGLE_CLIENT_SECRET
echo ""
echo ""

# Save to file
CREDENTIALS_FILE="$(dirname "$0")/../../.credentials/google-oauth.env"
mkdir -p "$(dirname "$CREDENTIALS_FILE")"

cat > "$CREDENTIALS_FILE" <<EOF
# Google OAuth 2.0 Credentials
# Generated: $(date)
# DO NOT COMMIT THIS FILE TO GIT

GOOGLE_CLIENT_ID=$GOOGLE_CLIENT_ID
GOOGLE_CLIENT_SECRET=$GOOGLE_CLIENT_SECRET
B2C_TENANT_NAME=$B2C_TENANT_NAME
B2C_REDIRECT_URI=$B2C_REDIRECT_URI
EOF

chmod 600 "$CREDENTIALS_FILE"

echo "✅ Credentials saved to: $CREDENTIALS_FILE"
echo ""

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔒 Security Reminder"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "- Credentials saved with restrictive permissions (600)"
echo "- Add .credentials/ to .gitignore"
echo "- For production, store in Azure Key Vault"
echo ""

# Add to .gitignore if not already there
GITIGNORE="$(dirname "$0")/../../.gitignore"
if ! grep -q ".credentials/" "$GITIGNORE" 2>/dev/null; then
    echo "" >> "$GITIGNORE"
    echo "# OAuth credentials (do not commit)" >> "$GITIGNORE"
    echo ".credentials/" >> "$GITIGNORE"
    echo "✅ Added .credentials/ to .gitignore"
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ Google OAuth Setup Complete!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "Next steps:"
echo "1. Run the main deployment script:"
echo "   ./deployment/scripts/deploy-infrastructure.sh dev"
echo ""
echo "2. Or manually use these credentials in Azure AD B2C:"
echo "   Client ID: $GOOGLE_CLIENT_ID"
echo "   Client Secret: [saved in $CREDENTIALS_FILE]"
echo ""
