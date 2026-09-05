using System.Threading.Tasks;

using CodeGenerators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;

using NUnit.Framework;

namespace Tests.Analyzers.Internal
{
	[TestFixture]
	public class ProjectFlagsAnalyzerTests
	{
		// A faithful miniature of the real model. The analyzer derives its reachable value set from
		// GetProjectFlags rather than from a hardcoded table, so every snippet has to carry one - and that is
		// also what lets the drift fixtures below break the model on purpose.
		const string Model = """
			namespace LinqToDB.Internal.Linq.Builder
			{
				[System.Flags]
				enum ProjectFlags
				{
					None              = 0x00,
					SQL               = 1 << 0,
					Expression        = 1 << 1,
					Root              = 1 << 2,
					ExtractProjection = 1 << 3,
					Keys              = 1 << 5,
					Table             = 1 << 8,
					Expand            = 1 << 11,
					MemberRoot        = 1 << 12,
				}

				enum BuildPurpose { None, Sql, Table, Expression, Expand, Root, Extract }

				[System.Flags]
				enum BuildFlags { None = 0, ForKeys = 1, ForMemberRoot = 2 }

				static class ProjectFlagExtensions
				{
					public static bool IsSql(this ProjectFlags flags)               => flags.HasFlag(ProjectFlags.SQL);
					public static bool IsExpression(this ProjectFlags flags)        => flags.HasFlag(ProjectFlags.Expression);
					public static bool IsRoot(this ProjectFlags flags)              => flags.HasFlag(ProjectFlags.Root);
					public static bool IsExtractProjection(this ProjectFlags flags) => flags.HasFlag(ProjectFlags.ExtractProjection);
					// Statement body, as every real ProjectFlagExtensions predicate is written - the reader has a
					// separate branch for each form, and the expression-bodied ones below cover the other.
					public static bool IsKeys(this ProjectFlags flags)
					{
						return flags.HasFlag(ProjectFlags.Keys);
					}

					public static bool IsTable(this ProjectFlags flags)             => flags.HasFlag(ProjectFlags.Table);
					public static bool IsExpand(this ProjectFlags flags)            => flags.HasFlag(ProjectFlags.Expand);
					public static bool IsMemberRoot(this ProjectFlags flags)        => flags.HasFlag(ProjectFlags.MemberRoot);
					public static bool IsSqlOrExpression(this ProjectFlags flags)   => (flags & (ProjectFlags.SQL | ProjectFlags.Expression)) != 0;
				}

				sealed class ExpressionBuildVisitor
				{
					BuildPurpose _buildPurpose;
					BuildFlags   _buildFlags;

					ProjectFlags GetProjectFlags()
					{
						var flags = ProjectFlags.None;

						switch (_buildPurpose)
						{
							case BuildPurpose.Sql:
								flags |= ProjectFlags.SQL;
								if (_buildFlags.HasFlag(BuildFlags.ForKeys))
									flags |= ProjectFlags.Keys;
								break;
							case BuildPurpose.Expression:
								flags |= ProjectFlags.Expression;
								if (_buildFlags.HasFlag(BuildFlags.ForKeys))
									flags |= ProjectFlags.Keys;
								break;
							case BuildPurpose.Extract:
								flags |= ProjectFlags.ExtractProjection;
								if (_buildFlags.HasFlag(BuildFlags.ForKeys))
									flags |= ProjectFlags.Keys;
								break;
							case BuildPurpose.Root:
								flags |= ProjectFlags.Root;
								break;
							case BuildPurpose.Table:
								flags |= ProjectFlags.Table;
								break;
							case BuildPurpose.Expand:
								flags |= ProjectFlags.Expand;
								break;
							default:
								throw new System.ArgumentOutOfRangeException();
						}

						if (_buildFlags.HasFlag(BuildFlags.ForMemberRoot))
							flags |= ProjectFlags.MemberRoot;

						return flags;
					}
				}
			}

			""";

		static string Consumer(string body) => Model + $$"""
			namespace Consumer
			{
				using LinqToDB.Internal.Linq.Builder;

				static class Subject
				{
			{{body}}
				}
			}
			""";

		static Task Verify(string body, params DiagnosticResult[] expected) => AnalyzerVerifier<ProjectFlagsAnalyzer>.VerifyAsync(Consumer(body), expected);

		// Most fixtures pin only the id and the span, via {|LINQ2DB0004:...|} markup. The two below also pin the
		// message's reason argument, because that argument is the rule's affordance - it tells a reader whether
		// the clause is a modelling mistake or dead code behind an earlier return - and nothing else asserts it.
		static DiagnosticResult NeverTrue(string clause, string reason) =>
			new DiagnosticResult("LINQ2DB0004", DiagnosticSeverity.Warning).WithLocation(0).WithArguments(clause, reason);

		const string ExcludedEarlier = "every value that can still reach this point is already excluded by an earlier test on the same path";

