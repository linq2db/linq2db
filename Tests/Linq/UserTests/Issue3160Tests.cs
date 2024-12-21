using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3160Tests : TestBase
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
		public void NoDictionaryTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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
				.Select(t1 => db.GetTable<Table2>()
					.Where(x => x.ParentId2 == t1.Id1)
					.Select(t2 => db.GetTable<Table3>()
						.Where(x => x.ParentId3 == t2.Id2)
						.Select(t3 => t3.Id3)
						.FirstOrDefault())
					.FirstOrDefault())
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
		}

		[Test]
		public void NestedDictionaryTest1([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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
				.Select(t1 => db.GetTable<Table2>()
					.Where(x => x.ParentId2 == t1.Id1)
					.Select(t2 => db.GetTable<Table3>()
						.Where(x => x.ParentId3 == t2.Id2)
						.Select(t3 => new Dictionary<string, object>()
						{
							{
								"t3",
								t3.Id3
							}
						})
						.FirstOrDefault())
					.FirstOrDefault())
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
		}

		[Test]
		public void NestedDictionaryTest2([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
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
				.Select(t1 => db.GetTable<Table2>()
					.Where(x => x.ParentId2 == t1.Id1)
					.Select(t2 => new Dictionary<string, object?>()
					{
						{
							"t2",
							db.GetTable<Table3>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => new Dictionary<string, object>()
								{
									{
										"t3",
										t3.Id3
									}
								})
								.FirstOrDefault()
						}
					})
					.FirstOrDefault())
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
		}

		[Test]
		public void NestedDictionaryTest3([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
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
				.Select(t1 => new Dictionary<string, object?>()
				{
					{
						"t1",
						db.GetTable<Table2>()
							.Where(x => x.ParentId2 == t1.Id1)
							.Select(t2 => new Dictionary<string, object?>()
							{
								{
									"t2",
									db.GetTable<Table3>()
										.Where(x => x.ParentId3 == t2.Id2)
										.Select(t3 => new Dictionary<string, object>()
										{
											{
												"t3",
												t3.Id3
											}
										})
										.FirstOrDefault()
								}
							})
							.FirstOrDefault()
					}
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
		}

		[Test]
		public void NestedDictionaryTest3CrossApply([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
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
				.Select(t1 => new Dictionary<string, object?>()
				{
					{
						"t1",
						db.GetTable<Table2>()
							.Where(x => x.ParentId2 == t1.Id1)
							.Select(t2 => new Dictionary<string, object?>()
							{
								{
									"t2",
									db.GetTable<Table3>()
										.Where(x => x.ParentId3 == t2.Id2)
										.Select(t3 => new Dictionary<string, object?>()
										{
											{
												"t3",
												t3.Id3
											},
											{
												"name",
												t3.Name3
											}
										})
										.FirstOrDefault()
								},
								{
									"name",
									t2.Name2
								}
							})
							.FirstOrDefault()
					},
					{
						"name",
						t1.Name1
					}
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
		}

		[Test]
		public void OuterDictionaryTest1([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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
				.Select(t1 => new Dictionary<string, object?>()
				{
					{
						"t1",
						db.GetTable<Table2>()
							.Where(x => x.ParentId2 == t1.Id1)
							.Select(t2 => db.GetTable<Table3>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => t3.Id3)
								.FirstOrDefault())
							.FirstOrDefault()
					}
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
		}

		[Test]
		public void OuterDictionaryTest2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
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
				.Select(t1 => new Dictionary<string, object?>()
				{
					{
						"t1",
						db.GetTable<Table2>()
							.Where(x => x.ParentId2 == t1.Id1)
							.Select(t2 => new Dictionary<string, object?>()
							{
								{
									"t2",
									db.GetTable<Table3>()
										.Where(x => x.ParentId3 == t2.Id2)
										.Select(t3 => t3.Id3)
										.FirstOrDefault()
								}
							})
							.FirstOrDefault()
					}
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
		}
	}
}
