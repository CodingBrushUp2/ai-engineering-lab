namespace AiEngineeringLab.Demo.Review;

/// <summary>
/// Checks that a deserialized review satisfies the agent.md contract before it is treated as
/// an evaluation result. Returns every violation so malformed output is reported, not repaired.
/// </summary>
internal static class ReviewResultValidator
{
    public static readonly IReadOnlyList<string> Severities = ["high", "medium", "low"];

    public static IReadOnlyList<string> Validate(ReviewResult? result)
    {
        if (result is null)
            return ["Review result is missing."];

        var errors = new List<string>();
        RequireText(errors, "summary", result.Summary);

        if (result.Findings is null)
            errors.Add("findings is missing.");
        else
            for (var i = 0; i < result.Findings.Length; i++)
                ValidateFinding(errors, $"findings[{i}]", result.Findings[i]);

        if (result.Questions is null)
            errors.Add("questions is missing.");
        else
            for (var i = 0; i < result.Questions.Length; i++)
                ValidateQuestion(errors, $"questions[{i}]", result.Questions[i]);

        return errors;
    }

    private static void ValidateFinding(List<string> errors, string path, ReviewFinding? finding)
    {
        if (finding is null)
        {
            errors.Add($"{path} is null.");
            return;
        }

        if (!Severities.Contains(finding.Severity, StringComparer.Ordinal))
            errors.Add($"{path}.severity must be one of {string.Join(", ", Severities)} but was '{finding.Severity}'.");

        RequireText(errors, $"{path}.location", finding.Location);
        RequireText(errors, $"{path}.issue", finding.Issue);
        RequireText(errors, $"{path}.evidence", finding.Evidence);
        RequireText(errors, $"{path}.consequence", finding.Consequence);
    }

    private static void ValidateQuestion(List<string> errors, string path, ReviewQuestion? question)
    {
        if (question is null)
        {
            errors.Add($"{path} is null.");
            return;
        }

        RequireText(errors, $"{path}.location", question.Location);
        RequireText(errors, $"{path}.question", question.Question);
        RequireText(errors, $"{path}.whyItMatters", question.WhyItMatters);
    }

    private static void RequireText(List<string> errors, string path, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            errors.Add($"{path} must be non-empty text.");
    }
}
