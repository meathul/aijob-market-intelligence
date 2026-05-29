using System.Globalization;
using System.Text.Json;
using AiJobMarketIntelligence.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AiJobMarketIntelligence.Application.Services.Providers;

/// <summary>
/// Live job provider using the official Adzuna Job Search API.
/// 
/// Requires env/config:
/// - ADZUNA_APP_ID
/// - ADZUNA_APP_KEY
/// Optional:
/// - ADZUNA_COUNTRY (default: us)
/// - ADZUNA_RESULTS_PER_PAGE (default: 50)
/// - ADZUNA_WHAT (default: software engineer)
/// - ADZUNA_WHERE (default: remote)
/// </summary>
public sealed class AdzunaLiveJobProvider : IJobProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<AdzunaLiveJobProvider> _logger;
    private readonly IConfiguration _config;

    public AdzunaLiveJobProvider(HttpClient http, IConfiguration config, ILogger<AdzunaLiveJobProvider> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;

        // Identify ourselves (some APIs reject empty UA)
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("AiJobMarketIntelligence/1.0");
        }

        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<List<JobRaw>> FetchJobsAsync()
    {
        var appId = GetRequired("ADZUNA_APP_ID");
        var appKey = GetRequired("ADZUNA_APP_KEY");

        var country = Get("ADZUNA_COUNTRY") ?? "us";
        var what = Get("ADZUNA_WHAT") ?? "software engineer";
        var where = Get("ADZUNA_WHERE") ?? "remote";
        var resultsPerPage = GetInt("ADZUNA_RESULTS_PER_PAGE", 50);

        // Adzuna endpoint: /v1/api/jobs/{country}/search/{page}
        // Docs: https://developer.adzuna.com/ (exact params vary by plan)
        var page = 1;

        var url =
            $"https://api.adzuna.com/v1/api/jobs/{Uri.EscapeDataString(country)}/search/{page}" +
            $"?app_id={Uri.EscapeDataString(appId)}" +
            $"&app_key={Uri.EscapeDataString(appKey)}" +
            $"&results_per_page={resultsPerPage}" +
            $"&what={Uri.EscapeDataString(what)}" +
            $"&where={Uri.EscapeDataString(where)}" +
            $"&content-type=application/json";

        _logger.LogInformation("Fetching live jobs from Adzuna API: country={Country}, what={What}, where={Where}, resultsPerPage={Results}", country, what, where, resultsPerPage);

        using var resp = await _http.GetAsync(url);

        // Adzuna sometimes returns Content-Type charset=utf8 (non-standard). .NET treats that as invalid.
        // Read bytes and decode explicitly to avoid relying on the response charset.
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        var body = System.Text.Encoding.UTF8.GetString(bytes);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Adzuna API call failed: {StatusCode} {ReasonPhrase}. Body: {Body}", (int)resp.StatusCode, resp.ReasonPhrase, Truncate(body, 1000));
            return new List<JobRaw>();
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // Adzuna responses should contain a 'results' array, but in case of plan/region differences
            // or wrapper responses, try a couple of known/nested locations.
            JsonElement resultsEl;
            if (!TryGetArray(root, out resultsEl, "results") &&
                !TryGetArray(root, out resultsEl, "data", "results") &&
                !TryGetArray(root, out resultsEl, "response", "results"))
            {
                _logger.LogWarning("Adzuna response did not contain a results array. Top-level properties: {Props}. Body (truncated): {Body}",
                    string.Join(",", root.EnumerateObject().Select(p => p.Name)),
                    Truncate(body, 1000));
                return new List<JobRaw>();
            }

            var jobs = new List<JobRaw>();

            foreach (var item in resultsEl.EnumerateArray())
            {
                var parsed = ParseJob(item);
                if (parsed != null)
                    jobs.Add(parsed);
            }

            _logger.LogInformation("Adzuna parsed {Count} jobs", jobs.Count);
            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Adzuna response");
            return new List<JobRaw>();
        }
    }

    private JobRaw? ParseJob(JsonElement item)
    {
        try
        {
            var id = GetString(item, "id") ?? Guid.NewGuid().ToString("N");

            var title = GetString(item, "title") ?? "Untitled";
            var description = GetString(item, "description") ?? "";

            // company.display_name
            var company = GetString(item, "company", "display_name") ?? "Unknown";

            // location.display_name
            var location = GetString(item, "location", "display_name") ?? "Unknown";

            var redirectUrl = GetString(item, "redirect_url")
                              ?? GetString(item, "adref")
                              ?? $"https://www.adzuna.com/jobs/details/{id}";

            // created is usually an ISO8601 string
            var createdStr = GetString(item, "created");
            var posted = ParseDate(createdStr) ?? DateTime.UtcNow;

            // Salary fields vary. If present, we can store a friendly raw range.
            var salaryMin = GetDecimal(item, "salary_min");
            var salaryMax = GetDecimal(item, "salary_max");
            string? salaryRaw = null;
            if (salaryMin.HasValue || salaryMax.HasValue)
            {
                salaryRaw = salaryMin.HasValue && salaryMax.HasValue
                    ? $"{salaryMin.Value:0} - {salaryMax.Value:0} per year"
                    : $"{(salaryMin ?? salaryMax)!.Value:0} per year";
            }

            return new JobRaw
            {
                Title = title.Length > 500 ? title[..500] : title,
                Company = company.Length > 300 ? company[..300] : company,
                Location = location.Length > 300 ? location[..300] : location,
                Description = CleanDescription(description),
                SalaryRaw = salaryRaw?.Length > 200 ? salaryRaw[..200] : salaryRaw,
                Source = "Adzuna",
                Url = redirectUrl.Length > 2000 ? redirectUrl[..2000] : redirectUrl,
                PostedDate = posted,
                CreatedAt = DateTime.UtcNow,
                IsProcessed = false
            };
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Error parsing Adzuna job element");
            return null;
        }
    }

    private string GetRequired(string key)
        => Get(key) ?? throw new InvalidOperationException($"Missing required configuration value '{key}'. Add it to .env or environment variables.");

    private string? Get(string key)
        => _config[key] ?? Environment.GetEnvironmentVariable(key);

    private int GetInt(string key, int @default)
        => int.TryParse(Get(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : @default;

    private static string? GetString(JsonElement element, params string[] path)
    {
        try
        {
            var current = element;
            foreach (var p in path)
            {
                if (!current.TryGetProperty(p, out current))
                    return null;
            }

            return current.ValueKind switch
            {
                JsonValueKind.String => current.GetString(),
                JsonValueKind.Number => current.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static decimal? GetDecimal(JsonElement element, params string[] path)
    {
        var s = GetString(element, path);
        if (s == null) return null;

        if (decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return d;

        return null;
    }

    private static DateTime? ParseDate(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return null;

        if (DateTime.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt))
            return dt;

        return null;
    }

    private static string CleanDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "No description provided";

        try
        {
            // Remove HTML tags
            var cleaned = System.Text.RegularExpressions.Regex.Replace(description, "<[^>]*>", "");
            cleaned = System.Net.WebUtility.HtmlDecode(cleaned);

            if (cleaned.Length > 5000)
                cleaned = cleaned[..5000] + "...";

            return cleaned.Trim();
        }
        catch
        {
            return description;
        }
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "...";

    private static bool TryGetArray(JsonElement root, out JsonElement array, params string[] path)
    {
        array = default;

        try
        {
            var current = root;
            foreach (var p in path)
            {
                if (!current.TryGetProperty(p, out current))
                    return false;
            }

            if (current.ValueKind != JsonValueKind.Array)
                return false;

            array = current;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
