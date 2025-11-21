using System.Text.Json;
using Microsoft.Extensions.Logging;
using VirtualsAcp.Configs;
using VirtualsAcp.Models;

namespace VirtualsACP.Tests;

public abstract class TestBase : IDisposable
{
    protected TestConfig Config { get; private set; } = null!;
    protected ILogger? Logger { get; private set; }
    protected AcpContractConfig ContractConfig { get; private set; } = null!;

    protected TestBase()
    {
        LoadConfig();
        SetupLogger();
        SetupContractConfig();
    }

    private void LoadConfig()
    {
        // Try multiple locations for the config file
        // 1. Current directory (project root when running tests)
        // 2. Test project directory (relative to assembly)
        // 3. Output directory
        var assemblyLocation = typeof(TestBase).Assembly.Location;
        var assemblyDir = Path.GetDirectoryName(assemblyLocation) ?? "";
        
        var possiblePaths = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "test-config.json"),
            Path.Combine(assemblyDir, "test-config.json"),
            Path.Combine(AppContext.BaseDirectory, "test-config.json"),
        };

        string? configPath = null;
        foreach (var path in possiblePaths)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    configPath = fullPath;
                    break;
                }
            }
            catch
            {
                // Skip invalid paths
                continue;
            }
        }

        if (configPath == null)
        {
            var searchedPaths = string.Join("\n  - ", possiblePaths.Select(p => {
                try { return Path.GetFullPath(p); } catch { return p; }
            }));
            throw new FileNotFoundException(
                $"test-config.json not found. Searched in:\n  - {searchedPaths}\n\n" +
                "Please copy test-config.example.json to test-config.json in the test project directory and fill in your credentials.");
        }

        var json = File.ReadAllText(configPath);
        Config = JsonSerializer.Deserialize<TestConfig>(json) 
            ?? throw new InvalidOperationException("Failed to deserialize test-config.json");
    }

    private void SetupLogger()
    {
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        Logger = loggerFactory.CreateLogger<VirtualsACPClient>();
    }

    private void SetupContractConfig()
    {
        ContractConfig = Config.Network.ToLowerInvariant() switch
        {
            "base" or "base-mainnet" => Configurations.BaseMainnetConfigV2,
            "base-sepolia" => Configurations.BaseSepoliaConfigV2,
            _ => throw new ArgumentException($"Unknown network: {Config.Network}")
        };
    }

    protected VirtualsACPClient CreateTestClient()
    {
        // Support legacy config for Phase 1 tests
        var privateKey = Config.WalletPrivateKey ?? Config.BuyerPrivateKey;
        var walletAddress = Config.AgentWalletAddress ?? Config.BuyerWalletAddress;

        if (string.IsNullOrWhiteSpace(privateKey))
        {
            throw new InvalidOperationException("WalletPrivateKey or BuyerPrivateKey is not set in test-config.json");
        }

        if (string.IsNullOrWhiteSpace(walletAddress))
        {
            throw new InvalidOperationException("AgentWalletAddress or BuyerWalletAddress is not set in test-config.json");
        }

        return new VirtualsACPClient(
            walletPrivateKey: privateKey,
            agentWalletAddress: walletAddress,
            config: ContractConfig,
            logger: Logger
        );
    }

    protected VirtualsACPClient CreateBuyerClient()
    {
        if (string.IsNullOrWhiteSpace(Config.BuyerPrivateKey))
        {
            throw new InvalidOperationException("BuyerPrivateKey is not set in test-config.json");
        }

        if (string.IsNullOrWhiteSpace(Config.BuyerWalletAddress))
        {
            throw new InvalidOperationException("BuyerWalletAddress is not set in test-config.json");
        }

        return new VirtualsACPClient(
            walletPrivateKey: Config.BuyerPrivateKey,
            agentWalletAddress: Config.BuyerWalletAddress,
            config: ContractConfig,
            logger: Logger
        );
    }

    protected VirtualsACPClient CreateSellerClient()
    {
        if (string.IsNullOrWhiteSpace(Config.SellerPrivateKey))
        {
            throw new InvalidOperationException("SellerPrivateKey is not set in test-config.json");
        }

        if (string.IsNullOrWhiteSpace(Config.SellerWalletAddress))
        {
            throw new InvalidOperationException("SellerWalletAddress is not set in test-config.json");
        }

        return new VirtualsACPClient(
            walletPrivateKey: Config.SellerPrivateKey,
            agentWalletAddress: Config.SellerWalletAddress,
            config: ContractConfig,
            logger: Logger
        );
    }

    public virtual void Dispose()
    {
        Logger?.LogInformation("Test cleanup completed");
    }
}

