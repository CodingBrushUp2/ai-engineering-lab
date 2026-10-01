using AiEngineeringLab.Demo.OpenAi;

namespace AiEngineeringLab.Demo.Tests;

public sealed class OpenAiCompatibleOptionsTests
{
    private static Func<string, string?> Environment(string? apiKey = "sk-test-secret", string? model = "test-model", string? baseUrl = null) =>
        name => name switch
        {
            OpenAiCompatibleOptions.ApiKeyVariable => apiKey,
            OpenAiCompatibleOptions.ModelVariable => model,
            OpenAiCompatibleOptions.BaseUrlVariable => baseUrl,
            _ => null,
        };

    [Fact]
    public void Unset_api_key_is_a_configuration_error()
    {
        var ok = OpenAiCompatibleOptions.TryFromEnvironment(Environment(apiKey: null), out var options, out var error);

        Assert.False(ok);
        Assert.Null(options);
        Assert.Contains(OpenAiCompatibleOptions.ApiKeyVariable, error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_api_key_is_a_configuration_error(string apiKey)
    {
        var ok = OpenAiCompatibleOptions.TryFromEnvironment(Environment(apiKey: apiKey), out var options, out var error);

        Assert.False(ok);
        Assert.Null(options);
        Assert.Contains(OpenAiCompatibleOptions.ApiKeyVariable, error);
    }

    [Fact]
    public void Missing_model_is_a_configuration_error()
    {
        var ok = OpenAiCompatibleOptions.TryFromEnvironment(Environment(model: null), out _, out var error);

        Assert.False(ok);
        Assert.Contains(OpenAiCompatibleOptions.ModelVariable, error);
    }

    [Fact]
    public void Generic_openai_variable_is_not_used()
    {
        // Opt-in only: a globally exported OPENAI_API_KEY must not enable paid calls.
        Func<string, string?> environment = name => name == "OPENAI_API_KEY" ? "sk-global" : null;

        Assert.False(OpenAiCompatibleOptions.TryFromEnvironment(environment, out _, out _));
    }

    [Fact]
    public void Defaults_to_the_openai_base_url()
    {
        Assert.True(OpenAiCompatibleOptions.TryFromEnvironment(Environment(), out var options, out _));

        Assert.Equal(OpenAiCompatibleOptions.DefaultBaseUrl, options.BaseUrl);
        Assert.Equal("test-model", options.Model);
    }

    [Theory]
    [InlineData("https://example.test/v1", "https://example.test/v1/")]
    [InlineData("https://example.test/v1/", "https://example.test/v1/")]
    [InlineData("http://localhost:11434/v1", "http://localhost:11434/v1/")]
    [InlineData("http://127.0.0.1:8080/v1", "http://127.0.0.1:8080/v1/")]
    public void Accepts_https_or_loopback_http_base_urls(string configured, string expected)
    {
        Assert.True(OpenAiCompatibleOptions.TryFromEnvironment(Environment(baseUrl: configured), out var options, out _));

        Assert.Equal(new Uri(expected), options.BaseUrl);
    }

    [Theory]
    [InlineData("http://example.test/v1")]
    [InlineData("not a url")]
    [InlineData("ftp://example.test/v1")]
    public void Rejects_unsafe_or_invalid_base_urls(string configured)
    {
        var ok = OpenAiCompatibleOptions.TryFromEnvironment(Environment(baseUrl: configured), out _, out var error);

        Assert.False(ok);
        Assert.Contains(OpenAiCompatibleOptions.BaseUrlVariable, error);
    }

    [Fact]
    public void ToString_does_not_expose_the_api_key()
    {
        Assert.True(OpenAiCompatibleOptions.TryFromEnvironment(Environment(), out var options, out _));

        Assert.DoesNotContain("sk-test-secret", options.ToString());
    }
}
