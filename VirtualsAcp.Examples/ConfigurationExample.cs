using Microsoft.Extensions.Logging;
using VirtualsAcp.Configs;
using VirtualsAcp.Models;

namespace VirtualsAcp.Examples;

/// <summary>
/// Example showing different configuration options
/// </summary>
public class ConfigurationExample
{
    public static async Task RunExample()
    {
        Console.WriteLine("=== Configuration Example ===");

        // Example 1: Using different network configurations
        Console.WriteLine("1. Using Base Sepolia (Testnet)");
        await CreateClientWithConfig(Configurations.BaseSepoliaConfig, "Base Sepolia");

        Console.WriteLine("\n2. Using Base Mainnet (Production)");
        await CreateClientWithConfig(Configurations.BaseMainnetConfig, "Base Mainnet");

        Console.WriteLine("\n3. Using Default Configuration");
        await CreateClientWithConfig(Configurations.DefaultConfig, "Default");

        // Example 2: Using environment variables
        Console.WriteLine("\n4. Using Environment Variables");
        await CreateClientWithEnvironmentVariables();
    }

    private static async Task CreateClientWithConfig(AcpContractConfig config, string configName)
    {
        try
        {
            var loggerFactory = LoggerFactory.Create(builder => 
                builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            var logger = loggerFactory.CreateLogger<VirtualsACPClient>();

            var client = new VirtualsACPClient(
                walletPrivateKey: "0000000000000000000000000000000000000000000000000000000000000001",
                config: config,
                logger: logger
            );

            Console.WriteLine($"  Configuration: {configName}");
            Console.WriteLine($"  Chain: {config.ChainEnv}");
            Console.WriteLine($"  RPC URL: {config.RpcUrl}");
            Console.WriteLine($"  Chain ID: {config.ChainId}");
            Console.WriteLine($"  Contract: {config.ContractAddress}");
            Console.WriteLine($"  Payment Token: {config.PaymentTokenAddress}");
            Console.WriteLine($"  API URL: {config.AcpApiUrl}");

            // Test basic functionality
            var agents = await client.BrowseAgentsAsync("test", topK: 1);
            Console.WriteLine($"  ✅ Successfully connected and found {agents.Count} agents");

            client.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Error with {configName}: {ex.Message}");
        }
    }

    private static Task CreateClientWithEnvironmentVariables()
    {
        try
        {
            // Load environment settings
            var envSettings = EnvSettings.FromEnvironment();
            
            Console.WriteLine("  Environment Variables:");
            Console.WriteLine($"    Private Key: {(string.IsNullOrEmpty(envSettings.WhitelistedWalletPrivateKey) ? "Not set" : "Set")}");
            Console.WriteLine($"    Buyer Agent: {envSettings.BuyerAgentWalletAddress ?? "Not set"}");
            Console.WriteLine($"    Seller Agent: {envSettings.SellerAgentWalletAddress ?? "Not set"}");
            Console.WriteLine($"    Evaluator Agent: {envSettings.EvaluatorAgentWalletAddress ?? "Not set"}");
            Console.WriteLine($"    Buyer Entity ID: {envSettings.BuyerEntityId?.ToString() ?? "Not set"}");
            Console.WriteLine($"    Seller Entity ID: {envSettings.SellerEntityId?.ToString() ?? "Not set"}");
            Console.WriteLine($"    Evaluator Entity ID: {envSettings.EvaluatorEntityId?.ToString() ?? "Not set"}");

            // Validate wallet addresses if they exist
            try
            {
                envSettings.ValidateWalletAddresses();
                Console.WriteLine("  ✅ Wallet addresses are valid");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"  ⚠️  Wallet address validation warning: {ex.Message}");
            }

            // Create client with environment settings
            if (!string.IsNullOrEmpty(envSettings.WhitelistedWalletPrivateKey))
            {
                var loggerFactory = LoggerFactory.Create(builder => 
                    builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
                var logger = loggerFactory.CreateLogger<VirtualsACPClient>();

                var client = new VirtualsACPClient(
                    walletPrivateKey: envSettings.WhitelistedWalletPrivateKey,
                    agentWalletAddress: envSettings.BuyerAgentWalletAddress,
                    config: Configurations.DefaultConfig,
                    logger: logger
                );

                Console.WriteLine("  ✅ Client created successfully with environment variables");
                client.Dispose();
            }
            else
            {
                Console.WriteLine("  ⚠️  Private key not set in environment variables");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ Error with environment variables: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}
