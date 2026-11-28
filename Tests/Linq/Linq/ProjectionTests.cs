using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class ProjectionTests : TestBase
	{
		[Table]
		public class SomeEntity
		{
			[Column]
			public int Id { get; set; }
			[Column]
			public int? OtherId { get; set; }

			[Association(ThisKey = "OtherId", OtherKey = "Id", CanBeNull = true)]
			public SomeOtherEntity? Other { get; set; }

			[ExpressionMethod(nameof(GetThroughIsActual), IsColumn = false)]
			public bool? ThroughIsActual { get; set; }

			private static Expression<Func<SomeEntity, bool?>> GetThroughIsActual()
			{
				return t => Sql.ToNullable(t.Other!.IsActual);
			}
		}

		[Table]
		public class SomeOtherEntity
		{
			[Column]
			public int Id { get; set; }
			[Column]
			public string? Name { get; set; }
			[Column]
			public bool IsActual { get; set; }
		}

		[Test]
		public void AssociationTest([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<SomeEntity>(new[]{new SomeEntity{Id = 1, OtherId = 3} }))
			using (db.CreateLocalTable<SomeOtherEntity>(new[]{new SomeOtherEntity{Id = 2, IsActual = true} }))
			{
				var query = db.GetTable<SomeEntity>()
					.Select(t => new { t.Id, t.OtherId, t.ThroughIsActual });

				var result = query.First();

				Assert.That(result.ThroughIsActual, Is.Null);
			}
		}

		[Test]
		public void ToNullableTest([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<SomeEntity>(new[]{new SomeEntity{Id = 1, OtherId = 3} }))
			using (db.CreateLocalTable<SomeOtherEntity>(new[]{new SomeOtherEntity{Id = 2, IsActual = true} }))
			{
				var query = from t in db.GetTable<SomeEntity>()
					from t2 in db.GetTable<SomeOtherEntity>().LeftJoin(t2 => t2.Id == t.OtherId)
					select new { t.Id, t.OtherId, IsActual = Sql.ToNullable(t2.IsActual) };

				var result = query.First();

				Assert.That(result.IsActual, Is.Null);
			}
		}

		[Test]
		public void GroupByWithCast([DataSources(TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);
			var query = db.Person
					.GroupBy(_ => new
					{
						group = _.ID,
						key = new
						{
							// cast
							id = (int?)_.Patient!.PersonID,
							value = _.Patient.Diagnosis
						}
					})
					.Having(auto16031 => auto16031.Key.group == 1)
					.Select(_ => new
					{
						x = new
						{
							@value = _.Key.key.@value,
							id = _.Key.key.id
						},
						y = _.Average(auto16033 => auto16033.ID)
					})
					.OrderByDescending(_ => _.x.value)
					.Take(1000);

			var result = query.ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].y, Is.EqualTo(1));
				Assert.That(result[0].x.value, Is.Null);
				Assert.That(result[0].x.id, Is.Null);
			}
		}

		[Test]
		public void GroupByWithTwoCasts([DataSources(TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);
			var query = db.Person
					.GroupBy(_ => new
					{
						group = _.ID,
						key = new
						{
							// cast
							id = (int?)_.Patient!.PersonID,
							value = _.Patient.Diagnosis
						}
					})
					.Having(auto16031 => auto16031.Key.group == 1)
					.Select(_ => new
					{
						x = new
						{
							@value = _.Key.key.@value,
							// cast int? to int?
							id = (int?)_.Key.key.id
						},
						y = _.Average(auto16033 => auto16033.ID)
					})
					.OrderByDescending(_ => _.x.value)
					.Take(1000);

			var result = query.ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].y, Is.EqualTo(1));
				Assert.That(result[0].x.value, Is.Null);
				Assert.That(result[0].x.id, Is.Null);
			}
		}

		[Test]
		public void GroupByWithToNullable([DataSources(TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);
			var query = db.Person
					.GroupBy(_ => new
					{
						group = _.ID,
						key = new
						{
							id = Sql.ToNullable(_.Patient!.PersonID),
							value = _.Patient.Diagnosis
						}
					})
					.Having(auto16031 => auto16031.Key.group == 1)
					.Select(_ => new
					{
						x = new
						{
							@value = _.Key.key.@value,
							id = _.Key.key.id
						},
						y = _.Average(auto16033 => auto16033.ID)
					})
					.OrderByDescending(_ => _.x.value)
					.Take(1000);

			var result = query.ToList();

			Assert.That(result, Has.Count.EqualTo(1));
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result[0].y, Is.EqualTo(1));
				Assert.That(result[0].x.value, Is.Null);
				Assert.That(result[0].x.id, Is.Null);
			}
		}

		[Table]
		sealed class StringTestTable
		{
			[PrimaryKey] public int     ID   { get; set; }
			[Column]     public string? Data { get; set; }
		}

		[Test(Description = "Complex client-side string expression in Insert projection should not throw NRE")]
		public void ComplexStringProjectionTest([DataSources] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateLocalTable([new StringTestTable { ID = 1, Data = null }]);

			string? data = null;

			var affected = tmp.Insert(() => new StringTestTable
			{
				ID   = 2,
				Data = string.IsNullOrWhiteSpace(data) ? data : data!.Length > 50 ? data.Substring(0, 50) : data
			});

			if (context.SupportsRowcount())
				Assert.That(affected, Is.EqualTo(1));

			var row = tmp.Single(t => t.ID == 2);
			Assert.That(row.Data, Is.Null);
		}

		[Table(Name = "ProjectedListSource")]
		public partial class ProjectedListSource
		{
			[Column, NotNull    ] public int   Id       { get; set; } // integer
			[Column, NotNull    ] public int   ParentId { get; set; } // integer
			[Column,    Nullable] public bool? IsTrue   { get; set; } // boolean
		}

		[Table(Name = "ProjectedListStatus")]
		public partial class ProjectedListStatus
		{
			[PrimaryKey, NotNull] public int    StatusId   { get; set; } // integer
			[Column,     NotNull] public string StatusName { get; set; } = null!; // character varying
		}
		public class ProjectedListModel
		{
			public int       Id         { get; set; }
			public int       Status     { get; set; }
			public string    Text       { get; set; } = null!;
			public List<int> StatusData { get; set; } = null!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3014")]
		public void ProjectedListMemberWithConstants([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<ProjectedListSource>(new []{new ProjectedListSource{Id = 1, IsTrue = true}}))
			using (db.CreateLocalTable<ProjectedListStatus>(new []{new ProjectedListStatus{StatusId = 1, StatusName = "Sample"}}))
			{
				var query = from t in db.GetTable<ProjectedListSource>()
					select new ProjectedListModel
					{
						Id         = t.Id,
						Status     = 1,
						Text       = "0",
						StatusData = db.GetTable<ProjectedListStatus>().Select(x => 1).ToList(),
					};
				query.ToList().Count().ShouldBe(1);
				query.Where(x => x.Id == 0).ShouldBeEmpty();
				query.Where(x => x.Status == 0).ShouldBeEmpty();
				query.Where(e => e.Text == "0").ToList().Count().ShouldBe(1);

			}
		}

		[Table("DictionaryRoot")]
		public partial class DictionaryRoot
		{
			[Column("ID1"), PrimaryKey, NotNull] public int Id1 { get; set; }
			[Column("NAME1"), Nullable] public string? Name1 { get; set; }
		}

		[Table("DictionaryMiddle")]
		public partial class DictionaryMiddle
		{
			[Column("ID2"), PrimaryKey, NotNull] public int Id2 { get; set; }
			[Column("PARENTID2"), NotNull] public int ParentId2 { get; set; }
			[Column("NAME2"), Nullable] public string? Name2 { get; set; }
		}

		[Table("DictionaryLeaf")]
		public partial class DictionaryLeaf
		{
			[Column("ID3"), PrimaryKey, NotNull] public int Id3 { get; set; }
			[Column("PARENTID3"), NotNull] public int ParentId3 { get; set; }
			[Column("NAME3"), Nullable] public string? Name3 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3160")]
		public void NestedFirstOrDefaultSubqueriesWithScalarResult([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new DictionaryRoot { Id1 = 1, Name1 = "Some1" },
				new DictionaryRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new DictionaryMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new DictionaryMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new DictionaryMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new DictionaryLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new DictionaryLeaf { Id3 = 22, ParentId3 = 11, Name3 = "Child22" },
				new DictionaryLeaf { Id3 = 23, ParentId3 = 12, Name3 = "Child23" },
			});
			var ret = db.GetTable<DictionaryRoot>()
				.Select(t1 => db.GetTable<DictionaryMiddle>()
					.Where(x => x.ParentId2 == t1.Id1)
					.Select(t2 => db.GetTable<DictionaryLeaf>()
						.Where(x => x.ParentId3 == t2.Id2)
						.Select(t3 => t3.Id3)
						.FirstOrDefault())
					.FirstOrDefault())
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3160")]
		public void NestedFirstOrDefaultSubqueryProjectedToDictionary([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new DictionaryRoot { Id1 = 1, Name1 = "Some1" },
				new DictionaryRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new DictionaryMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new DictionaryMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new DictionaryMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new DictionaryLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new DictionaryLeaf { Id3 = 22, ParentId3 = 11, Name3 = "Child22" },
				new DictionaryLeaf { Id3 = 23, ParentId3 = 12, Name3 = "Child23" },
			});
			var ret = db.GetTable<DictionaryRoot>()
				.Select(t1 => db.GetTable<DictionaryMiddle>()
					.Where(x => x.ParentId2 == t1.Id1)
					.Select(t2 => db.GetTable<DictionaryLeaf>()
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3160")]
		public void DictionaryProjectionNestedTwoLevels([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new DictionaryRoot { Id1 = 1, Name1 = "Some1" },
				new DictionaryRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new DictionaryMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new DictionaryMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new DictionaryMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new DictionaryLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new DictionaryLeaf { Id3 = 22, ParentId3 = 11, Name3 = "Child22" },
				new DictionaryLeaf { Id3 = 23, ParentId3 = 12, Name3 = "Child23" },
			});
			var ret = db.GetTable<DictionaryRoot>()
				.Select(t1 => db.GetTable<DictionaryMiddle>()
					.Where(x => x.ParentId2 == t1.Id1)
					.Select(t2 => new Dictionary<string, object?>()
					{
						{
							"t2",
							db.GetTable<DictionaryLeaf>()
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3160")]
		public void DictionaryProjectionNestedThreeLevels([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new DictionaryRoot { Id1 = 1, Name1 = "Some1" },
				new DictionaryRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new DictionaryMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new DictionaryMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new DictionaryMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new DictionaryLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new DictionaryLeaf { Id3 = 22, ParentId3 = 11, Name3 = "Child22" },
				new DictionaryLeaf { Id3 = 23, ParentId3 = 12, Name3 = "Child23" },
			});
			var ret = db.GetTable<DictionaryRoot>()
				.Select(t1 => new Dictionary<string, object?>()
				{
					{
						"t1",
						db.GetTable<DictionaryMiddle>()
							.Where(x => x.ParentId2 == t1.Id1)
							.Select(t2 => new Dictionary<string, object?>()
							{
								{
									"t2",
									db.GetTable<DictionaryLeaf>()
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3160")]
		public void DictionaryProjectionNestedThreeLevelsWithExtraColumns([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new DictionaryRoot { Id1 = 1, Name1 = "Some1" },
				new DictionaryRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new DictionaryMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new DictionaryMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new DictionaryMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new DictionaryLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new DictionaryLeaf { Id3 = 22, ParentId3 = 11, Name3 = "Child22" },
				new DictionaryLeaf { Id3 = 23, ParentId3 = 12, Name3 = "Child23" },
			});
			var ret = db.GetTable<DictionaryRoot>()
				.Select(t1 => new Dictionary<string, object?>()
				{
					{
						"t1",
						db.GetTable<DictionaryMiddle>()
							.Where(x => x.ParentId2 == t1.Id1)
							.Select(t2 => new Dictionary<string, object?>()
							{
								{
									"t2",
									db.GetTable<DictionaryLeaf>()
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

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3160")]
		public void OuterDictionaryProjectionWithScalarSubqueries([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new DictionaryRoot { Id1 = 1, Name1 = "Some1" },
				new DictionaryRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new DictionaryMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new DictionaryMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new DictionaryMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new DictionaryLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new DictionaryLeaf { Id3 = 22, ParentId3 = 11, Name3 = "Child22" },
				new DictionaryLeaf { Id3 = 23, ParentId3 = 12, Name3 = "Child23" },
			});
			var ret = db.GetTable<DictionaryRoot>()
				.Select(t1 => new Dictionary<string, object?>()
				{
					{
						"t1",
						db.GetTable<DictionaryMiddle>()
							.Where(x => x.ParentId2 == t1.Id1)
							.Select(t2 => db.GetTable<DictionaryLeaf>()
								.Where(x => x.ParentId3 == t2.Id2)
								.Select(t3 => t3.Id3)
								.FirstOrDefault())
							.FirstOrDefault()
					}
				})
				.ToList();
			Assert.That(ret, Has.Count.EqualTo(2));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3160")]
		public void OuterDictionaryProjectionWithNestedDictionary([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new DictionaryRoot { Id1 = 1, Name1 = "Some1" },
				new DictionaryRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new DictionaryMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new DictionaryMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new DictionaryMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new DictionaryLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new DictionaryLeaf { Id3 = 22, ParentId3 = 11, Name3 = "Child22" },
				new DictionaryLeaf { Id3 = 23, ParentId3 = 12, Name3 = "Child23" },
			});
			var ret = db.GetTable<DictionaryRoot>()
				.Select(t1 => new Dictionary<string, object?>()
				{
					{
						"t1",
						db.GetTable<DictionaryMiddle>()
							.Where(x => x.ParentId2 == t1.Id1)
							.Select(t2 => new Dictionary<string, object?>()
							{
								{
									"t2",
									db.GetTable<DictionaryLeaf>()
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

		[Table("CrossApplyRoot")]
		public partial class CrossApplyRoot
		{
			[Column("ID1"), PrimaryKey, NotNull] public int Id1 { get; set; }
			[Column("NAME1"), Nullable] public string? Name1 { get; set; }
		}

		[Table("CrossApplyMiddle")]
		public partial class CrossApplyMiddle
		{
			[Column("ID2"), PrimaryKey, NotNull] public int Id2 { get; set; }
			[Column("PARENTID2"), NotNull] public int ParentId2 { get; set; }
			[Column("NAME2"), Nullable] public string? Name2 { get; set; }
		}

		[Table("CrossApplyLeaf")]
		public partial class CrossApplyLeaf
		{
			[Column("ID3"), PrimaryKey, NotNull] public int Id3 { get; set; }
			[Column("PARENTID3"), NotNull] public int ParentId3 { get; set; }
			[Column("NAME3"), Nullable] public string? Name3 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3161")]
		public void CrossApplyOnce([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new CrossApplyRoot { Id1 = 1, Name1 = "Some1" },
				new CrossApplyRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new CrossApplyMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new CrossApplyMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new CrossApplyMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			var ret = db.GetTable<CrossApplyRoot>()
				.Select(t1 => new
				{
					Name1 = t1.Name1,
					Value1 = db.GetTable<CrossApplyMiddle>()
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

			ret.Count.ShouldBe(2);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3161")]
		public void CrossApplyTwice([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var tbl1 = db.CreateLocalTable(new[]
			{
				new CrossApplyRoot { Id1 = 1, Name1 = "Some1" },
				new CrossApplyRoot { Id1 = 2, Name1 = "Some2" },
			});
			using var tbl2 = db.CreateLocalTable(new[]
			{
				new CrossApplyMiddle { Id2 = 11, ParentId2 = 1, Name2 = "Child11" },
				new CrossApplyMiddle { Id2 = 12, ParentId2 = 1, Name2 = "Child12" },
				new CrossApplyMiddle { Id2 = 13, ParentId2 = 2, Name2 = "Child13" },
			});
			using var tbl3 = db.CreateLocalTable(new[]
			{
				new CrossApplyLeaf { Id3 = 21, ParentId3 = 11, Name3 = "Child21" },
				new CrossApplyLeaf { Id3 = 22, ParentId3 = 11, Name3 = "Child22" },
				new CrossApplyLeaf { Id3 = 23, ParentId3 = 12, Name3 = "Child23" },
			});
			var ret = db.GetTable<CrossApplyRoot>()
				.Select(t1 => new
				{
					Name1 = t1.Name1,
					Value1 = db.GetTable<CrossApplyMiddle>()
						.Where(x => x.ParentId2 == t1.Id1)
						.Select(t2 => new
						{
							//first cross apply
							Name2 = t2.Name2,
							Value2 = db.GetTable<CrossApplyLeaf>()
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
			baselines.ShouldContain("SELECT", Exactly.Times(3));
			baselines.ShouldContain("SELECT TOP", Exactly.Twice());

			// LastQuery will only return a single query, so if it was split into two queries, not all name fields would be present
			var lastQuery = ((DataConnection)db).LastQuery!;
			lastQuery.ShouldContain("NAME1", Exactly.Once());
			lastQuery.ShouldContain("NAME2", Exactly.Once());
			lastQuery.ShouldContain("NAME3", Exactly.Once());
		}

		[Table]
		public class NestedCountAdvice
		{
			[Column] public int Id { get; set; }
		}

		[Table]
		public class NestedCountInventory
		{
			[Column] public int InfeedAdviceID { get; set; }
		}
		public class NestedCountInner
		{
			public NestedCountAdvice? InfeedAdvice { get; set; }

			public int? Count { get; set; }
		}

		public class NestedCountOuter
		{
			public NestedCountInner? MlogCombined1 { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4390")]
		public void NestedProjectionWithCountSubqueryFilteredAfterJoin([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<NestedCountAdvice>())
			using (db.CreateLocalTable<NestedCountInventory>())
			{
				db.Insert(new NestedCountInventory() { InfeedAdviceID = 1 });
				db.Insert(new NestedCountAdvice() { Id = 1 });

				var irs = from ir in db.GetTable<NestedCountInventory>() select ir;

				var qry = from infeed in db.GetTable<NestedCountAdvice>()
								join inventory in db.GetTable<NestedCountInventory>() on infeed.Id equals inventory.InfeedAdviceID
								select new NestedCountOuter
								{
									MlogCombined1 = new NestedCountInner
									{
										InfeedAdvice = infeed,
										Count = irs.Count(x => x.InfeedAdviceID == infeed.Id),
									}
								};

				var count = qry.Where(x => x.MlogCombined1 != null).Count();

				count.ShouldBe(1);
			}
		}

		[Table("Returns")]
		public class SubqueryProjectionReturn
		{
			[Column("Id"), PrimaryKey, NotNull]
			public int Id { get; set; }

			// Always contains a valid value in our tests.
			[Column("OriginalSaleId"), NotNull]
			public int OriginalSaleId { get; set; }

			// Will be null in our tests.
			[Column("ReshipSaleId"), Nullable]
			public int? ReshipSaleId { get; set; }
		}

		[Table("Sales")]
		public class SubqueryProjectionSale
		{
			[Column("Id"), PrimaryKey, NotNull]
			public int Id { get; set; }
		}

		/// <summary>
		/// This test demonstrates the correct behavior when not using dictionary projections.
		/// Expected:
		/// - OriginalSale is materialized as an object with Id=43757.
		/// - ReshipSale is null.
		/// </summary>
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4813")]
		public void AnonymousProjectionSubqueriesWithNullForeignKey(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			// Create a Returns table with one row.
			using var returnsTable = db.CreateLocalTable(new[]
			{
				new SubqueryProjectionReturn { Id = 1, OriginalSaleId = 43757, ReshipSaleId = null }
			});

			// Create a Sales table with one row.
			using var salesTable = db.CreateLocalTable(new[]
			{
				new SubqueryProjectionSale { Id = 43757 }
			});

			var queryWorksCorrectly =
				from r in db.GetTable<SubqueryProjectionReturn>()
				where r.ReshipSaleId == null
				select new
				{
					r.Id,
					r.OriginalSaleId,
					OriginalSale = db.GetTable<SubqueryProjectionSale>()
						.Where(x => x.Id == r.OriginalSaleId)
						.Select(x => new { x.Id })
						.FirstOrDefault(),
					r.ReshipSaleId,
					ReshipSale = db.GetTable<SubqueryProjectionSale>()
						.Where(x => x.Id == r.ReshipSaleId)
						.Select(x => new { x.Id })
						.FirstOrDefault()
				};

			var result = queryWorksCorrectly.Take(1).ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result, Has.Count.EqualTo(1), "Expected exactly one result row.");
				var row = result.First();

				// Verify that OriginalSale is correctly materialized.
				Assert.That(row.OriginalSale, Is.Not.Null, "OriginalSale should not be null.");
				Assert.That(row.OriginalSale.Id, Is.EqualTo(43757), "OriginalSale Id should be 43757.");

				// Verify that ReshipSale is null.
				Assert.That(row.ReshipSale, Is.Null, "ReshipSale should be null.");
			}
		}

		/// <summary>
		/// This test uses dictionary projections with OriginalSale projected first.
		/// Expected:
		/// - OriginalSale: a dictionary with {"Id": 43757}.
		/// - ReshipSale: null because ReshipSaleId is null.
		/// </summary>
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4813")]
		public void DictionaryProjectionSubqueriesOriginalSaleFirst(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			using var returnsTable = db.CreateLocalTable(new[]
			{
				new SubqueryProjectionReturn { Id = 1, OriginalSaleId = 43757, ReshipSaleId = null }
			});

			using var salesTable = db.CreateLocalTable(new[]
			{
				new SubqueryProjectionSale { Id = 43757 }
			});

			var queryFailsNotNull =
				from r in db.GetTable<SubqueryProjectionReturn>()
				where r.ReshipSaleId == null
				select new
				{
					r.Id,
					r.OriginalSaleId,
					OriginalSale = db.GetTable<SubqueryProjectionSale>()
						.Where(x => x.Id == r.OriginalSaleId)
						.Select(x => new Dictionary<string, object> { { "Id", x.Id } })
						.FirstOrDefault(),
					r.ReshipSaleId,
					ReshipSale = db.GetTable<SubqueryProjectionSale>()
						.Where(x => x.Id == r.ReshipSaleId)
						.Select(x => new Dictionary<string, object> { { "Id", x.Id } })
						.FirstOrDefault()
				};

			var result = queryFailsNotNull.Take(1).ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result, Has.Count.EqualTo(1), "Expected exactly one result row.");
				var row = result.First();

				// Expected behavior: OriginalSale should be a dictionary with {"Id": 43757}.
				Assert.That(row.OriginalSale, Is.Not.Null, "OriginalSale dictionary should not be null.");
				Assert.That(row.OriginalSale.ContainsKey("Id"), Is.True, "OriginalSale dictionary should contain key 'Id'.");
				Assert.That(row.OriginalSale["Id"], Is.EqualTo(43757), "OriginalSale dictionary should have Id 43757.");

				// Expected behavior: ReshipSale should be null because ReshipSaleId is null.
				Assert.That(row.ReshipSale, Is.Null, "ReshipSale dictionary should be null when ReshipSaleId is null.");
			}
		}

		/// <summary>
		/// This test uses dictionary projections with the order reversed.
		/// Expected:
		/// - ReshipSale: null (because ReshipSaleId is null).
		/// - OriginalSale: a dictionary with {"Id": 43757}.
		/// </summary>
		[Test(Description = "https://github.com/linq2db/linq2db/issues/4813")]
		public void DictionaryProjectionSubqueriesReshipSaleFirst(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			using var returnsTable = db.CreateLocalTable(new[]
			{
				new SubqueryProjectionReturn { Id = 1, OriginalSaleId = 43757, ReshipSaleId = null }
			});

			using var salesTable = db.CreateLocalTable(new[]
			{
				new SubqueryProjectionSale { Id = 43757 }
			});

			var queryFailsNull =
				from r in db.GetTable<SubqueryProjectionReturn>()
				where r.ReshipSaleId == null
				select new
				{
					r.Id,
					r.ReshipSaleId,
					ReshipSale = db.GetTable<SubqueryProjectionSale>()
						.Where(x => x.Id == r.ReshipSaleId)
						.Select(x => new Dictionary<string, object> { { "Id", x.Id } })
						.FirstOrDefault(),
					r.OriginalSaleId,
					OriginalSale = db.GetTable<SubqueryProjectionSale>()
						.Where(x => x.Id == r.OriginalSaleId)
						.Select(x => new Dictionary<string, object> { { "Id", x.Id } })
						.FirstOrDefault()
				};

			var result = queryFailsNull.Take(1).ToList();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result, Has.Count.EqualTo(1), "Expected exactly one result row.");
				var row = result.First();

				// Expected behavior: ReshipSale should be null.
				Assert.That(row.ReshipSale, Is.Null, "ReshipSale should be null because ReshipSaleId is null.");

				// Expected behavior: OriginalSale should be a dictionary with {"Id": 43757}.
				Assert.That(row.OriginalSale, Is.Not.Null, "OriginalSale dictionary should not be null.");
				Assert.That(row.OriginalSale.ContainsKey("Id"), Is.True, "OriginalSale dictionary should contain key 'Id'.");
				Assert.That(row.OriginalSale["Id"], Is.EqualTo(43757), "OriginalSale dictionary should have Id 43757.");
			}
		}

		[Table]
		sealed class HasValueSourceTable
		{
			[PrimaryKey] public int Id    { get; set; }
			[Column]     public int Value { get; set; }
		}

		// A nullable member that is left unbound (never assigned) in one projection and then
		// consumed through Nullable<T>.HasValue in a later projection. Building used to throw
		// "InvalidOperationException: Called when root is not initialized." — see #5575.
		sealed class HasValueStat
		{
			public int  Id        { get; set; }
			public int? LeadCount { get; set; }
		}

		// Reported shape: HasValue inside a conditional projection.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5575")]
		public void ConditionalNullableHasValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(new[] { new HasValueSourceTable { Id = 1, Value = 4 } });

			var query = tb
				.Select(c => new HasValueStat { Id = c.Id }) // LeadCount left unbound
				.Select(s => new
				{
					s.Id,
					Rate = s.LeadCount.HasValue ? (decimal?)(s.LeadCount.Value / 2m) : null
				});

			AssertQuery(query);
		}

		// Minimal trigger: HasValue on the unbound member, without a conditional.
		[Test(Description = "https://github.com/linq2db/linq2db/issues/5575")]
		public void UnboundNullableHasValue([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(new[] { new HasValueSourceTable { Id = 1, Value = 4 } });

			var query = tb
				.Select(c => new HasValueStat { Id = c.Id }) // LeadCount left unbound
				.Select(s => new { s.Id, Has = s.LeadCount.HasValue });

			AssertQuery(query);
		}

		[ThrowsRequiresCorrelatedSubquery]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/873")]
		public void NestedAnonymousProjectionWithCorrelatedSubqueries([DataSources(ProviderName.SqlCe, TestProvName.AllNativeAccess)] string context)
		{
			using var db = GetDataContext(context);
			var q = db.Child;

			var query = db.Parent
					.Select(e => new
					{
						Fields = new
						{
							Label = " " + (e.Value1 ?? 0).ToString(),
							Sum = new { SubSum = q.Where(c => c.Parent == e).Sum(c => c.ChildID) },
							Any = q.Where(c => c.Parent == e).Any(),
							Count = q.Where(p => p.Parent == e).Count()
						},
					})
					.Where(f => f.Fields.Label.Contains("1") && f.Fields.Sum.SubSum > 0);

			var qc = Child;

			var expected = Parent
					.Select(e => new
					{
						Fields = new
						{
							Label = " " + (e.Value1 ?? 0).ToString(),
							Sum = new { SubSum = qc.Where(c => c.Parent == e).Sum(c => c.ChildID) },
							Any = qc.Where(c => c.Parent == e).Any(),
							Count = qc.Where(p => p.Parent == e).Count()
						},
					})
					.Where(f => f.Fields.Label.Contains("1") && f.Fields.Sum.SubSum > 0);

			AreEqual(expected, query);
		}
	}
}
