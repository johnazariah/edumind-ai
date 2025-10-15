#!/bin/bash
# One-command setup for GitHub branch protection
# This script guides you through authentication and configuration

set -e

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🚀 EduMind.AI Branch Protection Setup"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "This script will:"
echo "  1. Verify GitHub CLI is installed ✓ (already done)"
echo "  2. Help you authenticate with GitHub"
echo "  3. Configure branch protection for 'main' branch"
echo ""

# Check if authenticated
if gh auth status &> /dev/null; then
    CURRENT_USER=$(gh api user -q .login)
    echo "✅ Already authenticated as: $CURRENT_USER"
    echo ""
    read -p "Continue with this account? (y/n) " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Please logout first: gh auth logout"
        exit 1
    fi
else
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "🔐 Step 1: Authenticate with GitHub"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "Choose authentication method:"
    echo ""
    echo "  1) Browser login (easiest, recommended)"
    echo "  2) Personal Access Token"
    echo "  3) Exit and authenticate manually"
    echo ""
    read -p "Your choice (1-3): " choice
    echo ""
    
    case $choice in
        1)
            echo "🌐 Starting browser authentication..."
            echo ""
            echo "You'll need to:"
            echo "  1. Copy the one-time code that will be displayed"
            echo "  2. Press Enter to open the browser"
            echo "  3. Paste the code in your browser"
            echo "  4. Click 'Authorize' to complete authentication"
            echo ""
            read -p "Ready? Press Enter to continue..."
            echo ""
            
            # Run gh auth login with pre-selected options
            gh auth login <<EOF
GitHub.com
HTTPS
Y
Login with a web browser
EOF
            ;;
        2)
            echo "🔑 Personal Access Token authentication"
            echo ""
            echo "You'll need a token with these scopes:"
            echo "  - repo (Full control of private repositories)"
            echo "  - read:org (Read org and team membership)"
            echo ""
            echo "Create a token at:"
            echo "  https://github.com/settings/tokens/new?scopes=repo,admin:org"
            echo ""
            read -p "Press Enter when you have your token ready..."
            echo ""
            echo "Paste your token (it won't be displayed):"
            read -s token
            echo ""
            
            if [ -z "$token" ]; then
                echo "❌ No token provided"
                exit 1
            fi
            
            echo "$token" | gh auth login --with-token
            
            if [ $? -eq 0 ]; then
                echo "✅ Authentication successful"
            else
                echo "❌ Authentication failed"
                exit 1
            fi
            ;;
        3)
            echo "Manual authentication:"
            echo "  Run: gh auth login"
            echo ""
            echo "Then run this script again:"
            echo "  ./deployment/scripts/easy-setup.sh"
            echo ""
            exit 0
            ;;
        *)
            echo "Invalid choice"
            exit 1
            ;;
    esac
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔧 Step 2: Configure Branch Protection"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Run the branch protection setup script
./deployment/scripts/setup-branch-protection.sh

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🎉 All Done!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "What's protected:"
echo "  - main branch requires PR review (1 approval)"
echo "  - All CI/CD checks must pass before merge"
echo "  - Integration tests run on every PR"
echo ""
echo "Next steps:"
echo "  1. Create a test PR to verify protection works"
echo "  2. Check branch settings:"
echo "     https://github.com/johnazariah/edumind-ai/settings/branches"
echo "  3. Read the full guide:"
echo "     docs/PR_INTEGRATION_TESTS.md"
echo ""
