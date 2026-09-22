using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ApiGuard.Core;

namespace ApiGuard.Cli;

public static class GitHubReporter
{
    public static async Task TryPostPullRequestCommentAsync(IReadOnlyList<BreakingChange> changes)
    {
        var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
        var repo = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY");
        var eventName = Environment.GetEnvironmentVariable("GITHUB_EVENT_NAME");
        var eventPath = Environment.GetEnvironmentVariable("GITHUB_EVENT_PATH");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(repo)
            || eventName != "pull_request" || string.IsNullOrEmpty(eventPath) || !File.Exists(eventPath))
        {
            return;
        }

        int prNumber;
        try
        {
            using var eventDoc = JsonDocument.Parse(await File.ReadAllTextAsync(eventPath));
            prNumber = eventDoc.RootElement.GetProperty("pull_request").GetProperty("number").GetInt32();
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            Console.Error.WriteLine($"ApiGuard: could not read PR number from event payload: {ex.Message}");
            return;
        }

        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("ApiGuard-Action");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var body = BuildCommentBody(changes);
        var payload = JsonSerializer.Serialize(new { body });
        var url = $"https://api.github.com/repos/{repo}/issues/{prNumber}/comments";

        using var response = await http.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode)
        {
            var responseBody = await response.Content.ReadAsStringAsync();
            Console.Error.WriteLine($"ApiGuard: failed to post PR comment ({(int)response.StatusCode}): {responseBody}");
        }
    }

    private static string BuildCommentBody(IReadOnlyList<BreakingChange> changes)
    {
        var breaking = changes.Where(c => c.Severity == ChangeSeverity.Breaking).ToList();
        var warnings = changes.Where(c => c.Severity == ChangeSeverity.Warning).ToList();

        var sb = new StringBuilder();
        sb.AppendLine(breaking.Count > 0
            ? "## 🛑 ApiGuard: breaking API changes detected"
            : "## ⚠️ ApiGuard: API changes detected");
        sb.AppendLine();

        if (breaking.Count > 0)
        {
            sb.AppendLine("### Breaking");
            foreach (var change in breaking)
            {
                var location = change.Method is null ? change.Path : $"`{change.Method} {change.Path}`";
                sb.AppendLine($"- {location}: {change.Description}");
            }
            sb.AppendLine();
        }

        if (warnings.Count > 0)
        {
            sb.AppendLine("### Warnings");
            foreach (var change in warnings)
            {
                var location = change.Method is null ? change.Path : $"`{change.Method} {change.Path}`";
                sb.AppendLine($"- {location}: {change.Description}");
            }
        }

        return sb.ToString();
    }
}
