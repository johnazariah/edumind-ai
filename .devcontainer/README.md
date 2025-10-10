# EduMind.AI Development Container

## Overview

This development container provides a complete, reproducible development environment for the EduMind.AI Academic Assessment System. It includes all necessary tools, SDKs, and services pre-configured and ready to use.

## 🛠️ Included Tools

### Core Development
- **.NET 8.0 SDK** - Latest .NET framework
- **C# Dev Kit** - Enhanced C# development experience
- **Entity Framework Core Tools** - Database migrations and tooling
- **Docker** - Container support and Docker Compose

### Cloud & DevOps
- **GitHub CLI (`gh`)** - GitHub operations from command line
- **Azure CLI (`az`)** - Azure resource management
- **PowerShell** - Cross-platform PowerShell

### Database & Caching
- **PostgreSQL 16** - Primary database (via docker-compose)
- **Redis 7** - Caching and session management (via docker-compose)
- **pgAdmin 4** - PostgreSQL administration UI
- **Redis Commander** - Redis administration UI

### Development Tools
- **Git** - Version control with latest features
- **Node.js LTS** - For frontend tooling if needed
- **jq** - JSON processing
- **vim** - Text editor

## 🚀 Getting Started

### Prerequisites
- **VS Code** with Remote-Containers extension
- **Docker Desktop** running on your machine

### Opening in Container

1. **Open in VS Code:**
   ```bash
   code .
   ```

2. **Reopen in Container:**
   - Press `F1` or `Ctrl+Shift+P`
   - Select "Dev Containers: Reopen in Container"
   - Wait for container to build (first time takes 5-10 minutes)

3. **Container is Ready!**
   - All tools are pre-installed
   - Dependencies are restored
   - Ports are forwarded automatically

## 📦 Services

The development environment includes these services via Docker Compose:

| Service | Port | Access | Credentials |
|---------|------|--------|-------------|
| PostgreSQL | 5432 | `localhost:5432` | User: `edumind_user`<br>Password: `edumind_dev_password`<br>Database: `edumind_dev` |
| Redis | 6379 | `localhost:6379` | No authentication |
| pgAdmin | 5050 | http://localhost:5050 | Email: `admin@edumind.ai`<br>Password: `admin` |
| Redis Commander | 8081 | http://localhost:8081 | No authentication |
| Web API (HTTP) | 5000 | http://localhost:5000 | - |
| Web API (HTTPS) | 5001 | https://localhost:5001 | - |
| Dashboard | 5002 | http://localhost:5002 | - |
| Student App | 5003 | http://localhost:5003 | - |

## 🎯 Quick Commands

The container includes helpful aliases for common tasks:

### .NET Development
```bash
build          # Build the entire solution
test           # Run all tests
run-api        # Start the Web API
run-dashboard  # Start the Admin Dashboard
run-student    # Start the Student App
clean          # Clean build artifacts
restore        # Restore NuGet packages
format         # Format code with dotnet-format
```

### Git Operations
```bash
gs             # git status
ga             # git add
gc             # git commit
gp             # git push
gl             # git log --oneline --graph
gd             # git diff
```

### Docker Compose
```bash
dc             # docker-compose
dcu            # docker-compose up -d
dcd            # docker-compose down
dcl            # docker-compose logs -f
```

### Database
```bash
# Connect to PostgreSQL
psql -h localhost -U edumind_user -d edumind_dev

# Run Entity Framework migrations
dotnet ef migrations add MigrationName --project src/AcademicAssessment.Infrastructure
dotnet ef database update --project src/AcademicAssessment.Infrastructure
```

### GitHub CLI
```bash
# Authenticate with GitHub
gh auth login

# Create a pull request
gh pr create

# View repository
gh repo view
```

### Azure CLI
```bash
# Authenticate with Azure
az login

# List subscriptions
az account list --output table

# Set active subscription
az account set --subscription "subscription-name"
```

## 🔧 Configuration

### Environment Variables

The container sets these environment variables automatically:
- `DOTNET_CLI_TELEMETRY_OPTOUT=1` - Disable telemetry
- `DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1` - Skip first-run experience
- `ASPNETCORE_ENVIRONMENT=Development` - Development mode

### VS Code Extensions

These extensions are automatically installed:
- C# Dev Kit & Language Support
- GitHub Copilot & Chat
- GitLens
- Docker
- Azure Tools
- Kubernetes Tools
- PowerShell
- Markdown Linting
- YAML Support

### Mounted Volumes

The container mounts these directories from your host:
- `~/.azure` - Azure CLI credentials
- `~/.gitconfig` - Git configuration

## 🗄️ Database Management

### Start Database Services
```bash
docker-compose up -d postgres redis
```

### Access pgAdmin
1. Open http://localhost:5050
2. Login with `admin@edumind.ai` / `admin`
3. Add server connection:
   - Name: `EduMind Local`
   - Host: `postgres`
   - Port: `5432`
   - Username: `edumind_user`
   - Password: `edumind_dev_password`

### Access Redis Commander
1. Open http://localhost:8081
2. Browse Redis keys and values

### Stop Services
```bash
docker-compose down
```

## 🐛 Troubleshooting

### Container Won't Build
- Ensure Docker Desktop is running
- Check Docker has enough memory (8GB+ recommended)
- Try: "Dev Containers: Rebuild Container"

### Port Already in Use
- Check if services are running outside container
- Modify port mappings in `devcontainer.json`

### Database Connection Issues
- Verify PostgreSQL is running: `docker-compose ps`
- Check logs: `docker-compose logs postgres`
- Ensure connection string is correct

### Package Restore Fails
```bash
dotnet restore --force
dotnet nuget locals all --clear
```

## 📚 Additional Resources

- [VS Code Dev Containers Documentation](https://code.visualstudio.com/docs/devcontainers/containers)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET CLI Documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/)
- [GitHub CLI Documentation](https://cli.github.com/manual/)
- [Azure CLI Documentation](https://learn.microsoft.com/en-us/cli/azure/)

## 🔄 Updating the Container

When dependencies change:
1. "Dev Containers: Rebuild Container" (without cache)
2. Or manually: `docker-compose build --no-cache`

---

**Happy Coding! 🎓**