		// TO-1 - the #5727 shape. Keys accompanies only SQL / Expression / ExtractProjection, so under IsKeys()
		// the Expand test can never be true and the 43 lines it guarded never ran.
		[Test]
		public Task KeysWithExpandIsNeverTrue() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsKeys() && {|#0:flags.IsExpand()|})
							return 1;

						return 0;
					}
			""", NeverTrue("flags.IsExpand()", ExcludedEarlier));

		// TO-1 control - the same shape with a pair the model permits. Without this arm the rule could be
		// flagging every conjunction and still pass the test above.
		[Test]
		public Task KeysWithSqlIsFine() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsSql() && flags.IsKeys())
							return 1;

						return 0;
					}
			""");

		// TO-2 - reported on the negation, not on its operand: the redundant text a reader deletes is the clause.
		[Test]
		public Task ExpandWithNotKeysIsRedundant() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsExpand() && {|LINQ2DB0005:!flags.IsKeys()|})
							return 1;

						return 0;
					}
			""");

		// TO-2 control - Keys IS permitted with SQL, so this one carries information. Mirrors GroupByBuilder.cs:596.
		[Test]
		public Task SqlWithNotKeysIsMeaningful() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsSql() && !flags.IsKeys())
							return 1;

						return 0;
					}
			""");

		// TO-2, HasFlag arm - the only failable proof that the direct-HasFlag atom class is recognised rather
		// than opaque. Is*() and HasFlag are matched by different code paths and every other fixture uses the
		// first, so without this an analyzer that never unwraps Enum.HasFlag passes the whole suite.
		[Test]
		public Task ExpandWithNotHasFlagKeysIsRedundant() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsExpand() && {|LINQ2DB0005:!flags.HasFlag(ProjectFlags.Keys)|})
							return 1;

						return 0;
					}
			""");

		// TO-3 - an unconditional early return excludes the value on every path to the later test.
		[Test]
		public Task TestExcludedByUnconditionalEarlyReturn() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsTable())
							return 1;

						if ({|#0:flags.IsTable()|})
							return 2;

						return 0;
					}
			""", NeverTrue("flags.IsTable()", ExcludedEarlier));

		// TO-3 control - the load-bearing false-positive guard. The early return is nested under an unrelated
		// condition, so the join at the merge point restores the value and the later test is genuine. A dataflow
		// that ignores joins passes the positive arm above and fails here.
		[Test]
		public Task NotExcludedWhenEarlyReturnIsNested() => Verify("""
					public static int M(ProjectFlags flags, bool unrelated)
					{
						if (unrelated)
						{
							if (flags.IsTable())
								return 1;
						}

						if (flags.IsTable())
							return 2;

						return 0;
					}
			""");

		// The block after a try/catch is reached from the handler as well as from the try body, so a value the
		// try body excluded is still live there. The flow graph models no branch into a handler, so its first
		// block has no predecessor and a dataflow that seeds only the graph entry leaves it - and every merge
		// below it - over-constrained, which shows up here as a false LINQ2DB0004.
		[Test]
		public Task NotExcludedWhenEarlyReturnIsInsideTry() => Verify("""
					public static int M(ProjectFlags flags)
					{
						try
						{
							if (flags.IsTable())
								return 1;
						}
						catch (System.Exception)
						{
						}

						if (flags.IsTable())
							return 2;

						return 0;
					}
			""");

		// Discriminator for the arm above. If the analyzer declined every method containing a try, that arm would
		// pass vacuously; this one fails instead, so its green cannot be read as "no defect" on its own.
		[Test]
		public Task ImpossiblePairInsideTryIsReported() => Verify("""
					public static int M(ProjectFlags flags)
					{
						try
						{
							if (flags.IsKeys() && {|LINQ2DB0004:flags.IsExpand()|})
								return 1;
						}
						catch (System.Exception)
						{
						}

						return 0;
					}
			""");

		// Direct test of handler-block seeding: a handler left with an empty state is skipped as unreachable, so
		// an impossible pair written inside a catch is not reported at all.
		[Test]
		public Task ImpossiblePairInsideCatchIsReported() => Verify("""
					public static int M(ProjectFlags flags)
					{
						try
						{
							return 1;
						}
						catch (System.Exception)
						{
							if (flags.IsKeys() && {|LINQ2DB0004:flags.IsExpand()|})
								return 2;
						}

						return 0;
					}
			""");

		// Counterpart to the arm above: a predecessor-less block is a handler entry only sometimes - unreachable
		// code has no predecessor either, and seeding one analyses code the compiler already discarded. Nothing
		// may be reported here even though the pair is impossible.
		[Test]
		public Task ImpossiblePairInDeadCodeIsNotReported() => Verify("""
					public static int M(ProjectFlags flags)
					{
						return 0;

						if (flags.IsKeys() && flags.IsExpand())
							return 1;
					}
			""");

		// A test hoisted into a local is still a test. The rule reports "where it stands", so a value the
		// enclosing condition has already fixed has to be caught in the assignment, not only in an if.
		[Test]
		public Task HoistedTestIsReported() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsKeys())
						{
							var isExpand = {|LINQ2DB0004:flags.IsExpand()|};
							if (isExpand)
								return 1;
						}

						return 0;
					}
			""");

		// Control for the arm above - Keys is permitted with SQL, so the hoisted test is genuine and reporting
		// it would be a false positive. Without this arm the wider walk could be flagging every hoisted test.
		[Test]
		public Task HoistedTestThatIsSatisfiableIsNotReported() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsSql())
						{
							var isKeys = flags.IsKeys();
							if (isKeys)
								return 1;
						}

						return 0;
					}
			""");

		// The violation every drift fixture below embeds, so each can show what the rule does once the model it
		// depends on has changed underneath it.
		const string KnownViolation = """
					public static int M(ProjectFlags flags)
					{
						if (flags.IsKeys() && flags.IsExpand())
							return 1;

						return 0;
					}
			""";

		// TO-4a - a switch section shape the reader cannot parse. The condition rules must go silent rather than
		// answer from a half-read model: the embedded violation is deliberately NOT marked.
		[Test]
		public Task DriftedSwitchShapeSilencesTheConditionRules() => AnalyzerVerifier<ProjectFlagsAnalyzer>.VerifyAsync(
			Consumer(KnownViolation)
				.Replace("ProjectFlags GetProjectFlags()", "ProjectFlags {|LINQ2DB0006:GetProjectFlags|}()", System.StringComparison.Ordinal)
				.Replace("flags |= ProjectFlags.Table;", "flags |= ProjectFlags.Table; System.Console.WriteLine();", System.StringComparison.Ordinal));

		// TO-4b - a flag no switch arm produces, so the analyzer cannot classify it as purpose or modifier. This
		// is the arm that fails if the implementation ships without the completeness check D-1 specified: without
		// it the model reads "successfully", stays usable, and the embedded violation gets reported.
		[Test]
		public Task UnclassifiableFlagSilencesTheConditionRules() => AnalyzerVerifier<ProjectFlagsAnalyzer>.VerifyAsync(
			Consumer(KnownViolation)
				.Replace("ProjectFlags GetProjectFlags()", "ProjectFlags {|LINQ2DB0006:GetProjectFlags|}()", System.StringComparison.Ordinal)
				.Replace("MemberRoot        = 1 << 12,", "MemberRoot        = 1 << 12, ForSetProjection = 1 << 13,", System.StringComparison.Ordinal));

		// TO-4c - an unreadable *predicate*. Unlike the two arms above the domain is intact, so the rules keep
		// working and only the unreadable predicate becomes opaque: reporting the drift while still catching the
		// embedded violation is the correct outcome, and the marked violation here is what pins that difference.
		[Test]
		public Task UnreadablePredicateReportsDriftButKeepsAnalyzing() => AnalyzerVerifier<ProjectFlagsAnalyzer>.VerifyAsync(
			Consumer(KnownViolation.Replace("flags.IsExpand()", "{|LINQ2DB0004:flags.IsExpand()|}", System.StringComparison.Ordinal))
				.Replace(
					"public static bool IsSqlOrExpression",
					"public static bool {|LINQ2DB0006:IsWeird|}(this ProjectFlags flags) => flags.ToString().Length > 3;\n\t\tpublic static bool IsSqlOrExpression",
					System.StringComparison.Ordinal));

		// TO-5 - a different [Flags] enum with its own same-shaped predicates, in a method whose local is even
		// called 'flags'. Every pair here would be reported were the type ProjectFlags. Fails unless the analyzer
		// compares the receiver's type symbol instead of matching the identifier or the Is* naming shape - which
		// is exactly what InsertOrUpdateBuilder.cs and ColumnDescriptor.cs do with SqlProviderFlags.
		[Test]
		public Task AnUnrelatedFlagsEnumWithTheSamePredicateShapeIsNotMatched() => AnalyzerVerifier<ProjectFlagsAnalyzer>.VerifyAsync(Model + """
			namespace Other
			{
				[System.Flags]
				enum OtherFlags { None = 0, SQL = 1, Expand = 2, Keys = 4 }

				static class OtherFlagExtensions
				{
					public static bool IsSql(this OtherFlags flags)    => flags.HasFlag(OtherFlags.SQL);
					public static bool IsExpand(this OtherFlags flags) => flags.HasFlag(OtherFlags.Expand);
					public static bool IsKeys(this OtherFlags flags)   => flags.HasFlag(OtherFlags.Keys);
				}

				static class Subject
				{
					public static int M(OtherFlags flags)
					{
						if (flags.IsKeys() && flags.IsExpand())
							return 1;

						if (flags.IsExpand() && !flags.IsKeys())
							return 2;

						if (flags.HasFlag(OtherFlags.SQL) && flags.HasFlag(OtherFlags.Expand))
							return 3;

						return 0;
					}
				}
			}
			""");

		// TO-8 - the symmetry guard on the unchanged path. The atom recogniser is reachable from every method in
		// the assembly, including the two that *define* the model, so the model's own source must analyze clean.
		[Test]
		public Task ModelDefinitionSitesAreNotFlagged() => AnalyzerVerifier<ProjectFlagsAnalyzer>.VerifyAsync(Model);
	}
}
