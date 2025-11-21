using Microsoft.Extensions.Logging;
using VirtualsAcp.Configs;
using VirtualsAcp.Models;

namespace VirtualsAcp.Examples;

/// <summary>
/// Example showing how to manage jobs (create, respond, pay, deliver)
/// </summary>
public class JobManagementExample
{
    public static async Task RunExample()
    {
        Console.WriteLine("=== Job Management Example ===");

        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<VirtualsACPClient>();

        // ⚠️ CONFIGURATION REQUIRED:
        // Set the seller address (where to send jobs)
        
        // Provider (seller) - agent address (running separately)
        // We only need the ADDRESS to send jobs to, not the private key
        string agentWalletSeller = ""; 
        
        // Requester (buyer) - Test requester credentials (pre-configured)
        string privateKeyBuyer = ""; 
        string agentWalletBuyer = ""; 

        // Validate configuration
        if (string.IsNullOrWhiteSpace(agentWalletSeller))
        {
            Console.WriteLine("❌ ERROR: seller address not set!");
            Console.WriteLine("Please set agentWalletSeller in JobManagementExample.cs (line 26)");
            Console.WriteLine("\nExample: agentWalletSeller = \"0x1234...\";");
            return;
        }

        Console.WriteLine($"✅ Sending jobs to: {agentWalletSeller}");
        Console.WriteLine($"✅ Using test buyer: {agentWalletBuyer}");     


        // Initialize client with event handlers
        // COMMENTED OUT: Seller/provider functionality is not tested in this example
        // We only need the buyer/client in this example to avoid wallet conflicts
        // VirtualsACPClient provider = null;
        VirtualsACPClient client = null;
        
        /* PROVIDER COMMENTED OUT - example is currently only tested as buyer
        provider = new VirtualsACPClient(
            walletPrivateKey: privateKeySeller,
            agentWalletAddress: agentWalletSeller,
            config: Configurations.BaseMainnetConfig,
            onNewTask: async (job, memoToSign) =>
            {
                LogTask(job, memoToSign);
               if (job.Phase == AcpJobPhase.Request && job.Memos.Any(x => x.NextPhase == AcpJobPhase.Negotiation))
                {
                    Console.WriteLine("\nResponding to job...");
                    var responseTxHash = await provider.RespondToJobAsync(
                        jobId: job.Id,
                        memoId: memoToSign.Id,
                        accept: true,
                        content: "I can complete this project within 3 days",
                        reason: "I have experience with similar projects"
                    );
                    Console.WriteLine($"✅ Job response sent: {responseTxHash}");
                }
                else if (job.Phase == AcpJobPhase.Transaction && job.Memos.Any(x => x.NextPhase == AcpJobPhase.Evaluation))
                {
                    Console.WriteLine("Delivering job");
                    var deliverable = new Deliverable
                    {
                        Type = "website",
                        Value = new
                        {
                            url = "https://example.com/website",
                            files = new[] { "index.html", "style.css", "script.js" },
                            description = "Responsive website with contact form"
                        }
                    };

                    var deliveryTxHash = await provider.DeliverJobAsync(job.Id, deliverable);
                    Console.WriteLine($"✅ Work delivered: {deliveryTxHash}");
                }
            },
            onEvaluate: async (job, memo) =>
            {
                // shouldn't be called on provider
                Console.WriteLine($"🔍 Evaluating job: {job.Id}");
                //Simulate evaluation logic
                await Task.Delay(1000);
                return (true, "Work completed successfully");
            },
            logger: logger
        );
        */

        client = new VirtualsACPClient(
           walletPrivateKey: privateKeyBuyer,
           agentWalletAddress: agentWalletBuyer,
           config: Configurations.BaseMainnetConfig,
           onNewTask: async (job, memoToSign) =>
            {
                LogTask(job, memoToSign);
                
                if (job.Phase == AcpJobPhase.Negotiation && memoToSign?.NextPhase == AcpJobPhase.Transaction)
                {
                    Console.WriteLine($"💰 Paying for job {job.Id}...");
                    var paymentResult = await client.PayJobAsync(
                        jobId: job.Id,
                        memoId: memoToSign.Id,
                        amount: 0.1m,
                        reason: "Payment for completed work"
                    );

                    Console.WriteLine($"✅ Payment processed: {paymentResult["txHash"]}");
                }
                else if (memoToSign?.Type == "DELIVER_SERVICE" && memoToSign?.NextPhase == AcpJobPhase.Completed)
                {
                    Console.WriteLine($"📦 Approving delivery for job {job.Id}...");
                    
                    await client.SignMemoAsync(
                        memoId: memoToSign.Id,
                        accept: true,
                        reason: "Delivery approved, job complete"
                    );
                    
                    Console.WriteLine($"✅ Delivery approved, job moving to completion");
                }
                else if (job.Phase == AcpJobPhase.Completed)
                {
                    Console.WriteLine($"✅ Job completed: {job.Id}");
                }
                else if (job.Phase == AcpJobPhase.Rejected)
                {
                    Console.WriteLine($"\n❌ Job {job.Id} was REJECTED");
                    
                    var rejectedMemo = job.Memos.FirstOrDefault(m => m.Status == "REJECTED");
                    if (rejectedMemo != null)
                    {
                        Console.WriteLine($"Rejection reason: {rejectedMemo.SignedReason}");
                        if (!string.IsNullOrWhiteSpace(rejectedMemo.Content))
                        {
                            Console.WriteLine($"\nButler/Buyer would see:\n{rejectedMemo.Content}\n");
                        }
                    }
                }
            },
            onEvaluate: async (job, memo) =>
            {
                // Note: This callback only fires for external evaluation scenarios (when buyer ≠ evaluator)
                Console.WriteLine($"🔍 Evaluating delivered job {job.Id}...");
                
                await Task.Delay(1000);
                
                Console.WriteLine($"✅ Auto-approving job {job.Id}");
                return (true, "Job delivered successfully, payment approved");
            },
           logger: logger
       );

        try
        {
            // await provider.StartAsync(); // provider (seller) functionality is not tested in this example
            
            // Start client with evaluatorAddress to receive onEvaluate events
            // In self-evaluation scenarios (buyer = evaluator), this is required for onEvaluate to fire
            await client.StartAsync(agentWalletBuyer, evaluatorAddress: agentWalletBuyer);

            // Example 1: Create a job (Real trading request)
            Console.WriteLine("Creating a new job...");
            var jobId = await client.InitiateJobAsync(
                providerAddress: agentWalletSeller,
                serviceRequirement: "Long BTC at 105k with $100 total. Add take profit at 108k and stop loss at 103k",
                amount: 0.1m,
                expiredAt: DateTime.UtcNow.AddDays(7)
            );

            Console.WriteLine($"✅ Job created with ID: {jobId}");

            await Task.Delay(10_000); // wait else memos might be empty
            // wait indefinetly, rest should be handled in event han
            Console.WriteLine("Waiting indefinetly, all logic should be handled by events");
            Console.ReadLine();
            return;          
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");
            logger.LogError(ex, "Error in job management example");
        }
        finally
        {
            // provider.Dispose(); // provider (seller) functionality is not tested in this example
            client.Dispose();
        }
    }

