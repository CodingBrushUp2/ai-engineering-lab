using System.Net;
using System.Text.Json;
using AiEngineeringLab.Demo.OpenAi;
using AiEngineeringLab.Demo.Review;

namespace AiEngineeringLab.Demo.Tests;

public sealed class OpenAiCompatibleChangeReviewerTests
{
    private const string ApiKey = "sk-test-secret";

    private static readonly ReviewInput Input = new("DIFF-TEXT", "CONTEXT-TEXT");

    private static OpenAiCompatibleOptions Options(string baseUrl = "https://example.test/v1")
    {
        Func<string, string?> environment = name => name switch
        {
            OpenAiCompatibleOptions.ApiKeyVariable => ApiKey,
            OpenAiCompatibleOptions.ModelVariable => "test-model",
            OpenAiCompatibleOptions.BaseUrlVariable => baseUrl,
            _ => null,
        };
        Assert.True(OpenAiCompatibleOptions.TryFromEnvironment(environment, out var options, out _));
        return options;
    }

    private static OpenAiCompatibleChangeReviewer Reviewer(HttpMessageHandler handler, TimeSpan? timeout = null) =>
        new(new HttpClient(handler) { Timeout = timeout ?? TimeSpan.FromSeconds(30) }, Options());

    /// <summary>Wraps model content in a Chat Completions envelope.</summary>
    private static string Envelope(string? content, string finishReason = "stop", string? refusal = null) =>
        JsonSerializer.Serialize(new
        {
            id = "chatcmpl-test",
            @object = "chat.completion",
            choices = new[]
            {
                new { index = 0, message = new { role = "assistant", content, refusal }, finish_reason = finishReason },
            },
            usage = new { prompt_tokens = 1, completion_tokens = 1, total_tokens = 2 },
        });

    private const string ValidContent =
        """
        {
          "summary": "One cancellation propagation issue found.",
          "findings": [{
            "severity": "medium",
            "location": "src/WeatherClient.cs:GetForecastAsync",
            "issue": "The outbound request no longer receives the caller's token.",
            "evidence": "GetAsync(\"/forecast\", cancellationToken) became GetAsync(\"/forecast\").",
            "consequence": "Cancelled requests keep outbound HTTP calls running."
          }],
          "questions": []
        }
        """;

    // ---- Request construction ------------------------------------------------------------

