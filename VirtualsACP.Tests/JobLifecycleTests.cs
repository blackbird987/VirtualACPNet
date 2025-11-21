using Microsoft.Extensions.Logging;
using VirtualsAcp.Models;

namespace VirtualsACP.Tests;

public class JobLifecycleTests : TestBase
{
    [Fact]
    public async Task FullJobLifecycle_ShouldCompleteSuccessfully()
    {
        // Arrange - Create buyer and seller clients
        var buyer = CreateBuyerClient();
        var seller = CreateSellerClient();

        var jobCreated = false;
        var jobId = 0;
        var sellerResponded = false;
        var buyerPaid = false;
        var sellerDelivered = false;
        var buyerApproved = false;

        // Set up seller event handlers (responds to job and delivers)
        seller.OnNewTask += async (job, memoToSign) =>
        {
            if (job.Phase == AcpJobPhase.Request && 
                job.Memos.Any(x => x.NextPhase == AcpJobPhase.Negotiation))
            {
                // Seller accepts the job
                var responseTxHash = await seller.RespondToJobAsync(
                    jobId: job.Id,
                    memoId: memoToSign!.Id,
                    accept: true,
                    content: "I can complete this project within 3 days",
                    reason: "I have experience with similar projects"
                );
                sellerResponded = true;
                Logger?.LogInformation("Seller responded to job {JobId}: {TxHash}", job.Id, responseTxHash);
            }
            else if (job.Phase == AcpJobPhase.Transaction && 
                     job.Memos.Any(x => x.NextPhase == AcpJobPhase.Evaluation))
            {
                // Seller delivers the work
                var deliverable = new Deliverable
                {
                    Type = "test",
                    Value = new
                    {
                        message = "Test deliverable for integration testing",
                        completed = true,
                        timestamp = DateTime.UtcNow.ToString("O")
                    }
                };

                var deliveryTxHash = await seller.DeliverJobAsync(job.Id, deliverable);
                sellerDelivered = true;
                Logger?.LogInformation("Seller delivered job {JobId}: {TxHash}", job.Id, deliveryTxHash);
            }
        };

        // Set up buyer event handlers (pays and approves delivery)
        buyer.OnNewTask += async (job, memoToSign) =>
        {
            if (job.Phase == AcpJobPhase.Negotiation && 
                memoToSign?.NextPhase == AcpJobPhase.Transaction)
            {
                // Buyer pays for the job
                var paymentResult = await buyer.PayJobAsync(
                    jobId: job.Id,
                    memoId: memoToSign.Id,
                    amount: 0.01m,
                    reason: "Payment for completed work"
                );
                buyerPaid = true;
                Logger?.LogInformation("Buyer paid for job {JobId}: {TxHash}", job.Id, paymentResult["txHash"]);
            }
            else if (memoToSign?.Type == "ObjectUrl" && 
                     memoToSign?.NextPhase == AcpJobPhase.Completed)
            {
                // Buyer approves delivery
                await buyer.SignMemoAsync(
                    memoId: memoToSign.Id,
                    accept: true,
                    reason: "Delivery approved, job complete"
                );
                buyerApproved = true;
                Logger?.LogInformation("Buyer approved delivery for job {JobId}", job.Id);
            }
            else if (job.Phase == AcpJobPhase.Completed)
            {
                Logger?.LogInformation("Job {JobId} completed", job.Id);
            }
        };

        try
        {
            // Start both clients
            await seller.StartAsync(Config.SellerWalletAddress);
            await buyer.StartAsync(Config.BuyerWalletAddress, evaluatorAddress: Config.BuyerWalletAddress);

            // Act - Buyer creates a job
            var serviceRequirement = new
            {
                name = "Test Service",
                requirement = "This is a test service requirement for integration testing",
                priceType = "fixed",
                priceValue = 0.01m
            };

            jobId = await buyer.InitiateJobAsync(
                providerAddress: Config.SellerWalletAddress,
                serviceRequirement: serviceRequirement,
                amount: 0.01m,
                expiredAt: DateTime.UtcNow.AddDays(1)
            );

            jobCreated = true;
            Logger?.LogInformation("Buyer created job {JobId}", jobId);

            // Wait for workflow to complete (with timeout)
            var timeout = TimeSpan.FromMinutes(5);
            var startTime = DateTime.UtcNow;
            
            while (!buyerApproved && (DateTime.UtcNow - startTime) < timeout)
            {
                await Task.Delay(5000);
                
                // Check job status
                var job = await buyer.GetJobByIdAsync(jobId);
                if (job != null && job.Phase == AcpJobPhase.Completed)
                {
                    break;
                }
            }

            // Assert
            Assert.True(jobCreated, "Job should be created");
            Assert.True(jobId > 0, "Job ID should be valid");
            Assert.True(sellerResponded, "Seller should respond to job");
            Assert.True(buyerPaid, "Buyer should pay for job");
            Assert.True(sellerDelivered, "Seller should deliver work");
            Assert.True(buyerApproved, "Buyer should approve delivery");

            // Verify final job state
            var finalJob = await buyer.GetJobByIdAsync(jobId);
            Assert.NotNull(finalJob);
            Assert.Equal(jobId, finalJob.Id);
            Assert.Equal(AcpJobPhase.Completed, finalJob.Phase);
        }
        finally
        {
            buyer.Dispose();
            seller.Dispose();
        }
    }

