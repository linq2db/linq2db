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
					ForSetProjection  = 1 << 13,
				}

				enum BuildPurpose { None, Sql, Table, Expression, Expand, Root, Extract }

				[System.Flags]
				enum BuildFlags { None = 0, ForKeys = 1, ForMemberRoot = 2, ForSetProjection = 4 }

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
					public static bool IsForSetProjection(this ProjectFlags flags)  => flags.HasFlag(ProjectFlags.ForSetProjection);
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

						// Two free modifiers, as production has. One would be a smaller model that still passes
						// every fixture: ReadDomain builds their powerset, and a single bit cannot tell a powerset
						// from an implementation that merely appends each bare bit - {0, a} either way.
						if (_buildFlags.HasFlag(BuildFlags.ForSetProjection))
							flags |= ProjectFlags.ForSetProjection;

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

		// Most fixtures pin only the id and the span, via {|LINQ2DB0004:...|} markup. Those built on the helper
		// below also pin the message's reason argument, because that argument is the rule's affordance - it tells
		// a reader whether the clause is a modelling mistake or dead code behind an earlier return - and nothing
		// else asserts it. Deliberately not a count: every fixture added since has moved one.
		// Both reasons need coverage: a && or || gives each operand its own branch value, so every condition in
		// Source/LinqToDB reports ExcludedEarlier and only a non-short-circuit & reaches UniformOverDomain.
		static DiagnosticResult NeverTrue(string clause, string reason) =>
			new DiagnosticResult("LINQ2DB0004", DiagnosticSeverity.Warning).WithLocation(0).WithArguments(clause, reason);

		const string ExcludedEarlier   = "an earlier test on the same path has already excluded every value that would answer differently";
		const string UniformOverDomain = "no ProjectFlags value GetProjectFlags can produce gives a different answer";

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

		// TO-1, non-short-circuit form - the same impossible pair written with a single &, which evaluates both
		// operands in one branch value. The report therefore lands on the conjunction rather than on an operand,
		// and the conjunction is false for every value GetProjectFlags can produce, so this is the one shape that
		// reaches Explain's UniformOverDomain reason - the modelling-mistake half of the affordance. It is also
		// the suite's coverage of Evaluate's BinaryOperatorKind.And arm; the Or arm is covered by the pair below.
		[Test]
		public Task KeysWithExpandInOneBranchValueIsNeverTrue() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if ({|#0:flags.IsKeys() & flags.IsExpand()|})
							return 1;

						return 0;
					}
			""", NeverTrue("flags.IsKeys() & flags.IsExpand()", UniformOverDomain));

		// Control for the arm above - a pair the model permits, still evaluated in one branch value. Without it
		// the & handling could be reporting every non-short-circuit conjunction and still pass.
		[Test]
		public Task SqlWithKeysInOneBranchValueIsFine() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsSql() & flags.IsKeys())
							return 1;

						return 0;
					}
			""");

		// Evaluate's Or arm. Reaching it needs a non-short-circuit | - Roslyn lowers || into separate branch
		// blocks, so each operand gets its own branch value and the disjunction node never reaches Evaluate. The
		// short-circuit || above excludes both purposes on the path below it, which is what makes the | constant.
		// No production site has this shape; without this pair the arm is exercised by nothing.
		[Test]
		public Task ExcludedDisjunctionInOneBranchValueIsNeverTrue() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsSql() || flags.IsExpression())
							return 1;

						if ({|#0:flags.IsSql() | flags.IsExpression()|})
							return 2;

						return 0;
					}
			""", NeverTrue("flags.IsSql() | flags.IsExpression()", ExcludedEarlier));

		// Control for the arm above - the same | with nothing excluding it, so both operands stay reachable.
		// Without it the arm above would pass an implementation that reports every non-short-circuit disjunction.
		[Test]
		public Task SatisfiableDisjunctionInOneBranchValueIsFine() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsSql() | flags.IsExpression())
							return 1;

						return 0;
					}
			""");

		// The failable proof that ReadDomain builds a real powerset of the free modifiers rather than just adding
		// each bare bit. Both are free, so a value carrying both exists and this pair is satisfiable; an
		// implementation whose Subsets never produces MemberRoot|ForSetProjection would report a false
		// LINQ2DB0004 here. Needs two free modifiers in the stub to discriminate - with one, {0, a} comes out of
		// either implementation.
		[Test]
		public Task TwoFreeModifiersCanBeSetTogether() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsMemberRoot() && flags.IsForSetProjection())
							return 1;

						return 0;
					}
			""");

		// P12's one carried residual risk: whether Roslyn's CFG lowers !(a && b) the way D-3 assumes. It does not
		// hand the negation over whole - the && is De Morgan'd into two conditional blocks, so no !(a && b) node
		// ever reaches a branch value and ClimbNegations, which climbs only parens and !, stops at the &&. The
		// report therefore lands on the inner atom rather than on the negation. That is why LINQ2DB0004's message
		// claims nothing about the guarded code and only names the constant: here the guard is always taken, so
		// "the code it guards is unreachable" would be false. Pinning it makes a future Roslyn lowering change a
		// failing fixture instead of a silently moved diagnostic.
		[Test]
		public Task NegatedImpossibleConjunctionIsReportedOnTheInnerAtom() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (!(flags.IsExpand() && {|#0:flags.IsKeys()|}))
							return 1;

						return 0;
					}
			""", NeverTrue("flags.IsKeys()", ExcludedEarlier));

		// Control - the same negated shape over a pair the model permits. Without it the arm above would pass an
		// implementation that reports every atom inside a negated conjunction.
		[Test]
		public Task NegatedSatisfiableConjunctionIsNotReported() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (!(flags.IsSql() && flags.IsKeys()))
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

		// The AnyOf atom class. IsSqlOrExpression is the one predicate written as a bitmask rather than a HasFlag,
		// so it is the only source of an Atom with AllOf false, and Holds' two branches are as separate as Is*()
		// and HasFlag are above. This arm is the failable one: SQL and Expression are two of the ten purposes, so
		// under (value & Mask) != 0 the test is true for some reachable values and false for others, and nothing
		// is reported. Read the same mask as (value & Mask) == Mask and it is false for all 52 - no value carries
		// two purpose bits - a false LINQ2DB0004 here and at all 18 call sites in Source/LinqToDB.
		[Test]
		public Task SqlOrExpressionIsSatisfiable() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsSqlOrExpression())
							return 1;

						return 0;
					}
			""");

		// Companion to the arm above: proves the bitmask predicate is read as an atom at all rather than left
		// opaque. Under Table both bits are clear, so the clause is constant either way and the diagnostic itself
		// cannot discriminate - the pinned reason does. Only the AnyOf reading leaves the clause satisfiable
		// elsewhere in the domain, so AllOf moves Explain from ExcludedEarlier to UniformOverDomain. Measured.
		[Test]
		public Task SqlOrExpressionUnderTableIsNeverTrue() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsTable() && {|#0:flags.IsSqlOrExpression()|})
							return 1;

						return 0;
					}
			""", NeverTrue("flags.IsSqlOrExpression()", ExcludedEarlier));

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

		// Direct test of handler-block seeding. The flow graph models no branch into a handler, so a catch
		// region's first block has no predecessor; an implementation that seeds only the graph entry would leave
		// it empty, skip it as unreachable, and report nothing here - which is what this arm fails on.
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

		// The dataflow assumes a tracked value is fixed once produced, so CollectTrackedSymbols drops any candidate
		// the body writes to. The pair below would otherwise be reported exactly as TO-1's is; the reassignment is
		// what makes the analysis unsound, and dropping the symbol is the bail-out. Nothing else in the suite
		// writes to flags, so without this arm removing that guard leaves every fixture green.
		[Test]
		public Task ReassignedFlagsIsNotTracked() => Verify("""
					public static int M(ProjectFlags flags)
					{
						if (flags.IsKeys() && flags.IsExpand())
							return 1;

						flags |= ProjectFlags.Keys;

						return (int)flags;
					}
			""");

		// A lambda body is its own control-flow graph, reached by descending the enclosing one. Returning the
		// lambda puts it in the block's BranchValue rather than in Operations - the same split the reporting loop
		// covers deliberately - so a descent that walks only Operations never hands this body over and the pair
		// inside it goes unreported. The captured parameter is the same symbol, and the nested graph is seeded
		// all-possible, so the pair is judged exactly as it would be inline.
		[Test]
		public Task ImpossiblePairInsideReturnedLambdaIsReported() => Verify("""
					public static System.Func<bool> M(ProjectFlags flags)
					{
						return () => flags.IsKeys() && {|#0:flags.IsExpand()|};
					}
			""", NeverTrue("flags.IsExpand()", ExcludedEarlier));

		// Control - the same returned-lambda shape over a pair the model permits. Without it the arm above would
		// pass an implementation that reports every flag test it finds inside a lambda.
		[Test]
		public Task SatisfiablePairInsideReturnedLambdaIsNotReported() => Verify("""
					public static System.Func<bool> M(ProjectFlags flags)
					{
						return () => flags.IsSql() && flags.IsKeys();
					}
			""");

		// The sibling of the lambda pair above: a local function is its own graph too, reached through
		// graph.LocalFunctions rather than by descending operations. DefaultIfEmptyBuilder's ProjectWithDefaultValue
		// is a real local function already taking a ProjectFlags parameter, so a flag test inside one is a single
		// edit away - and until this pair existed, deleting the LocalFunctions descent left the suite green.
		[Test]
		public Task ImpossiblePairInsideLocalFunctionIsReported() => Verify("""
					public static int M(ProjectFlags flags)
					{
						return Inner();

						int Inner()
						{
							if (flags.IsKeys() && {|#0:flags.IsExpand()|})
								return 1;

							return 0;
						}
					}
			""", NeverTrue("flags.IsExpand()", ExcludedEarlier));

		// Control - the same local-function shape over a pair the model permits. Without it the arm above would
		// pass an implementation that reports every flag test it finds inside a local function.
		[Test]
		public Task SatisfiablePairInsideLocalFunctionIsNotReported() => Verify("""
					public static int M(ProjectFlags flags)
					{
						return Inner();

						int Inner()
						{
							if (flags.IsSql() && flags.IsKeys())
								return 1;

							return 0;
						}
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
				.Replace("MemberRoot        = 1 << 12,", "MemberRoot        = 1 << 12, Unclassified = 1 << 20,", System.StringComparison.Ordinal));

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

		// TO-4d - a non-None seed. SQL is a classified bit, so the completeness check cannot see this: the domain
		// simply loses the bit on every other purpose, and flags.IsSql() would then be reported never-true where
		// it is in fact always true. Reading the seed rather than accepting any local declaration is what turns
		// that into drift.
		[Test]
		public Task DriftedSeedSilencesTheConditionRules() => AnalyzerVerifier<ProjectFlagsAnalyzer>.VerifyAsync(
			Consumer(KnownViolation)
				.Replace("ProjectFlags GetProjectFlags()", "ProjectFlags {|LINQ2DB0006:GetProjectFlags|}()", System.StringComparison.Ordinal)
				.Replace("var flags = ProjectFlags.None;", "var flags = ProjectFlags.SQL;", System.StringComparison.Ordinal));

		// TO-4e - a composed return. The accumulator no longer carries the whole answer, so every derived value is
		// missing the composed bit.
		[Test]
		public Task DriftedReturnSilencesTheConditionRules() => AnalyzerVerifier<ProjectFlagsAnalyzer>.VerifyAsync(
			Consumer(KnownViolation)
				.Replace("ProjectFlags GetProjectFlags()", "ProjectFlags {|LINQ2DB0006:GetProjectFlags|}()", System.StringComparison.Ordinal)
				.Replace("return flags;", "return flags | ProjectFlags.MemberRoot;", System.StringComparison.Ordinal));

		// TO-4f - a |= into something other than the accumulator. This is the dangerous direction: read as a free
		// modifier it would *widen* the domain, permitting Keys with every purpose and silently retiring the
		// #5727 shape the rule exists for. Requires the reader to check the assignment's target, not just its mask.
		[Test]
		public Task DriftedAccumulatorTargetSilencesTheConditionRules() => AnalyzerVerifier<ProjectFlagsAnalyzer>.VerifyAsync(
			Consumer(KnownViolation)
				.Replace("ProjectFlags GetProjectFlags()", "ProjectFlags {|LINQ2DB0006:GetProjectFlags|}()", System.StringComparison.Ordinal)
				.Replace("BuildFlags   _buildFlags;", "BuildFlags   _buildFlags;\n\t\t\tProjectFlags _other;", System.StringComparison.Ordinal)
				.Replace("flags |= ProjectFlags.MemberRoot;", "_other |= ProjectFlags.MemberRoot;", System.StringComparison.Ordinal));

		// TO-4g - a purpose arm that adds nothing. It has to be a *new* arm rather than an emptied existing one:
		// emptying one orphans its ProjectFlags member and TO-4b's completeness check fires instead, so the arm
		// would go LINQ2DB0006 either way and prove nothing. BuildPurpose.None orphans no member, so the model
		// still reads as usable and the section produces ProjectFlags.None - a value the domain loses unless the
		// reader distinguishes a section that breaks from the default that throws.
		[Test]
		public Task DriftedEmptyPurposeArmSilencesTheConditionRules() => AnalyzerVerifier<ProjectFlagsAnalyzer>.VerifyAsync(
			Consumer(KnownViolation)
				.Replace("ProjectFlags GetProjectFlags()", "ProjectFlags {|LINQ2DB0006:GetProjectFlags|}()", System.StringComparison.Ordinal)
				.Replace("case BuildPurpose.Sql:", "case BuildPurpose.None:\n\t\t\t\t\t\tbreak;\n\t\t\t\t\tcase BuildPurpose.Sql:", System.StringComparison.Ordinal));

		// The counterpart to the drift arms above: a shape the reader must *tolerate*. Bracing a case body is a
		// cosmetic edit, and D-1's failure mode warns that a reader matching form rather than structure taxes those
		// too. It asserts the violation is still reported rather than merely that no drift fired - which is what
		// shows the model was read through the braces instead of coming back empty.
		[Test]
		public Task BracedSwitchSectionIsRead() => AnalyzerVerifier<ProjectFlagsAnalyzer>.VerifyAsync(
			Consumer(KnownViolation.Replace("flags.IsExpand()", "{|LINQ2DB0004:flags.IsExpand()|}", System.StringComparison.Ordinal))
				.Replace("case BuildPurpose.Table:", "case BuildPurpose.Table:\n\t\t\t\t\t{", System.StringComparison.Ordinal)
				.Replace("case BuildPurpose.Expand:", "\t\t\t\t\t}\n\t\t\t\t\tcase BuildPurpose.Expand:", System.StringComparison.Ordinal));

		// TO-5 - a different [Flags] enum with its own same-shaped predicates, in a method whose parameter is even
		// called 'flags'. Every pair here would be reported were the type ProjectFlags. Fails unless the analyzer
		// compares the receiver's type symbol instead of matching the identifier or the Is* naming shape.
		// Constructed rather than taken from the tree: the real 'flags'-named values of a foreign flags enum are
		// read through a property (InsertOrUpdateBuilder.cs:145) or a raw bitmask over SkipModification
		// (ColumnDescriptor.cs:397), and neither is an invocation the recogniser could be fooled by.
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
