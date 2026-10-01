using System.Text.Encodings.Web;
using System.Text.Json;

namespace AiEngineeringLab.Demo.Review;

/// <summary>JSON settings for the review contract. Property names are camelCase, as in agent.md.</summary>
internal static class ReviewJson
{
    /// <summary>For printing and recording results. Relaxed escaping keeps quotes readable; output is never embedded in HTML.</summary>
    public static readonly JsonSerializerOptions Output = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };
}
