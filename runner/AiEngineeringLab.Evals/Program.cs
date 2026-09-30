using System.Text.Json;

if (args.Length != 2 || args[0] != "--result")
{
    Console.Error.WriteLine("Usage: dotnet run --project runner/AiEngineeringLab.Evals -- --result <result.json>");
    return 2;
}

var path = args[1];
if (!File.Exists(path))
{
    Console.Error.WriteLine($"Result file not found: {path}");
    return 2;
}

var json = await File.ReadAllTextAsync(path);
var result = JsonSerializer.Deserialize<EvalResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

if (result is null || string.IsNullOrWhiteSpace(result.CaseId) || string.IsNullOrWhiteSpace(result.Observed))
{
    Console.Error.WriteLine("Result must contain caseId and observed.");
    return 2;
}

var expected = result.Expected?.Trim().ToLowerInvariant();
var observed = result.Observed.Trim().ToLowerInvariant();
var passed = expected is not null && expected == observed;

Console.WriteLine($"{result.CaseId}: {(passed ? "PASS" : "FAIL")} (expected={expected ?? "<missing>"}, observed={observed})");
return passed ? 0 : 1;

internal sealed record EvalResult(string CaseId, string? Expected, string Observed);