    [Fact]
    public async Task Sends_one_strict_structured_output_request_to_chat_completions()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, Envelope(ValidContent));

        await Reviewer(handler).ReviewAsync(Input, CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal(new Uri("https://example.test/v1/chat/completions"), handler.Request.RequestUri);
        Assert.Equal("Bearer", handler.Request.Headers.Authorization!.Scheme);
        Assert.Equal(ApiKey, handler.Request.Headers.Authorization.Parameter);
        Assert.True(handler.Request.Content!.Headers.ContentLength > 0); // buffered, not chunked

        using var body = JsonDocument.Parse(handler.RequestBody!);
        var root = body.RootElement;
        Assert.Equal("test-model", root.GetProperty("model").GetString());

        var messages = root.GetProperty("messages").EnumerateArray().ToArray();
        Assert.Equal(2, messages.Length);
        Assert.Equal("system", messages[0].GetProperty("role").GetString());
        Assert.Equal(ReviewPrompt.SystemInstructions, messages[0].GetProperty("content").GetString());
        Assert.Equal("user", messages[1].GetProperty("role").GetString());
        Assert.Equal(ReviewPrompt.BuildUserMessage(Input), messages[1].GetProperty("content").GetString());

        var format = root.GetProperty("response_format");
        Assert.Equal("json_schema", format.GetProperty("type").GetString());
        Assert.True(format.GetProperty("json_schema").GetProperty("strict").GetBoolean());
        Assert.Equal("review_result", format.GetProperty("json_schema").GetProperty("name").GetString());
    }

    [Fact]
    public void User_message_contains_the_diff_and_context()
    {
        var message = ReviewPrompt.BuildUserMessage(Input);

        Assert.Contains("<diff>\nDIFF-TEXT\n</diff>", message.ReplaceLineEndings("\n"));
        Assert.Contains("<context>\nCONTEXT-TEXT\n</context>", message.ReplaceLineEndings("\n"));
    }

    [Fact]
    public async Task Request_for_a_committed_fixture_contains_no_evaluator_metadata()
    {
        var input = await FixtureLoader.LoadAsync(RepositoryPaths.Fixture("dropped-cancellation"), CancellationToken.None);
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, Envelope(ValidContent));

        await Reviewer(handler).ReviewAsync(input, CancellationToken.None);

        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(RepositoryPaths.CasesManifest));
        var evalCase = manifest.RootElement.GetProperty("cases").EnumerateArray()
            .Single(c => c.GetProperty("id").GetString() == "dropped-cancellation");

        // Compare against the decoded prompt text, not the JSON-escaped request body.
        using var body = JsonDocument.Parse(handler.RequestBody!);
        var promptText = string.Join("\n", body.RootElement.GetProperty("messages").EnumerateArray()
            .Select(m => m.GetProperty("content").GetString()));

        Assert.Contains(input.Diff.ReplaceLineEndings("\n").Trim(), promptText.ReplaceLineEndings("\n"));
        Assert.DoesNotContain(evalCase.GetProperty("description").GetString()!, promptText);
        Assert.DoesNotContain("dropped-cancellation", promptText);
        Assert.DoesNotContain("expect", promptText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"focus\"", promptText);
    }

    // ---- Schema --------------------------------------------------------------------------

    [Fact]
    public void Schema_satisfies_strict_mode_rules_on_every_object()
    {
        AssertStrictObject(OpenAiCompatibleChangeReviewer.ReviewResultSchema);

        static void AssertStrictObject(JsonElement schema)
        {
            if (schema.GetProperty("type").GetString() == "array")
            {
                AssertStrictObject(schema.GetProperty("items"));
                return;
            }

            if (schema.GetProperty("type").GetString() != "object")
                return;

            Assert.False(schema.GetProperty("additionalProperties").GetBoolean());
            var properties = schema.GetProperty("properties").EnumerateObject().ToArray();
            var required = schema.GetProperty("required").EnumerateArray().Select(r => r.GetString()!).Order().ToArray();
            Assert.Equal(properties.Select(p => p.Name).Order().ToArray(), required);

            foreach (var property in properties)
                AssertStrictObject(property.Value);
        }
    }

    [Fact]
    public void Schema_property_names_match_the_review_contract_records()
    {
        var schema = OpenAiCompatibleChangeReviewer.ReviewResultSchema;

        AssertMatches(typeof(ReviewResult), schema);
        AssertMatches(typeof(ReviewFinding), schema.GetProperty("properties").GetProperty("findings").GetProperty("items"));
        AssertMatches(typeof(ReviewQuestion), schema.GetProperty("properties").GetProperty("questions").GetProperty("items"));

        static void AssertMatches(Type record, JsonElement objectSchema)
        {
            var expected = record.GetConstructors().Single().GetParameters().Select(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name!)).Order().ToArray();
            var actual = objectSchema.GetProperty("properties").EnumerateObject().Select(p => p.Name).Order().ToArray();
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void Schema_severity_enum_matches_the_validator()
    {
        var severity = OpenAiCompatibleChangeReviewer.ReviewResultSchema
            .GetProperty("properties").GetProperty("findings").GetProperty("items")
            .GetProperty("properties").GetProperty("severity");

        var values = severity.GetProperty("enum").EnumerateArray().Select(v => v.GetString()!).ToArray();

        Assert.Equal(ReviewResultValidator.Severities.ToArray(), values);
    }

    // ---- Response handling ---------------------------------------------------------------

    [Fact]
    public async Task Returns_a_validated_review_for_well_formed_output()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, Envelope(ValidContent));

        var result = await Reviewer(handler).ReviewAsync(Input, CancellationToken.None);

        var finding = Assert.Single(result.Findings);
        Assert.Equal("medium", finding.Severity);
        Assert.Equal("src/WeatherClient.cs:GetForecastAsync", finding.Location);
        Assert.Empty(result.Questions);
    }

    [Fact]
    public async Task Accepts_zero_findings_and_zero_questions()
    {
        var content = """{ "summary": "No supported finding.", "findings": [], "questions": [] }""";
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, Envelope(content));

        var result = await Reviewer(handler).ReviewAsync(Input, CancellationToken.None);

        Assert.Empty(result.Findings);
        Assert.Empty(result.Questions);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("""{ "summary": "s", "findings": [], "questions": [], "verdict": "approve" }""")]
    [InlineData("""{ "summary": "s", "questions": [] }""")]
    [InlineData("""{ "summary": "s", "findings": [{ "severity": "critical", "location": "a", "issue": "i", "evidence": "e", "consequence": "c" }], "questions": [] }""")]
    [InlineData("""{ "summary": "s", "findings": [{ "severity": "low", "location": "a", "issue": "i", "evidence": "", "consequence": "c" }], "questions": [] }""")]
    [InlineData("""{ "summary": "s", "findings": [], "questions": [{ "location": "a", "question": "q" }] }""")]
    public async Task Rejects_malformed_or_contract_violating_output(string content)
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, Envelope(content));

        var ex = await Assert.ThrowsAsync<ReviewFailedException>(
            () => Reviewer(handler).ReviewAsync(Input, CancellationToken.None));

        Assert.Equal(content, ex.RawOutput);
    }

    [Fact]
    public async Task Rejects_a_refusal()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, Envelope(null, refusal: "I can't help with that."));

        var ex = await Assert.ThrowsAsync<ReviewFailedException>(
            () => Reviewer(handler).ReviewAsync(Input, CancellationToken.None));

        Assert.Contains("refused", ex.Message);
    }

    [Theory]
    [InlineData("length")]
    [InlineData("content_filter")]
    public async Task Rejects_incomplete_output(string finishReason)
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, Envelope(ValidContent, finishReason));

        var ex = await Assert.ThrowsAsync<ReviewFailedException>(
            () => Reviewer(handler).ReviewAsync(Input, CancellationToken.None));

        Assert.Contains(finishReason, ex.Message);
    }

    [Fact]
    public async Task Rejects_empty_content()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, Envelope(""));

        await Assert.ThrowsAsync<ReviewFailedException>(() => Reviewer(handler).ReviewAsync(Input, CancellationToken.None));
    }

    [Theory]
    [InlineData("<html>gateway error</html>")]
    [InlineData("""{ "choices": [] }""")]
    [InlineData("""{ "id": "x" }""")]
    public async Task Rejects_an_unexpected_response_envelope(string body)
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, body);

        await Assert.ThrowsAsync<ReviewFailedException>(() => Reviewer(handler).ReviewAsync(Input, CancellationToken.None));
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Reports_http_errors_without_exposing_the_api_key(HttpStatusCode status)
    {
        var handler = FakeHttpMessageHandler.Returning(status, """{ "error": { "message": "nope" } }""");

        var ex = await Assert.ThrowsAsync<ReviewFailedException>(
            () => Reviewer(handler).ReviewAsync(Input, CancellationToken.None));

        Assert.Contains(((int)status).ToString(), ex.Message);
        Assert.DoesNotContain(ApiKey, ex.Message);
    }

    [Fact]
    public async Task Reports_an_unreachable_endpoint()
    {
        var handler = new FakeHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));

        var ex = await Assert.ThrowsAsync<ReviewFailedException>(
            () => Reviewer(handler).ReviewAsync(Input, CancellationToken.None));

        Assert.Contains("connection refused", ex.Message);
    }

    // ---- Cancellation and timeout --------------------------------------------------------

    [Fact]
    public async Task Propagates_caller_cancellation_to_the_http_request()
    {
        var handler = new FakeHttpMessageHandler(async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            throw new InvalidOperationException("unreachable");
        });
        using var cancellation = new CancellationTokenSource();

        var review = Reviewer(handler).ReviewAsync(Input, cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => review);
        Assert.True(handler.ObservedToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Reports_an_http_timeout_as_a_review_failure_not_a_cancellation()
    {
        var handler = new FakeHttpMessageHandler(async token =>
        {
            await Task.Delay(Timeout.Infinite, token);
            throw new InvalidOperationException("unreachable");
        });

        var ex = await Assert.ThrowsAsync<ReviewFailedException>(
            () => Reviewer(handler, TimeSpan.FromMilliseconds(50)).ReviewAsync(Input, CancellationToken.None));

        Assert.Contains("timeout", ex.Message);
    }
}
