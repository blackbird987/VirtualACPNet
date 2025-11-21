using VirtualsAcp.Models;

namespace VirtualsACP.Tests;

public class MemoOperationsTests : TestBase
{
    [Fact]
    public async Task CreateMemo_ShouldCreateMemo()
    {
        // Arrange
        var client = CreateTestClient();
        
        // First, get an existing job or create one
        var activeJobs = await client.GetActiveJobsAsync(page: 1, pageSize: 1);
        if (activeJobs.Count == 0)
        {
            // Skip if no active jobs found
            return;
        }

        var jobId = activeJobs[0].Id;
        var message = new GenericPayload
        {
            Type = PayloadType.FundResponse,
            Data = new { message = "Test memo content" }
        };

        // Act
        var txHash = await client.SendMessageAsync(
            jobId: jobId,
            message: message,
            nextPhase: AcpJobPhase.Transaction
        );

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(txHash));
    }

    [Fact]
    public async Task SignMemo_ShouldSignMemo()
    {
        // Arrange
        var client = CreateTestClient();
        
        // First, get a job with pending memos
        var pendingJobs = await client.GetPendingMemoJobsAsync(page: 1, pageSize: 5);
        if (pendingJobs.Count == 0)
        {
            // Skip if no pending memo jobs found
            return;
        }

        ACPJob? jobWithMemo = null;
        ACPMemo? memoToSign = null;

        foreach (var job in pendingJobs)
        {
            if (job.Memos.Count > 0)
            {
                // Find a memo that needs signing (not already signed)
                var unsignedMemo = job.Memos.FirstOrDefault(m => 
                    m.Status != "ACCEPTED" && m.Status != "REJECTED");
                if (unsignedMemo != null)
                {
                    jobWithMemo = job;
                    memoToSign = unsignedMemo;
                    break;
                }
            }
        }

        if (jobWithMemo == null || memoToSign == null)
        {
            // Skip if no signable memo found
            return;
        }

        // Act
        var txHash = await client.SignMemoAsync(
            memoId: memoToSign.Id,
            accept: true,
            reason: "Test acceptance for integration testing"
        );

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(txHash));
    }

    [Fact]
    public async Task GetMemoById_ShouldReturnMemo()
    {
        // Arrange
        var client = CreateTestClient();
        
        // First, get a job with memos
        var activeJobs = await client.GetActiveJobsAsync(page: 1, pageSize: 5);
        if (activeJobs.Count == 0)
        {
            // Skip if no jobs found
            return;
        }

        ACPJob? jobWithMemos = null;
        foreach (var job in activeJobs)
        {
            if (job.Memos.Count > 0)
            {
                jobWithMemos = job;
                break;
            }
        }

        if (jobWithMemos == null || jobWithMemos.Memos.Count == 0)
        {
            // Skip if no memos found
            return;
        }

        var jobId = jobWithMemos.Id;
        var memoId = jobWithMemos.Memos[0].Id;

        // Act
        var memo = await client.GetMemoByIdAsync(jobId, memoId);

        // Assert
        Assert.NotNull(memo);
        Assert.Equal(memoId, memo.Id);
    }
}

