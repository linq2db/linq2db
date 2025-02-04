using System;
using System.Linq;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue4090Tests : TestBase
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
			[Column("PARENTID2"), Nullable] public int? ParentId2 { get; set; }
			[Column("NAME2"), Nullable] public string? Name2 { get; set; }
		}

		[Table("TABLE3")]
		public partial class Table3
		{
			[Column("ID3"), PrimaryKey, NotNull] public int Id3 { get; set; }
			[Column("PARENTID3"), Nullable] public int? ParentId3 { get; set; }
			[Column("NAME3"), Nullable] public string? Name3 { get; set; }
		}

		[Test]
		public void WithIdFirst_NoData([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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
			using var tbl3 = db.CreateLocalTable(Array.Empty<Table3>());
			var ret = db.GetTable<Table1>()
				.Select(t1 => new
				{
					Name1 = t1.Name1,
					Value1 = db.GetTable<Table2>()
						.Where(x => x.ParentId2 == t1.Id1)
						.Select(t2 => new
						{
							//first cross apply
							Id2 = t2.Id2,
							Value2 = db.GetTable<Table3>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => new
								{
									//nested cross apply
									Name3 = t3.Name3,
									Value3 = t3.Id3
								})
								.FirstOrDefault(),
							Name2 = t2.Name2
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
			Assert.That(ret[0].Value1, Is.Not.Null);
			Assert.That(ret[0].Value1!.Value2, Is.Null);
		}

		[Test]
		public void WithIdFirst_WithData([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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
							Id2 = t2.Id2,
							Value2 = db.GetTable<Table3>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => new
								{
									//nested cross apply
									Name3 = t3.Name3,
									Value3 = t3.Id3
								})
								.FirstOrDefault(),
							Name2 = t2.Name2
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
			Assert.That(ret[0].Value1, Is.Not.Null);
			Assert.That(ret[0].Value1!.Value2, Is.Not.Null);
		}

		[Test]
		public void WithIdAfter_NoData([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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
			using var tbl3 = db.CreateLocalTable(Array.Empty<Table3>());
			var ret = db.GetTable<Table1>()
				.Select(t1 => new
				{
					Name1 = t1.Name1,
					Value1 = db.GetTable<Table2>()
						.Where(x => x.ParentId2 == t1.Id1)
						.Select(t2 => new
						{
							//first cross apply
							Value2 = db.GetTable<Table3>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => new
								{
									//nested cross apply
									Name3 = t3.Name3,
									Value3 = t3.Id3
								})
								.FirstOrDefault(),
							Id2 = t2.Id2,
							Name2 = t2.Name2
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
			Assert.That(ret[0].Value1, Is.Not.Null);
			Assert.That(ret[0].Value1!.Value2, Is.Null);
		}

		[Test]
		public void WithIdAfter_WithData([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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
							Value2 = db.GetTable<Table3>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => new
								{
									//nested cross apply
									Name3 = t3.Name3,
									Value3 = t3.Id3
								})
								.FirstOrDefault(),
							Id2 = t2.Id2,
							Name2 = t2.Name2
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
			Assert.That(ret[0].Value1, Is.Not.Null);
			Assert.That(ret[0].Value1!.Value2, Is.Not.Null);
		}

		[Test]
		public void CrossApply_NullableFields_WithIds([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id1 = 1, Name1 = "Some1" },
				new Table1 { Id1 = 2, Name1 = null },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new Table2 { Id2 = 12, ParentId2 = 2, Name2 = "Child12" },
				new Table2 { Id2 = 13, ParentId2 = null, Name2 = "Child13" },
				new Table2 { Id2 = 14, ParentId2 = 1, Name2 = null },
				new Table2 { Id2 = 15, ParentId2 = 2, Name2 = null },
				new Table2 { Id2 = 16, ParentId2 = null, Name2 = null },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new Table3 { Id3 = 22, ParentId3 = 12, Name3 = "Child22" },
				new Table3 { Id3 = 23, ParentId3 = 13, Name3 = "Child23" },
				new Table3 { Id3 = 24, ParentId3 = 14, Name3 = "Child24" },
				new Table3 { Id3 = 25, ParentId3 = 15, Name3 = "Child25" },
				new Table3 { Id3 = 26, ParentId3 = 16, Name3 = "Child26" },
				new Table3 { Id3 = 27, ParentId3 = null, Name3 = "Child27" },
			});
			var ret = db.GetTable<Table3>()
				.OrderBy(x => x.Id3)
				.Select(t3 => new
				{
					Name3 = t3.Name3,
					Value2 = db.GetTable<Table2>()
						.Where(x => x.Id2 == t3.ParentId3)
						.Select(t2 => new
						{
							//first cross apply
							Value1 = db.GetTable<Table1>()
								.Where(x => x.Id1 == t2.ParentId2)
								.Select(t1 => new
								{
									//nested cross apply
									Name1 = t1.Name1,
									Id1 = t1.Id1,
								})
								.FirstOrDefault(),
							Name2 = t2.Name2,
							Id2 = t2.Id2,
						})
						.FirstOrDefault()
				});

			// assert the generated SQL
			var ret = AssertQuery(query);

			// assert the result (more important than the SQL for these tests)
			Assert.That(ret, Has.Count.EqualTo(7));

			Assert.Multiple(() =>
			{
				Assert.That(ret[0].Name3!, Is.EqualTo("Child21"));
				Assert.That(ret[0].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[0].Value2!.Name2!, Is.EqualTo("Child11"));
				Assert.That(ret[0].Value2!.Value1, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[0].Value2!.Value1!.Name1!, Is.EqualTo("Some1"));

				Assert.That(ret[1].Name3!, Is.EqualTo("Child22"));
				Assert.That(ret[1].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[1].Value2!.Name2!, Is.EqualTo("Child12"));
				Assert.That(ret[1].Value2!.Value1, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[1].Value2!.Value1!.Name1, Is.Null);

				Assert.That(ret[2].Name3!, Is.EqualTo("Child23"));
				Assert.That(ret[2].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[2].Value2!.Name2!, Is.EqualTo("Child13"));
				Assert.That(ret[2].Value2!.Value1, Is.Null);

				Assert.That(ret[3].Name3!, Is.EqualTo("Child24"));
				Assert.That(ret[3].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[3].Value2!.Name2!, Is.Null);
				Assert.That(ret[3].Value2!.Value1, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[3].Value2!.Value1!.Name1!, Is.EqualTo("Some1"));

				Assert.That(ret[4].Name3!, Is.EqualTo("Child25"));
				Assert.That(ret[4].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[4].Value2!.Name2!, Is.Null);
				Assert.That(ret[4].Value2!.Value1, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[4].Value2!.Value1!.Name1, Is.Null);

				Assert.That(ret[5].Name3!, Is.EqualTo("Child26"));
				Assert.That(ret[5].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[5].Value2!.Name2!, Is.Null);
				Assert.That(ret[5].Value2!.Value1, Is.Null);

				Assert.That(ret[6].Name3!, Is.EqualTo("Child27"));
				Assert.That(ret[6].Value2, Is.Null);
			});
		}

		[Test]
		public void CrossApply_NullableFields_WithoutIds([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id1 = 1, Name1 = "Some1" },
				new Table1 { Id1 = 2, Name1 = null },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new Table2 { Id2 = 12, ParentId2 = 2, Name2 = "Child12" },
				new Table2 { Id2 = 13, ParentId2 = null, Name2 = "Child13" },
				new Table2 { Id2 = 14, ParentId2 = 1, Name2 = null },
				new Table2 { Id2 = 15, ParentId2 = 2, Name2 = null },
				new Table2 { Id2 = 16, ParentId2 = null, Name2 = null },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new Table3 { Id3 = 22, ParentId3 = 12, Name3 = "Child22" },
				new Table3 { Id3 = 23, ParentId3 = 13, Name3 = "Child23" },
				new Table3 { Id3 = 24, ParentId3 = 14, Name3 = "Child24" },
				new Table3 { Id3 = 25, ParentId3 = 15, Name3 = "Child25" },
				new Table3 { Id3 = 26, ParentId3 = 16, Name3 = "Child26" },
				new Table3 { Id3 = 27, ParentId3 = null, Name3 = "Child27" },
			});
			var ret = db.GetTable<Table3>()
				.OrderBy(x => x.Id3)
				.Select(t3 => new
				{
					Name3 = t3.Name3,
					Value2 = db.GetTable<Table2>()
						.Where(x => x.Id2 == t3.ParentId3)
						.Select(t2 => new
						{
							//first cross apply
							Value1 = db.GetTable<Table1>()
								.Where(x => x.Id1 == t2.ParentId2)
								.Select(t1 => new
								{
									//nested cross apply
									Name1 = t1.Name1,
								})
								.FirstOrDefault(),
							Name2 = t2.Name2,
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret, Has.Length.EqualTo(7));

			Assert.Multiple(() =>
			{
				Assert.That(ret[0].Name3!, Is.EqualTo("Child21"));
				Assert.That(ret[0].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[0].Value2!.Name2!, Is.EqualTo("Child11"));
				Assert.That(ret[0].Value2!.Value1, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[0].Value2!.Value1!.Name1!, Is.EqualTo("Some1"));

				Assert.That(ret[1].Name3!, Is.EqualTo("Child22"));
				Assert.That(ret[1].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[1].Value2!.Name2!, Is.EqualTo("Child12"));
				Assert.That(ret[1].Value2!.Value1, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[1].Value2!.Value1!.Name1, Is.Null);

				Assert.That(ret[2].Name3!, Is.EqualTo("Child23"));
				Assert.That(ret[2].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[2].Value2!.Name2!, Is.EqualTo("Child13"));
				Assert.That(ret[2].Value2!.Value1, Is.Null);

				Assert.That(ret[3].Name3!, Is.EqualTo("Child24"));
				Assert.That(ret[3].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[3].Value2!.Name2!, Is.Null);
				Assert.That(ret[3].Value2!.Value1, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[3].Value2!.Value1!.Name1!, Is.EqualTo("Some1"));

				Assert.That(ret[4].Name3!, Is.EqualTo("Child25"));
				Assert.That(ret[4].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[4].Value2!.Name2!, Is.Null);
				Assert.That(ret[4].Value2!.Value1, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[4].Value2!.Value1!.Name1, Is.Null);

				Assert.That(ret[5].Name3!, Is.EqualTo("Child26"));
				Assert.That(ret[5].Value2, Is.Not.Null);
			});
			Assert.Multiple(() =>
			{
				Assert.That(ret[5].Value2!.Name2!, Is.Null);
				Assert.That(ret[5].Value2!.Value1, Is.Null);

				Assert.That(ret[6].Name3!, Is.EqualTo("Child27"));
				Assert.That(ret[6].Value2, Is.Null);
			});
		}
	}
}