    [Fact]
    public async Task InitiateJob_ShouldCreateJobOnBlockchain()
    {
        // Arrange
        var buyer = CreateBuyerClient();

        var serviceRequirement = new
        {
            name = "Test Service",
            requirement = "This is a test service requirement for integration testing",
            priceType = "fixed",
            priceValue = 0.01m
        };

        // Act
        var jobId = await buyer.InitiateJobAsync(
            providerAddress: Config.SellerWalletAddress,
            serviceRequirement: serviceRequirement,
            amount: 0.01m,
            expiredAt: DateTime.UtcNow.AddDays(1)
        );

        // Assert
        Assert.True(jobId > 0);
        
        buyer.Dispose();
    }

    [Fact]
    public async Task GetJobById_AfterCreation_ShouldReturnJob()
    {
        // Arrange
        var buyer = CreateBuyerClient();

        var serviceRequirement = new
        {
            name = "Test Service",
            requirement = "This is a test service requirement for integration testing",
            priceType = "fixed",
            priceValue = 0.01m
        };

        // Act - Create job
        var jobId = await buyer.InitiateJobAsync(
            providerAddress: Config.SellerWalletAddress,
            serviceRequirement: serviceRequirement,
            amount: 0.01m,
            expiredAt: DateTime.UtcNow.AddDays(1)
        );

        // Wait a bit for the job to be indexed
        await Task.Delay(5000);

        // Act - Retrieve job
        var job = await buyer.GetJobByIdAsync(jobId);

        // Assert
        Assert.NotNull(job);
        Assert.Equal(jobId, job.Id);
        Assert.Equal(AcpJobPhase.Request, job.Phase);
        
        buyer.Dispose();
    }

    [Fact]
    public async Task Memos_ShouldBeCreated()
    {
        // Arrange
        var buyer = CreateBuyerClient();

        var serviceRequirement = new
        {
            name = "Test Service",
            requirement = "This is a test service requirement for integration testing",
            priceType = "fixed",
            priceValue = 0.01m
        };

        // Act - Create job
        var jobId = await buyer.InitiateJobAsync(
            providerAddress: Config.SellerWalletAddress,
            serviceRequirement: serviceRequirement,
            amount: 0.01m,
            expiredAt: DateTime.UtcNow.AddDays(1)
        );

        // Wait for job and memos to be indexed
        await Task.Delay(5000);

        // Act - Retrieve job
        var job = await buyer.GetJobByIdAsync(jobId);

        // Assert - Job should have at least one memo (the initial negotiation memo)
        Assert.NotNull(job);
        Assert.True(job.Memos.Count > 0, "Job should have at least one memo");
        
        var negotiationMemo = job.Memos.FirstOrDefault(m => m.NextPhase == AcpJobPhase.Negotiation);
        Assert.NotNull(negotiationMemo);
        
        buyer.Dispose();
    }
}
