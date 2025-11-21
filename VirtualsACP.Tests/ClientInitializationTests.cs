using VirtualsAcp.Models;

namespace VirtualsACP.Tests;

public class ClientInitializationTests : TestBase
{
    [Fact]
    public void ClientCreation_WithValidCredentials_ShouldSucceed()
    {
        // Arrange & Act
        var client = CreateTestClient();

        // Assert
        Assert.NotNull(client);
        Assert.NotNull(client.AgentAddress);
        Assert.NotNull(client.SignerAddress);
    }

    [Fact]
    public void ClientProperties_ShouldBeSet()
    {
        // Arrange
        var client = CreateTestClient();

        // Act & Assert
        Assert.False(string.IsNullOrWhiteSpace(client.AgentAddress));
        Assert.False(string.IsNullOrWhiteSpace(client.SignerAddress));
        Assert.Equal(Config.AgentWalletAddress, client.AgentAddress, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void ClientDisposal_ShouldCleanup()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        client.Dispose();

        // Assert - No exception should be thrown
        Assert.True(true);
    }
}

