using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

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

    /// <summary>
    /// For reading untrusted reviewer output. Unknown properties are rejected instead of ignored.
    /// Missing or null properties are caught afterwards by <see cref="ReviewResultValidator"/>.
    /// </summary>
    public static readonly JsonSerializerOptions StrictInput = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };
}
