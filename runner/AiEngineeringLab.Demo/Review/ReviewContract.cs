namespace AiEngineeringLab.Demo.Review;

/// <summary>
/// Model-facing input. This is everything a reviewer is allowed to see: the diff and the
/// explicitly allowed context. Evaluator metadata (expect, focus, case description) has no
/// place here, so it cannot reach a prompt by construction.
/// </summary>
internal sealed record ReviewInput(string Diff, string Context);

/// <summary>Structured review output. Mirrors the contract in agents/dotnet-pr-reviewer/agent.md.</summary>
internal sealed record ReviewResult(string Summary, ReviewFinding[] Findings, ReviewQuestion[] Questions);

internal sealed record ReviewFinding(string Severity, string Location, string Issue, string Evidence, string Consequence);

internal sealed record ReviewQuestion(string Location, string Question, string WhyItMatters);

/// <summary>
/// The single reviewer boundary. Implementations may be deterministic or model-backed.
/// Async with cancellation because a model-backed reviewer performs network I/O.
/// </summary>
internal interface IChangeReviewer
{
    Task<ReviewResult> ReviewAsync(ReviewInput input, CancellationToken cancellationToken);
}
