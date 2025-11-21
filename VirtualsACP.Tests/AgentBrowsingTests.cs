using VirtualsAcp.Models;

namespace VirtualsACP.Tests;

public class AgentBrowsingTests : TestBase
{
    [Fact]
    public async Task BrowseAgents_ShouldReturnResults()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var agents = await client.BrowseAgentsAsync("AI", topK: 5);

        // Assert
        Assert.NotNull(agents);
        Assert.NotEmpty(agents);
    }

    [Fact]
    public async Task BrowseAgents_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var client = CreateTestClient();
        var sortBy = new List<AcpAgentSort> { AcpAgentSort.successfulJobCount };

        // Act
        var agents = await client.BrowseAgentsAsync(
            keyword: "AI",
            topK: 3,
            sortBy: sortBy,
            graduationStatus: AcpGraduationStatus.All,
            onlineStatus: AcpOnlineStatus.All
        );

        // Assert
        Assert.NotNull(agents);
    }

    [Fact]
    public async Task BrowseAgents_ExcludesOwnWallet()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var agents = await client.BrowseAgentsAsync("AI", topK: 10);

        // Assert
        Assert.NotNull(agents);
        var ownWalletInResults = agents.Any(a => 
            a.WalletAddress.Equals(Config.AgentWalletAddress, StringComparison.OrdinalIgnoreCase));
        Assert.False(ownWalletInResults, "Own wallet should be excluded from results");
    }

    [Fact]
    public async Task BrowseAgents_FiltersByContractAddress()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var agents = await client.BrowseAgentsAsync("AI", topK: 10);

        // Assert
        Assert.NotNull(agents);
        foreach (var agent in agents)
        {
            if (!string.IsNullOrEmpty(agent.ContractAddress))
            {
                Assert.Equal(
                    ContractConfig.ContractAddress,
                    agent.ContractAddress,
                    StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public async Task GetAgent_ShouldReturnAgent()
    {
        // Arrange
        var client = CreateTestClient();
        
        // First, browse to get a valid agent address
        var agents = await client.BrowseAgentsAsync("AI", topK: 1);
        if (agents.Count == 0)
        {
            // Skip if no agents found
            return;
        }

        var agentAddress = agents[0].WalletAddress;

        // Act
        var agent = await client.GetAgentAsync(agentAddress);

        // Assert
        Assert.NotNull(agent);
        Assert.Equal(agentAddress, agent.WalletAddress, StringComparer.OrdinalIgnoreCase);
    }
}

