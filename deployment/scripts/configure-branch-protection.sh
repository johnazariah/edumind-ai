#!/bin/bash

# Script to configure branch protection rules for the main branch
# Requires: GitHub CLI (gh) authenticated with admin permissions

set -e

REPO="johnazariah/edumind-ai"
BRANCH="main"

echo "================================================"
echo "Configuring Branch Protection for: $REPO/$BRANCH"
echo "================================================"
echo ""

# Check if gh is installed and authenticated
if ! command -v gh &> /dev/null; then
    echo "❌ Error: GitHub CLI (gh) is not installed"
    echo "Install: https://cli.github.com/"
    exit 1
fi

if ! gh auth status &> /dev/null; then
    echo "❌ Error: Not authenticated with GitHub CLI"
    echo "Run: gh auth login"
    exit 1
fi

echo "✅ GitHub CLI is installed and authenticated"
echo ""

# Get current user for verification
CURRENT_USER=$(gh api user -q .login)
echo "📋 Authenticated as: $CURRENT_USER"
echo ""

echo "🔧 Configuring branch protection rules..."
echo ""

# Note: GitHub's API requires specific format for branch protection
# We'll use the REST API directly via gh api

# Create the protection configuration
gh api \
  --method PUT \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  "/repos/$REPO/branches/$BRANCH/protection" \
  -f "required_status_checks[strict]=true" \
  -f "required_status_checks[contexts][]=build-and-test" \
  -f "required_status_checks[contexts][]=code-quality" \
  -f "required_status_checks[contexts][]=build-matrix (ubuntu-latest)" \
  -f "required_status_checks[contexts][]=build-matrix (windows-latest)" \
  -f "required_status_checks[contexts][]=build-matrix (macos-latest)" \
  -f "enforce_admins=true" \
  -f "required_pull_request_reviews[dismiss_stale_reviews]=true" \
  -f "required_pull_request_reviews[require_code_owner_reviews]=false" \
  -f "required_pull_request_reviews[required_approving_review_count]=1" \
  -f "restrictions=null" \
  -f "required_linear_history=true" \
  -f "allow_force_pushes=false" \
  -f "allow_deletions=false" \
  -f "block_creations=false" \
  -f "required_conversation_resolution=true" \
  -f "lock_branch=false" \
  -f "allow_fork_syncing=true"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Branch protection rules configured successfully!"
    echo ""
    echo "📋 Summary of protection rules:"
    echo "   - Require pull request before merging"
    echo "   - Require 1 approval"
    echo "   - Dismiss stale reviews on new commits"
    echo "   - Require status checks to pass:"
    echo "     • build-and-test"
    echo "     • code-quality"
    echo "     • build-matrix (ubuntu-latest)"
    echo "     • build-matrix (windows-latest)"
    echo "     • build-matrix (macos-latest)"
    echo "   - Require conversation resolution"
    echo "   - Require linear history"
    echo "   - Enforce for administrators"
    echo "   - Block force pushes"
    echo "   - Block branch deletion"
    echo ""
    echo "🔗 View rules: https://github.com/$REPO/settings/branches"
    echo ""
else
    echo ""
    echo "❌ Failed to configure branch protection"
    echo ""
    echo "Manual configuration required:"
    echo "1. Go to: https://github.com/$REPO/settings/branches"
    echo "2. Click 'Add branch protection rule'"
    echo "3. Follow: .github/ISSUE_TEMPLATE/branch-protection-setup.md"
    exit 1
fi

# Verify the configuration
echo "🔍 Verifying configuration..."
echo ""

gh api \
  -H "Accept: application/vnd.github+json" \
  -H "X-GitHub-Api-Version: 2022-11-28" \
  "/repos/$REPO/branches/$BRANCH/protection" \
  --jq '.required_status_checks.contexts'

echo ""
echo "✅ Branch protection is active!"
echo ""
echo "📝 Next steps:"
echo "1. Create a test PR to verify protection works"
echo "2. Ensure CI/CD pipeline is working"
echo "3. Notify team of new workflow"
echo ""
echo "For troubleshooting, see: docs/PR_INTEGRATION_TESTS.md"
