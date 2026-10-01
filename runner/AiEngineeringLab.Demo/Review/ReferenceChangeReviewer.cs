namespace AiEngineeringLab.Demo.Review;

/// <summary>
/// Deterministic reference reviewer. It is NOT an AI implementation: it recognises the two
/// committed fixtures by string matching so the execution contract can be exercised without
/// model variability or credentials. Unknown input yields zero findings and zero questions.
/// </summary>
internal sealed class ReferenceChangeReviewer : IChangeReviewer
{
    public Task<ReviewResult> ReviewAsync(ReviewInput input, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(Review(input));
    }

    private static ReviewResult Review(ReviewInput input)
    {
        if (input.Diff.Contains("GetAsync(\"/forecast\")", StringComparison.Ordinal) &&
            input.Context.Contains("request cancellation", StringComparison.OrdinalIgnoreCase))
            return new("One cancellation propagation issue found.",
                [new("medium", "src/WeatherClient.cs:GetForecastAsync",
                    "The outbound HTTP request no longer receives the caller's CancellationToken.",
                    "The diff changes GetAsync(\"/forecast\", cancellationToken) to GetAsync(\"/forecast\") while the method still accepts the request cancellation token.",
                    "A cancelled ASP.NET Core request can leave the outbound HTTP operation running until it completes or times out.")], []);

        if (input.Diff.Contains("await using var ownedInput = input", StringComparison.Ordinal) &&
            input.Context.Contains("does not state", StringComparison.OrdinalIgnoreCase))
            return new("Stream ownership cannot be determined from the available contract.", [],
                [new("src/ImportService.cs:ImportAsync",
                    "Does ImportAsync own the caller-provided stream and therefore have permission to dispose it?",
                    "The change disposes the input stream, but the available public contract does not define ownership.")]);

        return new("No supported finding from the available evidence.", [], []);
    }
}
