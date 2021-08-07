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
			[Column("ID"),      PrimaryKey,  NotNull] public int     Id   { get; set; } 
			[Column("NAME"),    Nullable            ] public string? Name { get; set; } 
		}

		[Table("TABLE2")]
		public partial class Table2
		{
			[Column("ID"),       PrimaryKey,  NotNull] public int     Id       { get; set; } 
			[Column("PARENTID"), NotNull             ] public int     ParentId { get; set; } 
			[Column("NAME"),     Nullable            ] public string? Name     { get; set; } 
		}
		
		[Table("TABLE3")]
		public partial class Table3
		{
			[Column("ID"),       PrimaryKey,  NotNull] public int     Id       { get; set; } 
			[Column("PARENTID"), NotNull             ] public int     ParentId { get; set; } 
			[Column("NAME"),     Nullable            ] public string? Name     { get; set; } 
		}

		[Test]
		public void NoDictionaryTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id = 1, Name = "Some1" },
				new Table1 { Id = 2, Name = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id = 11, ParentId = 1, Name = "Child11" },
				new Table2 { Id = 12, ParentId = 1, Name = "Child12" },
				new Table2 { Id = 13, ParentId = 2, Name = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id = 21, ParentId = 11, Name = "Child21" },
				new Table3 { Id = 22, ParentId = 11, Name = "Child22" },
				new Table3 { Id = 23, ParentId = 12, Name = "Child23" },
			});
			var ret = db.GetTable<Table1>()
				.Select(t1 => db.GetTable<Table2>()
					.Where(x => x.ParentId == t1.Id)
					.Select(t2 => db.GetTable<Table3>()
						.Where(x => x.ParentId == t2.Id)
						.Select(t3 => t3.Id)
						.FirstOrDefault())
					.FirstOrDefault())
				.ToList();
			Assert.That(ret.Count, Is.EqualTo(2));
		}

		[Test]
		public void NoDictionaryTestAlt([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id = 1, Name = "Some1" },
				new Table1 { Id = 2, Name = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id = 11, ParentId = 1, Name = "Child11" },
				new Table2 { Id = 12, ParentId = 1, Name = "Child12" },
				new Table2 { Id = 13, ParentId = 2, Name = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id = 21, ParentId = 11, Name = "Child21" },
				new Table3 { Id = 22, ParentId = 11, Name = "Child22" },
				new Table3 { Id = 23, ParentId = 12, Name = "Child23" },
			});
			var ret = db.GetTable<Table1>()
				.Select(t1 => new
				{
					Name = "t1",
					Value = db.GetTable<Table2>()
						.Where(x => x.ParentId == t1.Id)
						.Select(t2 => new
						{
							Name = "t2",
							Value = db.GetTable<Table3>()
								.Where(x => x.ParentId == t2.Id)
								.Select(t3 => new
								{
									Name = "t3",
									Value = t3.Id
								})
								.FirstOrDefault()
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret.Count, Is.EqualTo(2));
		}

		[Test]
		public void NoDictionaryTestCrossApply([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id = 1, Name = "Some1" },
				new Table1 { Id = 2, Name = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id = 11, ParentId = 1, Name = "Child11" },
				new Table2 { Id = 12, ParentId = 1, Name = "Child12" },
				new Table2 { Id = 13, ParentId = 2, Name = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id = 21, ParentId = 11, Name = "Child21" },
				new Table3 { Id = 22, ParentId = 11, Name = "Child22" },
				new Table3 { Id = 23, ParentId = 12, Name = "Child23" },
			});
			var ret = db.GetTable<Table1>()
				.Select(t1 => new
				{
					Name = t1.Name,
					Value = db.GetTable<Table2>()
						.Where(x => x.ParentId == t1.Id)
						.Select(t2 => new
						{
							Name = t2.Name,
							Value = db.GetTable<Table3>()
								.Where(x => x.ParentId == t2.Id)
								.Select(t3 => new
								{
									Name = t3.Name,
									Value = t3.Id
								})
								.FirstOrDefault()
						})
						.FirstOrDefault()
				})
				.ToList();
			Assert.That(ret.Count, Is.EqualTo(2));
		}

		[Test]
		public void NestedDictionaryTest1([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id = 1, Name = "Some1" },
				new Table1 { Id = 2, Name = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id = 11, ParentId = 1, Name = "Child11" },
				new Table2 { Id = 12, ParentId = 1, Name = "Child12" },
				new Table2 { Id = 13, ParentId = 2, Name = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id = 21, ParentId = 11, Name = "Child21" },
				new Table3 { Id = 22, ParentId = 11, Name = "Child22" },
				new Table3 { Id = 23, ParentId = 12, Name = "Child23" },
			});
			var ret = db.GetTable<Table1>()
				.Select(t1 => db.GetTable<Table2>()
					.Where(x => x.ParentId == t1.Id)
					.Select(t2 => db.GetTable<Table3>()
						.Where(x => x.ParentId == t2.Id)
						.Select(t3 => new Dictionary<string, object>()
						{
							{
								"t3",
								t3.Id
							}
						})
						.FirstOrDefault())
					.FirstOrDefault())
				.ToList();
			Assert.That(ret.Count, Is.EqualTo(2));
		}

		[Test]
		public void NestedDictionaryTest2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id = 1, Name = "Some1" },
				new Table1 { Id = 2, Name = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id = 11, ParentId = 1, Name = "Child11" },
				new Table2 { Id = 12, ParentId = 1, Name = "Child12" },
				new Table2 { Id = 13, ParentId = 2, Name = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id = 21, ParentId = 11, Name = "Child21" },
				new Table3 { Id = 22, ParentId = 11, Name = "Child22" },
				new Table3 { Id = 23, ParentId = 12, Name = "Child23" },
			});
			var ret = db.GetTable<Table1>()
				.Select(t1 => db.GetTable<Table2>()
					.Where(x => x.ParentId == t1.Id)
					.Select(t2 => new Dictionary<string, object?>()
					{
						{
							"t2",
							db.GetTable<Table3>()
								.Where(x => x.ParentId == t2.Id)
								.Select(t3 => new Dictionary<string, object>()
								{
									{
										"t3",
										t3.Id
									}
								})
								.FirstOrDefault()
						}
					})
					.FirstOrDefault())
				.ToList();
			Assert.That(ret.Count, Is.EqualTo(2));
		}

		[Test]
		public void NestedDictionaryTest3([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id = 1, Name = "Some1" },
				new Table1 { Id = 2, Name = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id = 11, ParentId = 1, Name = "Child11" },
				new Table2 { Id = 12, ParentId = 1, Name = "Child12" },
				new Table2 { Id = 13, ParentId = 2, Name = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id = 21, ParentId = 11, Name = "Child21" },
				new Table3 { Id = 22, ParentId = 11, Name = "Child22" },
				new Table3 { Id = 23, ParentId = 12, Name = "Child23" },
			});
			var ret = db.GetTable<Table1>()
				.Select(t1 => new Dictionary<string, object?>()
				{
					{
						"t1",
						db.GetTable<Table2>()
							.Where(x => x.ParentId == t1.Id)
							.Select(t2 => new Dictionary<string, object?>()
							{
								{
									"t2",
									db.GetTable<Table3>()
										.Where(x => x.ParentId == t2.Id)
										.Select(t3 => new Dictionary<string, object>()
										{
											{
												"t3",
												t3.Id
											}
										})
										.FirstOrDefault()
								}
							})
							.FirstOrDefault()
					}
				})
				.ToList();
			Assert.That(ret.Count, Is.EqualTo(2));
		}

		[Test]
		public void NestedDictionaryTest3CrossApply([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id = 1, Name = "Some1" },
				new Table1 { Id = 2, Name = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id = 11, ParentId = 1, Name = "Child11" },
				new Table2 { Id = 12, ParentId = 1, Name = "Child12" },
				new Table2 { Id = 13, ParentId = 2, Name = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id = 21, ParentId = 11, Name = "Child21" },
				new Table3 { Id = 22, ParentId = 11, Name = "Child22" },
				new Table3 { Id = 23, ParentId = 12, Name = "Child23" },
			});
			var ret = db.GetTable<Table1>()
				.Select(t1 => new Dictionary<string, object?>()
				{
					{
						t1.Name,
						db.GetTable<Table2>()
							.Where(x => x.ParentId == t1.Id)
							.Select(t2 => new Dictionary<string, object?>()
							{
								{
									t2.Name,
									db.GetTable<Table3>()
										.Where(x => x.ParentId == t2.Id)
										.Select(t3 => new Dictionary<string, object>()
										{
											{
												t3.Name,
												t3.Id
											}
										})
										.FirstOrDefault()
								}
							})
							.FirstOrDefault()
					}
				})
				.ToList();
			Assert.That(ret.Count, Is.EqualTo(2));
		}

		[Test]
		public void OuterDictionaryTest1([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id = 1, Name = "Some1" },
				new Table1 { Id = 2, Name = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id = 11, ParentId = 1, Name = "Child11" },
				new Table2 { Id = 12, ParentId = 1, Name = "Child12" },
				new Table2 { Id = 13, ParentId = 2, Name = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id = 21, ParentId = 11, Name = "Child21" },
				new Table3 { Id = 22, ParentId = 11, Name = "Child22" },
				new Table3 { Id = 23, ParentId = 12, Name = "Child23" },
			});
			var ret = db.GetTable<Table1>()
				.Select(t1 => new Dictionary<string, object>()
				{
					{
						"t1",
						db.GetTable<Table2>()
							.Where(x => x.ParentId == t1.Id)
							.Select(t2 => db.GetTable<Table3>()
								.Where(x => x.ParentId == t2.Id)
								.Select(t3 => t3.Id)
								.FirstOrDefault())
							.FirstOrDefault()
					}
				})
				.ToList();
			Assert.That(ret.Count, Is.EqualTo(2));
		}

		[Test]
		public void OuterDictionaryTest2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new Table1 { Id = 1, Name = "Some1" },
				new Table1 { Id = 2, Name = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new Table2 { Id = 11, ParentId = 1, Name = "Child11" },
				new Table2 { Id = 12, ParentId = 1, Name = "Child12" },
				new Table2 { Id = 13, ParentId = 2, Name = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new Table3 { Id = 21, ParentId = 11, Name = "Child21" },
				new Table3 { Id = 22, ParentId = 11, Name = "Child22" },
				new Table3 { Id = 23, ParentId = 12, Name = "Child23" },
			});
			var ret = db.GetTable<Table1>()
				.Select(t1 => new Dictionary<string, object?>()
				{
					{
						"t1",
						db.GetTable<Table2>()
							.Where(x => x.ParentId == t1.Id)
							.Select(t2 => new Dictionary<string, object>()
							{
								{
									"t2",
									db.GetTable<Table3>()
										.Where(x => x.ParentId == t2.Id)
										.Select(t3 => t3.Id)
										.FirstOrDefault()
								}
							})
							.FirstOrDefault()
					}
				})
				.ToList();
			Assert.That(ret.Count, Is.EqualTo(2));
		}
	}
}
