<#
.SYNOPSIS
    Creates GitHub labels for the EduMind.AI project
    
.DESCRIPTION
    Creates standardized labels for priority, status, type, and effort tracking
    
.EXAMPLE
    .\scripts\create-github-labels.ps1
#>

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

Write-Host "Creating GitHub labels for EduMind.AI..." -ForegroundColor Cyan
Write-Host ""

# Define labels: name, color, description
$labels = @(
    # Priority labels
    @{
        name = "priority:critical"
        color = "d73a4a"
        description = "P0 - Critical blocker, must fix immediately"
    },
    @{
        name = "priority:high"
        color = "ff6b6b"
        description = "P1 - High priority, fix soon"
    },
    @{
        name = "priority:medium"
        color = "fbca04"
        description = "P2 - Medium priority, fix when possible"
    },
    @{
        name = "priority:low"
        color = "0e8a16"
        description = "P3 - Low priority, nice to have"
    },
    
    # Status labels
    @{
        name = "status:ready"
        color = "0075ca"
        description = "Ready for implementation"
    },
    @{
        name = "status:backlog"
        color = "c5def5"
        description = "In backlog, not yet prioritized"
    },
    @{
        name = "status:in-progress"
        color = "fef2c0"
        description = "Currently being worked on"
    },
    @{
        name = "status:blocked"
        color = "b60205"
        description = "Blocked by dependencies"
    },
    @{
        name = "status:review"
        color = "d876e3"
        description = "In code review"
    },
    
    # Type labels
    @{
        name = "type:story"
        color = "1d76db"
        description = "User story or feature"
    },
    @{
        name = "type:bug"
        color = "d73a4a"
        description = "Bug or defect"
    },
    @{
        name = "type:enhancement"
        color = "a2eeef"
        description = "Enhancement to existing feature"
    },
    @{
        name = "type:documentation"
        color = "0075ca"
        description = "Documentation improvement"
    },
    @{
        name = "type:infrastructure"
        color = "fbca04"
        description = "Infrastructure or deployment"
    },
    
    # Effort labels
    @{
        name = "effort:small"
        color = "c2e0c6"
        description = "Small effort (4-8 hours)"
    },
    @{
        name = "effort:medium"
        color = "bfdadc"
        description = "Medium effort (2-3 days)"
    },
    @{
        name = "effort:large"
        color = "f9d0c4"
        description = "Large effort (1-2 weeks)"
    }
)

$created = 0
$skipped = 0
$errors = 0

foreach ($label in $labels) {
    Write-Host "Creating label: $($label.name)" -ForegroundColor Yellow
    
    try {
        $result = gh label create $label.name --color $label.color --description $label.description 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✓ Created" -ForegroundColor Green
            $created++
        } else {
            if ($result -match "already exists") {
                Write-Host "  ⚠ Already exists" -ForegroundColor Yellow
                $skipped++
            } else {
                Write-Error "  ✗ Failed: $result"
                $errors++
            }
        }
    } catch {
        Write-Error "  ✗ Error: $_"
        $errors++
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Created: $created" -ForegroundColor Green
Write-Host "  Skipped: $skipped" -ForegroundColor Yellow
Write-Host "  Errors:  $errors" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Cyan
