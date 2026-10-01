using System.Text.Json;
using AiEngineeringLab.Demo.OpenAi;
using AiEngineeringLab.Demo.Review;

const string Usage =
    "Usage: dotnet run --project runner/AiEngineeringLab.Demo -- --fixture <fixture-directory> [--reviewer reference|openai] [--record <observation.json>]";

var options = ParseOptions(args, ["--fixture", "--reviewer", "--record"]);
if (options is null || !options.TryGetValue("--fixture", out var fixtureDirectory))
{
    Console.Error.WriteLine(Usage);
    return 2;
}

var reviewerChoice = options.GetValueOrDefault("--reviewer", "reference");
if (reviewerChoice is not ("reference" or "openai"))
{
    Console.Error.WriteLine(Usage);
    return 2;
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

ReviewInput input;
try
{
    input = await FixtureLoader.LoadAsync(fixtureDirectory, cancellation.Token);
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

// The reference reviewer needs no network. The model-backed reviewer is opt-in and reads its
// configuration from environment variables, so CI never makes paid calls by default.
IChangeReviewer reviewer;
string reviewerName;
using var httpClient = reviewerChoice == "openai" ? new HttpClient { Timeout = TimeSpan.FromSeconds(120) } : null;
if (httpClient is not null)
{
    if (!OpenAiCompatibleOptions.TryFromEnvironment(Environment.GetEnvironmentVariable, out var openAiOptions, out var configurationError))
    {
        Console.Error.WriteLine(configurationError);
        return 2;
    }

    var openAiReviewer = new OpenAiCompatibleChangeReviewer(httpClient, openAiOptions);
    reviewer = openAiReviewer;
    reviewerName = openAiReviewer.Name;
}
else
{
    reviewer = new ReferenceChangeReviewer();
    reviewerName = "reference";
}

ReviewResult result;
try
{
    result = await reviewer.ReviewAsync(input, cancellation.Token);
}
catch (ReviewFailedException ex)
{
    Console.Error.WriteLine($"Review failed ({reviewerName}): {ex.Message}");
    if (ex.RawOutput is not null)
        Console.Error.WriteLine($"Raw model output:{Environment.NewLine}{ex.RawOutput}");
    return 1;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine("Review cancelled.");
    return 1;
}

Console.WriteLine(JsonSerializer.Serialize(result, ReviewJson.Output));

if (options.TryGetValue("--record", out var recordPath))
{
    // The case id is the fixture directory name; the Evals runner rejects ids it does not know.
    var caseId = Path.GetFileName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(fixtureDirectory)));
    var observation = ReviewObservation.Create(caseId, reviewerName, result);

    var recordDirectory = Path.GetDirectoryName(Path.GetFullPath(recordPath));
    if (!string.IsNullOrEmpty(recordDirectory))
        Directory.CreateDirectory(recordDirectory);

    await File.WriteAllTextAsync(recordPath, JsonSerializer.Serialize(observation, ReviewJson.Output), cancellation.Token);
    Console.Error.WriteLine($"Recorded observation '{observation.Observed}' for case '{caseId}' to {recordPath}");
}

return 0;

// Accepts "--name value" pairs. Returns null for unknown, duplicate or valueless options.
static Dictionary<string, string>? ParseOptions(string[] args, string[] allowed)
{
    if (args.Length % 2 != 0)
        return null;

    var options = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var i = 0; i < args.Length; i += 2)
    {
        if (!allowed.Contains(args[i]) || !options.TryAdd(args[i], args[i + 1]))
            return null;
    }

    return options;
}
