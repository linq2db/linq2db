namespace LinqToDB.LINQPad;

/// <summary>
/// Database provider descriptor for specific database.
/// </summary>
/// <param name="Name">Provider identifier (e.g. value from <see cref="ProviderName"/> class).</param>
/// <param name="DisplayName">Provider display name in settings dialog.</param>
/// <param name="IsDefault">When set, specified provider dialect will be selected automatically.</param>
/// <param name="IsHidden">When set, specified provider will not be shown in list of available dialects and used only to support old connections with provider names, existed in older releases.</param>
internal sealed record ProviderInfo(string Name, string DisplayName, bool IsDefault = false, bool IsHidden = false);
