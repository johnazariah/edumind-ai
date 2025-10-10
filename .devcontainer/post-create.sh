#!/bin/bash
set -e

echo "🚀 Starting EduMind.AI Development Container Setup..."

# Update package list
echo "📦 Updating package lists..."
sudo apt-get update

# Install additional dependencies
echo "📦 Installing additional packages..."
sudo apt-get install -y \
    curl \
    wget \
    git \
    jq \
    vim \
    postgresql-client \
    redis-tools

# Verify .NET installation
echo "✅ Verifying .NET SDK..."
dotnet --version
dotnet --list-sdks

# Verify GitHub CLI installation
echo "✅ Verifying GitHub CLI..."
gh --version

# Verify Azure CLI installation
echo "✅ Verifying Azure CLI..."
az --version

# Install global .NET tools
echo "📦 Installing .NET global tools..."
dotnet tool install -g dotnet-ef || echo "dotnet-ef already installed"
dotnet tool install -g dotnet-outdated-tool || echo "dotnet-outdated-tool already installed"
dotnet tool install -g dotnet-format || echo "dotnet-format already installed"

# Add .NET tools to PATH
export PATH="$PATH:/home/vscode/.dotnet/tools"
echo 'export PATH="$PATH:/home/vscode/.dotnet/tools"' >> ~/.bashrc

# Restore .NET dependencies
echo "📦 Restoring .NET solution..."
if [ -f "EduMind.AI.sln" ]; then
    dotnet restore EduMind.AI.sln
    echo "✅ Solution restored successfully"
else
    echo "⚠️  Solution file not found, skipping restore"
fi

# Set up Git configuration if not already configured
if [ ! -f ~/.gitconfig ]; then
    echo "⚙️  Configuring Git..."
    git config --global init.defaultBranch main
    git config --global core.autocrlf input
    git config --global pull.rebase false
fi

# Create helpful aliases
echo "⚙️  Setting up shell aliases..."
cat >> ~/.bashrc << 'EOF'

# EduMind.AI Development Aliases
alias build='dotnet build'
alias test='dotnet test'
alias run-api='dotnet run --project src/AcademicAssessment.Web'
alias run-dashboard='dotnet run --project src/AcademicAssessment.Dashboard'
alias run-student='dotnet run --project src/AcademicAssessment.StudentApp'
alias clean='dotnet clean'
alias restore='dotnet restore'
alias format='dotnet format'
alias ll='ls -alh'

# Git aliases
alias gs='git status'
alias ga='git add'
alias gc='git commit'
alias gp='git push'
alias gl='git log --oneline --graph --decorate'
alias gd='git diff'

# Docker compose shortcuts
alias dc='docker-compose'
alias dcu='docker-compose up -d'
alias dcd='docker-compose down'
alias dcl='docker-compose logs -f'

echo "🎓 EduMind.AI Development Environment Ready!"
echo ""
echo "Useful commands:"
echo "  build          - Build the solution"
echo "  test           - Run all tests"
echo "  run-api        - Run the Web API"
echo "  run-dashboard  - Run the Dashboard"
echo "  run-student    - Run the Student App"
echo "  format         - Format code with dotnet format"
echo ""
EOF

# Install useful VS Code extensions (if not already installed via devcontainer.json)
echo "✅ Development container setup complete!"

# Display summary
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🎓 EduMind.AI Development Container Ready!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "📦 Installed Tools:"
echo "   ✅ .NET 8.0 SDK"
echo "   ✅ GitHub CLI (gh)"
echo "   ✅ Azure CLI (az)"
echo "   ✅ Docker"
echo "   ✅ PostgreSQL Client"
echo "   ✅ Redis Tools"
echo "   ✅ Entity Framework Core Tools (dotnet-ef)"
echo ""
echo "🚀 Quick Start:"
echo "   dotnet build              - Build solution"
echo "   dotnet test               - Run tests"
echo "   run-api                   - Start Web API"
echo "   gh auth login             - Authenticate with GitHub"
echo "   az login                  - Authenticate with Azure"
echo ""
echo "📚 Documentation: ./docs/"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
