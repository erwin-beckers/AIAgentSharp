# Pull Request Process

This guide outlines the process for contributing to AIAgentSharp through pull requests. Following this process ensures smooth collaboration and high-quality contributions.

## Before You Start

### 1. Check Existing Issues

Before creating a pull request, check if there's already an issue or discussion about your proposed changes:

- Search existing issues for similar requests
- Check if there's an open pull request for the same feature
- Review the project roadmap and priorities

### 2. Create an Issue (If Needed)

For significant changes, create an issue first to discuss the approach:

```markdown
## Feature Request: [Brief Description]

### Problem Statement
Describe the problem you're trying to solve.

### Proposed Solution
Outline your proposed approach.

### Alternatives Considered
List any alternative solutions you considered.

### Additional Context
Add any other context, screenshots, or examples.
```

### 3. Fork and Setup

```bash
# Fork the repository on GitHub
# Clone your fork
git clone https://github.com/YOUR_USERNAME/AIAgentSharp.git
cd AIAgentSharp

# Add upstream remote
git remote add upstream https://github.com/erwin-beckers/AIAgentSharp.git

# Create a feature branch
git checkout -b feature/your-feature-name
```

## Development Workflow

### 1. Branch Naming Convention

Use descriptive branch names that follow this pattern:

```bash
# Feature branches
git checkout -b feature/add-weather-tool
git checkout -b feature/improve-error-handling
git checkout -b feature/enhance-llm-integration

# Bug fix branches
git checkout -b bugfix/fix-null-reference-exception
git checkout -b bugfix/resolve-memory-leak

# Documentation branches
git checkout -b docs/update-api-documentation
git checkout -b docs/add-examples

# Hotfix branches
git checkout -b hotfix/critical-security-fix
```

### 2. Development Process

```bash
# 1. Update your branch with latest changes
git fetch upstream
git checkout main
git merge upstream/main
git checkout feature/your-feature-name
git merge main

# 2. Make your changes
# ... implement your feature ...

# 3. Write tests
# ... add unit tests, integration tests ...

# 4. Update documentation
# ... update relevant documentation ...

# 5. Run tests locally
dotnet test
dotnet build --configuration Release

# 6. Check code style
dotnet format --verify-no-changes
dotnet build --verbosity normal
```

### 3. Commit Guidelines

Follow conventional commit format:

```bash
# Format: type(scope): description

# Examples:
git commit -m "feat(tools): add weather API integration"
git commit -m "fix(agents): resolve null reference in state management"
git commit -m "docs(api): update agent configuration documentation"
git commit -m "test(tools): add unit tests for weather tool"
git commit -m "refactor(core): improve error handling in agent execution"
git commit -m "perf(llm): optimize prompt generation for better performance"
```

#### Commit Types

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation changes
- `style`: Code style changes (formatting, etc.)
- `refactor`: Code refactoring
- `test`: Adding or updating tests
- `chore`: Maintenance tasks
- `perf`: Performance improvements
- `ci`: CI/CD changes
- `build`: Build system changes

### 4. Code Quality Checklist

Before committing, ensure your code meets these standards:

- [ ] Code follows the [Code Style Guide](code-style.md)
- [ ] All tests pass
- [ ] New code has appropriate test coverage
- [ ] Documentation is updated
- [ ] No compiler warnings
- [ ] Code analysis passes
- [ ] Performance considerations addressed
- [ ] Security best practices followed

## Creating the Pull Request

### 1. Push Your Changes

```bash
# Push your branch to your fork
git push origin feature/your-feature-name
```

### 2. Create Pull Request

Go to your fork on GitHub and create a new pull request.

### 3. Pull Request Template

Use the provided template or follow this structure:

```markdown
## Description

Brief description of the changes made.

## Type of Change

- [ ] Bug fix (non-breaking change which fixes an issue)
- [ ] New feature (non-breaking change which adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update
- [ ] Performance improvement
- [ ] Refactoring (no functional changes)

## Related Issues

Closes #123
Related to #456

## Testing

- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed
- [ ] All tests pass

## Checklist

- [ ] My code follows the style guidelines of this project
- [ ] I have performed a self-review of my own code
- [ ] I have commented my code, particularly in hard-to-understand areas
- [ ] I have made corresponding changes to the documentation
- [ ] My changes generate no new warnings
- [ ] I have added tests that prove my fix is effective or that my feature works
- [ ] New and existing unit tests pass locally with my changes
- [ ] Any dependent changes have been merged and published

## Screenshots (if applicable)

Add screenshots to help explain your changes.

## Additional Notes

Any additional information or context.
```

## Pull Request Review Process

### 1. Automated Checks

Your pull request will automatically trigger several checks:

- **Build Status**: Ensures the code compiles successfully
- **Test Results**: Runs all unit and integration tests
- **Code Coverage**: Reports test coverage changes
- **Code Analysis**: Static analysis for potential issues
- **Security Scan**: Checks for security vulnerabilities
- **Performance Tests**: Runs performance benchmarks

### 2. Review Process

#### Initial Review

- A maintainer will review your pull request within 48 hours
- They may request changes or ask questions
- Automated checks must pass before review

#### Review Criteria

Reviewers will check for:

