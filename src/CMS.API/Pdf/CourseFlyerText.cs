using System.Globalization;

namespace CMS.API.Pdf;

/// <summary>
/// Pure text-shaping rules for the flyer, extracted from the QuestPDF layout so each rule is
/// unit-testable without parsing PDF bytes (eng review 3A).
/// </summary>
public static class CourseFlyerText
{
    /// <summary>Character budget (UTF-16 code units) for the objective blurb.</summary>
    public const int ObjectiveBudget = 280;

    public const string Ellipsis = "……";

    /// <summary>
    /// Truncates the objective to <paramref name="budget"/> code units, cutting only on text
    /// element boundaries — a surrogate pair (CJK Ext B, emoji) straddling the budget is dropped
    /// whole, never split. Returns null for null/whitespace input (the flyer omits the section).
    /// </summary>
    public static string? TruncateObjective(string? objective, int budget = ObjectiveBudget)
    {
        if (string.IsNullOrWhiteSpace(objective))
            return null;

        var text = objective.Trim();
        if (text.Length <= budget)
            return text;

        var safeLength = 0;
        var elements = StringInfo.GetTextElementEnumerator(text);
        while (elements.MoveNext())
        {
            var elementLength = ((string)elements.Current).Length;
            if (safeLength + elementLength > budget)
                break;
            safeLength += elementLength;
        }

        return text[..safeLength] + Ellipsis;
    }

    /// <summary>"NT$ 45,000" — thousands separator, no decimals (NT$ has no cents).</summary>
    public static string FormatPrice(decimal listPrice)
        => $"NT$ {listPrice.ToString("N0", CultureInfo.InvariantCulture)}";

    /// <summary>Trailing-zero-trimmed: 36 → "36", 4.5 → "4.5".</summary>
    public static string FormatCredit(decimal credit)
        => credit.ToString("0.##", CultureInfo.InvariantCulture);

    public static string FormatDate(DateOnly date)
        => date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);

    public static string FormatDateRange(DateOnly on, DateOnly off)
        => $"{FormatDate(on)} – {FormatDate(off)}";
}
