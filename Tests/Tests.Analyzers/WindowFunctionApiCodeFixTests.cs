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
					long M(int x) => {|LINQ2DB1001:Sql.Ext.RowNumber().Over().PartitionBy(x).OrderBy(x).ToValue()|};
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
					int M(int x) => {|LINQ2DB1001:Sql.Ext.Sum(x).Over().PartitionBy(x).OrderBy(x).ThenByDesc(x).ToValue()|};
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
					int M(int x) => {|LINQ2DB1001:Sql.Ext.Lag(x, 1, 0).Over().OrderBy(x).ToValue()|};
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
					int M(int x) => {|LINQ2DB1001:Sql.Ext.Count().Over().PartitionBy(x).OrderBy(x).Range.Between.UnboundedPreceding.And.CurrentRow.ToValue()|};
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
					int M(int x) => {|LINQ2DB1001:Sql.Ext.Count(x, Sql.AggregateModifier.Distinct).Over().PartitionBy(x).OrderBy(x).ToValue()|};
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
						{|LINQ2DB1001:Sql.Ext.RowNumber().Over().PartitionBy(x).OrderBy(x).ToValue()|};
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
					int M(int x) => {|LINQ2DB1001:Sql.Ext.Sum(x /* keep */).Over().PartitionBy(x).OrderBy(x).ToValue()|};
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
					int M(int x) => {|LINQ2DB1001:Sql.Ext.Min(x).KeepFirst().OrderBy(x).Over().PartitionBy(x).ToValue()|};
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
					object? M(int x) => {|LINQ2DB1001:Sql.Ext.Median(x).Over().PartitionBy(x).ToValue()|};
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
					object? M(int x) => {|LINQ2DB1001:Sql.Ext.RatioToReport<double>(x).Over().PartitionBy(x).ToValue()|};
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
					object? M(int x) => {|LINQ2DB1001:Sql.Ext.PercentileCont<double>(0.5).WithinGroup.OrderBy(x).Over().PartitionBy(x).ToValue()|};
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
					object? M(int x) => {|LINQ2DB1001:Sql.Ext.PercentileDisc<double>(0.5).WithinGroup.OrderByDesc(x).Over().PartitionBy(x).ToValue()|};
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
		public Task DoesNotOfferFixForGroupFormPercentile()
		{
			// WITHIN GROUP without OVER (the ordered-set group form) has no Sql.Window equivalent: reported, not fixed.
			const string source = """
				using LinqToDB;

				class C
				{
					object? M(int x) => {|LINQ2DB1001:Sql.Ext.PercentileCont<double>(0.5).WithinGroup.OrderBy(x).ToValue()|};
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
						var r = q.Select(_ => {|LINQ2DB1001:Sql.Ext.RowNumber().Over().OrderBy(x).ToValue()|});
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
	}
}
