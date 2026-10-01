namespace AiEngineeringLab.Demo.Review;

/// <summary>
/// Builds the model-facing <see cref="ReviewInput"/> from a fixture directory.
///
/// This is the evaluation boundary: only <c>diff.patch</c> and <c>context.md</c> are read.
/// Evaluator metadata lives in the evals manifest (cases.json) and is never opened here.
/// </summary>
internal static class FixtureLoader
{
    public const string DiffFileName = "diff.patch";
    public const string ContextFileName = "context.md";

    public static async Task<ReviewInput> LoadAsync(string fixtureDirectory, CancellationToken cancellationToken)
    {
        var diffPath = Path.Combine(fixtureDirectory, DiffFileName);
        var contextPath = Path.Combine(fixtureDirectory, ContextFileName);

        if (!File.Exists(diffPath) || !File.Exists(contextPath))
            throw new FileNotFoundException($"Fixture must contain {DiffFileName} and {ContextFileName}: {fixtureDirectory}");

        var diff = await File.ReadAllTextAsync(diffPath, cancellationToken);
        var context = await File.ReadAllTextAsync(contextPath, cancellationToken);
        return new ReviewInput(diff, context);
    }
}
