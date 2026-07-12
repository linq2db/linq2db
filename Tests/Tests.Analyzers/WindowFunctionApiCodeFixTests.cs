using System.Threading.Tasks;

using LinqToDB.Analyzers;
using LinqToDB.Analyzers.CodeFixes;

using NUnit.Framework;

using Verify = Tests.Analyzers.CodeFixVerifier<LinqToDB.Analyzers.WindowFunctionApiAnalyzer, LinqToDB.Analyzers.CodeFixes.WindowFunctionApiCodeFixProvider>;

namespace Tests.Analyzers
{
	[TestFixture]
	public sealed class WindowFunctionApiCodeFixTests
	{
		[Test]
		public Task RowNumber()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					long M(int x) => {|L2DB1001:Sql.Ext.RowNumber().Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					long M(int x) => Sql.Window.RowNumber(f => f.PartitionBy(x).OrderBy(x));
				}
				""");

		[Test]
		public Task AggregateWithThenByDesc()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Sum(x).Over().PartitionBy(x).OrderBy(x).ThenByDesc(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Sum(x, f => f.PartitionBy(x).OrderBy(x).ThenByDesc(x));
				}
				""");

		[Test]
		public Task LagWithOffsetAndDefault()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Lag(x, 1, 0).Over().OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Lag(x, 1, 0, f => f.OrderBy(x));
				}
				""");

		[Test]
		public Task FramedCount()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Count().Over().PartitionBy(x).OrderBy(x).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Count(f => f.PartitionBy(x).OrderBy(x).RangeBetween.Unbounded.And.CurrentRow);
				}
				""");

		[Test]
		public Task CountDistinct()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Count(x, Sql.AggregateModifier.Distinct).Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Count(x, f => f.Distinct().PartitionBy(x).OrderBy(x));
				}
				""");

		[Test]
		public Task PreservesLeadingComment()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					long M(int x) =>
						// keep this comment
						{|L2DB1001:Sql.Ext.RowNumber().Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					long M(int x) =>
						// keep this comment
						Sql.Window.RowNumber(f => f.PartitionBy(x).OrderBy(x));
				}
				""");

		[Test]
		public Task PreservesArgumentComment()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Sum(x /* keep */).Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Sum(x /* keep */, f => f.PartitionBy(x).OrderBy(x));
				}
				""");

		[Test]
		public Task Keep()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Min(x).KeepFirst().OrderBy(x).Over().PartitionBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Min(x, f => f.KeepFirst().OrderBy(x).PartitionBy(x));
				}
				""");

		[Test]
		public Task Median()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => {|L2DB1001:Sql.Ext.Median(x).Over().PartitionBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => Sql.Window.Median(x, f => f.PartitionBy(x));
				}
				""");

		[Test]
		public Task RatioToReport()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => {|L2DB1001:Sql.Ext.RatioToReport<double>(x).Over().PartitionBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => Sql.Window.RatioToReport(x, f => f.PartitionBy(x));
				}
				""");

		[Test]
		public Task PercentileContWindowed()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => {|L2DB1001:Sql.Ext.PercentileCont<double>(0.5).WithinGroup.OrderBy(x).Over().PartitionBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => Sql.Window.PercentileCont(0.5, f => f.OrderBy(x).PartitionBy(x));
				}
				""");

		[Test]
		public Task PercentileDiscWindowed()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => {|L2DB1001:Sql.Ext.PercentileDisc<double>(0.5).WithinGroup.OrderByDesc(x).Over().PartitionBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => Sql.Window.PercentileDisc(0.5, f => f.OrderByDesc(x).PartitionBy(x));
				}
				""");

		[Test]
		public Task DoesNotReportOrFixGroupFormPercentile()
		{
			// WITHIN GROUP without OVER is an ordered-set aggregate, not a window function, and has no Sql.Window
			// equivalent — so (like a plain aggregate) it is neither reported nor fixed.
			const string source = """
				using LinqToDB;

				class C
				{
					object? M(int x) => Sql.Ext.PercentileCont<double>(0.5).WithinGroup.OrderBy(x).ToValue();
				}
				""";

			return Verify.VerifyAsync(source, source);
		}

		[Test]
		public Task DoesNotOfferFixWhenNullableDoubleWouldNotFitSlot()
		{
			// Sql.Window.Median returns double?, which an int? slot won't accept — the fix must not be offered
			// (the legacy chain returns int here). The diagnostic still reports.
			const string source = """
				using LinqToDB;

				class C
				{
					int? M(int x) => {|L2DB1001:Sql.Ext.Median(x).Over().PartitionBy(x).ToValue()|};
				}
				""";

			return Verify.VerifyAsync(source, source);
		}

		[Test]
		public Task Statistical()
			// double?-returning statistical family into a double?-accepting (object?) slot: reported and fixed.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => {|L2DB1001:Sql.Ext.StdDev<double>(x).Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => Sql.Window.StdDev(x, f => f.PartitionBy(x).OrderBy(x));
				}
				""");

		[Test]
		public Task StatisticalFramed()
			// FrameableFunctions ∩ NullableDoubleReturning: the frame and the double?-fit path together.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => {|L2DB1001:Sql.Ext.StdDev<double>(x).Over().PartitionBy(x).OrderBy(x).Rows.Between.UnboundedPreceding.And.CurrentRow.ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					object? M(int x) => Sql.Window.StdDev(x, f => f.PartitionBy(x).OrderBy(x).RowsBetween.Unbounded.And.CurrentRow);
				}
				""");

		[Test]
		public Task FirstValueWithIgnoreNulls()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.FirstValue(x, Sql.Nulls.Ignore).Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.FirstValue(x, f => f.IgnoreNulls().PartitionBy(x).OrderBy(x));
				}
				""");

		[Test]
		public Task NthValueWithFromLastAndIgnoreNulls()
			// Covers the value-function family plus the From (FromLast) and Nulls (IgnoreNulls) modifier-to-builder paths.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.NthValue(x, 2L, Sql.From.Last, Sql.Nulls.Ignore).Over().OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.NthValue(x, 2L, f => f.FromLast().IgnoreNulls().OrderBy(x));
				}
				""");

		[Test]
		public Task LeadWithOffset()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Lead(x, 1).Over().OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Lead(x, 1, f => f.OrderBy(x));
				}
				""");

		[Test]
		public Task DoesNotReportOrFixPlainAggregateWithoutOver()
		{
			// No .Over(): a plain aggregate is not a window function, so it is neither reported nor fixed.
			const string source = """
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Ext.Sum(x).ToValue();
				}
				""";

			return Verify.VerifyAsync(source, source);
		}

		[Test]
		public Task DoesNotOfferFixForUsingStaticSqlExtRoot()
		{
			// `using static LinqToDB.Sql;` makes the root a bare `Ext.<Fn>` (identifier receiver, not `Sql.Ext`).
			// There's no Sql qualifier to reuse, so the fix bails gracefully — reported, not fixed, no crash.
			const string source = """
				using LinqToDB;
				using static LinqToDB.Sql;

				class C
				{
					long M(int x) => {|L2DB1001:Ext.RowNumber().Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""";

			return Verify.VerifyAsync(source, source);
		}

		[Test]
		public Task NthValueWithFromFirstAndRespectNulls()
			// Covers the From.First->FromFirst and Nulls.Respect->RespectNulls modifier branches (siblings of the
			// From.Last/Nulls.Ignore case), which map to distinct builder calls.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.NthValue(x, 2L, Sql.From.First, Sql.Nulls.Respect).Over().OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.NthValue(x, 2L, f => f.FromFirst().RespectNulls().OrderBy(x));
				}
				""");

		[Test]
		public Task KeepLast()
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Max(x).KeepLast().OrderBy(x).Over().PartitionBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Max(x, f => f.KeepLast().OrderBy(x).PartitionBy(x));
				}
				""");

		[Test]
		public Task SingleBoundaryFrameNormalizesToCurrentRow()
			// A single-boundary legacy frame (.Rows.UnboundedPreceding, no .Between...And) normalizes to the explicit
			// two-boundary Sql.Window form (frameEnd defaults to CurrentRow).
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Sum(x).Over().OrderBy(x).Rows.UnboundedPreceding.ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Sum(x, f => f.OrderBy(x).RowsBetween.Unbounded.And.CurrentRow);
				}
				""");

		[Test]
		public Task OrderByWithNullsPosition()
			// The ordering argument list is spliced wholesale, carrying a NullsPosition argument through unchanged.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					long M(int x) => {|L2DB1001:Sql.Ext.RowNumber().Over().OrderBy(x, Sql.NullsPosition.First).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					long M(int x) => Sql.Window.RowNumber(f => f.OrderBy(x, Sql.NullsPosition.First));
				}
				""");

		[Test]
		public Task FramedWithValueBoundaries()
			// Value-bearing frame boundaries splice the user's numeric args (ValuePreceding(3)/ValueFollowing(1))
			// through placeholders — the trivia-preserving path distinct from the arg-less Unbounded/CurrentRow form.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Sum(x).Over().OrderBy(x).Rows.Between.ValuePreceding(3).And.ValueFollowing(1).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Sum(x, f => f.OrderBy(x).RowsBetween.ValuePreceding(3).And.ValueFollowing(1));
				}
				""");

		[Test]
		public Task NTileIntoWideEnoughSlot()
			// Sql.Window.NTile returns long; a long slot accepts it, so the fix is offered.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					long M(int x) => {|L2DB1001:Sql.Ext.NTile(4).Over().OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					long M(int x) => Sql.Window.NTile(4, f => f.OrderBy(x));
				}
				""");

		[Test]
		public Task DoesNotOfferFixWhenReturnTypeWouldNotFitSlot()
		{
			// Legacy NTile<T> infers T=int here (returns int), but Sql.Window.NTile returns long — rewriting into an
			// int slot would be a narrowing assignment (CS0266), so the fix is withheld; the diagnostic still reports.
			const string source = """
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.NTile(4).Over().OrderBy(x).ToValue()|};
				}
				""";

			return Verify.VerifyAsync(source, source);
		}

		[Test]
		public Task DoesNotOfferFixForDistinctKeep()
		{
			// KEEP has no Sql.Window builder state for DISTINCT (Distinct and Keep are mutually exclusive there), so a
			// DISTINCT+KEEP chain has no mechanical equivalent — reported, not fixed. Dropping DISTINCT would silently
			// change the aggregate's result.
			const string source = """
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Sum(x, Sql.AggregateModifier.Distinct).KeepFirst().OrderBy(x).Over().PartitionBy(x).ToValue()|};
				}
				""";

			return Verify.VerifyAsync(source, source);
		}

		[Test]
		public Task DoesNotOfferFixForWindowedListAgg()
		{
			// ListAgg is a WITHIN GROUP ordered-set aggregate with no Sql.Window equivalent even in its windowed
			// (OVER) form — an unrecognized root, reported but not fixed.
			const string source = """
				using LinqToDB;

				class C
				{
					string M(int x) => {|L2DB1001:Sql.Ext.ListAgg(x).WithinGroup.OrderBy(x).Over().PartitionBy(x).ToValue()|};
				}
				""";

			return Verify.VerifyAsync(source, source);
		}

		[Test]
		public Task DoesNotOfferFixForFilterChain()
		{
			// A FILTER (WHERE ...) clause in the chain has no mechanical Sql.Window equivalent here — reported, not
			// fixed.
			const string source = """
				using LinqToDB;

				class C
				{
					long M(int x) => {|L2DB1001:Sql.Ext.RowNumber().Filter(x > 0).Over().OrderBy(x).ToValue()|};
				}
				""";

			return Verify.VerifyAsync(source, source);
		}

		[Test]
		public Task ConvertsInsideExpressionTree()
			=> Verify.VerifyAsync(
				"""
				using System.Linq;
				using LinqToDB;

				class C
				{
					void M(IQueryable<int> q, int x)
					{
						var r = q.Select(_ => {|L2DB1001:Sql.Ext.RowNumber().Over().OrderBy(x).ToValue()|});
					}
				}
				""",
				"""
				using System.Linq;
				using LinqToDB;

				class C
				{
					void M(IQueryable<int> q, int x)
					{
						var r = q.Select(_ => Sql.Window.RowNumber(f => f.OrderBy(x)));
					}
				}
				""");

		[Test]
		public Task LagReordersNamedValueArguments()
			// Named arguments can be written out of declaration order. The value args must be emitted in the order
			// Sql.Window.Lag expects (expr, offset, default) — not source order — or offset/default would silently
			// swap into a compiling-but-wrong call.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Lag(x, @default: 0, offset: 1).Over().OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Lag(x, 1, 0, f => f.OrderBy(x));
				}
				""");

		[Test]
		public Task NthValueReordersNamedValueArguments()
			// NthValue interleaves its two value slots (expr, n) with two modifier args (from, nulls). With every
			// argument named and written out of declaration order, the value args must still be emitted as (expr, n)
			// — sorted by parameter ordinal, not source order — and the modifiers routed to their builder steps.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.NthValue(n: 2L, expr: x, nulls: Sql.Nulls.Ignore, from: Sql.From.Last).Over().OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.NthValue(x, 2L, f => f.FromLast().IgnoreNulls().OrderBy(x));
				}
				""");

		[Test]
		public Task AggregateModifierAsReorderedNamedArgument()
			// The modifier arg is classified by its parameter, not its position: a named modifier written before the
			// value arg is still recognized as the aggregate modifier (and the value arg is emitted positionally).
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Count(modifier: Sql.AggregateModifier.Distinct, expr: x).Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x) => Sql.Window.Count(x, f => f.Distinct().PartitionBy(x).OrderBy(x));
				}
				""");

		[Test]
		public Task ConvertsAggregateModifierReferencedThroughConstAlias()
			// A DISTINCT modifier referenced through a const alias (a member access whose name is not the canonical
			// enum-member spelling) must be recognized by its constant value and preserved as .Distinct() — matching
			// the member name alone would leave `K.D` unrecognized and silently drop DISTINCT from the rewrite.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				static class K
				{
					public const Sql.AggregateModifier D = Sql.AggregateModifier.Distinct;
				}

				class C
				{
					int M(int x) => {|L2DB1001:Sql.Ext.Count(x, K.D).Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				static class K
				{
					public const Sql.AggregateModifier D = Sql.AggregateModifier.Distinct;
				}

				class C
				{
					int M(int x) => Sql.Window.Count(x, f => f.Distinct().PartitionBy(x).OrderBy(x));
				}
				""");

		[Test]
		public Task DoesNotFixWhenAggregateModifierIsNotCompileTimeConstant()
			// A modifier supplied through a non-constant expression (here a method parameter) cannot be resolved to a
			// specific enum member at compile time. Rather than guess, the fix is withheld — the diagnostic reports
			// but the chain is left unchanged.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					int M(int x, Sql.AggregateModifier m) => {|L2DB1001:Sql.Ext.Count(x, m).Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					int M(int x, Sql.AggregateModifier m) => {|L2DB1001:Sql.Ext.Count(x, m).Over().PartitionBy(x).OrderBy(x).ToValue()|};
				}
				""");

		[Test]
		public Task PreservesCommentOnChainScaffolding()
			// A comment living on an intermediate chain token (here inside .Over()) is neither leading trivia of the
			// statement nor part of a spliced value arg, so it travels the salvage path: collected off the old chain
			// and re-attached as trailing trivia so no comment is dropped.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					long M(int x) => {|L2DB1001:Sql.Ext.RowNumber().Over(/* keep */).OrderBy(x).ToValue()|};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					long M(int x) => Sql.Window.RowNumber(f => f.OrderBy(x)) /* keep */;
				}
				""");

		[Test]
		public Task PreservesSingleLineCommentOnChainScaffolding()
			// A single-line comment on the chain scaffolding travels the same salvage path as the block-comment case,
			// but runs to end of line — the salvaged comment must be line-terminated so the statement's ';' is not
			// swallowed into it (which would leave the rewritten code without a terminator and stop compiling).
			=> Verify.VerifyAsync(
"""
using LinqToDB;

