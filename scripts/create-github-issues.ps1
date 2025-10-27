<#
.SYNOPSIS
    Creates GitHub issues from story folders in .github/story/
    
.DESCRIPTION
    Reads each story folder, extracts title and priority from issue.md,
    and creates a GitHub issue with the full content as the body.
    
.PARAMETER DryRun
    If specified, shows what would be created without actually creating issues
    
.EXAMPLE
    .\scripts\create-github-issues.ps1
    
.EXAMPLE
    .\scripts\create-github-issues.ps1 -DryRun
#>

param(
    [switch]$DryRun
)

# Ensure we're in the repo root
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

# Check if gh CLI is installed
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is not installed. Please install it from https://cli.github.com/"
    exit 1
}

# Check if authenticated
$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Error "Not authenticated with GitHub CLI. Run 'gh auth login' first."
    exit 1
}

# Get all story folders
$storyPath = Join-Path $repoRoot ".github\story"
$storyFolders = Get-ChildItem -Path $storyPath -Directory | Where-Object { $_.Name -match '^p\d+-\d+' } | Sort-Object Name

Write-Host "Found $($storyFolders.Count) story folders" -ForegroundColor Cyan
Write-Host ""

$created = 0
$skipped = 0
$errors = 0

foreach ($folder in $storyFolders) {
    $issuePath = Join-Path $folder.FullName "issue.md"
    
    if (-not (Test-Path $issuePath)) {
        Write-Warning "Skipping $($folder.Name): No issue.md found"
        $skipped++
        continue
    }
    
    # Read the issue content
    $content = Get-Content -Path $issuePath -Raw
    
    # Extract title from folder name (e.g., "p0-001-fix-integration-test-serialization-bug")
    # Remove priority prefix and convert dashes to spaces, then title case
    $titleFromFolder = $folder.Name -replace '^p\d+-\d+-', '' -replace '-', ' '
    $title = (Get-Culture).TextInfo.ToTitleCase($titleFromFolder)
    
    # Check if GitHub issue link already exists
    if ($content -match 'GitHub Issue:\s*https://github\.com/.+/issues/\d+') {
        Write-Host "[$($folder.Name)] Skipping - GitHub issue already linked" -ForegroundColor Cyan
        $skipped++
        continue
    }
    
    # Check if issue already exists on GitHub by searching for the title
    Write-Host "[$($folder.Name)] Checking for existing issue: $title" -ForegroundColor Yellow
    $existingIssue = gh issue list --search "in:title $title" --json number,url,title --limit 1 2>&1 | ConvertFrom-Json
    
    if ($existingIssue -and $existingIssue.Count -gt 0 -and $existingIssue[0].title -eq $title) {
        # Issue exists, just add the link
        $issueUrl = $existingIssue[0].url
        Write-Host "  ✓ Found existing issue: $issueUrl" -ForegroundColor Green
        
        try {
            # Add GitHub issue link to the issue.md file
            $lines = Get-Content -Path $issuePath
            $insertIndex = -1
            
            # Look for the end of the front matter (after Priority, Status, Effort, Dependencies)
            for ($i = 0; $i -lt $lines.Count; $i++) {
                if ($lines[$i] -match '^---\s*$' -and $i -gt 5) {
                    $insertIndex = $i
                    break
                }
            }
            
            # If no --- found, insert after first blank line after header
            if ($insertIndex -eq -1) {
                for ($i = 5; $i -lt $lines.Count; $i++) {
                    if ($lines[$i] -match '^\s*$') {
                        $insertIndex = $i
                        break
                    }
                }
            }
            
            if ($insertIndex -gt 0) {
                # Insert the GitHub issue link
                $newLines = @()
                $newLines += $lines[0..($insertIndex - 1)]
                $newLines += ""
                $newLines += "**GitHub Issue:** $issueUrl"
                $newLines += ""
                if ($insertIndex -lt $lines.Count) {
                    $newLines += $lines[$insertIndex..($lines.Count - 1)]
                }
                
                Set-Content -Path $issuePath -Value $newLines -Encoding UTF8
                Write-Host "  ✓ Added GitHub issue link to $($folder.Name)/issue.md" -ForegroundColor Green
            }
            $skipped++
        } catch {
            Write-Warning "  ⚠ Could not update issue.md with GitHub link: $_"
            $errors++
        }
        Write-Host ""
        continue
    }
    
    # Extract priority
    $priority = "P2"
    if ($content -match '\*\*Priority:\*\*\s+(P\d+)') {
        $priority = $Matches[1]
    }
    
    # Map priority to label
    $labels = @()
    switch ($priority) {
        "P0" { $labels += @("priority:critical", "status:ready") }
        "P1" { $labels += @("priority:high", "status:ready") }
        "P2" { $labels += @("priority:medium", "status:backlog") }
        default { $labels += @("priority:low", "status:backlog") }
    }
    
    # Add story label
    $labels += "type:story"
    
    # Extract effort if present
    if ($content -match '\*\*Effort:\*\*\s+(Small|Medium|Large)') {
        $effort = $Matches[1].ToLower()
        $labels += "effort:$effort"
    }
    
    $labelArgs = $labels | ForEach-Object { "--label", $_ }
    
    Write-Host "[$($folder.Name)] Creating issue: $title" -ForegroundColor Yellow
    Write-Host "  Priority: $priority | Labels: $($labels -join ', ')" -ForegroundColor Gray
    
    if ($DryRun) {
        Write-Host "  [DRY RUN] Would create issue" -ForegroundColor Magenta
        $created++
    } else {
        try {
            # Create the issue using gh CLI
            $issueUrl = gh issue create --title $title --body $content @labelArgs 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-Host "  ✓ Created: $issueUrl" -ForegroundColor Green
                
                # Add GitHub issue link to the issue.md file
                try {
                    # Find the position to insert (after the header section)
                    $lines = Get-Content -Path $issuePath
                    $insertIndex = -1
                    
                    # Look for the end of the front matter (after Priority, Status, Effort, Dependencies)
                    for ($i = 0; $i -lt $lines.Count; $i++) {
                        if ($lines[$i] -match '^---\s*$' -and $i -gt 5) {
                            $insertIndex = $i
                            break
                        }
                    }
                    
                    # If no --- found, insert after first blank line after header
                    if ($insertIndex -eq -1) {
                        for ($i = 5; $i -lt $lines.Count; $i++) {
                            if ($lines[$i] -match '^\s*$') {
                                $insertIndex = $i
                                break
                            }
                        }
                    }
                    
                    if ($insertIndex -gt 0) {
                        # Insert the GitHub issue link
                        $newLines = @()
                        $newLines += $lines[0..($insertIndex - 1)]
                        $newLines += ""
                        $newLines += "**GitHub Issue:** $issueUrl"
                        $newLines += ""
                        $newLines += $lines[$insertIndex..($lines.Count - 1)]
                        
                        Set-Content -Path $issuePath -Value $newLines -Encoding UTF8
                        Write-Host "  ✓ Added GitHub issue link to $($folder.Name)/issue.md" -ForegroundColor Green
                    }
                } catch {
                    Write-Warning "  ⚠ Could not update issue.md with GitHub link: $_"
                }
                
                $created++
            } else {
                Write-Error "  ✗ Failed to create issue: $issueUrl"
                $errors++
            }
        } catch {
            Write-Error "  ✗ Error: $_"
            $errors++
        }
    }
    
    Write-Host ""
}

# Summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Created: $created" -ForegroundColor Green
Write-Host "  Skipped: $skipped" -ForegroundColor Yellow
Write-Host "  Errors:  $errors" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Cyan

if ($DryRun) {
    Write-Host ""
    Write-Host "This was a dry run. Run without -DryRun to create issues." -ForegroundColor Magenta
}