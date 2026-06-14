# Contributing to CreatorSaaS

Thank you for your interest in contributing to CreatorSaaS! We appreciate all contributions, whether they're bug reports, feature requests, documentation improvements, or code changes.

## Code of Conduct

Please note that this project is released with a [Contributor Code of Conduct](CODE_OF_CONDUCT.md). By participating in this project, you agree to abide by its terms.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check the [issue list](https://github.com/your-org/creatorsaas/issues) as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps which reproduce the problem**
- **Provide specific examples to demonstrate the steps**
- **Describe the behavior you observed after following the steps**
- **Explain which behavior you expected to see instead and why**
- **Include screenshots and animated GIFs if possible**
- **Include your environment (OS, version, Docker setup, etc.)**

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

- **Use a clear and descriptive title**
- **Provide a step-by-step description of the suggested enhancement**
- **Provide specific examples to demonstrate the steps**
- **Describe the current behavior and expected behavior**
- **Explain why this enhancement would be useful**

### Pull Requests

- Fill in the required template
- Follow the [Code Style](#code-style) guidelines
- Include appropriate test cases
- End all files with a newline
- Avoid commits with multiple features/fixes - keep them focused

## Development Setup

### Prerequisites

- .NET 8 SDK
- Node.js 20+
- PostgreSQL 14+
- Redis 7+
- Docker & Docker Compose (recommended)

### Local Setup

```bash
# Clone the repository
git clone https://github.com/your-org/creatorsaas.git
cd creatorsaas

# Setup environment
make dev-setup

# Run API (Terminal 1)
make run-api

# Run Frontend (Terminal 2)
make run-frontend

# Run workers (Terminal 3, optional)
make run-workers
```

## Code Style

### C# Guidelines

We follow the [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions) with the following specifics:

```csharp
// Use meaningful names
public class VideoProcessingService
{
    // Use PascalCase for public members
    public async Task ProcessVideoAsync(Guid videoJobId, CancellationToken ct)
    {
        // Use var for obvious types
        var job = await _repository.GetByIdAsync(videoJobId, ct);
        
        // Prefer LINQ to loops
        var scenes = job.Scenes
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Order)
            .ToList();
        
        // Use nullable reference types
        string? optionalValue = null;
    }
}
```

### TypeScript/React Guidelines

```typescript
// Use meaningful names and proper typing
interface VideoCreateRequest {
  topic: string;
  keywords?: string;
  language: string;
  style: 'tutorial' | 'vlog' | 'documentary';
}

// Use functional components
export const VideoCreateForm: React.FC = () => {
  const [loading, setLoading] = useState(false);
  
  return (
    <form>
      {/* JSX here */}
    </form>
  );
};

// Use custom hooks for logic
export const useVideoCreation = () => {
  const [videos, setVideos] = useState<VideoJob[]>([]);
  // Hook implementation
  return { videos, createVideo };
};
```

### General Style Rules

- **Naming**: Use PascalCase for classes, camelCase for variables/methods
- **Indentation**: 4 spaces (C#), 2 spaces (TypeScript/JSON)
- **Line length**: Max 120 characters (aim for readability)
- **Comments**: Use // for single-line, /** */ for documentation
- **Async**: Always use async/await, avoid callbacks
- **Error handling**: Use exceptions, log with context

### Format Code

```bash
# C#
dotnet format

# TypeScript
cd src/CreatorSaaS.Frontend
npm run format
```

## Testing

### Writing Tests

- Write unit tests for business logic
- Write integration tests for API endpoints
- Aim for > 80% code coverage
- Use meaningful test names: `Handle_WithValidInput_ReturnsSuccess`
- Test happy path and error cases

```csharp
// Example unit test
[Fact]
public async Task Handle_WithValidInput_CreatesVideoJob()
{
    // Arrange
    var command = new CreateVideoJobCommand(...);
    var handler = new CreateVideoJobCommandHandler(...);
    
    // Act
    var result = await handler.Handle(command, CancellationToken.None);
    
    // Assert
    result.Should().NotBeNull();
    result.Status.Should().Be(VideoJobStatus.Pending);
}
```

### Running Tests

```bash
# All tests
make test

# Unit tests only
make test-unit

# Integration tests only
make test-integration

# With coverage
make test-coverage
```

## Documentation

- Keep README.md up to date
- Document complex functions with XML comments
- Update API_EXAMPLES.md for new endpoints
- Add diagrams for complex processes
- Keep ARCHITECTURE.md synchronized

## Git Workflow

1. **Create a branch** for your feature/fix:
   ```bash
   git checkout -b feature/my-feature
   # or
   git checkout -b fix/issue-123
   ```

2. **Make commits** with clear messages:
   ```bash
   git commit -m "feat: add video variant support for A/B testing"
   git commit -m "fix: resolve token refresh race condition"
   git commit -m "docs: update API documentation"
   ```

3. **Keep commits focused** - one feature/fix per commit

4. **Push to your fork**:
   ```bash
   git push origin feature/my-feature
   ```

5. **Create a Pull Request** with:
   - Clear title and description
   - Reference to related issues
   - Screenshots/videos if UI changes
   - Testing notes

### Commit Message Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`

Example:
```
feat(videos): add A/B testing for variants

Implement video variant creation allowing users to test different
titles and descriptions. Includes analytics comparison.

Closes #123
```

## Pull Request Process

1. **Ensure all tests pass**:
   ```bash
   make test
   ```

2. **Run code quality checks**:
   ```bash
   make lint
   dotnet format
   ```

3. **Update documentation** if needed

4. **Push your changes** and create PR

5. **Respond to review comments** within reasonable time

6. **Ensure CI/CD pipeline passes**

## Community

- **Discussions**: Use GitHub Discussions for questions
- **Issues**: Report bugs and request features
- **Pull Requests**: Contribute code improvements
- **Discord** (optional): Join our community server
- **Email**: Contact maintainers at team@creatorsaas.io

## Recognition

Contributors will be recognized in:
- CONTRIBUTORS.md file
- GitHub contributors page
- Release notes for significant contributions

## Questions?

Feel free to:
- Open an issue asking for clarification
- Email the maintainers
- Join our community discussions
- Check existing documentation

## License

By contributing to CreatorSaaS, you agree that your contributions will be licensed under its MIT License.

---

Thank you for contributing! 🎉
