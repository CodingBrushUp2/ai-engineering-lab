using System.Text.Json;

if (args.Length != 4 || args[0] != "--cases" || args[2] != "--result")
{
    Console.Error.WriteLine("Usage: dotnet run --project runner/AiEngineeringLab.Evals -- --cases <cases.json> --result <result.json>");
    return 2;
}

var casesPath = args[1];
var resultPath = args[3];
if (!File.Exists(casesPath) || !File.Exists(resultPath))
{
    Console.Error.WriteLine("Cases or result file not found.");
    return 2;
}

var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var manifest = JsonSerializer.Deserialize<EvalManifest>(await File.ReadAllTextAsync(casesPath), options);
var result = JsonSerializer.Deserialize<EvalResult>(await File.ReadAllTextAsync(resultPath), options);

if (manifest?.Cases is null || result is null || string.IsNullOrWhiteSpace(result.CaseId) || string.IsNullOrWhiteSpace(result.Observed))
{
    Console.Error.WriteLine("Invalid cases or result document.");
    return 2;
}

var evalCase = manifest.Cases.SingleOrDefault(x => string.Equals(x.Id, result.CaseId, StringComparison.Ordinal));
if (evalCase is null)
{
    Console.Error.WriteLine($"Unknown caseId: {result.CaseId}");
    return 2;
}

var expected = evalCase.Expect.Trim().ToLowerInvariant();
var observed = result.Observed.Trim().ToLowerInvariant();
var passed = expected == observed;

Console.WriteLine($"{result.CaseId}: {(passed ? "PASS" : "FAIL")} (expected={expected}, observed={observed})");
return passed ? 0 : 1;

internal sealed record EvalManifest(int Version, EvalCase[] Cases);
internal sealed record EvalCase(string Id, string Expect);
internal sealed record EvalResult(string CaseId, string Observed);
