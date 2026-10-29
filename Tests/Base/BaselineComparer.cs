using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Tests
{
	/// <summary>Result of comparing the direct and the remote baseline of one test case.</summary>
	public enum BaselineComparison
	{
		/// <summary>Both contexts produced the same SQL.</summary>
		Matched,
		/// <summary>The remote context produced different SQL than the direct one.</summary>
		Mismatched,
	}

	/// <summary>
	/// Pairs the direct and the remote baseline of each test case, keeping only a hash per side so a whole run fits in
	/// memory. Not thread-safe; <see cref="BaselinesWriter"/> serializes access.
	/// </summary>
	public sealed class BaselineComparer
	{
		[Flags]
		enum Side
		{
			Direct = 1,
			Remote = 2,
		}

		sealed class Entry
		{
			public Side   Seen;
			public string Hash = null!;
		}

		// case-insensitive like the baseline files on Windows: two names differing only in case are one baseline
		readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Records <paramref name="baseline"/> for one side of the case <paramref name="key"/> and compares it with the
		/// other side when that one was recorded earlier.
		/// </summary>
		/// <returns><see langword="null"/> for the first side of a pair, otherwise the comparison.</returns>
		/// <exception cref="InvalidOperationException">The same side of <paramref name="key"/> was already recorded.</exception>
		public BaselineComparison? Compare(string key, bool isRemote, string baseline)
		{
			var side = isRemote ? Side.Remote : Side.Direct;
			var hash = Hash(baseline);

			if (!_entries.TryGetValue(key, out var entry))
			{
				_entries.Add(key, new Entry { Seen = side, Hash = hash });
				return null;
			}

			if ((entry.Seen & side) != 0)
				throw new InvalidOperationException($"Baseline already in use: {key} ({side})");

			entry.Seen |= side;

			return string.Equals(entry.Hash, hash, StringComparison.Ordinal) ? BaselineComparison.Matched : BaselineComparison.Mismatched;
		}

		static string Hash(string baseline)
		{
#if NETFRAMEWORK
			using var sha = SHA256.Create();
			return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(baseline)));
#else
			return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(baseline)));
#endif
		}
	}
}
