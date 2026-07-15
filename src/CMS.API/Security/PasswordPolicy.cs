namespace CMS.API.Security;

/// <summary>
/// Complexity rule for self-service password changes: at least 8 characters, containing at least
/// 3 of the 4 character classes (uppercase / lowercase / digit / symbol). The bilingual message is
/// the single source of truth — returned by the API and mirrored by the Angular validator.
/// </summary>
public static class PasswordPolicy
{
    public const string RequirementMessage =
        "密碼長度至少需 8 碼，且內容須至少包含四種字元的其中三種：大寫英文／小寫英文／數字／符號 " +
        "(Password must be at least 8 characters and contain at least 3 of the 4 classes: " +
        "uppercase / lowercase / digit / symbol.)";

    public static bool IsCompliant(string? password)
    {
        if (password is null || password.Length < 8)
            return false;

        var classes = 0;
        if (password.Any(char.IsUpper)) classes++;
        if (password.Any(char.IsLower)) classes++;
        if (password.Any(char.IsDigit)) classes++;
        // Symbol = anything that is not an upper/lower letter or digit.
        if (password.Any(c => !char.IsUpper(c) && !char.IsLower(c) && !char.IsDigit(c))) classes++;

        return classes >= 3;
    }
}