class C
{
	long M(int x) => {|L2DB1001:Sql.Ext.RowNumber().Over() // keep
		.OrderBy(x).ToValue()|};
}
""",
"""
using LinqToDB;

class C
{
	long M(int x) => Sql.Window.RowNumber(f => f.OrderBy(x)) // keep
;
}
""");

		[Test]
		public Task DoesNotOfferFixForNTileInInferredContextThatWouldWiden()
		{
			// A `var` slot has no explicit target to narrow into, but its inferred type follows the expression: the
			// legacy NTile ToValue() is int while Sql.Window.NTile returns long, so the fix would silently widen `r`
			// from int to long. The declaration alone compiles, but a downstream int consumer would break — so the
			// fix is withheld (the diagnostic still reports).
			const string source = """
				using LinqToDB;

				class C
				{
					void M(int x)
					{
						var r = {|L2DB1001:Sql.Ext.NTile(4).Over().OrderBy(x).ToValue()|};
					}
				}
				""";

			return Verify.VerifyAsync(source, source);
		}

		[Test]
		public Task DoesNotOfferFixForNTileInAnonymousObjectMemberThatWouldWiden()
		{
			// An anonymous-object member is type-inferred like a `var` slot: legacy NTile ToValue() is int while
			// Sql.Window.NTile returns long, so the fix would silently widen the member's inferred type. It is
			// withheld (the diagnostic still reports).
			const string source = """
				using LinqToDB;

				class C
				{
					void M(int x)
					{
						var r = new { N = {|L2DB1001:Sql.Ext.NTile(4).Over().OrderBy(x).ToValue()|} };
					}
				}
				""";

			return Verify.VerifyAsync(source, source);
		}

		[Test]
		public Task OffersFixInVarSlotWhenReturnTypeIsIdentical()
			// RowNumber returns long in both the legacy ToValue() slot and Sql.Window, so a `var` slot's inferred
			// type is unchanged by the rewrite — the identical-type inferred-context path offers the fix.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					void M(int x)
					{
						var r = {|L2DB1001:Sql.Ext.RowNumber().Over().OrderBy(x).ToValue()|};
					}
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					void M(int x)
					{
						var r = Sql.Window.RowNumber(f => f.OrderBy(x));
					}
				}
				""");

		[Test]
		public Task FixAllConvertsEveryClusteredChain()
			// Several convertible chains physically close together (one initializer). The testing SDK verifies the
			// Fix-All path in addition to single fixes; the custom DocumentBasedFixAllProvider must convert them ALL
			// in one pass. (WellKnownFixAllProviders.BatchFixer dropped the trailing ones here — the edits after the
			// first go stale against the original tree.)
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					object?[] N(int x) => new object?[]
					{
						{|L2DB1001:Sql.Ext.RowNumber().Over().OrderBy(x).ToValue()|},
						{|L2DB1001:Sql.Ext.Rank().Over().OrderBy(x).ToValue()|},
						{|L2DB1001:Sql.Ext.DenseRank().Over().OrderBy(x).ToValue()|},
					};
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					object?[] N(int x) => new object?[]
					{
						Sql.Window.RowNumber(f => f.OrderBy(x)),
						Sql.Window.Rank(f => f.OrderBy(x)),
						Sql.Window.DenseRank(f => f.OrderBy(x)),
					};
				}
				""");

		[Test]
		public Task AppliesReturnTypeMismatchFixWhenOptionEnabled()
			// With linq2db.L2DB1001.apply_fix_on_return_type_mismatch = true the return-type-fit gate is bypassed, so
			// the NTile int->long widening in a `var` slot (withheld by default — see DoesNotOfferFixFor...) is applied.
			// The user opts into resolving any resulting type change themselves; here the rewritten code still compiles.
			=> Verify.VerifyAsync(
				"""
				using LinqToDB;

				class C
				{
					void M(int x)
					{
						var r = {|L2DB1001:Sql.Ext.NTile(4).Over().OrderBy(x).ToValue()|};
					}
				}
				""",
				"""
				using LinqToDB;

				class C
				{
					void M(int x)
					{
						var r = Sql.Window.NTile(4, f => f.OrderBy(x));
					}
				}
				""",
				"""
				root = true

				[*.cs]
				linq2db.L2DB1001.apply_fix_on_return_type_mismatch = true
				""");
	}
}