- **Functionality**: Does the code work as intended?
- **Code Quality**: Is the code well-written and maintainable?
- **Testing**: Are there adequate tests?
- **Documentation**: Is the documentation updated?
- **Performance**: Are there any performance implications?
- **Security**: Are there any security concerns?
- **Breaking Changes**: Are breaking changes properly documented?

#### Review Comments

Reviewers may leave comments requesting:

```markdown
## Requested Changes

### Code Quality
- [ ] Add null checks for parameter validation
- [ ] Use more descriptive variable names
- [ ] Extract complex logic into separate methods

### Testing
- [ ] Add test for edge case scenario
- [ ] Mock external dependencies in tests
- [ ] Add integration test for new feature

### Documentation
- [ ] Update API documentation
- [ ] Add usage examples
- [ ] Document configuration options

### Performance
- [ ] Consider caching for expensive operations
- [ ] Optimize database queries
- [ ] Add performance benchmarks
```

### 3. Addressing Feedback

When addressing review feedback:

```bash
# Make the requested changes
# ... implement changes ...

# Commit the changes
git add .
git commit -m "fix: address review feedback - add null checks"

# Push the changes
git push origin feature/your-feature-name
```

The pull request will automatically update with your new commits.

### 4. Approval Process

- At least one maintainer must approve the pull request
- All automated checks must pass
- All requested changes must be addressed
- Breaking changes require additional review

## Merge Process

### 1. Merge Strategies

The project uses different merge strategies based on the type of change:

#### Squash and Merge (Default)
- Used for most feature branches
- Combines all commits into a single commit
- Maintains clean commit history

#### Rebase and Merge
- Used for complex features with multiple logical commits
- Preserves individual commit history
- Requires linear history

#### Create a Merge Commit
- Used for major features or releases
- Preserves branch history
- Shows feature development timeline

### 2. Before Merge

Before merging, ensure:

- [ ] All tests pass
- [ ] Code coverage is maintained or improved
- [ ] Documentation is complete
- [ ] Breaking changes are documented
- [ ] Release notes are updated (if applicable)

### 3. After Merge

After your pull request is merged:

```bash
# Clean up your local repository
git checkout main
git pull upstream main
git branch -d feature/your-feature-name

# Update your fork
git push origin main
```

## Special Cases

### 1. Breaking Changes

For breaking changes, follow these additional steps:

```markdown
## Breaking Change Notice

This PR introduces breaking changes to the API.

### Changes Made
- Removed deprecated method `OldMethod()`
- Changed return type of `GetData()` from `string` to `DataObject`
- Updated configuration schema

### Migration Guide
1. Replace `OldMethod()` calls with `NewMethod()`
2. Update code to handle new return type
3. Update configuration files

### Deprecation Timeline
- Version 2.0: Deprecation warnings added
- Version 3.0: Breaking changes implemented
```

### 2. Large Changes

For large changes (>500 lines), consider:

- Breaking into smaller pull requests
- Adding detailed design documentation
- Scheduling a design review meeting
- Creating a feature branch for the main repository

### 3. Security Fixes

For security-related changes:

- Mark the pull request as security-sensitive
- Contact maintainers privately if needed
- Follow responsible disclosure guidelines
- Coordinate release timing

## Troubleshooting

### Common Issues

#### Build Failures

```bash
# Check build locally
dotnet clean
dotnet restore
dotnet build --verbosity normal

# Check for specific errors
dotnet build --verbosity detailed
```

#### Test Failures

```bash
# Run tests locally
dotnet test --verbosity normal

# Run specific test categories
dotnet test --filter Category=Unit
dotnet test --filter Category=Integration
```

#### Code Style Issues

```bash
# Format code
dotnet format

# Check formatting
dotnet format --verify-no-changes
```

#### Merge Conflicts

```bash
# Resolve conflicts
git fetch upstream
git checkout main
git merge upstream/main
git checkout feature/your-feature-name
git merge main

# Resolve conflicts in your editor
# Then commit the resolution
git add .
git commit -m "fix: resolve merge conflicts"
```

### Getting Help

If you encounter issues:

1. **Check Documentation**: Review relevant documentation
2. **Search Issues**: Look for similar issues on GitHub
3. **Ask in Discussions**: Post questions in GitHub Discussions
4. **Contact Maintainers**: Reach out to maintainers for help

## Best Practices

### 1. Communication

- Be responsive to review feedback
- Ask questions if requirements are unclear
- Provide context for your changes
- Be respectful and constructive

### 2. Quality

- Write clean, maintainable code
- Add comprehensive tests
- Update documentation
- Consider performance implications

### 3. Collaboration

- Help review other pull requests
- Share knowledge and best practices
- Contribute to discussions
- Support the community

### 4. Continuous Improvement

- Learn from feedback
- Improve your skills
- Share your experiences
- Help improve the process

## Release Process

### 1. Release Branches

For releases, maintainers create release branches:

```bash
# Create release branch
git checkout -b release/v2.0.0

# Cherry-pick fixes
git cherry-pick <commit-hash>

# Create release commit
git commit -m "chore: prepare release v2.0.0"
```

### 2. Release Notes

Update release notes with:

- New features
- Bug fixes
- Breaking changes
- Performance improvements
- Security updates

### 3. Tagging

```bash
# Create release tag
git tag -a v2.0.0 -m "Release v2.0.0"
git push origin v2.0.0
```

Following this pull request process ensures smooth collaboration and high-quality contributions to AIAgentSharp. Thank you for contributing!
