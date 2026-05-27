using System.Net;

namespace McpServer.Integration.Tests;

public class RateLimitingTests
{
    [ClassDataSource<WebApplicationFactory>(Shared = SharedType.PerTestSession)]
    public required WebApplicationFactory Factory { get; init; }

    private const int FixedPermitLimit = 10; // Fixed policy from appsettings.Development.json

    // ──────────────────────────────────────────────────────────
    // / endpoint works
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task RootEndpoint_ReturnsExpectedBody()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).IsEqualTo("this is working");
    }

    // ──────────────────────────────────────────────────────────
    // Rate limit header is present
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task RootEndpoint_ReturnsRateLimitHeader()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/");

        await Assert.That(response.Headers.Contains("X-Rate-Limit-Limit")).IsTrue();
        await Assert.That(response.Headers.GetValues("X-Rate-Limit-Limit").First())
            .IsEqualTo(FixedPermitLimit.ToString());
    }

    // ──────────────────────────────────────────────────────────
    // Rate limit is enforced after exceeding permit limit
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task RootEndpoint_Returns429_WhenRateLimitExceeded()
    {
        var client = Factory.CreateClient();

        // Exhaust all Fixed policy permits
        for (int i = 0; i < FixedPermitLimit; i++)
            await client.GetAsync("/");

        // Next request should be rate-limited
        var response = await client.GetAsync("/");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
    }

    // ──────────────────────────────────────────────────────────
    // Rate limit rejection includes Retry-After header
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task RootEndpoint_429_IncludesRetryAfterHeader()
    {
        var client = Factory.CreateClient();

        for (int i = 0; i < FixedPermitLimit; i++)
            await client.GetAsync("/");

        var response = await client.GetAsync("/");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);
        await Assert.That(response.Headers.Contains("Retry-After")).IsTrue();

        var retryAfter = int.Parse(response.Headers.GetValues("Retry-After").First());
        await Assert.That(retryAfter).IsPositive();
    }

    // ──────────────────────────────────────────────────────────
    // Rate limit rejection body
    // ──────────────────────────────────────────────────────────

    [Test]
    public async Task RootEndpoint_429_ReturnsPlainTextBody()
    {
        var client = Factory.CreateClient();

        for (int i = 0; i < FixedPermitLimit; i++)
            await client.GetAsync("/");

        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();

        await Assert.That(response.Content.Headers.ContentType!.MediaType)
            .IsEqualTo("text/plain");
        await Assert.That(body).Contains("Rate limit reached")
            .And.Contains("Retry after");
    }
}
