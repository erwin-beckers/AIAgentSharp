# Development Setup Guide

This guide provides step-by-step instructions for setting up a development environment for contributing to AIAgentSharp. Follow these instructions to get your local development environment ready for coding, testing, and contributing.

## Prerequisites

### Required Software

1. **.NET 8.0 SDK**
   - Download from: https://dotnet.microsoft.com/download/dotnet/8.0
   - Verify installation: `dotnet --version`

2. **Visual Studio 2022** (Recommended)
   - Community Edition: https://visualstudio.microsoft.com/vs/community/
   - Or **Visual Studio Code** with C# extension
   - Or **JetBrains Rider**

3. **Git**
   - Download from: https://git-scm.com/
   - Verify installation: `git --version`

4. **Node.js** (for documentation and tooling)
   - Download from: https://nodejs.org/
   - Verify installation: `node --version`

### Optional Software

1. **Docker** (for containerized development)
   - Download from: https://www.docker.com/products/docker-desktop/

2. **PostgreSQL** (for database development)
   - Download from: https://www.postgresql.org/download/

3. **Redis** (for caching development)
   - Download from: https://redis.io/download

## Repository Setup

### 1. Fork and Clone

```bash
# Fork the repository on GitHub first, then clone your fork
git clone https://github.com/YOUR_USERNAME/AIAgentSharp.git
cd AIAgentSharp

# Add the upstream repository
git remote add upstream https://github.com/erwin-beckers/AIAgentSharp.git
```

### 2. Verify Setup

```bash
# Check .NET version
dotnet --version

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

## Project Structure

```
AIAgentSharp/
├── src/                          # Source code
│   ├── AIAgentSharp.Core/        # Core framework
│   ├── AIAgentSharp.Agents/      # Agent implementations
│   ├── AIAgentSharp.Tools/       # Tool framework
│   ├── AIAgentSharp.LLM/         # LLM integrations
│   ├── AIAgentSharp.Reasoning/   # Reasoning engines
│   ├── AIAgentSharp.Events/      # Event system
│   ├── AIAgentSharp.State/       # State management
│   └── AIAgentSharp.Configuration/ # Configuration
├── tests/                        # Test projects
│   ├── AIAgentSharp.Tests.Unit/  # Unit tests
│   ├── AIAgentSharp.Tests.Integration/ # Integration tests
│   └── AIAgentSharp.Tests.Performance/ # Performance tests
├── samples/                      # Sample applications
├── docs/                         # Documentation
├── tools/                        # Build tools and scripts
└── scripts/                      # Development scripts
```

## Development Environment Configuration

### 1. Visual Studio Configuration

1. **Install Required Workloads**
   - ASP.NET and web development
   - .NET desktop development
   - Azure development

2. **Install Extensions**
   - C# Dev Kit
   - IntelliCode
   - GitLens
   - SonarLint

3. **Configure Settings**
   ```json
   // .vscode/settings.json
   {
     "omnisharp.enableEditorConfigSupport": true,
     "omnisharp.enableImportCompletion": true,
     "csharp.format.enable": true,
     "editor.formatOnSave": true,
     "files.trimTrailingWhitespace": true
   }
   ```

### 2. EditorConfig Configuration

The project includes an `.editorconfig` file for consistent coding standards:

```ini
# .editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,csx,vb,vbx}]
indent_style = space
indent_size = 4

[*.{json,js,jsx,ts,tsx}]
indent_style = space
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

### 3. Git Configuration

```bash
# Configure Git
git config user.name "Your Name"
git config user.email "your.email@example.com"

# Configure line endings
git config core.autocrlf true  # Windows
git config core.autocrlf input # macOS/Linux
```

## Build Configuration

### 1. Solution Structure

```bash
# Build the entire solution
dotnet build AIAgentSharp.sln

# Build specific project
dotnet build src/AIAgentSharp.Core/AIAgentSharp.Core.csproj

# Build in Release mode
dotnet build AIAgentSharp.sln --configuration Release
```

### 2. Build Scripts

