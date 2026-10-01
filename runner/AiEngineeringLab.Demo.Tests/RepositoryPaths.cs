namespace AiEngineeringLab.Demo.Tests;

/// <summary>Locates committed fixtures relative to the repository root.</summary>
internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    public static string CasesManifest => Path.Combine(Root, "agents", "dotnet-pr-reviewer", "evals", "cases.json");

    public static string Fixture(string name) =>
        Path.Combine(Root, "agents", "dotnet-pr-reviewer", "evals", "fixtures", name);

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "agents", "dotnet-pr-reviewer")))
                return directory.FullName;
        }

        throw new InvalidOperationException("Repository root not found from " + AppContext.BaseDirectory);
    }
}
