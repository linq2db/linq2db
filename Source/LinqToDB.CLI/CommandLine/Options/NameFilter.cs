using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Database object name filter.
	/// </summary>
	internal sealed class NameFilter
	{
		private readonly HashSet<string>                               _exactNames    = new (StringComparer.Ordinal);
		private readonly Dictionary<string, HashSet<string>>           _schemaNames   = new (StringComparer.Ordinal);
		private readonly Dictionary<string, Regex>                     _regexes       = new (StringComparer.Ordinal);
		private readonly Dictionary<string, Dictionary<string, Regex>> _schemaRegexes = new (StringComparer.Ordinal);

		/// <summary>
		/// Register database object name.
		/// </summary>
		/// <param name="schema">Object schema (optional).</param>
		/// <param name="name">Object name.</param>
		/// <returns><see langword="true"/>, if name registered and <see langword="false"/> on error</returns>
		public bool AddName(string? schema, string name)
		{
			if (schema == null)
				return _exactNames.Add(name);
			else
			{
				if (!_schemaNames.TryGetValue(schema, out var names))
					_schemaNames.Add(schema, names = new (StringComparer.Ordinal));

				return names.Add(name);
			}
		}

		public bool AddRegularExpression(string? schema, string regex)
		{
			if (schema == null)
			{
				if (_regexes.ContainsKey(regex))
					return false;

				_regexes.Add(regex, new Regex(regex, RegexOptions.Compiled, TimeSpan.FromSeconds(1)));
				return true;
			}
			else
			{
				if (!_schemaRegexes.TryGetValue(schema, out var regexes))
					_schemaRegexes.Add(schema, regexes = new(StringComparer.Ordinal));

				if (regexes.ContainsKey(regex))
					return false;

				regexes.Add(regex, new Regex(regex, RegexOptions.Compiled, TimeSpan.FromSeconds(1)));
				return true;
			}
		}

		/// <summary>
		/// Apply filter to database object name.
		/// </summary>
		/// <param name="schema">Object schema name.</param>
		/// <param name="name">Object name.</param>
		/// <returns><see langword="true"/> if name pass filter, <c><see langword="false"/></c> otherwise.</returns>
		public bool ApplyTo(string? schema, string name)
		{
			if (_exactNames.Contains(name))
				return true;

			if (schema != null && _schemaNames.TryGetValue(schema, out var names) && names.Contains(name))
				return true;

			foreach (var regex in _regexes.Values)
				if (regex.IsMatch(name))
					return true;

			if (schema != null && _schemaRegexes.TryGetValue(schema, out var regexes))
				foreach (var regex in regexes.Values)
					if (regex.IsMatch(name))
						return true;

			return false;
		}
	}
}