```bash
# Clean build
./scripts/clean.sh

# Build and test
./scripts/build-and-test.sh

# Build documentation
./scripts/build-docs.sh
```

## Testing Setup

### 1. Unit Tests

```bash
# Run all unit tests
dotnet test tests/AIAgentSharp.Tests.Unit/

# Run specific test
dotnet test tests/AIAgentSharp.Tests.Unit/ --filter "TestName"

# Run with coverage
dotnet test tests/AIAgentSharp.Tests.Unit/ --collect:"XPlat Code Coverage"
```

### 2. Integration Tests

```bash
# Run integration tests
dotnet test tests/AIAgentSharp.Tests.Integration/

# Run with specific environment
dotnet test tests/AIAgentSharp.Tests.Integration/ --environment "Development"
```

### 3. Performance Tests

```bash
# Run performance tests
dotnet test tests/AIAgentSharp.Tests.Performance/

# Run with specific parameters
dotnet test tests/AIAgentSharp.Tests.Performance/ --logger "console;verbosity=detailed"
```

## API Key Configuration

### 1. Development API Keys

Create a `secrets.json` file in the project root (do not commit this file):

```json
{
  "OpenAI": {
    "ApiKey": "your-openai-api-key",
    "Organization": "your-organization-id"
  },
  "AzureOpenAI": {
    "ApiKey": "your-azure-api-key",
    "Endpoint": "https://your-resource.openai.azure.com/"
  }
}
```

### 2. Environment Variables

```bash
# Set environment variables
export OPENAI_API_KEY="your-openai-api-key"
export AZURE_OPENAI_API_KEY="your-azure-api-key"
export AZURE_OPENAI_ENDPOINT="https://your-resource.openai.azure.com/"
```

### 3. User Secrets (Development)

```bash
# Initialize user secrets
dotnet user-secrets init --project samples/AIAgentSharp.Samples.Console

# Add secrets
dotnet user-secrets set "OpenAI:ApiKey" "your-api-key" --project samples/AIAgentSharp.Samples.Console
dotnet user-secrets set "AzureOpenAI:ApiKey" "your-azure-api-key" --project samples/AIAgentSharp.Samples.Console
```

## Database Setup

### 1. PostgreSQL (Optional)

```bash
# Install PostgreSQL
# Windows: Use installer from postgresql.org
# macOS: brew install postgresql
# Linux: sudo apt-get install postgresql

# Create database
createdb aiagentsharp_dev

# Run migrations
dotnet ef database update --project src/AIAgentSharp.State
```

### 2. Redis (Optional)

```bash
# Install Redis
# Windows: Use Docker or WSL
# macOS: brew install redis
# Linux: sudo apt-get install redis-server

# Start Redis
redis-server

# Test connection
redis-cli ping
```

## Docker Setup

### 1. Development Containers

```bash
# Build development container
docker build -f Dockerfile.dev -t aiagentsharp-dev .

# Run development container
docker run -it --rm -v $(pwd):/workspace aiagentsharp-dev
```

### 2. Docker Compose

```yaml
# docker-compose.dev.yml
version: '3.8'
services:
  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: aiagentsharp_dev
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: password
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

volumes:
  postgres_data:
```

```bash
# Start development services
docker-compose -f docker-compose.dev.yml up -d
```

## IDE-Specific Setup

### Visual Studio 2022

1. **Open Solution**
   - Open `AIAgentSharp.sln`
   - Set startup project to a sample application

2. **Configure Debugging**
   - Set breakpoints in source code
   - Configure launch profiles in `launchSettings.json`

3. **Install Extensions**
   - ReSharper (optional)
   - CodeMaid
   - GitLens

### Visual Studio Code

1. **Install Extensions**
   ```json
   // .vscode/extensions.json
   {
     "recommendations": [
       "ms-dotnettools.csharp",
       "ms-dotnettools.vscode-dotnet-runtime",
       "ms-vscode.vscode-json",
       "eamodio.gitlens",
       "ms-vscode.vscode-docker"
     ]
   }
   ```

