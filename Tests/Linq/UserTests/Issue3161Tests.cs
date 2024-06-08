using System;
using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3161Tests : TestBase
	{
		[Table("TABLE1")]
		public partial class Table1
		{
			[Column("ID1"), PrimaryKey, NotNull] public int Id1 { get; set; }
			[Column("NAME1"), Nullable] public string? Name1 { get; set; }
		}

		[Table("TABLE2")]
		public partial class Table2
		{
			[Column("ID2"), PrimaryKey, NotNull] public int Id2 { get; set; }
			[Column("PARENTID2"), NotNull] public int ParentId2 { get; set; }
			[Column("NAME2"), Nullable] public string? Name2 { get; set; }
		}

		[Table("TABLE3")]
		public partial class Table3
		{
			[Column("ID3"), PrimaryKey, NotNull] public int Id3 { get; set; }
			[Column("PARENTID3"), NotNull] public int ParentId3 { get; set; }
			[Column("NAME3"), Nullable] public string? Name3 { get; set; }
		}

		[Test]
		public void CrossApplyOnce([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id1 = 1, Name1 = "Some1" },
				new Table1 { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new Table2 { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new Table2 { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			var ret = db.GetTable<Table1>()
				.Select(t1 => new
				{
					Name1 = t1.Name1,
					Value1 = db.GetTable<Table2>()
						.Where(x => x.ParentId2 == t1.Id1)
						.Select(t2 => new
						{
							//cross apply
							Name2 = t2.Name2,
							Value2 = t2.Id2
						})
						.FirstOrDefault()
				})
				.ToList();

			ret.Should().HaveCount(2);
		}

		[Test]
		public void CrossApplyTwice([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id1 = 1, Name1 = "Some1" },
				new Table1 { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new Table2 { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new Table2 { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new Table3 { Id3 = 22, ParentId3 = 11, Name3 = "Child22" },
				new Table3 { Id3 = 23, ParentId3 = 12, Name3 = "Child23" },
			});
			var ret = db.GetTable<Table1>()
				.Select(t1 => new
				{
					Name1 = t1.Name1,
					Value1 = db.GetTable<Table2>()
						.Where(x => x.ParentId2 == t1.Id1)
						.Select(t2 => new
						{
							//first cross apply
							Name2 = t2.Name2,
							Value2 = db.GetTable<Table3>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => new
								{
									//nested cross apply
									Name3 = t3.Name3,
									Value3 = t3.Id3
								})
								.FirstOrDefault()
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));

			// validate that the prior statement executed as a single query, not two distinct queries
			var baselines = GetCurrentBaselines();
			baselines.Should().Contain("SELECT", Exactly.Times(3));
			baselines.Should().Contain("SELECT TOP", Exactly.Twice());

			// LastQuery will only return a single query, so if it was split into two queries, not all name fields would be present
			var lastQuery = ((DataConnection)db).LastQuery;
			lastQuery.Should().Contain("NAME1", Exactly.Once());
			lastQuery.Should().Contain("NAME2", Exactly.Once());
			lastQuery.Should().Contain("NAME3", Exactly.Once());
		}
	}
}
