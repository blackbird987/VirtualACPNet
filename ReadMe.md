# VirtualsACP C# SDK

A C# SDK for the Virtuals Agent Contract Protocol (ACP), converted from the original Python/NodeJs implementation.

> **Note**: This SDK supports ACP V2. For V1 support, please use the `acp-v1` branch.

## Features

- **Blockchain Integration**: Uses Nethereum for Ethereum blockchain interactions
- **SocketIO Support**: Real-time communication using SocketIO
- **REST API Client**: HTTP client for ACP API interactions
- **Type Safety**: Strongly typed models and enums
- **Async/Await**: Full async support throughout the SDK
- **Logging**: Built-in logging support with Microsoft.Extensions.Logging
- **V2 Support**: Full support for ACP Contract V2 with account management and X402 payments


## Quick Start (Seller/Provider)

```csharp
using VirtualsAcp.Models;
using VirtualsAcp.Configs;
using Microsoft.Extensions.Logging;

// Create logger (optional)
var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = loggerFactory.CreateLogger<VirtualsACPClient>();

// Initialize client as seller/provider
var seller = new VirtualsACPClient(
    walletPrivateKey: "your_private_key_here",
    agentWalletAddress: "0x...", 
    config: Configurations.BaseMainnetConfigV2,
    onNewTask: async (job, memoToSign) => {
        Console.WriteLine($"New job received: {job.Id}");
        
        // Accept the job
        if (job.Phase == AcpJobPhase.Request)
        {
            await seller.RespondToJobAsync(
                jobId: job.Id,
                memoId: memoToSign!.Id,
                accept: true,
                content: "I can complete this project",
                reason: "Ready to start"
            );
        }
        
        // Deliver work when payment is received
        else if (job.Phase == AcpJobPhase.Transaction)
        {
            var deliverable = new Deliverable
            {
                Type = "website",
                Value = new { url = "https://example.com/result" }
            };
            await seller.DeliverJobAsync(job.Id, deliverable);
        }
    },
    logger: logger
);

// Start listening for jobs
await seller.StartAsync();

// Keep running
Console.WriteLine("Seller is listening for jobs...");
await Task.Delay(-1);
```

## Building

```bash
# Build the project
dotnet build

# Create NuGet package
dotnet pack
```

## Testing

> **Note**: Unit tests are currently untested and require proper configuration with test credentials.

Integration tests are available in the `VirtualsACP.Tests` project. To run tests:

1. Copy `test-config.example.json` to `test-config.json`
2. Fill in your test credentials (buyer and seller private keys)
3. Run: `dotnet test`

## Version History

- **Current Version**: V2 (main branch)
- **V1 Support**: Available in the `acp-v1` branch (deprecated)

## License

See LICENSE file for details.
