namespace CMS.API.Models;

/// <summary>
/// Slim lookup row for select/multiselect options. For AppUser the value carried is the
/// string <c>UserId</c>, and <see cref="Label"/> is <c>UserName (UserId)</c>.
/// </summary>
public class LookupItem
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
