using AiEngineeringLab.Demo.Review;

namespace AiEngineeringLab.Demo.Tests;

public sealed class ReferenceChangeReviewerTests
{
    private readonly ReferenceChangeReviewer _reviewer = new();

    [Fact]
    public async Task Dropped_cancellation_fixture_produces_one_finding()
    {
        var input = await FixtureLoader.LoadAsync(RepositoryPaths.Fixture("dropped-cancellation"), CancellationToken.None);

        var result = await _reviewer.ReviewAsync(input, CancellationToken.None);

        var finding = Assert.Single(result.Findings);
        Assert.Equal("medium", finding.Severity);
        Assert.Empty(result.Questions);
    }

    [Fact]
    public async Task Ambiguous_ownership_fixture_produces_one_question()
    {
        var input = await FixtureLoader.LoadAsync(RepositoryPaths.Fixture("ambiguous-stream-ownership"), CancellationToken.None);

        var result = await _reviewer.ReviewAsync(input, CancellationToken.None);

        Assert.Empty(result.Findings);
        Assert.Single(result.Questions);
    }

    [Fact]
    public async Task Unrecognised_input_produces_zero_findings_and_questions()
    {
        var result = await _reviewer.ReviewAsync(new ReviewInput("unrelated diff", "unrelated context"), CancellationToken.None);

        Assert.Empty(result.Findings);
        Assert.Empty(result.Questions);
    }

    [Fact]
    public async Task Honours_an_already_cancelled_token()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _reviewer.ReviewAsync(new ReviewInput("d", "c"), cancellation.Token));
    }
}
