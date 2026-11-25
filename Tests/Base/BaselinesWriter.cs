using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using NUnit.Framework.Internal;

namespace Tests
{
	public sealed class BaselinesWriter
	{
		// pairs direct and remote baselines on every run, with or without a baselines path, and detects baseline
		// overwrites by another test(case)
		static readonly BaselineComparer _comparer = new();

		// comparison results of cases gated by ActiveIssue(BaselinesMismatch = true), by NUnit test id: the gate reads
		// them after TearDown, when the per-test context is already released
		static readonly Dictionary<string, BaselineComparison> _gatedComparisons = new();

		static bool _filesWritten;

		// guards the comparer, the maps and the baseline file writes against concurrent
		// access when tests run in parallel across providers
		static readonly Lock _sync = new();

		public static void Write(string? baselinesPath, string baseline, bool isRemote, string? providerSuffix)
		{
			var test = TestExecutionContext.CurrentContext.CurrentTest;

			var context = GetTestContextName(test)?.StripRemote();

			if (context == null)
				return;

			var fileName = $"{NormalizeFileName(test.FullName)}.sql";
			if (isRemote)
				fileName = fileName.StripRemote();

			var relativePath = Path.Combine(context + providerSuffix, test.ClassName!.Replace('.', Path.DirectorySeparatorChar), fileName);

			baseline = Normalize(baseline);

			var gated = ActiveIssueAttribute.GovernsBaselinesMismatch(test);

			BaselineComparison? comparison;

			// Serialize access to the shared comparer and the per-baseline files: under parallel runs this is hit
			// from multiple provider threads (direct + remote of one provider stay on a single lane, so the two
			// writes of one baseline remain ordered).
			lock (_sync)
			{
				comparison = Record(_comparer, baselinesPath, relativePath, isRemote, baseline, gated);

				if (baselinesPath != null && WritesBaseline(gated, isRemote, comparison))
					_filesWritten = true;

				if (gated && comparison != null)
					_gatedComparisons[test.Id] = comparison.Value;
			}

			if (comparison == BaselineComparison.Mismatched)
			{
				throw new InvalidOperationException(
					$"Baselines for remote context doesn't match direct access baselines. Test: {test.FullName}"
					+ (baselinesPath == null ? $"\nBaselinesPath is not set, so only this side's SQL is available:\n{baseline}" : null));
			}
		}

		/// <summary>
		/// Compares <paramref name="baseline"/> with the other side of the case and, when <paramref name="baselinesPath"/>
		/// is set, writes the first side to <c>&lt;path&gt;/&lt;relativePath&gt;</c> and a mismatching second side to
		/// <c>.other</c> next to it. When <paramref name="gated"/> is <see langword="true"/> (a gate already records the
		/// mismatch), no <c>.other</c> is written and the file holds the direct side, whichever side runs first.
		/// </summary>
		public static BaselineComparison? Record(BaselineComparer comparer, string? baselinesPath, string relativePath, bool isRemote, string baseline, bool gated)
		{
			var comparison = comparer.Compare(relativePath, isRemote, baseline);

			if (baselinesPath != null)
			{
				var fullPath = Path.Combine(baselinesPath, relativePath);

				if (WritesBaseline(gated, isRemote, comparison))
				{
					Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
					File.WriteAllText(fullPath, baseline, Encoding.UTF8);
				}
				else if (comparison == BaselineComparison.Mismatched && !gated)
				{
					File.WriteAllText(fullPath + ".other", baseline, Encoding.UTF8);
				}
			}

			return comparison;
		}

		// a gated pair differs, so writing whichever side runs first would make the file depend on the run order
		static bool WritesBaseline(bool gated, bool isRemote, BaselineComparison? comparison)
		{
			return gated ? !isRemote : comparison == null;
		}

