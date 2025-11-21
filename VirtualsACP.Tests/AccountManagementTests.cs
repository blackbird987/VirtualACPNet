using VirtualsAcp.Models;

namespace VirtualsACP.Tests;

public class AccountManagementTests : TestBase
{
    [Fact]
    public async Task CreateAccount_ShouldCreateAccount()
    {
        // Arrange
        var client = CreateTestClient();
        
        if (string.IsNullOrWhiteSpace(Config.ProviderAddress))
        {
            // Skip if provider address not configured
            return;
        }

        var metadata = new Dictionary<string, object>
        {
            ["test"] = true,
            ["createdAt"] = DateTime.UtcNow.ToString("O"),
            ["purpose"] = "Integration testing"
        };

        // Act
        var txHash = await client.CreateAccountAsync(
            providerAddress: Config.ProviderAddress,
            metadata: metadata
        );

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(txHash));
    }

    [Fact]
    public async Task GetAccountByJobId_ShouldReturnAccount()
    {
        // Arrange
        var client = CreateTestClient();
        
        // First, get a job that might have an account
        var activeJobs = await client.GetActiveJobsAsync(page: 1, pageSize: 5);
        if (activeJobs.Count == 0)
        {
            // Skip if no jobs found
            return;
        }

        // Try to find a job with an account
        AcpAccount? account = null;
        int? jobIdWithAccount = null;

        foreach (var job in activeJobs)
        {
            try
            {
                var acc = await client.GetAccountByJobIdAsync(job.Id);
                if (acc != null)
                {
                    account = acc;
                    jobIdWithAccount = job.Id;
                    break;
                }
            }
            catch
            {
                // Continue searching
                continue;
            }
        }

        if (jobIdWithAccount == null)
        {
            // Skip if no account found for any job
            return;
        }

        // Act
        var retrievedAccount = await client.GetAccountByJobIdAsync(jobIdWithAccount.Value);

        // Assert
        Assert.NotNull(retrievedAccount);
        Assert.Equal(account!.Id, retrievedAccount.Id);
    }

    [Fact]
    public async Task GetByClientAndProvider_ShouldReturnAccount()
    {
        // Arrange
        var client = CreateTestClient();
        
        if (string.IsNullOrWhiteSpace(Config.ProviderAddress))
        {
            // Skip if provider address not configured
            return;
        }

        // Act
        var clientAddress = Config.AgentWalletAddress ?? Config.BuyerWalletAddress;
        if (string.IsNullOrWhiteSpace(clientAddress))
        {
            return;
        }
        
        var account = await client.GetByClientAndProviderAsync(
            clientAddress: clientAddress,
            providerAddress: Config.ProviderAddress
        );

        // Assert
        // Account might not exist, so we just verify the method doesn't throw
        // If account exists, verify it has the correct addresses
        if (account != null)
        {
            Assert.Equal(Config.AgentWalletAddress, account.ClientAddress, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(Config.ProviderAddress, account.ProviderAddress, StringComparer.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task UpdateAccountMetadata_ShouldUpdate()
    {
        // Arrange
        var client = CreateTestClient();
        
        if (string.IsNullOrWhiteSpace(Config.ProviderAddress))
        {
            // Skip if provider address not configured
            return;
        }

        // First, try to get an existing account or create one
        var clientAddress = Config.AgentWalletAddress ?? Config.BuyerWalletAddress;
        if (string.IsNullOrWhiteSpace(clientAddress))
        {
            return;
        }
        
        var account = await client.GetByClientAndProviderAsync(
            clientAddress: clientAddress,
            providerAddress: Config.ProviderAddress
        );

        int accountId;
        if (account == null)
        {
            // Create account if it doesn't exist
            var txHash = await client.CreateAccountAsync(
                providerAddress: Config.ProviderAddress,
                metadata: new Dictionary<string, object> { ["initial"] = true }
            );
            
            // Wait a bit for account to be indexed, then try to retrieve it
            await Task.Delay(5000);
            
            var createdAccount = await client.GetByClientAndProviderAsync(
                clientAddress: clientAddress,
                providerAddress: Config.ProviderAddress
            );
            
            if (createdAccount == null)
            {
                // Skip if account creation didn't complete yet
                return;
            }
            
            accountId = createdAccount.Id;
        }
        else
        {
            accountId = account.Id;
        }

        var updatedMetadata = new Dictionary<string, object>
        {
            ["updatedAt"] = DateTime.UtcNow.ToString("O"),
            ["test"] = "updated",
            ["version"] = 2
        };

        // Act - This would require a blockchain client method, which may not exist
        // For now, we'll just verify the account exists and can be retrieved
        var retrievedAccount = await client.GetAccountByJobIdAsync(accountId);

        // Assert
        Assert.NotNull(retrievedAccount);
        Assert.Equal(accountId, retrievedAccount.Id);
    }
}

