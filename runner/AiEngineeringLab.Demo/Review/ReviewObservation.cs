namespace AiEngineeringLab.Demo.Review;

/// <summary>
/// A recorded review, written after inference. <see cref="CaseId"/> and <see cref="Observed"/>
/// are the fields the existing Evals runner scores; <see cref="Review"/> keeps the full
/// structured output for manual inspection. The Evals runner ignores the extra fields.
/// </summary>
internal sealed record ReviewObservation(string CaseId, string Observed, string Reviewer, ReviewResult Review)
{
    public const string Finding = "finding";
    public const string Question = "question";
    public const string NoFinding = "no-finding";

    public static ReviewObservation Create(string caseId, string reviewer, ReviewResult review) =>
        new(caseId, Classify(review), reviewer, review);

    /// <summary>
    /// Maps a review onto the coarse outcome vocabulary used in cases.json.
    /// Findings take precedence over questions; this is deliberately lossy.
    /// </summary>
    public static string Classify(ReviewResult review) =>
        review.Findings.Length > 0 ? Finding
        : review.Questions.Length > 0 ? Question
        : NoFinding;
}
