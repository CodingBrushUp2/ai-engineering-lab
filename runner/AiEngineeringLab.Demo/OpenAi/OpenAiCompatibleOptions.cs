using System.Diagnostics.CodeAnalysis;

namespace AiEngineeringLab.Demo.OpenAi;

/// <summary>
/// Settings for an OpenAI-compatible Chat Completions endpoint, read from environment variables.
///
/// The variable names are project-prefixed on purpose: a globally exported OPENAI_API_KEY must not
/// make this lab spend money unless the user opts in explicitly. This is a class rather than a
/// record so the compiler-generated ToString cannot print the API key.
/// </summary>
internal sealed class OpenAiCompatibleOptions
{
    public const string ApiKeyVariable = "AI_LAB_OPENAI_API_KEY";
    public const string ModelVariable = "AI_LAB_OPENAI_MODEL";
    public const string BaseUrlVariable = "AI_LAB_OPENAI_BASE_URL";

    public static readonly Uri DefaultBaseUrl = new("https://api.openai.com/v1/");

    private OpenAiCompatibleOptions(Uri baseUrl, string apiKey, string model)
    {
        BaseUrl = baseUrl;
        ApiKey = apiKey;
        Model = model;
    }

    /// <summary>Base URL ending in a slash, so "chat/completions" resolves beneath it.</summary>
    public Uri BaseUrl { get; }

    public string ApiKey { get; }

    public string Model { get; }

    public static bool TryFromEnvironment(
        Func<string, string?> getVariable,
        [NotNullWhen(true)] out OpenAiCompatibleOptions? options,
        out string error)
    {
        options = null;

        var apiKey = getVariable(ApiKeyVariable)?.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            error = $"{ApiKeyVariable} is not set. Export it in your shell; never commit it.";
            return false;
        }

        var model = getVariable(ModelVariable)?.Trim();
        if (string.IsNullOrEmpty(model))
        {
            error = $"{ModelVariable} is not set. Choose the model explicitly so cost and behaviour are deliberate.";
            return false;
        }

        var baseUrl = DefaultBaseUrl;
        var configuredBaseUrl = getVariable(BaseUrlVariable)?.Trim();
        if (!string.IsNullOrEmpty(configuredBaseUrl))
        {
            if (!Uri.TryCreate(configuredBaseUrl.TrimEnd('/') + "/", UriKind.Absolute, out var parsed) ||
                !(parsed.Scheme == Uri.UriSchemeHttps || (parsed.Scheme == Uri.UriSchemeHttp && parsed.IsLoopback)))
            {
                error = $"{BaseUrlVariable} must be an absolute https URL (plain http is accepted only for localhost).";
                return false;
            }

            baseUrl = parsed;
        }

        options = new OpenAiCompatibleOptions(baseUrl, apiKey, model);
        error = string.Empty;
        return true;
    }
}
