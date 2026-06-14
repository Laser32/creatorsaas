using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using CreatorSaaS.Application.DTOs;

namespace CreatorSaaS.IntegrationTests.Controllers;

public class AuthControllerIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerIntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>();
        _httpClient = _factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        // Seed database if needed
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _httpClient.Dispose();
        _factory.Dispose();
        await Task.CompletedTask;
    }

    [Fact]
    public async Task Register_WithValidInput_ReturnsOkAndAuthResponse()
    {
        // Arrange
        var registerRequest = new
        {
            tenantName = "Test Tenant " + Guid.NewGuid(),
            email = "test" + Guid.NewGuid() + "@example.com",
            password = "SecurePassword123!",
            firstName = "Test",
            lastName = "User"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _httpClient.PostAsync("/api/auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(jsonResponse);

        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.RefreshToken.Should().NotBeNullOrEmpty();
        authResponse.User.Email.Should().Be(registerRequest.email);
        authResponse.Tenant.Name.Should().Be(registerRequest.tenantName);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        var email = "duplicate-" + Guid.NewGuid() + "@example.com";
        var registerRequest = new
        {
            tenantName = "Test Tenant",
            email = email,
            password = "SecurePassword123!",
            firstName = "Test",
            lastName = "User"
        };

        // Act - First registration
        var content1 = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json"
        );
        var response1 = await _httpClient.PostAsync("/api/auth/register", content1);
        response1.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Second registration with same email
        var content2 = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json"
        );
        var response2 = await _httpClient.PostAsync("/api/auth/register", content2);

        // Assert
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkAndAuthResponse()
    {
        // Arrange
        var email = "login-test-" + Guid.NewGuid() + "@example.com";
        var password = "SecurePassword123!";

        // Register first
        var registerRequest = new
        {
            tenantName = "Test Tenant",
            email = email,
            password = password,
            firstName = "Test",
            lastName = "User"
        };
        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json"
        );
        var registerResponse = await _httpClient.PostAsync("/api/auth/register", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Login
        var loginRequest = new { email = email, password = password };
        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json"
        );
        var loginResponse = await _httpClient.PostAsync("/api/auth/login", loginContent);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var jsonResponse = await loginResponse.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(jsonResponse);

        authResponse.Should().NotBeNull();
        authResponse!.AccessToken.Should().NotBeNullOrEmpty();
        authResponse.User.Email.Should().Be(email);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsBadRequest()
    {
        // Arrange
        var email = "invalid-pass-" + Guid.NewGuid() + "@example.com";
        var password = "SecurePassword123!";

        // Register first
        var registerRequest = new
        {
            tenantName = "Test Tenant",
            email = email,
            password = password,
            firstName = "Test",
            lastName = "User"
        };
        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json"
        );
        var registerResponse = await _httpClient.PostAsync("/api/auth/register", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Login with wrong password
        var loginRequest = new { email = email, password = "WrongPassword123!" };
        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json"
        );
        var loginResponse = await _httpClient.PostAsync("/api/auth/login", loginContent);

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        // Arrange - Register and get initial tokens
        var email = "refresh-" + Guid.NewGuid() + "@example.com";
        var password = "SecurePassword123!";

        var registerRequest = new
        {
            tenantName = "Test Tenant",
            email = email,
            password = password,
            firstName = "Test",
            lastName = "User"
        };
        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json"
        );
        var registerResponse = await _httpClient.PostAsync("/api/auth/register", registerContent);
        var registerJson = await registerResponse.Content.ReadAsStringAsync();
        var initialAuth = JsonSerializer.Deserialize<AuthResponse>(registerJson);

        // Act - Refresh token
        var refreshRequest = new { refreshToken = initialAuth!.RefreshToken };
        var refreshContent = new StringContent(
            JsonSerializer.Serialize(refreshRequest),
            Encoding.UTF8,
            "application/json"
        );
        var refreshResponse = await _httpClient.PostAsync("/api/auth/refresh", refreshContent);

        // Assert
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var refreshJson = await refreshResponse.Content.ReadAsStringAsync();
        var newAuth = JsonSerializer.Deserialize<AuthResponse>(refreshJson);

        newAuth.Should().NotBeNull();
        newAuth!.AccessToken.Should().NotBeNullOrEmpty();
        newAuth.AccessToken.Should().NotBe(initialAuth.AccessToken);
    }

    [Fact]
    public async Task GetCurrentUser_WithValidToken_ReturnsUserInfo()
    {
        // Arrange - Register first
        var email = "current-user-" + Guid.NewGuid() + "@example.com";
        var password = "SecurePassword123!";

        var registerRequest = new
        {
            tenantName = "Test Tenant",
            email = email,
            password = password,
            firstName = "John",
            lastName = "Doe"
        };
        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json"
        );
        var registerResponse = await _httpClient.PostAsync("/api/auth/register", registerContent);
        var registerJson = await registerResponse.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(registerJson);

        // Act - Get current user with token
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);
        var response = await _httpClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        var userInfo = JsonSerializer.Deserialize<JsonElement>(json);

        userInfo.GetProperty("email").GetString().Should().Be(email);
        userInfo.GetProperty("firstName").GetString().Should().Be("John");
        userInfo.GetProperty("lastName").GetString().Should().Be("Doe");
    }

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response = await _httpClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
