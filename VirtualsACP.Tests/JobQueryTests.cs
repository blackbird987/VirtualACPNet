using VirtualsAcp.Models;

namespace VirtualsACP.Tests;

public class JobQueryTests : TestBase
{
    [Fact]
    public async Task GetActiveJobs_ShouldReturnJobs()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var jobs = await client.GetActiveJobsAsync(page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(jobs);
    }

    [Fact]
    public async Task GetCompletedJobs_ShouldReturnJobs()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var jobs = await client.GetCompletedJobsAsync(page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(jobs);
    }

    [Fact]
    public async Task GetCancelledJobs_ShouldReturnJobs()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var jobs = await client.GetCancelledJobsAsync(page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(jobs);
    }

    [Fact]
    public async Task GetPendingMemoJobs_ShouldReturnJobs()
    {
        // Arrange
        var client = CreateTestClient();

        // Act
        var jobs = await client.GetPendingMemoJobsAsync(page: 1, pageSize: 10);

        // Assert
        Assert.NotNull(jobs);
    }

    [Fact]
    public async Task GetJobById_ShouldReturnJob()
    {
        // Arrange
        var client = CreateTestClient();
        
        // First, get active jobs to find a valid job ID
        var activeJobs = await client.GetActiveJobsAsync(page: 1, pageSize: 1);
        int testJobId;
        
        if (activeJobs.Count == 0)
        {
            // Try completed jobs
            var completedJobs = await client.GetCompletedJobsAsync(page: 1, pageSize: 1);
            if (completedJobs.Count == 0)
            {
                // Skip if no jobs found
                return;
            }
            testJobId = completedJobs[0].Id;
        }
        else
        {
            testJobId = activeJobs[0].Id;
        }

        // Act
        var job = await client.GetJobByIdAsync(testJobId);

        // Assert
        Assert.NotNull(job);
        Assert.Equal(testJobId, job.Id);
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

