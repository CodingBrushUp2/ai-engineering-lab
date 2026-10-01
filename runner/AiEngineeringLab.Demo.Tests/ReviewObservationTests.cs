using System.Text.Json;
using AiEngineeringLab.Demo.Review;

namespace AiEngineeringLab.Demo.Tests;

public sealed class ReviewObservationTests
{
    private static readonly ReviewFinding Finding = new("low", "a.cs:1", "i", "e", "c");
    private static readonly ReviewQuestion Question = new("a.cs:1", "q?", "w");

    [Fact]
    public void Findings_classify_as_finding_even_when_questions_exist()
    {
        Assert.Equal("finding", ReviewObservation.Classify(new ReviewResult("s", [Finding], [Question])));
    }

    [Fact]
    public void Questions_without_findings_classify_as_question()
    {
        Assert.Equal("question", ReviewObservation.Classify(new ReviewResult("s", [], [Question])));
    }

    [Fact]
    public void Empty_review_classifies_as_no_finding()
    {
        Assert.Equal("no-finding", ReviewObservation.Classify(new ReviewResult("s", [], [])));
    }

    [Fact]
    public void Serialized_observation_exposes_the_fields_the_evals_runner_reads()
    {
        var observation = ReviewObservation.Create("dropped-cancellation", "reference", new ReviewResult("s", [Finding], []));

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(observation, ReviewJson.Output));

        Assert.Equal("dropped-cancellation", json.RootElement.GetProperty("caseId").GetString());
        Assert.Equal("finding", json.RootElement.GetProperty("observed").GetString());
        Assert.Equal("reference", json.RootElement.GetProperty("reviewer").GetString());
        Assert.Equal("s", json.RootElement.GetProperty("review").GetProperty("summary").GetString());
    }
}
