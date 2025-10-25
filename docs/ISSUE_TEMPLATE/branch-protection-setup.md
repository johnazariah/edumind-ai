---
name: Configure Branch Protection Rules
about: Checklist for setting up branch protection on main branch
title: '[SETUP] Configure Branch Protection Rules for Main Branch'
labels: ['devops', 'setup', 'high-priority']
assignees: ''
---

## Objective

Configure GitHub branch protection rules to enforce integration tests on all PRs before merging to main.

## Prerequisites

- [ ] Repository admin access
- [ ] CI/CD pipeline is working (check latest workflow run)
- [ ] Integration tests passing locally

## Branch Protection Configuration

### Step 1: Navigate to Settings

1. Go to repository: <https://github.com/johnazariah/edumind-ai>
2. Click **Settings** (top right)
3. Click **Branches** (left sidebar)
4. Click **Add branch protection rule**

### Step 2: Configure Main Branch Protection

**Branch name pattern:** `main`

#### Required Status Checks

- [x] ✅ Require status checks to pass before merging
- [x] ✅ Require branches to be up to date before merging

**Select these required status checks:**

- [ ] `build-and-test`
- [ ] `code-quality`
- [ ] `build-matrix (ubuntu-latest)`
- [ ] `build-matrix (windows-latest)`
- [ ] `build-matrix (macos-latest)`

> **Note:** These job names must match exactly. Run a test PR to see the exact names in the "Checks" tab.

#### Pull Request Requirements

- [x] ✅ Require a pull request before merging
- [x] ✅ Require approvals: **1**
- [x] ✅ Dismiss stale pull request approvals when new commits are pushed
- [ ] Require review from Code Owners (optional)

#### Additional Settings

- [x] ✅ Require conversation resolution before merging
- [x] ✅ Do not allow bypassing the above settings
- [x] ✅ Restrict who can push to matching branches
  - **Allow:** Repository administrators, Maintainers only

#### Restrictions

- [x] ⚠️ **Allow force pushes:** DISABLED
- [x] ⚠️ **Allow deletions:** DISABLED

### Step 3: Save and Verify

- [ ] Click **Create** or **Save changes**
- [ ] Verify rule appears in branch protection list
- [ ] Test with a new PR to ensure checks are required

## Verification Checklist

### Test the Protection

- [ ] Create a test branch: `git checkout -b test/branch-protection`
- [ ] Make a trivial change and push
- [ ] Create a PR to main
- [ ] Verify you see "Required" labels next to status checks
- [ ] Verify merge button is disabled until checks pass
- [ ] Verify merge button is disabled until 1 approval received
- [ ] Close the test PR (do not merge)

### Expected Behavior

✅ **Should work:**

- Creating PRs to main
- Pushing to feature branches
- Merging PRs after checks pass and approval
- Viewing status check results

❌ **Should be blocked:**

- Direct push to main (bypassing PR)
- Merging PR with failing tests
- Merging PR without approval
- Force pushing to main
- Deleting main branch

## Troubleshooting

### Status checks not appearing

**Cause:** Job names don't match or CI hasn't run yet

**Fix:**

1. Push a commit to trigger CI
2. Check workflow file for exact job names: `.github/workflows/ci.yml`
3. Update required status checks to match exact names

### Can't merge despite passing tests

**Possible causes:**

- Branch not up to date with main
- Missing approval
- Unresolved conversations

**Fix:**

1. Update branch: `git pull origin main`
2. Push: `git push`
3. Request review if not approved
4. Resolve all conversations

### Admin can still push to main

**Expected:** Admins can bypass rules by default

**Fix:**

- Enable "Do not allow bypassing the above settings"
- Or accept that admins have emergency access

## Documentation

- [x] Read full guide: [`docs/PR_INTEGRATION_TESTS.md`](../docs/PR_INTEGRATION_TESTS.md)
- [x] Review testing strategy: [`docs/TESTING_STRATEGY.md`](../docs/TESTING_STRATEGY.md)
- [x] Check CI/CD workflow: [`.github/workflows/ci.yml`](../.github/workflows/ci.yml)

## Alternative: GitHub CLI Method

If you prefer command-line setup:

```bash
gh api repos/johnazariah/edumind-ai/branches/main/protection \
  --method PUT \
  --field required_status_checks='{"strict":true,"contexts":["build-and-test","code-quality","build-matrix (ubuntu-latest)","build-matrix (windows-latest)","build-matrix (macos-latest)"]}' \
  --field enforce_admins=true \
  --field required_pull_request_reviews='{"required_approving_review_count":1,"dismiss_stale_reviews":true}' \
  --field restrictions=null
```

## Success Criteria

- [x] Branch protection rule created for `main`
- [x] All 5 status checks required
- [x] 1 approval required
- [x] Test PR demonstrates checks are enforced
- [x] Team notified of new workflow

## Timeline

**Target completion:** Within 24 hours of this issue creation

**Assigned to:** Repository administrator

## Related Issues

- Closes #(issue number if applicable)

## Notes

- Integration tests now run on **every PR** (~3-5 minutes)
- Total CI/CD time: ~8-10 minutes per PR
- OLLAMA tests run separately (manual/nightly) to avoid slowing down PRs
- All tests use `LLM_PROVIDER=Stub` in CI for speed

---

**Created:** October 15, 2025  
**Priority:** High  
**Type:** DevOps Setup
