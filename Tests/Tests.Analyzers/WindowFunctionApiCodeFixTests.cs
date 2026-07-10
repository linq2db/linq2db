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
