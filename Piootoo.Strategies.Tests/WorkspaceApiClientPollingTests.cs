using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using piootooapp.clientform;

namespace Piootoo.Strategies.Tests;

public sealed class WorkspaceApiClientPollingTests
{
    [Fact]
    public async Task Polling_CanBeCancelledLocallyWithoutServerCancel()
    {
        var handler = new RunningStatusHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var client = new WorkspaceApiClient(httpClient, new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        });
        using var cancellation = new CancellationTokenSource();
        cancellation.CancelAfter(100);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.PollBacktestingUntilTerminalAsync(
                "job-1",
                cancellationToken: cancellation.Token));

        Assert.True(handler.StatusRequests >= 1);
        Assert.Equal(0, handler.CancelRequests);
    }

    private sealed class RunningStatusHandler : HttpMessageHandler
    {
        public int StatusRequests;
        public int CancelRequests;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath.Contains("/cancel/", StringComparison.OrdinalIgnoreCase))
                Interlocked.Increment(ref CancelRequests);
            else
                Interlocked.Increment(ref StatusRequests);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """{"jobId":"job-1","status":"Running","progressPercent":25,"phase":"Running"}""",
                    Encoding.UTF8,
                    "application/json")
            });
        }
    }
}
