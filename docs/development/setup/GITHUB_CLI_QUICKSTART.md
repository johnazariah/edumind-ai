# GitHub CLI Quick Start Guide

This guide shows you how to set up GitHub branch protection using the GitHub CLI from your dev container.

## Step 1: Install GitHub CLI (if needed)

GitHub CLI should already be installed in your dev container. Verify with:

```bash
gh --version
```

If not installed, run:

```bash
curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | sudo dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg
sudo chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | sudo tee /etc/apt/sources.list.d/github-cli.list > /dev/null
sudo apt update
sudo apt install gh -y
```

## Step 2: Authenticate with GitHub

### Option A: Browser-based authentication (RECOMMENDED)

This is the easiest method:

```bash
gh auth login
```

Follow the prompts:

1. **What account?** → `GitHub.com`
2. **Preferred protocol?** → `HTTPS`
3. **Authenticate Git?** → `Yes`
4. **How to authenticate?** → `Login with a web browser`
5. Copy the one-time code shown
6. Press Enter to open the browser
7. Paste the code in your browser and authorize

### Option B: Personal Access Token (for automation/scripting)

1. **Create a Personal Access Token:**
   - Go to: <https://github.com/settings/tokens/new?scopes=repo,admin:org>
   - Note: "EduMind.AI Branch Protection Setup"
   - Expiration: Choose appropriate duration
   - **Required scopes:**
     - ✓ `repo` (Full control of private repositories)
     - ✓ `admin:org` > `read:org` (Read org and team membership)
   - Click "Generate token"
   - **COPY THE TOKEN** (you won't see it again!)

2. **Authenticate using the token:**

   ```bash
   # Method 1: Direct input
   echo 'YOUR_TOKEN_HERE' | gh auth login --with-token
   
   # Method 2: From file (safer)
   echo 'YOUR_TOKEN_HERE' > /tmp/gh-token.txt
   gh auth login --with-token < /tmp/gh-token.txt
   rm /tmp/gh-token.txt  # Clean up
   ```

3. **Verify authentication:**

   ```bash
   gh auth status
   ```

   You should see:

   ```
   ✓ Logged in to github.com as YOUR_USERNAME
   ✓ Git operations for github.com configured to use https protocol.
   ✓ Token: *******************
   ```

## Step 3: Run the Branch Protection Setup Script

Once authenticated, run:

```bash
cd /workspaces/edumind-ai
./deployment/scripts/setup-branch-protection.sh
```

The script will:

1. ✅ Verify GitHub CLI is installed
2. ✅ Verify you're authenticated
3. ✅ Check you have admin access to the repo
4. ✅ Apply branch protection rules to `main`
5. ✅ Verify the configuration

Expected output:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ Branch protection rules configured successfully!
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📋 Protection rules applied to 'main' branch:

✓ Require pull request before merging
  └─ Require 1 approval
  └─ Dismiss stale reviews on new commits

✓ Require status checks to pass:
  └─ build-and-test
  └─ code-quality
  └─ build-matrix (ubuntu-latest)
  └─ build-matrix (windows-latest)
  └─ build-matrix (macos-latest)

✓ Require conversation resolution before merging
✓ Require linear history
✓ Do not allow bypassing the above settings
✓ Enforce for administrators
✓ Block force pushes
✓ Block branch deletion
```

## Step 4: Verify Branch Protection

### Option 1: Via CLI

```bash
gh api repos/johnazariah/edumind-ai/branches/main/protection | jq
```

### Option 2: Via Web UI

Visit: <https://github.com/johnazariah/edumind-ai/settings/branches>

### Option 3: Test with a PR

```bash
# Create a test branch
git checkout -b test/branch-protection
echo "# Test" >> README.md
git add README.md
git commit -m "test: verify branch protection"
git push origin test/branch-protection

# Create a PR
gh pr create --title "Test: Branch Protection" --body "Testing branch protection rules"

# Try to merge (should be blocked)
gh pr merge --auto

# You should see:
# "Pull request is not mergeable: Required status checks are not passing"
```

## Troubleshooting

### Error: "Not authenticated with GitHub CLI"

**Solution:** Run `gh auth login` and follow the authentication steps above.

### Error: "You do not have admin access"

**Cause:** Branch protection requires admin/owner permissions on the repository.

**Solution:**

1. Check your permissions: <https://github.com/johnazariah/edumind-ai/settings/access>
2. Ask the repository owner to grant you admin access
3. Or, ask the owner to run the script

### Error: "gh: command not found"

**Solution:** GitHub CLI is not installed. Follow Step 1 above.

### Error: "API rate limit exceeded"

**Cause:** Too many API calls in a short period.

**Solution:**

1. Wait 60 minutes for the rate limit to reset
2. Or authenticate (authenticated users have higher limits)
3. Check rate limit status:

   ```bash
   gh api rate_limit
   ```

### Token expired or invalid

**Solution:**

1. Revoke the old token: <https://github.com/settings/tokens>
2. Create a new token with the required scopes
3. Re-authenticate:

   ```bash
   gh auth logout
   gh auth login --with-token < new-token.txt
   ```

## Quick Reference Commands

```bash
# Check if authenticated
gh auth status

# Login
gh auth login

# Logout
gh auth logout

# View current repo settings
gh repo view johnazariah/edumind-ai

# List branch protection rules
gh api repos/johnazariah/edumind-ai/branches/main/protection

# View PRs
gh pr list

# View PR status
gh pr status

# View CI/CD workflow runs
gh run list

# View specific workflow run
gh run view <run-id>

# Watch a workflow run
gh run watch
```

## Security Best Practices

1. **Never commit tokens to git**
   - Tokens are like passwords
   - Add to .gitignore if saving to a file

2. **Use minimal scopes**
   - Only grant permissions needed for the task
   - For branch protection: `repo` and `read:org` are sufficient

3. **Set token expiration**
   - Choose shortest duration that works for you
   - Rotate tokens regularly

4. **Revoke tokens when done**
   - If using for one-time setup, revoke after use
   - Manage tokens: <https://github.com/settings/tokens>

5. **Use browser auth for interactive sessions**
   - More secure than PATs
   - Better for dev container work

## Additional Resources

- **GitHub CLI Documentation:** <https://cli.github.com/manual/>
- **GitHub Branch Protection:** <https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches>
- **GitHub API:** <https://docs.github.com/en/rest/branches/branch-protection>
- **Project Documentation:** `docs/PR_INTEGRATION_TESTS.md`
