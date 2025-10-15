#!/bin/bash
# Script to configure branch protection rules for the main branch
# Requires: GitHub CLI (gh) authenticated with admin permissions

set -e

REPO="johnazariah/edumind-ai"
BRANCH="main"

echo "================================================"
echo "GitHub Branch Protection Setup"
echo "================================================"
echo "Repository: $REPO"
echo "Branch: $BRANCH"
echo ""

# =============================================
# Step 1: Check GitHub CLI Installation
# =============================================
echo "🔍 Checking GitHub CLI installation..."

if ! command -v gh &> /dev/null; then
    echo "❌ ERROR: GitHub CLI (gh) is not installed"
    echo ""
    echo "📦 To install GitHub CLI:"
    echo ""
    echo "For Debian/Ubuntu (this dev container):"
    echo "  curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg"
    echo "  sudo chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg"
    echo "  echo \"deb [arch=\$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main\" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null"
    echo "  sudo apt update"
    echo "  sudo apt install gh -y"
    echo ""
    echo "For macOS:"
    echo "  brew install gh"
    echo ""
    echo "For Windows:"
    echo "  winget install --id GitHub.cli"
    echo ""
    echo "Or see: https://github.com/cli/cli#installation"
    exit 1
fi

echo "✅ GitHub CLI is installed ($(gh --version | head -n1))"
echo ""

# =============================================
# Step 2: Check Authentication
# =============================================
echo "🔐 Checking GitHub authentication..."

if ! gh auth status &> /dev/null; then
    echo "⚠️  You are not authenticated with GitHub CLI"
    echo ""
    echo "🔑 To authenticate, choose one of these methods:"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "Option 1: Browser-based authentication (RECOMMENDED)"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "Run the following command and follow the prompts:"
    echo ""
    echo "  gh auth login"
    echo ""
    echo "When prompted:"
    echo "  - What account? → GitHub.com"
    echo "  - What is your preferred protocol? → HTTPS"
    echo "  - Authenticate Git with credentials? → Yes"
    echo "  - How would you like to authenticate? → Login with a web browser"
    echo "  - Copy the one-time code and press Enter"
    echo "  - Paste the code in your browser and authorize"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "Option 2: Personal Access Token (for automation)"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "1. Create a Personal Access Token:"
    echo "   https://github.com/settings/tokens/new?scopes=repo,admin:org"
    echo ""
    echo "   Required scopes:"
    echo "   ✓ repo (Full control of private repositories)"
    echo "   ✓ admin:org > read:org (Read org and team membership)"
    echo ""
    echo "2. Save the token to a file (e.g., token.txt)"
    echo ""
    echo "3. Authenticate with:"
    echo "   echo 'YOUR_TOKEN' | gh auth login --with-token"
    echo ""
    echo "   Or:"
    echo "   gh auth login --with-token < token.txt"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "After authentication, run this script again:"
    echo "  ./deployment/scripts/configure-branch-protection.sh"
    echo ""
    exit 1
fi

echo "✅ GitHub CLI is authenticated"
echo ""

# Get current user and permissions
CURRENT_USER=$(gh api user -q .login)
echo "👤 Authenticated as: $CURRENT_USER"
echo ""

# Check if user has admin access to the repo
echo "🔍 Checking repository permissions..."
REPO_PERMS=$(gh api "repos/$REPO" -q .permissions)
HAS_ADMIN=$(echo "$REPO_PERMS" | grep -o '"admin":[^,}]*' | cut -d: -f2 | tr -d ' ')

if [ "$HAS_ADMIN" != "true" ]; then
    echo "❌ ERROR: You do not have admin access to $REPO"
    echo ""
    echo "Branch protection requires admin permissions."
    echo "Please contact the repository owner to:"
    echo "  1. Grant you admin access, OR"
    echo "  2. Run this script with an admin account"
    echo ""
    exit 1
fi

echo "✅ You have admin access to $REPO"
echo ""

# =============================================
# Step 3: Configure Branch Protection
# =============================================
echo "🔧 Configuring branch protection rules..."
echo ""

# Create a temporary JSON file for the protection configuration
PROTECTION_CONFIG=$(cat <<EOF
{
  "required_status_checks": {
    "strict": true,
    "contexts": [
      "build-and-test",
      "code-quality",
      "build-matrix (ubuntu-latest)",
      "build-matrix (windows-latest)",
      "build-matrix (macos-latest)"
    ]
  },
  "enforce_admins": true,
  "required_pull_request_reviews": {
    "dismiss_stale_reviews": true,
    "require_code_owner_reviews": false,
    "required_approving_review_count": 1,
    "require_last_push_approval": false
  },
  "restrictions": null,
  "required_linear_history": true,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "block_creations": false,
  "required_conversation_resolution": true,
  "lock_branch": false,
  "allow_fork_syncing": true
}
EOF
)

# Apply the protection using gh api
echo "$PROTECTION_CONFIG" | gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  "repos/$REPO/branches/$BRANCH/protection" \
  --input -

if [ $? -eq 0 ]; then
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "✅ Branch protection rules configured successfully!"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "📋 Protection rules applied to '$BRANCH' branch:"
    echo ""
    echo "✓ Require pull request before merging"
    echo "  └─ Require 1 approval"
    echo "  └─ Dismiss stale reviews on new commits"
    echo ""
    echo "✓ Require status checks to pass:"
    echo "  └─ build-and-test"
    echo "  └─ code-quality"
    echo "  └─ build-matrix (ubuntu-latest)"
    echo "  └─ build-matrix (windows-latest)"
    echo "  └─ build-matrix (macos-latest)"
    echo ""
    echo "✓ Require conversation resolution before merging"
    echo "✓ Require linear history"
    echo "✓ Do not allow bypassing the above settings"
    echo "✓ Enforce for administrators"
    echo "✓ Block force pushes"
    echo "✓ Block branch deletion"
    echo ""
    echo "🔗 View rules at:"
    echo "   https://github.com/$REPO/settings/branches"
    echo ""
    
    # Verify the configuration
    echo "🔍 Verifying configuration..."
    gh api "repos/$REPO/branches/$BRANCH/protection" > /dev/null 2>&1
    
    if [ $? -eq 0 ]; then
        echo "✅ Branch protection is active and verified"
        echo ""
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo "🎉 Setup complete!"
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo ""
        echo "Next steps:"
        echo "1. Create a test PR to verify the protection works"
        echo "2. Verify that merge is blocked until all checks pass"
        echo "3. See: docs/PR_INTEGRATION_TESTS.md for full guide"
        echo ""
    else
        echo "⚠️  Configuration applied but verification failed"
        echo "Please check the branch protection settings manually"
        echo ""
    fi
else
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "❌ Failed to configure branch protection"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "This could be due to:"
    echo "  - Insufficient permissions (need admin access)"
    echo "  - Network connectivity issues"
    echo "  - GitHub API rate limits"
    echo ""
    echo "📝 Manual configuration required:"
    echo "1. Go to: https://github.com/$REPO/settings/branches"
    echo "2. Click 'Add branch protection rule'"
    echo "3. Follow: .github/ISSUE_TEMPLATE/branch-protection-setup.md"
    echo ""
    echo "Or try running this script again after checking your permissions"
    echo ""
    exit 1
fi
