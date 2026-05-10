using System.Globalization;
using System.Text.RegularExpressions;
using AiJobMarketIntelligence.Domain.Entities;

namespace AiJobMarketIntelligence.Application.Services.Salary;

/// <summary>
/// Heuristic salary parser. Extracts salary ranges and normalizes to a period.
/// Looks at SalaryRaw first, then falls back to job description.
/// </summary>
public sealed class SalaryParserService : ISalaryParserService
{
    // Examples handled:
    // "$80k - $120k", "$40/hour", "100000 yearly", "₹12 LPA", "€70,000 per year"

    // currency symbols and common codes
    private static readonly (string Token, string Currency)[] CurrencyTokens =
    [
        ("$", "USD"),
        ("€", "EUR"),
        ("£", "GBP"),
        ("₹", "INR"),
        ("AUD", "AUD"),
        ("CAD", "CAD"),
        ("USD", "USD"),
        ("EUR", "EUR"),
        ("GBP", "GBP"),
        ("INR", "INR")
    ];

    // capture: currency? number (with separators) optional k/m
    private static readonly Regex MoneyRegex = new(
        @"(?<cur>\$|€|£|₹|AUD|CAD|USD|EUR|GBP|INR)?\s*(?<num>\d{1,3}(?:[\,\.]\d{3})*|\d+)(?<suffix>k|K|m|M)?",
        RegexOptions.Compiled);

    private static readonly Regex RangeSepRegex = new(@"\s*(?:-|–|—|to)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public SalaryParseResult Parse(string? salaryRaw, string? description)
    {
        var text = FirstNonEmpty(salaryRaw, description);
        if (string.IsNullOrWhiteSpace(text)) return new SalaryParseResult();

        var currency = DetectCurrency(text);
        var period = DetectPeriod(text);

        // Try to parse ranges like "80k - 120k" or "80000 to 120000"
        var parts = RangeSepRegex.Split(text);
        if (parts.Length >= 2)
        {
            var a = ParseFirstMoney(parts[0]);
            var b = ParseFirstMoney(parts[1]);
            if (a != null && b != null)
            {
                var min = Math.Min(a.Value, b.Value);
                var max = Math.Max(a.Value, b.Value);

                // Handle LPA (lakhs per annum): if text contains LPA and values are small, treat as lakhs.
                if (ContainsLpa(text))
                {
                    min *= 100000;
                    max *= 100000;
                    period = SalaryPeriod.Yearly;
                    currency ??= "INR";
                }

                return new SalaryParseResult
                {
                    SalaryMin = ClampToInt(min),
                    SalaryMax = ClampToInt(max),
                    Currency = currency,
                    SalaryPeriod = period
                };
            }
        }

        // Single value: "$40/hour" or "100000 yearly"
        var single = ParseFirstMoney(text);
        if (single != null)
        {
            var value = single.Value;

            if (ContainsLpa(text))
            {
                value *= 100000;
                period = SalaryPeriod.Yearly;
                currency ??= "INR";
            }

            return new SalaryParseResult
            {
                SalaryMin = ClampToInt(value),
                SalaryMax = null,
                Currency = currency,
                SalaryPeriod = period
            };
        }

        return new SalaryParseResult { Currency = currency, SalaryPeriod = period };
    }

    private static string? DetectCurrency(string text)
    {
        foreach (var (token, code) in CurrencyTokens)
        {
            if (text.Contains(token, StringComparison.OrdinalIgnoreCase))
                return code;
        }

        return null;
    }

    private static SalaryPeriod DetectPeriod(string text)
    {
        var t = text.ToLowerInvariant();

        if (t.Contains("hour") || t.Contains("hr") || t.Contains("/h") || t.Contains("per hour"))
            return SalaryPeriod.Hourly;

        if (t.Contains("month") || t.Contains("/mo") || t.Contains("per month"))
            return SalaryPeriod.Monthly;

        if (t.Contains("year") || t.Contains("/yr") || t.Contains("annual") || t.Contains("per annum") || t.Contains("annum") || t.Contains("yearly") || t.Contains("pa"))
            return SalaryPeriod.Yearly;

        if (ContainsLpa(t))
            return SalaryPeriod.Yearly;

        return SalaryPeriod.Unknown;
    }

    private static bool ContainsLpa(string text)
        => text.Contains("lpa", StringComparison.OrdinalIgnoreCase);

    private static double? ParseFirstMoney(string text)
    {
        var match = MoneyRegex.Matches(text).Cast<Match>().FirstOrDefault();
        if (match is null) return null;

        var rawNum = match.Groups["num"].Value;
        if (string.IsNullOrWhiteSpace(rawNum)) return null;

        // normalize separators
        rawNum = rawNum.Replace(",", string.Empty);

        if (!double.TryParse(rawNum, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
            return null;

        var suffix = match.Groups["suffix"].Value;
        if (!string.IsNullOrEmpty(suffix))
        {
            if (suffix.Equals("k", StringComparison.OrdinalIgnoreCase)) value *= 1000;
            else if (suffix.Equals("m", StringComparison.OrdinalIgnoreCase)) value *= 1000000;
        }

        return value;
    }

    private static int? ClampToInt(double? value)
    {
        if (value is null) return null;
        if (value < 0) return 0;
        if (value > int.MaxValue) return int.MaxValue;
        return (int)Math.Round(value.Value);
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
}
