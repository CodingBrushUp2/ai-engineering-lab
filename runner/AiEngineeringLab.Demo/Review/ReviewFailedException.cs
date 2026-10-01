namespace AiEngineeringLab.Demo.Review;

/// <summary>
/// A reviewer could not produce a contract-valid <see cref="ReviewResult"/>: the endpoint failed,
/// the model refused or was truncated, or its output was malformed. Such a run must not be
/// recorded as an observation. <see cref="RawOutput"/> keeps the model text for diagnosis.
/// </summary>
internal sealed class ReviewFailedException(string message, string? rawOutput = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string? RawOutput { get; } = rawOutput;
}
