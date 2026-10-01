using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiEngineeringLab.Demo.Review;

namespace AiEngineeringLab.Demo.OpenAi;

/// <summary>
/// Model-backed <see cref="IChangeReviewer"/> for any endpoint that implements the OpenAI
/// Chat Completions API with structured outputs (response_format: json_schema, strict).
///
/// This is the only provider-specific code in the runner. It makes exactly one HTTP request per
/// review, with no retries, and accepts the result only if it parses strictly and satisfies
/// <see cref="ReviewResultValidator"/>. Anything else becomes a <see cref="ReviewFailedException"/>.
/// </summary>
internal sealed class OpenAiCompatibleChangeReviewer(HttpClient httpClient, OpenAiCompatibleOptions options) : IChangeReviewer
{
    private const int MaxErrorBodyLength = 500;

    private static readonly JsonSerializerOptions WireOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    /// <summary>
    /// JSON Schema for <see cref="ReviewResult"/>. Strict structured outputs require every property
    /// to be listed in "required" and every object to set "additionalProperties": false.
    /// </summary>
    internal static readonly JsonElement ReviewResultSchema = JsonSerializer.Deserialize<JsonElement>(
        """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["summary", "findings", "questions"],
          "properties": {
            "summary": { "type": "string", "description": "One or two sentences describing what the review found." },
            "findings": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["severity", "location", "issue", "evidence", "consequence"],
                "properties": {
                  "severity": { "type": "string", "enum": ["high", "medium", "low"] },
                  "location": { "type": "string", "description": "File path from the diff plus line number or symbol." },
                  "issue": { "type": "string" },
                  "evidence": { "type": "string", "description": "The concrete code or context that supports the finding." },
                  "consequence": { "type": "string", "description": "The production consequence if the change ships." }
                }
              }
            },
            "questions": {
              "type": "array",
              "items": {
                "type": "object",
                "additionalProperties": false,
                "required": ["location", "question", "whyItMatters"],
                "properties": {
                  "location": { "type": "string" },
                  "question": { "type": "string" },
                  "whyItMatters": { "type": "string" }
                }
              }
            }
          }
        }
        """);

    public string Name => $"openai-compatible:{options.Model}";

    public async Task<ReviewResult> ReviewAsync(ReviewInput input, CancellationToken cancellationToken)
    {
        using var request = CreateRequest(input);

        string responseBody;
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new ReviewFailedException(
                    $"Model endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(responseBody)}");
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout surfaces as a cancellation that the caller did not request.
            throw new ReviewFailedException("Model endpoint did not respond before the HTTP timeout.", innerException: ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ReviewFailedException($"Model endpoint could not be reached: {ex.Message}", innerException: ex);
        }

        return ParseResponse(responseBody);
    }

    internal HttpRequestMessage CreateRequest(ReviewInput input)
    {
        var body = new ChatCompletionRequest(
            options.Model,
            [
                new ChatMessage("system", ReviewPrompt.SystemInstructions),
                new ChatMessage("user", ReviewPrompt.BuildUserMessage(input)),
            ],
            new ResponseFormat("json_schema", new JsonSchemaFormat("review_result", Strict: true, ReviewResultSchema)));

        var request = new HttpRequestMessage(HttpMethod.Post, new Uri(options.BaseUrl, "chat/completions"))
        {
            // Buffered string content sends a Content-Length header. JsonContent streams with chunked
            // transfer encoding, which some OpenAI-compatible servers and proxies do not accept.
            Content = new StringContent(JsonSerializer.Serialize(body, WireOptions), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        return request;
    }

    internal static ReviewResult ParseResponse(string responseBody)
    {
        ChatCompletionResponse? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ChatCompletionResponse>(responseBody, WireOptions);
        }
        catch (JsonException ex)
        {
            throw new ReviewFailedException("Model endpoint returned a body that is not valid JSON.", Truncate(responseBody), ex);
        }

        if (envelope?.Choices is not [var choice])
            throw new ReviewFailedException(
                $"Expected exactly one choice but received {envelope?.Choices?.Length ?? 0}.", Truncate(responseBody));

        var content = choice.Message?.Content;

        if (!string.IsNullOrWhiteSpace(choice.Message?.Refusal))
            throw new ReviewFailedException($"Model refused to review: {choice.Message.Refusal}", content);

        if (choice.FinishReason != "stop")
            throw new ReviewFailedException(
                $"Model output is incomplete (finish_reason '{choice.FinishReason ?? "missing"}').", content);

        if (string.IsNullOrWhiteSpace(content))
            throw new ReviewFailedException("Model returned no content.");

        ReviewResult? result;
        try
        {
            result = JsonSerializer.Deserialize<ReviewResult>(content, ReviewJson.StrictInput);
        }
        catch (JsonException ex)
        {
            throw new ReviewFailedException($"Model output does not match the review contract: {ex.Message}", content, ex);
        }

        var errors = ReviewResultValidator.Validate(result);
        if (errors.Count > 0)
            throw new ReviewFailedException("Model output violates the review contract: " + string.Join(" ", errors), content);

        return result!;
    }

    private static string Truncate(string text) =>
        text.Length <= MaxErrorBodyLength ? text : text[..MaxErrorBodyLength] + "…";

    // Wire format of the Chat Completions API. Only the fields this reviewer uses are modelled;
    // unknown response fields are ignored because providers add them freely.
    private sealed record ChatCompletionRequest(string Model, ChatMessage[] Messages, ResponseFormat ResponseFormat);

    private sealed record ChatMessage(string Role, string Content);

    private sealed record ResponseFormat(string Type, JsonSchemaFormat JsonSchema);

    private sealed record JsonSchemaFormat(string Name, bool Strict, JsonElement Schema);

    private sealed record ChatCompletionResponse(ChatChoice[]? Choices);

    private sealed record ChatChoice(ChatChoiceMessage? Message, string? FinishReason);

    private sealed record ChatChoiceMessage(string? Content, string? Refusal);
}
