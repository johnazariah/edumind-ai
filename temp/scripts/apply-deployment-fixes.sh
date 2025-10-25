#!/bin/bash

# Script to apply deployment pipeline fixes
# Run this manually to fix the 6-hour timeout and azd installation issues

set -e

echo "Applying deployment pipeline fixes..."

# Backup the original file
cp .github/workflows/deploy-azure-azd.yml .github/workflows/deploy-azure-azd.yml.backup
echo "✓ Created backup: .github/workflows/deploy-azure-azd.yml.backup"

# Fix #1: Replace first instance of Azure/setup-azd action (around line 82)
sed -i '/- name: Install Azure Developer CLI/{N;s/uses: Azure\/setup-azd@v1\.0\.0/run: |\n          curl -fsSL https:\/\/aka.ms\/install-azd.sh | bash\n          echo "$HOME\/.azd\/bin" >> $GITHUB_PATH/}' .github/workflows/deploy-azure-azd.yml

echo "✓ Fixed first Azure Developer CLI installation (deploy job)"

# Fix #2: Replace second instance of Azure/setup-azd action (around line 168)
# This requires a more complex sed since there are two instances
# We'll use a different approach - replace both instances at once

cat <<'EOF' > /tmp/fix-azd.py
import re

with open('.github/workflows/deploy-azure-azd.yml', 'r') as f:
    content = f.read()

# Pattern to match the Azure/setup-azd action
old_pattern = r'      - name: Install Azure Developer CLI\n        uses: Azure/setup-azd@v1\.0\.0'

new_text = '''      - name: Install Azure Developer CLI
        run: |
          curl -fsSL https://aka.ms/install-azd.sh | bash
          echo "$HOME/.azd/bin" >> $GITHUB_PATH'''

# Replace all occurrences
content = re.sub(old_pattern, new_text, content)

with open('.github/workflows/deploy-azure-azd.yml', 'w') as f:
    f.write(content)

print("Replaced Azure/setup-azd action instances")
EOF

python3 /tmp/fix-azd.py
echo "✓ Fixed second Azure Developer CLI installation (integration-tests job)"

# Verify the changes
echo ""
echo "Verifying changes..."
if grep -q "aka.ms/install-azd.sh" .github/workflows/deploy-azure-azd.yml; then
    count=$(grep -c "aka.ms/install-azd.sh" .github/workflows/deploy-azure-azd.yml)
    echo "✓ Found $count instance(s) of the new azd installation method"
else
    echo "✗ ERROR: Changes not applied correctly!"
    echo "Restoring backup..."
    mv .github/workflows/deploy-azure-azd.yml.backup .github/workflows/deploy-azure-azd.yml
    exit 1
fi

# Show the diff
echo ""
echo "=== Changes applied ===="
git diff .github/workflows/deploy-azure-azd.yml | head -50

echo ""
echo "✓ All fixes applied successfully!"
echo ""
echo "Next steps:"
echo "1. Review the changes above"
echo "2. Test: git diff .github/workflows/deploy-azure-azd.yml"
echo "3. Commit: git add .github/workflows/deploy-azure-azd.yml DEPLOYMENT_FIXES_SUMMARY.md"
echo "4. Commit: git commit -m 'fix(ci): Replace Azure/setup-azd action with direct installation'"
echo "5. Push: git push origin main"
echo ""
echo "To restore backup if needed: mv .github/workflows/deploy-azure-azd.yml.backup .github/workflows/deploy-azure-azd.yml"
