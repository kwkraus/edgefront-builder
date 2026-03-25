using EdgeFront.Builder.Features.Sessions;
using EdgeFront.Builder.Features.Sessions.Dtos;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EdgeFront.Builder.Tests.Features.Sessions;

/// <summary>
/// Unit tests for RegistrationParsingService configuration and validation.
/// Full API integration tests are deferred to integration test suite.
/// </summary>
public class RegistrationParsingServiceTests
{
    [Fact]
    public void Constructor_WithValidConfiguration_InitializesSuccessfully()
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        var httpClientFactory = new Mock<IHttpClientFactory>();

        // Act & Assert - Should not throw
        var service = new RegistrationParsingService(httpClientFactory.Object, config.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullHttpClientFactory_ThrowsArgumentNullException()
    {
        // Arrange
        var config = new Mock<IConfiguration>();

        // Act & Assert
        var act = () => new RegistrationParsingService(null!, config.Object);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClientFactory = new Mock<IHttpClientFactory>();

        // Act & Assert
        var act = () => new RegistrationParsingService(httpClientFactory.Object, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ParseRegistrationsAsync_WithMissingEndpoint_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["AzureAI:Endpoint"]).Returns((string?)null);
        config.Setup(c => c["AzureAI:ProjectName"]).Returns("test-project");
        
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var service = new RegistrationParsingService(httpClientFactory.Object, config.Object);

        // Act & Assert
        var act = async () => await service.ParseRegistrationsAsync("data", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Azure AI Foundry configuration is missing*");
    }

    [Fact]
    public async Task ParseRegistrationsAsync_WithMissingProjectName_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["AzureAI:Endpoint"]).Returns("https://example.com");
        config.Setup(c => c["AzureAI:ProjectName"]).Returns((string?)null);
        
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var service = new RegistrationParsingService(httpClientFactory.Object, config.Object);

        // Act & Assert
        var act = async () => await service.ParseRegistrationsAsync("data", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ParseRegistrationsAsync_WithEmptyInput_ReturnsEmptyList()
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var service = new RegistrationParsingService(httpClientFactory.Object, config.Object);

        // Act
        var result = await service.ParseRegistrationsAsync("", CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseRegistrationsAsync_WithWhitespaceInput_ReturnsEmptyList()
    {
        // Arrange
        var config = new Mock<IConfiguration>();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var service = new RegistrationParsingService(httpClientFactory.Object, config.Object);

        // Act
        var result = await service.ParseRegistrationsAsync("   \n\n   ", CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
