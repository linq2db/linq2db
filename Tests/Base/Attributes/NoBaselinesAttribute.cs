using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using NUnit.Framework.Interfaces;

namespace Tests
{
	/// <summary>
	/// Declares that a test cannot produce a stable SQL baseline — it embeds a generated GUID, the current time, a
	/// sequence value, runs queries on several threads, or emits SQL whose volume is the subject of the test — so its
	/// SQL is neither captured nor compared between the direct and remote contexts.
	/// <para>
	/// Covers the whole test case. Exclude only setup or cleanup SQL with a <see cref="DisableBaseline"/> scope instead,
	/// and gate a known direct-vs-remote SQL difference with
	/// <see cref="ActiveIssueAttribute.BaselinesMismatch"/>, which reports when the difference is gone.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
	public sealed class NoBaselinesAttribute : Attribute
	{
		string[]? _configurations;

		/// <summary>Declares that the test cannot produce a stable SQL baseline.</summary>
		/// <param name="reason">Why the SQL is not stable.</param>
		public NoBaselinesAttribute(string reason)
		{
			Reason = reason;
		}

		/// <summary>Gets why the SQL is not stable.</summary>
		public string Reason { get; }

		/// <summary>
		/// Gets or sets configuration names this attribute applies to; unset applies to every configuration. Matches the
		/// direct and the remote (<c>.LinqService</c>) context of each name.
		/// </summary>
		[MaybeNull]
		public string[] Configurations
		{
			get => _configurations;
			set => _configurations = ConfigurationNames.Split(value);
		}

		/// <summary>Gets or sets comma-separated configuration names this attribute applies to. See <see cref="Configurations"/>.</summary>
		public string Configuration
		{
			get => ConfigurationNames.Join(_configurations);
			set => _configurations = ConfigurationNames.Split(value);
		}

		/// <summary>Whether this instance applies to a test case running against <paramref name="provider"/>.</summary>
		public bool AppliesTo(string? provider) => provider == null || ConfigurationNames.Matches(_configurations, provider);

		/// <summary>
		/// Whether an instance on the test method or its fixture applies to <paramref name="test"/>. Called from the
		/// <c>[SetUp]</c> of the test base classes, after the per-test context exists: an attribute hook running earlier
		/// would set the flag on a context that is then replaced.
		/// </summary>
		public static bool AppliesTo(ITest test)
		{
			if (test.Method == null)
				return false;

			var (provider, _) = NUnitUtils.GetContext(test);

			return test.Method.GetCustomAttributes<NoBaselinesAttribute>(true)
				.Concat(test.Method.TypeInfo.GetCustomAttributes<NoBaselinesAttribute>(true))
				.Any(a => a.AppliesTo(provider));
		}
	}
}
