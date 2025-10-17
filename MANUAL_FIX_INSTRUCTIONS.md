# Manual Fix Instructions for Deployment Pipeline

## Problem Summary

Two critical issues in `.github/workflows/deploy-azure-azd.yml`:

1. ✅ **6-hour timeout** - Already fixed in commit `ec6c219`
2. ❌ **azd installation failure** - Needs to be fixed

## What Needs to Change

Replace the `Azure/setup-azd@v1.0.0` action (appears **2 times**) with direct installation.

---

## Option 1: Use the Script (Recommended)

Run the provided script:
```bash
./apply-deployment-fixes.sh
```

This will automatically apply both fixes and show you a diff.

---

## Option 2: Manual Editing

### Location 1: Deploy Job (around line 82)

**Find this:**
```yaml
      - name: Install Azure Developer CLI
        uses: Azure/setup-azd@v1.0.0
```

**Replace with:**
```yaml
      - name: Install Azure Developer CLI
        run: |
          curl -fsSL https://aka.ms/install-azd.sh | bash
          echo "$HOME/.azd/bin" >> $GITHUB_PATH
```

### Location 2: Integration Tests Job (around line 168)

**Find this:**
```yaml
      - name: Install Azure Developer CLI
        uses: Azure/setup-azd@v1.0.0
```

**Replace with:**
```yaml
      - name: Install Azure Developer CLI
        run: |
          curl -fsSL https://aka.ms/install-azd.sh | bash
          echo "$HOME/.azd/bin" >> $GITHUB_PATH
```

---

## Verification

After making changes, verify:

```bash
# Should show 2 matches
grep -c "aka.ms/install-azd.sh" .github/workflows/deploy-azure-azd.yml

# Should show 0 matches
grep -c "Azure/setup-azd@v1.0.0" .github/workflows/deploy-azure-azd.yml

# View the diff
git diff .github/workflows/deploy-azure-azd.yml
```

---

## Commit Instructions

```bash
# Add the files
git add .github/workflows/deploy-azure-azd.yml
git add DEPLOYMENT_FIXES_SUMMARY.md
git add MANUAL_FIX_INSTRUCTIONS.md

# Commit
git commit -m "fix(ci): Replace Azure/setup-azd action with direct installation

Problem: Azure/setup-azd@v1.0.0 fails with network error accessing
azdrelease.azureedge.net

Solution: Use Microsoft's official install script via aka.ms redirect
which is more reliable and installs the latest version.

Fixes: https://github.com/johnazariah/edumind-ai/actions/runs/18595004832"

# Push
git push origin main
```

---

## Why This Fix Works

- ✅ Uses Microsoft's official installer (`https://aka.ms/install-azd.sh`)
- ✅ Bypasses the broken `azdrelease.azureedge.net` endpoint
- ✅ More reliable aka.ms redirect service  
- ✅ Automatically gets latest version
- ✅ Properly adds azd to PATH for subsequent steps

---

## Expected Results

After this fix is deployed:
- ✓ azd installation completes in ~30 seconds
- ✓ No more "ENOTFOUND azdrelease.azureedge.net" errors
- ✓ Deployment workflow can proceed to actual deployment
- ✓ Total time: ~15 minutes (down from 6 hours + failures)

---

## Monitoring

Watch the next deployment run:
```bash
# After pushing, watch the workflow
gh run list --workflow=deploy-azure-azd.yml --limit 1
gh run watch <run-id>
```

Expected to see:
- ✓ Build & test: ~5 minutes
- ✓ Deploy: ~10 minutes  
- ✓ Total: ~15 minutes
