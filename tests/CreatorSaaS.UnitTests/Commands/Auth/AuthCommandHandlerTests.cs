using Xunit;
using Moq;
using FluentAssertions;
using CreatorSaaS.Application.Commands.Auth;
using CreatorSaaS.Core.Entities;
using CreatorSaaS.Core.Interfaces;
using CreatorSaaS.Application.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CreatorSaaS.UnitTests.Commands.Auth;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IJwtService> _mockJwt;
    private readonly Mock<IEmailService> _mockEmail;
    private readonly Mock<ILogger<RegisterCommandHandler>> _mockLogger;

    public RegisterCommandHandlerTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockJwt = new Mock<IJwtService>();
        _mockEmail = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<RegisterCommandHandler>>();
    }

    [Fact]
    public async Task Handle_WithValidInput_CreatesUserAndReturnsAuthResponse()
    {
        // Arrange
        var command = new RegisterCommand(
            "Test Tenant",
            "user@example.com",
            "SecurePassword123!",
            "John",
            "Doe"
        );

        var mockUserRepo = new Mock<IRepository<User>>();
        var mockTenantRepo = new Mock<IRepository<Tenant>>();

        _mockUow.Setup(u => u.Users).Returns(mockUserRepo.Object);
        _mockUow.Setup(u => u.Tenants).Returns(mockTenantRepo.Object);
        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        mockUserRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        _mockJwt.Setup(j => j.GenerateTokens(It.IsAny<User>()))
            .Returns(("access_token", "refresh_token"));

        var handler = new RegisterCommandHandler(_mockUow.Object, _mockJwt.Object, _mockEmail.Object, _mockLogger.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("access_token");
        result.RefreshToken.Should().Be("refresh_token");
        result.User.Email.Should().Be("user@example.com");

        _mockUow.Verify(u => u.Tenants.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUow.Verify(u => u.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WithDuplicateEmail_ThrowsException()
    {
        // Arrange
        var command = new RegisterCommand(
            "Test Tenant",
            "existing@example.com",
            "SecurePassword123!",
            "John",
            "Doe"
        );

        var mockUserRepo = new Mock<IRepository<User>>();
        _mockUow.Setup(u => u.Users).Returns(mockUserRepo.Object);

        var existingUser = new User { Email = "existing@example.com" };
        mockUserRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var handler = new RegisterCommandHandler(_mockUow.Object, _mockJwt.Object, _mockEmail.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command, CancellationToken.None));
    }
}

public class LoginCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUow;
    private readonly Mock<IJwtService> _mockJwt;

    public LoginCommandHandlerTests()
    {
        _mockUow = new Mock<IUnitOfWork>();
        _mockJwt = new Mock<IJwtService>();
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            FirstName = "John",
            LastName = "Doe",
            PasswordHash = hashedPassword,
            TenantId = Guid.NewGuid(),
            Role = "member"
        };

        var tenant = new Tenant
        {
            Id = user.TenantId,
            Name = "Test Tenant",
            Plan = "starter",
            IsActive = true
        };

        var command = new LoginCommand("user@example.com", password);

        var mockUserRepo = new Mock<IRepository<User>>();
        var mockTenantRepo = new Mock<IRepository<Tenant>>();

        _mockUow.Setup(u => u.Users).Returns(mockUserRepo.Object);
        _mockUow.Setup(u => u.Tenants).Returns(mockTenantRepo.Object);
        _mockUow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        mockUserRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        mockTenantRepo.Setup(r => r.GetByIdAsync(user.TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _mockJwt.Setup(j => j.GenerateTokens(It.IsAny<User>()))
            .Returns(("access_token", "refresh_token"));

        var handler = new LoginCommandHandler(_mockUow.Object, _mockJwt.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.User.Email.Should().Be("user@example.com");
        result.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ThrowsException()
    {
        // Arrange
        var password = "SecurePassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new User
        {
            Email = "user@example.com",
            PasswordHash = hashedPassword
        };

        var command = new LoginCommand("user@example.com", "WrongPassword");

        var mockUserRepo = new Mock<IRepository<User>>();
        _mockUow.Setup(u => u.Users).Returns(mockUserRepo.Object);

        mockUserRepo.Setup(r => r.FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<System.Func<User, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var handler = new LoginCommandHandler(_mockUow.Object, _mockJwt.Object);

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(command, CancellationToken.None));
    }
}
