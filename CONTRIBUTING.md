# Contributing to AIAgentSharp

Thank you for your interest in contributing to AIAgentSharp! This document provides guidelines for contributing to the project.

## Getting Started

1. **Fork the repository** on GitHub
2. **Clone your fork** locally
3. **Create a feature branch** for your changes
4. **Make your changes** following the coding standards below
5. **Test your changes** thoroughly
6. **Submit a pull request**

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, VS Code, or JetBrains Rider
- Git

### Building the Project

```bash
# Clone the repository
git clone https://github.com/your-username/AIAgentSharp.git
cd AIAgentSharp

# Restore dependencies
dotnet restore

# Build the solution
dotnet build

# Run tests
dotnet test
```

## Coding Standards

### General Guidelines

- **Follow C# coding conventions** as defined by Microsoft
- **Use meaningful names** for variables, methods, and classes
- **Add XML documentation** for all public APIs
- **Keep methods focused** and single-purpose
- **Write unit tests** for new functionality

### Code Style

- Use **4 spaces** for indentation (not tabs)
- Use **camelCase** for private fields and local variables
- Use **PascalCase** for public properties, methods, and classes
- Use **ALL_CAPS** for constants
- Prefer **var** for local variable declarations when the type is obvious

### Documentation

- Add **XML documentation comments** for all public APIs
- Include **parameter descriptions** and **return value descriptions**
- Add **exception documentation** where applicable
- Keep documentation **concise but comprehensive**

### Testing

- Write **unit tests** for all new functionality
- Aim for **high test coverage** (80%+)
- Use **descriptive test names** that explain the scenario
- Follow the **Arrange-Act-Assert** pattern
- Mock external dependencies appropriately

## Pull Request Guidelines

### Before Submitting

1. **Ensure all tests pass** locally
2. **Update documentation** if needed
3. **Add tests** for new functionality
4. **Check for code style** compliance
5. **Rebase on main** if there are conflicts

### Pull Request Template

When creating a pull request, please include:

- **Description** of the changes
- **Related issue** (if applicable)
- **Testing performed**
- **Breaking changes** (if any)
- **Screenshots** (for UI changes)

## Issue Reporting

When reporting issues, please include:

- **Clear description** of the problem
- **Steps to reproduce** the issue
- **Expected behavior** vs actual behavior
- **Environment details** (OS, .NET version, etc.)
- **Code samples** if applicable

## Code of Conduct

This project adheres to the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code.

## License

By contributing to AIAgentSharp, you agree that your contributions will be licensed under the MIT License.
