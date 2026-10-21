using System.Linq;

namespace Tests
{
	/// <summary>
	/// Configuration-name lists of the provider-targeted test attributes (<see cref="ActiveIssueAttribute"/>,
	/// <see cref="NoBaselinesAttribute"/>). Entries may themselves be comma-separated (the <c>TestProvName.All*</c>
	/// constants are), and are matched exactly against the context name with the <c>.LinqService</c> suffix stripped.
	/// </summary>
	static class ConfigurationNames
	{
		public static string[] Split(string value) => value.Split(',').Select(_ => _.Trim()).ToArray();

		public static string[] Split(string[] values) => values.SelectMany(Split).ToArray();

		public static string Join(string[]? values) => values != null ? string.JoinStrings(',', values) : string.Empty;

		/// <summary>An empty or unset list matches every provider.</summary>
		public static bool Matches(string[]? configurations, string provider) =>
			configurations == null || configurations.Length == 0 || configurations.Contains(provider);
	}
}
