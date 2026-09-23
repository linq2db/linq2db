namespace LinqToDB.LINQPad;

/// <summary>
/// Database provider descriptor for specific database.
/// </summary>
/// <param name="Name">Provider identifier (e.g. value from <see cref="ProviderName"/> class).</param>
/// <param name="DisplayName">Provider display name in settings dialog.</param>
/// <param name="IsDefault">When set, specified provider dialect will be selected automatically.</param>
/// <param name="IsHidden">When set, specified provider will not be shown in list of available dialects and used only to support old connections with provider names, existed in older releases.</param>
/// <param name="Troubleshoot">Provider-specific troubleshoot notes.</param>
/// <param name="MinimumRuntime">Lowest .NET major version the provider client runs on. Its packages are never provisioned for an older query runtime.</param>
/// <param name="SecondaryName">When set, the connection also takes a second connection string for this provider, used only to fetch schema; <paramref name="Name"/> still runs queries.</param>
internal sealed record ProviderInfo(string Name, string DisplayName, bool IsDefault = false, bool IsHidden = false, string? Troubleshoot = null, int MinimumRuntime = 0, string? SecondaryName = null);