    private static void LogTask(ACPJob job, ACPMemo? memoToSign)
    {
        Console.WriteLine($"📨 New task received: Job {job.Id}");
        Console.WriteLine($"   Service: {job.ServiceName}");
        Console.WriteLine($"   Provider: {job.ProviderAddress}");
        Console.WriteLine($"   Price: {job.Price}");
        Console.WriteLine($"   Phase: {job.Phase}");
        Console.WriteLine($"   Memos: {job.Memos.Count}");

        if (memoToSign != null)
            Console.WriteLine($"   Memo to sign: {memoToSign.Id}");
    }

    private static async Task<ACPJob?> RefreshJob(VirtualsACPClient client, int jobId)
    {
        var job = await client.GetJobByIdAsync(jobId);
        if (job != null)
        {
            Console.WriteLine($"Job {job.Id} details:");
            Console.WriteLine($"  Status: {job.Phase}");
            Console.WriteLine($"  Provider: {job.ProviderAddress}");
            Console.WriteLine($"  Client: {job.ClientAddress}");
            Console.WriteLine($"  Evaluator: {job.EvaluatorAddress}");
            Console.WriteLine($"  Price: {job.Price}");
            Console.WriteLine($"  Memos: {job.Memos.Count}");
        }

        return job;
    }
}
