using System.Text.Json;
using AiEngineeringLab.Demo.Review;

namespace AiEngineeringLab.Demo.Tests;

public sealed class FixtureLoaderTests
{
    [Fact]
    public async Task Loads_only_diff_and_context_from_the_fixture_directory()
    {
        var directory = Directory.CreateTempSubdirectory("review-fixture-");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(directory.FullName, "diff.patch"), "DIFF-CONTENT");
            await File.WriteAllTextAsync(Path.Combine(directory.FullName, "context.md"), "CONTEXT-CONTENT");
            // Decoy evaluator metadata placed next to the fixture must never be read.
            await File.WriteAllTextAsync(Path.Combine(directory.FullName, "cases.json"), "{\"expect\":\"finding\",\"focus\":\"LEAKED\"}");

            var input = await FixtureLoader.LoadAsync(directory.FullName, CancellationToken.None);

            Assert.Equal("DIFF-CONTENT", input.Diff);
            Assert.Equal("CONTEXT-CONTENT", input.Context);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    [Theory]
    [InlineData("dropped-cancellation")]
    [InlineData("ambiguous-stream-ownership")]
    public async Task Committed_fixture_input_contains_no_evaluator_metadata(string caseId)
    {
        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(RepositoryPaths.CasesManifest));
        var evalCase = manifest.RootElement.GetProperty("cases").EnumerateArray()
            .Single(c => c.GetProperty("id").GetString() == caseId);

        var input = await FixtureLoader.LoadAsync(RepositoryPaths.Fixture(caseId), CancellationToken.None);
        var modelFacing = input.Diff + "\n" + input.Context;

        Assert.DoesNotContain(evalCase.GetProperty("description").GetString()!, modelFacing);
        Assert.DoesNotContain("\"expect\"", modelFacing);
        Assert.DoesNotContain("\"focus\"", modelFacing);
    }

    [Fact]
    public async Task Missing_fixture_files_are_reported()
    {
        var directory = Directory.CreateTempSubdirectory("review-fixture-");
        try
        {
            await Assert.ThrowsAsync<FileNotFoundException>(
                () => FixtureLoader.LoadAsync(directory.FullName, CancellationToken.None));
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }
}
