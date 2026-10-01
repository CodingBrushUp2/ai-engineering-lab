using System.Text.Json;
using AiEngineeringLab.Demo.Review;

if (args.Length != 2 || args[0] != "--fixture")
{
    Console.Error.WriteLine("Usage: dotnet run --project runner/AiEngineeringLab.Demo -- --fixture <fixture-directory>");
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
    input = await FixtureLoader.LoadAsync(args[1], cancellation.Token);
}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

IChangeReviewer reviewer = new ReferenceChangeReviewer();
var result = await reviewer.ReviewAsync(input, cancellation.Token);
Console.WriteLine(JsonSerializer.Serialize(result, ReviewJson.Output));
return 0;