		/// <summary>
		/// Returns and forgets the comparison recorded for a case gated by
		/// <see cref="ActiveIssueAttribute.BaselinesMismatch"/>; <see langword="null"/> when its other side did not run
		/// in this process.
		/// </summary>
		internal static BaselineComparison? TakeGatedComparison(string testId)
		{
			lock (_sync)
			{
				if (_gatedComparisons.TryGetValue(testId, out var comparison))
				{
					_gatedComparisons.Remove(testId);
					return comparison;
				}

				return null;
			}
		}

		static string Normalize(string baseline)
		{
			return baseline
				.Replace("\r\n", "\n")
				.Replace(" (asynchronously)", string.Empty)
				.Replace("BeginTransaction\n", string.Empty)
				.Replace("BeginTransaction(Unspecified)\n", string.Empty)
				.Replace("BeginTransaction(Serializable)\n", string.Empty)
				.Replace("BeginTransaction(RepeatableRead)\n", string.Empty)
				.Replace("BeginTransaction(ReadCommitted)\n", string.Empty)
				.Replace("BeginTransaction(Snapshot)\n", string.Empty)
				.Replace("BeginTransactionAsync(Unspecified)\n", string.Empty)
				.Replace("BeginTransactionAsync(Serializable)\n", string.Empty)
				.Replace("BeginTransactionAsync(RepeatableRead)\n", string.Empty)
				.Replace("BeginTransactionAsync(ReadCommitted)\n", string.Empty)
				.Replace("BeginTransactionAsync(Snapshot)\n", string.Empty)
				.Replace("BeforeExecute\n", string.Empty)
				.Replace("CommitTransaction\n", string.Empty)
				.Replace("CommitTransactionAsync\n", string.Empty)
				.Replace("DisposeTransaction\n", string.Empty)
				.Replace("DisposeTransactionAsync\n", string.Empty)
				;
		}

		private static string NormalizeFileName(string name)
		{
			// " used in each test name, for now we just remove it
			var normalized = name
				.Replace("\"", string.Empty)
				.Replace("\\" , $"0x{(ushort)'\\':X4}")
				.Replace(">" , $"0x{(ushort)'>':X4}")
				.Replace("<" , $"0x{(ushort)'<':X4}")
				.Replace("/" , $"0x{(ushort)'/':X4}")
				.Replace(":" , $"0x{(ushort)':':X4}")
				.Replace("*" , $"0x{(ushort)'*':X4}")
				.Replace("?" , $"0x{(ushort)'?':X4}")
				.Replace("|" , $"0x{(ushort)'|':X4}")
				;

			// Control characters are path-illegal too, and arrive here whenever a test puts one in an identifier.
			// Escaped in a loop rather than by name because spelling all thirty-two would dwarf the method.
			var sb = new StringBuilder(normalized.Length);

			foreach (var chr in normalized)
			{
				if (char.IsControl(chr))
					sb.Append($"0x{(ushort)chr:X4}");
				else
					sb.Append(chr);
			}

			return sb.ToString();
		}

		private static string? GetTestContextName(Test test)
		{
			var parameters = test.Method!.GetParameters();

			for (var i = 0; i < parameters.Length; i++)
			{
				var attr = parameters[i].GetCustomAttributes<DataSourcesBaseAttribute>(true);

				if (attr.Length != 0)
				{
					return (string)test.Arguments[i]!;
				}
			}

			return null;
		}

		public static void WriteMetrics(string baselinesPath, string baseline)
		{
			// Nothing to report for a run that captured no baselines - the condition the removed
			// last-writer-wins _context slot used to express.
			lock (_sync)
			{
				if (!_filesWritten)
					return;
			}

			var target = TestUtils.GetConfigName();

			var fixturePath = Path.Combine(baselinesPath, target);

			Directory.CreateDirectory(fixturePath);

			// The ActivityStatistics report is process-wide, so the configuration name identifies it on its
			// own. It used to be prefixed with whichever provider wrote a baseline most recently, which under
			// a parallel multi-provider run named an arbitrary one, differently from run to run.
			var fileName = $"{Environment.OSVersion.Platform}.Metrics.txt";

			var fullPath = Path.Combine(fixturePath, fileName);

			File.WriteAllText(fullPath, baseline);
		}
	}
}
