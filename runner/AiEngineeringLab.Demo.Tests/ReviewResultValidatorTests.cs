using AiEngineeringLab.Demo.Review;

namespace AiEngineeringLab.Demo.Tests;

public sealed class ReviewResultValidatorTests
{
    private static ReviewFinding ValidFinding() =>
        new("medium", "src/A.cs:Run", "Issue text", "Evidence text", "Consequence text");

    private static ReviewQuestion ValidQuestion() =>
        new("src/A.cs:Run", "Question text?", "Why it matters");

    [Fact]
    public void Accepts_a_complete_review()
    {
        var errors = ReviewResultValidator.Validate(new ReviewResult("Summary", [ValidFinding()], [ValidQuestion()]));

        Assert.Empty(errors);
    }

    [Fact]
    public void Accepts_zero_findings_and_zero_questions()
    {
        var errors = ReviewResultValidator.Validate(new ReviewResult("Nothing supported.", [], []));

        Assert.Empty(errors);
    }

    [Fact]
    public void Rejects_a_missing_result()
    {
        Assert.NotEmpty(ReviewResultValidator.Validate(null));
    }

    [Fact]
    public void Rejects_missing_collections_and_summary()
    {
        var errors = ReviewResultValidator.Validate(new ReviewResult(" ", null!, null!));

        Assert.Equal(3, errors.Count);
    }

    [Theory]
    [InlineData("critical")]
    [InlineData("Medium")]
    [InlineData("")]
    public void Rejects_severity_outside_the_contract(string severity)
    {
        var finding = ValidFinding() with { Severity = severity };

        var error = Assert.Single(ReviewResultValidator.Validate(new ReviewResult("Summary", [finding], [])));

        Assert.Contains("findings[0].severity", error);
    }

    [Fact]
    public void Rejects_a_finding_without_evidence()
    {
        var finding = ValidFinding() with { Evidence = "" };

        var error = Assert.Single(ReviewResultValidator.Validate(new ReviewResult("Summary", [finding], [])));

        Assert.Contains("findings[0].evidence", error);
    }

    [Fact]
    public void Rejects_a_question_without_why_it_matters()
    {
        var question = ValidQuestion() with { WhyItMatters = null! };

        var error = Assert.Single(ReviewResultValidator.Validate(new ReviewResult("Summary", [], [question])));

        Assert.Contains("questions[0].whyItMatters", error);
    }

    [Fact]
    public void Rejects_null_entries()
    {
        var errors = ReviewResultValidator.Validate(new ReviewResult("Summary", [null!], [null!]));

        Assert.Equal(2, errors.Count);
    }

    [Theory]
    [InlineData("dropped-cancellation")]
    [InlineData("ambiguous-stream-ownership")]
    public async Task Reference_reviewer_output_satisfies_the_contract(string fixture)
    {
        var input = await FixtureLoader.LoadAsync(RepositoryPaths.Fixture(fixture), CancellationToken.None);

        var result = await new ReferenceChangeReviewer().ReviewAsync(input, CancellationToken.None);

        Assert.Empty(ReviewResultValidator.Validate(result));
    }
}