2. **Configure Tasks**
   ```json
   // .vscode/tasks.json
   {
     "version": "2.0.0",
     "tasks": [
       {
         "label": "build",
         "command": "dotnet",
         "type": "process",
         "args": ["build"],
         "problemMatcher": "$msCompile"
       },
       {
         "label": "test",
         "command": "dotnet",
         "type": "process",
         "args": ["test"],
         "group": "test"
       }
     ]
   }
   ```

### JetBrains Rider

1. **Open Solution**
   - Open `AIAgentSharp.sln`
   - Configure .NET SDK version

2. **Configure Code Style**
   - Import `.editorconfig` settings
   - Configure ReSharper settings

## Development Workflow

### 1. Branch Strategy

```bash
# Create feature branch
git checkout -b feature/your-feature-name

# Create bugfix branch
git checkout -b bugfix/your-bug-description

# Create hotfix branch
git checkout -b hotfix/urgent-fix
```

### 2. Development Process

```bash
# 1. Update from upstream
git fetch upstream
git checkout main
git merge upstream/main

# 2. Create feature branch
git checkout -b feature/new-feature

# 3. Make changes and commit
git add .
git commit -m "feat: add new feature"

# 4. Push to your fork
git push origin feature/new-feature

# 5. Create pull request on GitHub
```

### 3. Code Quality

```bash
# Run code analysis
dotnet build --verbosity normal

# Run style checks
dotnet format --verify-no-changes

# Run security analysis
dotnet list package --vulnerable

# Run performance analysis
dotnet run --project tools/AIAgentSharp.Tools.Performance
```

## Troubleshooting

### Common Issues

1. **Build Errors**
   ```bash
   # Clean and rebuild
   dotnet clean
   dotnet restore
   dotnet build
   ```

2. **Test Failures**
   ```bash
   # Run tests with detailed output
   dotnet test --logger "console;verbosity=detailed"
   ```

3. **API Key Issues**
   ```bash
   # Verify environment variables
   echo $OPENAI_API_KEY
   
   # Check user secrets
   dotnet user-secrets list
   ```

4. **Database Connection Issues**
   ```bash
   # Check database status
   sudo systemctl status postgresql
   
   # Test connection
   psql -h localhost -U postgres -d aiagentsharp_dev
   ```

### Performance Issues

1. **Slow Builds**
   ```bash
   # Enable parallel builds
   dotnet build --maxcpucount:0
   
   # Use incremental builds
   dotnet build --no-restore
   ```

2. **Memory Issues**
   ```bash
   # Monitor memory usage
   dotnet build --verbosity normal | grep "Memory"
   
   # Clean solution
   dotnet clean
   ```

## Next Steps

1. **Read Documentation**
   - Review the main documentation
   - Understand the architecture
   - Read contributing guidelines

2. **Explore Codebase**
   - Start with sample applications
   - Review unit tests
   - Understand the API design

3. **Join Community**
   - Join Discord/Slack channels
   - Follow GitHub discussions
   - Attend community events

4. **Start Contributing**
   - Pick an issue labeled "good first issue"
   - Create a simple feature
   - Submit your first pull request

## Support

If you encounter issues during setup:

1. **Check Documentation**: Review this guide and main documentation
2. **Search Issues**: Look for similar issues on GitHub
3. **Ask Community**: Post questions in discussions or Discord
4. **Create Issue**: Report bugs or request help

## Environment Verification

Run this script to verify your setup:

```bash
#!/bin/bash
echo "Verifying development environment..."

# Check .NET
echo "Checking .NET..."
dotnet --version

# Check Git
echo "Checking Git..."
git --version

# Check Node.js
echo "Checking Node.js..."
node --version

# Build solution
echo "Building solution..."
dotnet build AIAgentSharp.sln

# Run tests
echo "Running tests..."
dotnet test --no-build

echo "Environment verification complete!"
```

This setup guide should get you started with contributing to AIAgentSharp. If you have any questions or encounter issues, please reach out to the community for help.
