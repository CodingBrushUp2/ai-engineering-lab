using System.Text.Json;

if (args.Length != 2 || args[0] != "--fixture")
{
    Console.Error.WriteLine("Usage: dotnet run --project runner/AiEngineeringLab.Demo -- --fixture <fixture-directory>");
    return 2;
}

var fixtureDirectory = args[1];
var diffPath = Path.Combine(fixtureDirectory, "diff.patch");
var contextPath = Path.Combine(fixtureDirectory, "context.md");

if (!File.Exists(diffPath) || !File.Exists(contextPath))
{
    Console.Error.WriteLine("Fixture must contain diff.patch and context.md.");
    return 2;
}

var input = new ReviewInput(await File.ReadAllTextAsync(diffPath), await File.ReadAllTextAsync(contextPath));
IChangeReviewer reviewer = new ReferenceChangeReviewer();
var result = reviewer.Review(input);
Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
return 0;

internal sealed record ReviewInput(string Diff, string Context);
internal sealed record ReviewFinding(string Severity, string Location, string Issue, string Evidence, string Consequence);
internal sealed record ReviewQuestion(string Location, string Question, string WhyItMatters);
internal sealed record ReviewResult(string Summary, ReviewFinding[] Findings, ReviewQuestion[] Questions);

internal interface IChangeReviewer { ReviewResult Review(ReviewInput input); }

internal sealed class ReferenceChangeReviewer : IChangeReviewer
{
    public ReviewResult Review(ReviewInput input)
    {
        if (input.Diff.Contains("GetAsync(\"/forecast\")", StringComparison.Ordinal) &&
            input.Context.Contains("request cancellation", StringComparison.OrdinalIgnoreCase))
            return new("One cancellation propagation issue found.",
                [new("medium","src/WeatherClient.cs:GetForecastAsync",
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
