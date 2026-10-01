using System.Text.Json;
using AiEngineeringLab.Demo.Review;

const string Usage =
    "Usage: dotnet run --project runner/AiEngineeringLab.Demo -- --fixture <fixture-directory> [--record <observation.json>]";

var options = ParseOptions(args, ["--fixture", "--record"]);
if (options is null || !options.TryGetValue("--fixture", out var fixtureDirectory))
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

IChangeReviewer reviewer = new ReferenceChangeReviewer();
const string reviewerName = "reference";

var result = await reviewer.ReviewAsync(input, cancellation.Token);
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
