using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5984Tests : TestBase
	{
		[Table]
		sealed class JoinProbeEntity
		{
			[PrimaryKey] public int     Id     { get; set; }
			[Column]     public int     Value1 { get; set; }
			[Column]     public string? Name   { get; set; }
		}

		static readonly JoinProbeEntity[] Data = [new JoinProbeEntity { Id = 1, Value1 = 10, Name = "a" }];

		// SQLite binds `||` tighter than `+`, so `[Value1] + 1 || ',' || [Value1]` used to be evaluated as `[Value1] + (1 || ',' || [Value1])`
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5984")]
		public void StringJoinArithmeticFirst([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Data);

			var result = table.Select(e => string.Join(",", new object[] { e.Value1 + 1, e.Value1 })).ToArray();

			result.ShouldBe(["11,10"]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5984")]
		public void StringJoinArithmeticMiddle([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Data);

			var result = table.Select(e => string.Join(",", new object[] { e.Id, e.Value1 - 3, e.Id })).ToArray();

			result.ShouldBe(["1,7,1"]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5984")]
		public void StringJoinArithmeticLast([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Data);

			var result = table.Select(e => string.Join(",", new object[] { e.Value1, e.Value1 * 2 + 1 })).ToArray();

			result.ShouldBe(["10,21"]);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5984")]
		public void StringJoinPlainItems([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Data);

			var result = table.Select(e => string.Join(",", new object[] { e.Value1, e.Id, e.Name!.ToUpper(), e.Name + "-" + e.Id, -e.Value1 })).ToArray();

			result.ShouldBe(["10,1,A,a-1,-10"]);

			// columns, literals, functions, CAST, unary minus and a nested concat must stay without parentheses
			LastQuery!.ShouldContain("[e].[Value1] || ',' || [e].[Id] || ',' || Coalesce(Upper([e].[Name]), '') || ',' || Coalesce([e].[Name], '') || '-' || CAST([e].[Id] AS NVarChar(11)) || ',' || -[e].[Value1]");
		}
	}
}
