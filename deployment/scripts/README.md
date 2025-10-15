# 🚀 Quick Setup: Branch Protection

Complete setup in **3 commands** - no manual UI navigation required!

## ⚡ Fast Track (One Command)

```bash
cd /workspaces/edumind-ai
./deployment/scripts/easy-setup.sh
```

This interactive script will guide you through authentication and configuration.

---

## 📋 Step-by-Step

### Step 1: Authenticate with GitHub

**Option A: Browser Login (Easiest)**

```bash
gh auth login
```

When prompted:

- What account? → **GitHub.com**
- Preferred protocol? → **HTTPS**
- Authenticate Git? → **Yes**
- How to authenticate? → **Login with a web browser**
- Copy the code, press Enter, paste in browser

**Option B: Personal Access Token**

1. Create token: <https://github.com/settings/tokens/new?scopes=repo,admin:org>
2. Authenticate:

   ```bash
   echo 'YOUR_TOKEN' | gh auth login --with-token
   ```

### Step 2: Run Branch Protection Setup

```bash
./deployment/scripts/setup-branch-protection.sh
```

That's it! ✅

---

## 🔍 Verify It Worked

```bash
# Check protection is active
gh api repos/johnazariah/edumind-ai/branches/main/protection

# Or view in browser
open https://github.com/johnazariah/edumind-ai/settings/branches
```

---

## 🧪 Test with a PR

```bash
# Create test branch
git checkout -b test/branch-protection
echo "# Test" >> README.md
git add README.md
git commit -m "test: verify branch protection"
git push origin test/branch-protection

# Create PR (should require approval + passing tests)
gh pr create --title "Test: Branch Protection" --body "Testing protection rules"
```

Expected behavior:

- ❌ Can't merge until all checks pass
- ❌ Can't merge without 1 approval
- ✅ Integration tests run automatically

---

## ❓ Troubleshooting

### "You are not logged in"

**Solution:** Run `gh auth login` first

### "You do not have admin access"

**Cause:** Branch protection requires admin permissions

**Solution:** Ask repository owner to:

1. Grant you admin access, OR
2. Run the setup script themselves

### "gh: command not found"

**Solution:** GitHub CLI should be pre-installed in dev container. Verify:

```bash
gh --version
```

### More Help

See detailed guide: [docs/GITHUB_CLI_QUICKSTART.md](./GITHUB_CLI_QUICKSTART.md)

---

## 📚 What Gets Protected

When you run the setup script, these rules are applied to the `main` branch:

✅ **Require Pull Request**

- Need 1 approval before merge
- Stale reviews dismissed on new commits

✅ **Require Status Checks**

- `build-and-test` must pass
- `code-quality` must pass  
- `build-matrix` (ubuntu, windows, macos) must pass
- Integration tests run on every PR

✅ **Other Protections**

- Require conversation resolution
- Require linear history
- Block force pushes
- Block branch deletion
- Enforce for administrators

---

## 🔗 Related Documentation

- **Full Integration Test Guide:** [docs/PR_INTEGRATION_TESTS.md](../../docs/PR_INTEGRATION_TESTS.md)
- **GitHub CLI Quick Start:** [docs/GITHUB_CLI_QUICKSTART.md](../../docs/GITHUB_CLI_QUICKSTART.md)
- **Manual Setup Checklist:** [.github/ISSUE_TEMPLATE/branch-protection-setup.md](../../.github/ISSUE_TEMPLATE/branch-protection-setup.md)

---

## 💡 Quick Commands Reference

```bash
# Check authentication
gh auth status

# View current repo
gh repo view

# List branch protection
gh api repos/johnazariah/edumind-ai/branches/main/protection

# Create a PR
gh pr create

# View PR status
gh pr status

# Watch CI/CD run
gh run watch
```

---

**Ready?** Just run:

```bash
./deployment/scripts/easy-setup.sh
```

🎉 That's all you need!
