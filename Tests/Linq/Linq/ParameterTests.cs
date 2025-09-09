using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public partial class ParameterTests : TestBase
	{
		[Test]
		public void InlineParameter([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.InlineParameters = true;

				var id = 1;

				var parent1 = db.Parent.FirstOrDefault(p => p.ParentID == id)!;
				id++;
				var parent2 = db.Parent.FirstOrDefault(p => p.ParentID == id)!;

				Assert.That(parent1.ParentID, Is.Not.EqualTo(parent2.ParentID));
			}
		}

		[Test]
		public void TestQueryCacheWithNullParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				int? id = null;
				Assert.That(db.Person.Where(_ => _.ID == id).Count(), Is.Zero);

				id = 1;
				Assert.That(db.Person.Where(_ => _.ID == id).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void TestOptimizingParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;
				Assert.That(db.Person.Where(_ => _.ID == id || _.ID <= id || _.ID == id).Count(), Is.EqualTo(1));
			}
		}

		[Test]
		public void InlineTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;
				var query = from t in db.Person
					where t.ID == id
					select t;

				var queryInlined = query.InlineParameters();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query.GetStatement().CollectParameters(), Has.Length.EqualTo(1));
					Assert.That(queryInlined.GetStatement().CollectParameters(), Is.Empty);
				}
			}
		}

		[Test]
		public void InlineWithSkipTake([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;
				var query = from t in db.Person
					where t.ID == id
					select t;

				var queryInlined = query.InlineParameters().Skip(1).Take(2);
				query = query.Skip(1).Take(2);
				using (Assert.EnterMultipleScope())
				{
					Assert.That(query.GetStatement().CollectParameters(), Has.Length.EqualTo(3));
					Assert.That(queryInlined.GetStatement().CollectParameters(), Is.Empty);
				}
			}
		}

		[ActiveIssue(
			@"Sybase providers explicitly cut string value if it contains 0x00 character and the only way to send it to database is to use literals.
			But here we test parameters.
			For reference: https://github.com/DataAction/AdoNetCore.AseClient/issues/51#issuecomment-417981677",
			Configuration = TestProvName.AllSybase)]
		[Test]
		public void CharAsSqlParameter1(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracleDevartOCI,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "0 \x0 ' 0";
				var s2 = db.Select(() => Sql.AsSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[ActiveIssue(
			@"Sybase providers explicitly cut string value if it contains 0x00 character and the only way to send it to database is to use literals.
			But here we test parameters.
			For reference: https://github.com/DataAction/AdoNetCore.AseClient/issues/51#issuecomment-417981677",
			Configuration = TestProvName.AllSybase)]
		[Test]
		public void CharAsSqlParameter2(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracleDevartOCI,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x0 \x0 ' \x0";
				var s2 = db.Select(() => Sql.AsSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[ActiveIssue(
			@"Sybase providers explicitly cut string value if it contains 0x00 character and the only way to send it to database is to use literals.
			But here we test parameters.
			For reference: https://github.com/DataAction/AdoNetCore.AseClient/issues/51#issuecomment-417981677",
			Configuration = TestProvName.AllSybase)]
		[Test]
		public void CharAsSqlParameter3(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracleDevartOCI,
				TestProvName.AllInformix,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x0";
				var s2 = db.Select(() => Sql.AsSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test]
		public void CharAsSqlParameter4([DataSources] string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x1-\x2-\x3";
				var s2 = db.Select(() => Sql.AsSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[ActiveIssue(
			@"Sybase providers explicitly cut string value if it contains 0x00 character and the only way to send it to database is to use literals.
			But here we test parameters.
			For reference: https://github.com/DataAction/AdoNetCore.AseClient/issues/51#issuecomment-417981677",
			Configuration = TestProvName.AllSybase)]
		[Test]
		public void CharAsSqlParameter5(
			[DataSources(
				TestProvName.AllPostgreSQL,
				TestProvName.AllInformix)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = '\x0';
				var s2 = db.Select(() => Sql.AsSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		sealed class AllTypes
		{
			public decimal DecimalDataType;
			public byte[]? BinaryDataType;
			public byte[]? VarBinaryDataType;
			[Column(DataType = DataType.VarChar)]
			public string? VarcharDataType;
		}

		// Excluded providers inline such parameter or miss mappings
		[Test]
		public void ExposeSqlDecimalParameter([DataSources(false, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllSapHana, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllDB2, TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var p   = 123.456m;
				db.GetTable<AllTypes>().Where(t => t.DecimalDataType == p).ToArray();

				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(6, 3)"));
			}
		}

		// Excluded providers inline such parameter or miss mappings
		[Test]
		public void ExposeSqlBinaryParameter([DataSources(false, ProviderName.SqlCe, TestProvName.AllSybase, TestProvName.AllDB2, TestProvName.AllSapHana, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllInformix, TestProvName.AllFirebird, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var p   = new byte[] { 0, 1, 2 };
				db.GetTable<AllTypes>().Where(t => t.BinaryDataType == p).ToArray();

				var sql = GetCurrentBaselines();

				Assert.That(sql, Contains.Substring("(3)").Or.Contains("Blob").Or.Contains("(8000)"));
			}
		}

		[Test]
		public void Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var dt = TestData.DateTime;

				if (context.IsAnyOf(TestProvName.AllInformix))
					dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);

				var _ = db.Types.Where(t => t.DateTimeValue == Sql.AsSql(dt)).ToList();
			}
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				int id1 = 1, id2 = 10000;

				var parent1 = db.Parent.OrderBy(p => p.ParentID).FirstOrDefault(p => p.ParentID == id1 || p.ParentID >= id1 || p.ParentID >= id2)!;
				id1++;
				var parent2 = db.Parent.OrderBy(p => p.ParentID).FirstOrDefault(p => p.ParentID == id1 || p.ParentID >= id1 || p.ParentID >= id2)!;

				Assert.That(parent1.ParentID, Is.Not.EqualTo(parent2.ParentID));
			}
		}

		static class AdditionalSql
		{
			[Sql.Expression("(({2} * ({1} - {0}) / {2}) * {0})", ServerSideOnly = true)]
			public static int Operation(int item1, int item2, int item3)
			{
				return (item3 * (item2 - item1) / item3) * item1;
			}
		}

		[Test]
		public void TestPositionedParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var x3  = 3;
				var y10 = 10;
				var z2  = 2;

				var query = from child in db.Child
					select new
					{
						Value1 = Sql.AsSql(AdditionalSql.Operation(child.ChildID,
							AdditionalSql.Operation(z2, y10, AdditionalSql.Operation(z2, y10, x3)),
							AdditionalSql.Operation(z2, y10, x3)))
					};

				var expected = from child in Child
					select new
					{
						Value1 = AdditionalSql.Operation(child.ChildID,
							AdditionalSql.Operation(z2, y10, AdditionalSql.Operation(z2, y10, x3)),
							AdditionalSql.Operation(z2, y10, x3))
					};

				AreEqual(expected, query);
			}
		}

		[Test]
		public void TestQueryableCall([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Where(p => GetChildren(db).Select(c => c.ParentID).Contains(p.ParentID)).ToList();
			}
		}

		[Test]
		public void TestQueryableCallWithParameters([DataSources(TestProvName.AllClickHouse)] string context)
		{
			// baselines could be affected by cache
			using var db = GetDataContext(context, o => o.UseDisableQueryCache(true));
			db.Parent.Where(p => GetChildrenFiltered(db, c => c.ChildID != 5).Select(c => c.ParentID).Contains(p.ParentID)).ToList();
		}

		[Test]
		public void TestQueryableCallWithParametersWorkaround([DataSources(TestProvName.AllClickHouse)] string context)
		{
			// baselines could be affected by cache
			using var db = GetDataContext(context, o => o.UseDisableQueryCache(true));
			db.Parent.Where(p => GetChildrenFiltered(db, ChildFilter).Select(c => c.ParentID).Contains(p.ParentID)).ToList();
		}

		[Test]
		public void TestInternationalParamName([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var параметр = 1;
				var result1 = db.Parent.Where(p => p.ParentID == параметр).ToList();

				var 参数 = 1;
				var result2 = db.Parent.Where(p => p.ParentID == 参数).ToList();

				var パラメータ = 1;
				var result3 = db.Parent.Where(p => p.ParentID == パラメータ).ToList();
			}
		}

		[Test]
		public void TestQueryableCallWithParametersWorkaround2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Where(p => GetChildrenFiltered2(db, ChildFilter).Select(c => c.ParentID).Contains(p.ParentID)).ToList();
			}
		}

		[Test]
		public void TestQueryableCallMustFail([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				// we use external parameter p in GetChildrenFiltered parameter expression
				// Sequence 'GetChildrenFiltered(value(Tests.Linq.ParameterTests+<>c__DisplayClass18_0).db, c => (c.ChildID != p.ParentID))' cannot be converted to SQL.
				Assert.Throws<LinqToDBException>(()
					=> db.Parent.Where(p => GetChildrenFiltered(db, c => c.ChildID != p.ParentID).Select(c => c.ParentID).Contains(p.ParentID)).ToList());
			}
		}

		private static bool ChildFilter(Model.Child c) => c.ChildID != 5;

		private static IQueryable<Model.Child> GetChildren(Model.ITestDataContext db)
		{
			return db.Child;
		}

		private static IQueryable<Model.Child> GetChildrenFiltered(Model.ITestDataContext db, Func<Model.Child, bool> filter)
		{
			// looks strange, but it's just to make testcase work
			var list = db.Child.Where(filter).Select(r => r.ChildID).ToList();
			return db.Child.Where(c => list.Contains(c.ChildID));
		}

		private static IQueryable<Model.Child> GetChildrenFiltered2(Model.ITestDataContext db, Func<Model.Child, bool> filter)
		{
			var list = db.Child.ToList();
			return db.Child.Where(c => list.Where(filter).Select(r => r.ChildID).Contains(c.ChildID));
		}

		enum Issue404
		{
			Value1,
			Value2,
		}

		[Table]
		sealed class Table404One
		{
			[Column] public int Id { get; set; }

			public static readonly Table404One[] Data = new[]
			{
				new Table404One() { Id = 1 },
				new Table404One() { Id = 2 }
			};
		}

		[Table]
		sealed class Table404Two
		{
			[Column] public int Id { get; set; }

			[Column] public Issue404 Usage { get; set; }

			[Column] public int FirstTableId { get; set; }

			public static readonly Table404Two[] Data = new[]
			{
				new Table404Two() { Id = 1, Usage = Issue404.Value1, FirstTableId = 1 },
				new Table404Two() { Id = 2, Usage = Issue404.Value1, FirstTableId = 1 },
				new Table404Two() { Id = 3, Usage = Issue404.Value2, FirstTableId = 1 },
				new Table404Two() { Id = 4, Usage = Issue404.Value1, FirstTableId = 2 },
				new Table404Two() { Id = 5, Usage = Issue404.Value2, FirstTableId = 2 },
				new Table404Two() { Id = 6, Usage = Issue404.Value2, FirstTableId = 2 },
			};
		}

		sealed class FirstTable
		{
			public int Id;
			public List<Table404Two>? Values;
		}

		[Test]
		public void Issue404Test([DataSources(TestProvName.AllSybase)] string context)
		{
			// executed twice to test issue #2174
			Execute();
			Execute();

			void Execute()
			{
				using (var db = GetDataContext(context))
				using (var t1 = db.CreateLocalTable(Table404One.Data))
				using (var t2 = db.CreateLocalTable(Table404Two.Data))
				{
					Issue404? usage = null;
					var allUsages = !usage.HasValue;
					var res1 = Test()!;
					using (Assert.EnterMultipleScope())
					{
						Assert.That(res1.Id, Is.EqualTo(1));
						Assert.That(res1.Values!, Has.Count.EqualTo(3));
						Assert.That(res1.Values!.Where(v => v.FirstTableId == 1).Count(), Is.EqualTo(3));
					}

					usage = Issue404.Value1;
					allUsages = false;
					var res2 = Test()!;
					using (Assert.EnterMultipleScope())
					{
						Assert.That(res2.Id, Is.EqualTo(1));
						Assert.That(res2.Values!, Has.Count.EqualTo(2));
						Assert.That(res2.Values!.Where(v => v.Usage == usage).Count(), Is.EqualTo(2));
						Assert.That(res2.Values!.Where(v => v.FirstTableId == 1).Count(), Is.EqualTo(2));
					}

					usage = Issue404.Value2;
					allUsages = false;
					var res3 = Test()!;
					using (Assert.EnterMultipleScope())
					{
						Assert.That(res2.Id, Is.EqualTo(1));
						Assert.That(res3.Values!, Has.Count.EqualTo(1));
						Assert.That(res3.Values!.Where(v => v.Usage == usage).Count(), Is.EqualTo(1));
						Assert.That(res3.Values!.Where(v => v.FirstTableId == 1).Count(), Is.EqualTo(1));
					}

					FirstTable? Test()
					{
						return t1
						  .GroupJoin(t2.Where(v =>
							allUsages || v.Usage == usage.GetValueOrDefault()), c => c.Id, v => v.FirstTableId,
							 (c, v) => new FirstTable { Id = c.Id, Values = v.ToList() })
						  .ToList().OrderBy(_ => _.Id).FirstOrDefault();
					}
				}
			}
		}

		[Table(IsColumnAttributeRequired = true)]
		public partial class Issue1189Customer
		{
			[Column("ID"), PrimaryKey, NotNull] public int Id { get; set; } // integer

			[Column("NAME"), NotNull] public string Name { get; set; } = null!; // varchar(20)

			[ExpressionMethod(nameof(DefaultDateTime), IsColumn = true)]
			public DateTime? ToDelete { get; set; }

			static Expression<Func<Issue1189Customer, DateTime>> DefaultDateTime()
			{
				return p => Sql.AsSql(TestData.DateTime);
			}
		}

		[Test]
		public void Issue1189Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<Issue1189Customer>())
			{
				table.Where(k => k.ToDelete <= TestData.NonReadonlyDateTime).ToList();
			}
		}

		[Table]
		sealed class TestEqualsTable1
		{
			[Column]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(TestEqualsTable2.FK), CanBeNull = true)]
			public IQueryable<TestEqualsTable2> Relation { get; } = null!;
		}

		[Table]
		sealed class TestEqualsTable2
		{
			[Column]
			public int Id { get; set; }

			[Column]
			public int? FK { get; set; }
		}

		[Test]
		public void TestParameterInEquals([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (var table1 = db.CreateLocalTable<TestEqualsTable1>())
			using (var table2 = db.CreateLocalTable<TestEqualsTable2>())
			{
				int? param = null;
				table1
				.Where(_ => _.Relation
					.Select(__ => __.Id)
					.Any(__ => __.Equals(param)))
				.ToList();
			}
		}

		IQueryable<Person> GetPersons(ITestDataContext db, int personId)
		{
			return db.Person.Where(p => p.ID == personId);
		}

		IQueryable<Person> GetPersons2(ITestDataContext db, int? personId)
		{
			return db.Person.Where(p => p.ID == personId!.Value);
		}

		[Test]
		public void TestParametersByEquality([DataSources(TestProvName.AllSQLite)] string context, [Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);

			// Tho identical for translator query, but they are different by parameteter values
			{
				int personId = 1;

				var ctn = new { personId = 1 };

				var query =
					from p in GetPersons(db, personId)
					from p2 in GetPersons2(db, personId).Where(p2 => p2.ID == p.ID)
					where p.ID == ctn.personId
					select new { p, p2 };

				var cacheMiss = query.GetCacheMissCount();

				query.ToList().Count.ShouldBe(1);

				if (iteration > 1)
					query.GetCacheMissCount().ShouldBe(cacheMiss);

				var parameters = new List<SqlParameter>();
				query.GetSelectQuery().CollectParameters(parameters);
				parameters.Distinct().Count().ShouldBe(2);
			}

			{
				int personId = 1;

				var ctn = new { personId = 2 };

				var query =
					from p in GetPersons(db, personId)
					from p2 in GetPersons2(db, personId).Where(p2 => p2.ID == p.ID)
					where p.ID == ctn.personId
					select new { p, p2 };

				var cacheMiss = query.GetCacheMissCount();

				query.ToList().Count.ShouldBe(0);

				if (iteration > 1)
					query.GetCacheMissCount().ShouldBe(cacheMiss);

				var parameters = new List<SqlParameter>();
				query.GetSelectQuery().CollectParameters(parameters);
				parameters.Distinct().Count().ShouldBe(2);
			}
		}

		[Table]
		public class ParameterDeduplication
		{
			[PrimaryKey                          ] public int     Id      { get; set; }
			[Column                              ] public int     Int1    { get; set; }
			[Column                              ] public int     Int2    { get; set; }
			[Column                              ] public int?    IntN1   { get; set; }
			[Column                              ] public int?    IntN2   { get; set; }
			[Column(DataType = DataType.VarChar) ] public string? String1 { get; set; }
			[Column(DataType = DataType.NVarChar)] public string? String2 { get; set; }
			[Column(DataType = DataType.NVarChar)] public string? String3 { get; set; }

			public static readonly ParameterDeduplication[] UpdateData = new[]
			{
				new ParameterDeduplication() { Id = 1 },
				new ParameterDeduplication() { Id = 2 },
			};
		}

		[Test]
		public void ParameterDeduplication_Insert([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable<ParameterDeduplication>())
			{
				var id    = 1;
				var int1  = 2;
				var int2  = 2;
				var intN1 = 2;
				var intN2 = 2;
				var str1  = "str";
				var str2  = "str";
				var str3  = "str";

				table.Insert(() => new ParameterDeduplication()
				{
					Id      = id,
					Int1    = int1,
					Int2    = int2,
					IntN1   = intN1,
					IntN2   = intN2,
					String1 = str1,
					String2 = str2,
					String3 = str3,
				});

				var cacheMiss = table.GetCacheMissCount();
				var sql       = db.LastQuery!;

				sql.ShouldContain("@id");
				sql.ShouldContain("@int1");
				sql.ShouldContain("@int2");
				sql.ShouldContain("@intN1");
				sql.ShouldContain("@intN2");
				sql.ShouldContain("@str1");
				sql.ShouldContain("@str2");
				sql.ShouldContain("@str3");

				id    = 2;
				int1  = 3;
				int2  = 4;
				intN1 = 5;
				intN2 = 6;
				str1  = "str1";
				str2  = "str2";
				str3  = "str3";

				table.Insert(() => new ParameterDeduplication()
				{
					Id      = id,
					Int1    = int1,
					Int2    = int2,
					IntN1   = intN1,
					IntN2   = intN2,
					String1 = str1,
					String2 = str2,
					String3 = str3,
				});

				table.GetCacheMissCount().ShouldBe(cacheMiss);
				sql = db.LastQuery!;

				sql.ShouldContain("@id");
				sql.ShouldContain("@int1");
				sql.ShouldContain("@int2");
				sql.ShouldContain("@intN1");
				sql.ShouldContain("@intN2");
				sql.ShouldContain("@str1");
				sql.ShouldContain("@str2");
				sql.ShouldContain("@str3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Length.ShouldBe(2);

				res[0].Id.ShouldBe(1);
				res[0].Int1.ShouldBe(2);
				res[0].Int2.ShouldBe(2);
				res[0].IntN1.ShouldBe(2);
				res[0].IntN2.ShouldBe(2);
				res[0].String1.ShouldBe("str");
				res[0].String2.ShouldBe("str");
				res[0].String3.ShouldBe("str");

				res[1].Id.ShouldBe(2);
				res[1].Int1.ShouldBe(3);
				res[1].Int2.ShouldBe(4);
				res[1].IntN1.ShouldBe(5);
				res[1].IntN2.ShouldBe(6);
				res[1].String1.ShouldBe("str1");
				res[1].String2.ShouldBe("str2");
				res[1].String3.ShouldBe("str3");
			}
		}

		[Test]
		public void ParameterDeduplication_InsertObject([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable<ParameterDeduplication>())
			{
				db.Insert(new ParameterDeduplication()
				{
					Id      = 1,
					Int1    = 2,
					Int2    = 2,
					IntN1   = 2,
					IntN2   = 2,
					String1 = "str",
					String2 = "str",
					String3 = "str",
				});

				var cacheMiss = table.GetCacheMissCount();
				var sql       = db.LastQuery!;

				sql.ShouldContain("@Id");
				sql.ShouldContain("@Int1");
				sql.ShouldContain("@Int2");
				sql.ShouldContain("@IntN1");
				sql.ShouldContain("@IntN2");
				sql.ShouldContain("@String1");
				sql.ShouldContain("@String2");
				sql.ShouldContain("@String3");

				db.Insert(new ParameterDeduplication()
				{
					Id      = 2,
					Int1    = 3,
					Int2    = 4,
					IntN1   = 5,
					IntN2   = 6,
					String1 = "str1",
					String2 = "str2",
					String3 = "str3",
				});

				table.GetCacheMissCount().ShouldBe(cacheMiss);
				sql = db.LastQuery!;

				sql.ShouldContain("@Id");
				sql.ShouldContain("@Int1");
				sql.ShouldContain("@Int2");
				sql.ShouldContain("@IntN1");
				sql.ShouldContain("@IntN2");
				sql.ShouldContain("@String1");
				sql.ShouldContain("@String2");
				sql.ShouldContain("@String3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Length.ShouldBe(2);

				res[0].Id.ShouldBe(1);
				res[0].Int1.ShouldBe(2);
				res[0].Int2.ShouldBe(2);
				res[0].IntN1.ShouldBe(2);
				res[0].IntN2.ShouldBe(2);
				res[0].String1.ShouldBe("str");
				res[0].String2.ShouldBe("str");
				res[0].String3.ShouldBe("str");

				res[1].Id.ShouldBe(2);
				res[1].Int1.ShouldBe(3);
				res[1].Int2.ShouldBe(4);
				res[1].IntN1.ShouldBe(5);
				res[1].IntN2.ShouldBe(6);
				res[1].String1.ShouldBe("str1");
				res[1].String2.ShouldBe("str2");
				res[1].String3.ShouldBe("str3");
			}
		}

		[Test]
		public void ParameterDeduplication_ValueValue([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable<ParameterDeduplication>())
			{
				table
					.Value(_ => _.Id     , 1)
					.Value(_ => _.Int1   , 2)
					.Value(_ => _.Int2   , 2)
					.Value(_ => _.IntN1  , 2)
					.Value(_ => _.IntN2  , 2)
					.Value(_ => _.String1, "str")
					.Value(_ => _.String2, "str")
					.Value(_ => _.String3, "str")
					.Insert();

				var cacheMiss = table.GetCacheMissCount();
				var sql       = db.LastQuery!;

				sql.ShouldContain("@Id");
				sql.ShouldContain("@Int1");
				sql.ShouldContain("@Int2");
				sql.ShouldContain("@IntN1");
				sql.ShouldContain("@IntN2");
				sql.ShouldContain("@String1");
				sql.ShouldContain("@String2");
				sql.ShouldContain("@String3");

				table
					.Value(_ => _.Id     , 2)
					.Value(_ => _.Int1   , 3)
					.Value(_ => _.Int2   , 4)
					.Value(_ => _.IntN1  , 5)
					.Value(_ => _.IntN2  , 6)
					.Value(_ => _.String1, "str1")
					.Value(_ => _.String2, "str2")
					.Value(_ => _.String3, "str3")
					.Insert();

				table.GetCacheMissCount().ShouldBe(cacheMiss);
				sql = db.LastQuery!;

				sql.ShouldContain("@Id");
				sql.ShouldContain("@Int1");
				sql.ShouldContain("@Int2");
				sql.ShouldContain("@IntN1");
				sql.ShouldContain("@IntN2");
				sql.ShouldContain("@String1");
				sql.ShouldContain("@String2");
				sql.ShouldContain("@String3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Length.ShouldBe(2);

				res[0].Id.ShouldBe(1);
				res[0].Int1.ShouldBe(2);
				res[0].Int2.ShouldBe(2);
				res[0].IntN1.ShouldBe(2);
				res[0].IntN2.ShouldBe(2);
				res[0].String1.ShouldBe("str");
				res[0].String2.ShouldBe("str");
				res[0].String3.ShouldBe("str");

				res[1].Id.ShouldBe(2);
				res[1].Int1.ShouldBe(3);
				res[1].Int2.ShouldBe(4);
				res[1].IntN1.ShouldBe(5);
				res[1].IntN2.ShouldBe(6);
				res[1].String1.ShouldBe("str1");
				res[1].String2.ShouldBe("str2");
				res[1].String3.ShouldBe("str3");
			}
		}

		[Test]
		public void ParameterDeduplication_ValueExpr([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable<ParameterDeduplication>())
			{
				var id    = 1;
				var int1  = 2;
				var int2  = 2;
				var intN1 = 2;
				var intN2 = 2;
				var str1  = "str";
				var str2  = "str";
				var str3  = "str";

				table
					.Value(_ => _.Id     , () => id)
					.Value(_ => _.Int1   , () => int1)
					.Value(_ => _.Int2   , () => int2)
					.Value(_ => _.IntN1  , () => intN1)
					.Value(_ => _.IntN2  , () => intN2)
					.Value(_ => _.String1, () => str1)
					.Value(_ => _.String2, () => str2)
					.Value(_ => _.String3, () => str3)
					.Insert();

				var cacheMiss = table.GetCacheMissCount();
				var sql       = db.LastQuery!;

				sql.ShouldContain("@id");
				sql.ShouldContain("@int1");
				sql.ShouldContain("@int2");
				sql.ShouldContain("@intN1");
				sql.ShouldContain("@intN2");
				sql.ShouldContain("@str1");
				sql.ShouldContain("@str2");
				sql.ShouldContain("@str3");

				id    = 2;
				int1  = 3;
				int2  = 4;
				intN1 = 5;
				intN2 = 6;
				str1  = "str1";
				str2  = "str2";
				str3  = "str3";

				table
					.Value(_ => _.Id, () => id)
					.Value(_ => _.Int1, () => int1)
					.Value(_ => _.Int2, () => int2)
					.Value(_ => _.IntN1, () => intN1)
					.Value(_ => _.IntN2, () => intN2)
					.Value(_ => _.String1, () => str1)
					.Value(_ => _.String2, () => str2)
					.Value(_ => _.String3, () => str3)
					.Insert();

				table.GetCacheMissCount().ShouldBe(cacheMiss);

				sql = db.LastQuery!;

				sql.ShouldContain("@id");
				sql.ShouldContain("@int1");
				sql.ShouldContain("@int2");
				sql.ShouldContain("@intN1");
				sql.ShouldContain("@intN2");
				sql.ShouldContain("@str1");
				sql.ShouldContain("@str2");
				sql.ShouldContain("@str3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Length.ShouldBe(2);

				res[0].Id.ShouldBe(1);
				res[0].Int1.ShouldBe(2);
				res[0].Int2.ShouldBe(2);
				res[0].IntN1.ShouldBe(2);
				res[0].IntN2.ShouldBe(2);
				res[0].String1.ShouldBe("str");
				res[0].String2.ShouldBe("str");
				res[0].String3.ShouldBe("str");

				res[1].Id.ShouldBe(2);
				res[1].Int1.ShouldBe(3);
				res[1].Int2.ShouldBe(4);
				res[1].IntN1.ShouldBe(5);
				res[1].IntN2.ShouldBe(6);
				res[1].String1.ShouldBe("str1");
				res[1].String2.ShouldBe("str2");
				res[1].String3.ShouldBe("str3");
			}
		}

		[Test]
		public void ParameterDeduplication_Update([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable(ParameterDeduplication.UpdateData))
			{
				var id    = 1;
				var int1  = 2;
				var int2  = 2;
				var intN1 = 2;
				var intN2 = 2;
				var str1  = "str";
				var str2  = "str";
				var str3  = "str";

				table.Where(_ => _.Id == id)
					.Update(_ => new ParameterDeduplication()
					{
						Int1    = int1,
						Int2    = int2,
						IntN1   = intN1,
						IntN2   = intN2,
						String1 = str1,
						String2 = str2,
						String3 = str3,
					});

				var cacheMiss = table.GetCacheMissCount();
				var sql       = db.LastQuery!;

				sql.ShouldContain("@id");
				sql.ShouldContain("@int1");
				sql.ShouldContain("@int2");
				sql.ShouldContain("@intN1");
				sql.ShouldContain("@intN2");
				sql.ShouldContain("@str1");
				sql.ShouldContain("@str2");
				sql.ShouldContain("@str3");

				id    = 2;
				int1  = 3;
				int2  = 4;
				intN1 = 5;
				intN2 = 6;
				str1  = "str1";
				str2  = "str2";
				str3  = "str3";

				table.Where(_ => _.Id == id)
					.Update(_ => new ParameterDeduplication()
					{
						Int1 = int1,
						Int2 = int2,
						IntN1 = intN1,
						IntN2 = intN2,
						String1 = str1,
						String2 = str2,
						String3 = str3,
					});

				table.GetCacheMissCount().ShouldBe(cacheMiss);
				sql = db.LastQuery!;

				sql.ShouldContain("@id");
				sql.ShouldContain("@int1");
				sql.ShouldContain("@int2");
				sql.ShouldContain("@intN1");
				sql.ShouldContain("@intN2");
				sql.ShouldContain("@str1");
				sql.ShouldContain("@str2");
				sql.ShouldContain("@str3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Length.ShouldBe(2);

				res[0].Id.ShouldBe(1);
				res[0].Int1.ShouldBe(2);
				res[0].Int2.ShouldBe(2);
				res[0].IntN1.ShouldBe(2);
				res[0].IntN2.ShouldBe(2);
				res[0].String1.ShouldBe("str");
				res[0].String2.ShouldBe("str");
				res[0].String3.ShouldBe("str");

				res[1].Id.ShouldBe(2);
				res[1].Int1.ShouldBe(3);
				res[1].Int2.ShouldBe(4);
				res[1].IntN1.ShouldBe(5);
				res[1].IntN2.ShouldBe(6);
				res[1].String1.ShouldBe("str1");
				res[1].String2.ShouldBe("str2");
				res[1].String3.ShouldBe("str3");
			}
		}

		[Test]
		public void ParameterDeduplication_UpdateObject([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable(ParameterDeduplication.UpdateData))
			{
				db.Update(new ParameterDeduplication()
				{
					Id      = 1,
					Int1    = 2,
					Int2    = 2,
					IntN1   = 2,
					IntN2   = 2,
					String1 = "str",
					String2 = "str",
					String3 = "str",
				});

				var cacheMiss = table.GetCacheMissCount();
				var sql       = db.LastQuery!;

				sql.ShouldContain("@Id");
				sql.ShouldContain("@Int1");
				sql.ShouldContain("@Int2");
				sql.ShouldContain("@IntN1");
				sql.ShouldContain("@IntN2");
				sql.ShouldContain("@String1");
				sql.ShouldContain("@String2");
				sql.ShouldContain("@String3");

				db.Update(new ParameterDeduplication()
				{
					Id      = 2,
					Int1    = 3,
					Int2    = 4,
					IntN1   = 5,
					IntN2   = 6,
					String1 = "str1",
					String2 = "str2",
					String3 = "str3",
				});

				table.GetCacheMissCount().ShouldBe(cacheMiss);
				sql = db.LastQuery!;

				sql.ShouldContain("@Id");
				sql.ShouldContain("@Int1");
				sql.ShouldContain("@Int2");
				sql.ShouldContain("@IntN1");
				sql.ShouldContain("@IntN2");
				sql.ShouldContain("@String1");
				sql.ShouldContain("@String2");
				sql.ShouldContain("@String3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Length.ShouldBe(2);

				res[0].Id.ShouldBe(1);
				res[0].Int1.ShouldBe(2);
				res[0].Int2.ShouldBe(2);
				res[0].IntN1.ShouldBe(2);
				res[0].IntN2.ShouldBe(2);
				res[0].String1.ShouldBe("str");
				res[0].String2.ShouldBe("str");
				res[0].String3.ShouldBe("str");

				res[1].Id.ShouldBe(2);
				res[1].Int1.ShouldBe(3);
				res[1].Int2.ShouldBe(4);
				res[1].IntN1.ShouldBe(5);
				res[1].IntN2.ShouldBe(6);
				res[1].String1.ShouldBe("str1");
				res[1].String2.ShouldBe("str2");
				res[1].String3.ShouldBe("str3");
			}
		}

		[Test]
		public void ParameterDeduplication_SetValue([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable(ParameterDeduplication.UpdateData))
			{
				var id = 1;
				table.Where(_ => _.Id == id)
					.Set(_ => _.Int1   , 2)
					.Set(_ => _.Int2   , 2)
					.Set(_ => _.IntN1  , 2)
					.Set(_ => _.IntN2  , 2)
					.Set(_ => _.String1, "str")
					.Set(_ => _.String2, "str")
					.Set(_ => _.String3, "str")
					.Update();

				var cacheMiss = table.GetCacheMissCount();
				var sql       = db.LastQuery!;

				sql.ShouldContain("@id");
				sql.ShouldContain("@Int1");
				sql.ShouldContain("@Int2");
				sql.ShouldContain("@IntN1");
				sql.ShouldContain("@IntN2");
				sql.ShouldContain("@String1");
				sql.ShouldContain("@String2");
				sql.ShouldContain("@String3");

				id = 2;
				table.Where(_ => _.Id == id)
					.Set(_ => _.Int1   , 3)
					.Set(_ => _.Int2   , 4)
					.Set(_ => _.IntN1  , 5)
					.Set(_ => _.IntN2  , 6)
					.Set(_ => _.String1, "str1")
					.Set(_ => _.String2, "str2")
					.Set(_ => _.String3, "str3")
					.Update();

				table.GetCacheMissCount().ShouldBe(cacheMiss);
				sql = db.LastQuery!;

				sql.ShouldContain("@id");
				sql.ShouldContain("@Int1");
				sql.ShouldContain("@Int2");
				sql.ShouldContain("@IntN1");
				sql.ShouldContain("@IntN2");
				sql.ShouldContain("@String1");
				sql.ShouldContain("@String2");
				sql.ShouldContain("@String3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Length.ShouldBe(2);

				res[0].Id.ShouldBe(1);
				res[0].Int1.ShouldBe(2);
				res[0].Int2.ShouldBe(2);
				res[0].IntN1.ShouldBe(2);
				res[0].IntN2.ShouldBe(2);
				res[0].String1.ShouldBe("str");
				res[0].String2.ShouldBe("str");
				res[0].String3.ShouldBe("str");

				res[1].Id.ShouldBe(2);
				res[1].Int1.ShouldBe(3);
				res[1].Int2.ShouldBe(4);
				res[1].IntN1.ShouldBe(5);
				res[1].IntN2.ShouldBe(6);
				res[1].String1.ShouldBe("str1");
				res[1].String2.ShouldBe("str2");
				res[1].String3.ShouldBe("str3");
			}
		}

		[Test]
		public void ParameterDeduplication_SetExpr([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = (DataConnection)GetDataContext(context))
			using (var table = db.CreateLocalTable(ParameterDeduplication.UpdateData))
			{
				var id    = 1;
				var int1  = 2;
				var int2  = 2;
				var intN1 = 2;
				var intN2 = 2;
				var str1  = "str";
				var str2  = "str";
				var str3  = "str";

				table.Where(_ => _.Id == id)
					.Set(_ => _.Int1   , () => int1)
					.Set(_ => _.Int2   , () => int2)
					.Set(_ => _.IntN1  , () => intN1)
					.Set(_ => _.IntN2  , () => intN2)
					.Set(_ => _.String1, () => str1)
					.Set(_ => _.String2, () => str2)
					.Set(_ => _.String3, () => str3)
					.Update();

				var cacheMiss = table.GetCacheMissCount();
				var sql       = db.LastQuery!;

				sql.ShouldContain("@id");
				sql.ShouldContain("@int1");
				sql.ShouldContain("@int2");
				sql.ShouldContain("@intN1");
				sql.ShouldContain("@intN2");
				sql.ShouldContain("@str1");
				sql.ShouldContain("@str2");
				sql.ShouldContain("@str3");

				id    = 2;
				int1  = 3;
				int2  = 4;
				intN1 = 5;
				intN2 = 6;
				str1  = "str1";
				str2  = "str2";
				str3  = "str3";

				table.Where(_ => _.Id == id)
					.Set(_ => _.Int1, () => int1)
					.Set(_ => _.Int2, () => int2)
					.Set(_ => _.IntN1, () => intN1)
					.Set(_ => _.IntN2, () => intN2)
					.Set(_ => _.String1, () => str1)
					.Set(_ => _.String2, () => str2)
					.Set(_ => _.String3, () => str3)
					.Update();

				table.GetCacheMissCount().ShouldBe(cacheMiss);

				sql = db.LastQuery!;

				sql.ShouldContain("@id");
				sql.ShouldContain("@int1");
				sql.ShouldContain("@int2");
				sql.ShouldContain("@intN1");
				sql.ShouldContain("@intN2");
				sql.ShouldContain("@str1");
				sql.ShouldContain("@str2");
				sql.ShouldContain("@str3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Length.ShouldBe(2);

				res[0].Id.ShouldBe(1);
				res[0].Int1.ShouldBe(2);
				res[0].Int2.ShouldBe(2);
				res[0].IntN1.ShouldBe(2);
				res[0].IntN2.ShouldBe(2);
				res[0].String1.ShouldBe("str");
				res[0].String2.ShouldBe("str");
				res[0].String3.ShouldBe("str");

				res[1].Id.ShouldBe(2);
				res[1].Int1.ShouldBe(3);
				res[1].Int2.ShouldBe(4);
				res[1].IntN1.ShouldBe(5);
				res[1].IntN2.ShouldBe(6);
				res[1].String1.ShouldBe("str1");
				res[1].String2.ShouldBe("str2");
				res[1].String3.ShouldBe("str3");
			}
		}

		private int _cnt;
		private int _cnt1;
		private int _cnt2;
		private int _cnt3;
		private int _param;

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3450")]
		public void TestIQueryableParameterEvaluation([DataSources(TestProvName.AllClickHouse)] string context)
		{
			// cached queries affect cnt values due to extra comparisons in cache
			LinqToDB.Internal.Linq.Query.ClearCaches();

			using (var db = GetDataContext(context))
			{
				_cnt1       = 0;
				_cnt2       = 0;
				_cnt3       = 0;
				_param      = 1;
				var persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(persons[0].ID, Is.EqualTo(1));
					Assert.That(_cnt1, Is.EqualTo(1));
					Assert.That(_cnt2, Is.EqualTo(1));
					Assert.That(_cnt3, Is.EqualTo(1));
				}

				_cnt1   = 0;
				_cnt2   = 0;
				_cnt3   = 0;
				_param  = 2;
				persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(persons.Count(_ => _.ID == 1), Is.EqualTo(1));
					Assert.That(persons.Count(_ => _.ID == 2), Is.EqualTo(1));
					Assert.That(persons.Count(_ => _.ID == 4), Is.EqualTo(1));
					Assert.That(_cnt1, Is.EqualTo(1));
					Assert.That(_cnt2, Is.EqualTo(1));
					Assert.That(_cnt3, Is.EqualTo(1));
				}

				_cnt1   = 0;
				_cnt2   = 0;
				_cnt3   = 0;
				_param  = 3;
				persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(persons.Count(_ => _.ID == 2), Is.EqualTo(1));
					Assert.That(persons.Count(_ => _.ID == 3), Is.EqualTo(1));
					Assert.That(_cnt1, Is.EqualTo(1));
					Assert.That(_cnt2, Is.EqualTo(1));
					Assert.That(_cnt3, Is.EqualTo(1));
				}

				_cnt1   = 0;
				_cnt2   = 0;
				_cnt3   = 0;
				_param  = 1;
				persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(1));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(persons[0].ID, Is.EqualTo(1));
					Assert.That(_cnt1, Is.EqualTo(1));
					Assert.That(_cnt2, Is.EqualTo(1));
					Assert.That(_cnt3, Is.EqualTo(1));
				}

				_cnt1   = 0;
				_cnt2   = 0;
				_cnt3   = 0;
				_param  = 3;
				persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(2));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(persons.Count(_ => _.ID == 2), Is.EqualTo(1));
					Assert.That(persons.Count(_ => _.ID == 3), Is.EqualTo(1));
					Assert.That(_cnt1, Is.EqualTo(1));
					Assert.That(_cnt2, Is.EqualTo(1));
					Assert.That(_cnt3, Is.EqualTo(1));
				}

				_cnt1   = 0;
				_cnt2   = 0;
				_cnt3   = 0;
				_param  = 2;
				persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(persons.Count(_ => _.ID == 1), Is.EqualTo(1));
					Assert.That(persons.Count(_ => _.ID == 2), Is.EqualTo(1));
					Assert.That(persons.Count(_ => _.ID == 4), Is.EqualTo(1));
					Assert.That(_cnt1, Is.EqualTo(1));
					Assert.That(_cnt2, Is.EqualTo(1));
					Assert.That(_cnt3, Is.EqualTo(1));
				}
			}

			List<Person> Query(ITestDataContext db)
			{
				return db.Person
					.Where(_ => 
					 GetQuery1(db).Select(p => p.ID).Contains(_.ID) &&
					(GetQuery2(db).Select(p => p.ID).Contains(_.ID) ||
					 GetQuery3(db).Select(p => p.ID).Contains(_.ID)))
					.ToList();
			}
		}

		private IQueryable<Person> GetQuery1(ITestDataContext db)
		{
			_cnt1++;
			var paramCopy = _param;
			if (paramCopy == 1)
				return db.Person.Where(p => p.ID == paramCopy);

			return db.Person.Where(p => paramCopy + 1 != p.ID);
		}

		private IQueryable<Person> GetQuery2(ITestDataContext db)
		{
			_cnt2++;
			var paramCopy = _param;
			if (paramCopy == 2)
				return db.Person.Where(p => paramCopy == p.ID);

			return db.Person.Where(p => p.ID == paramCopy - 1);
		}

		private IQueryable<Person> GetQuery3(ITestDataContext db)
		{
			_cnt3++;
			var paramCopy = _param;
			if (paramCopy == 3)
				return db.Person.Where(p => p.ID == paramCopy);

			return db.Person.Where(p => paramCopy + 1 != p.ID);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3450")]
		public void TestIQueryableParameterEvaluationCaching([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			{
				_cnt1       = 0;
				_param      = 1;
				var persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(1));
				Assert.That(persons[0].ID, Is.EqualTo(1));
				//Assert.AreEqual(1, _cnt1);

				_cnt1   = 0;
				_param  = 2;
				persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(1));
				Assert.That(persons[0].ID, Is.EqualTo(2));
				//Assert.AreEqual(1, _cnt1);

				_cnt1   = 0;
				_param  = 3;
				persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(1));
				Assert.That(persons[0].ID, Is.EqualTo(3));
				//Assert.AreEqual(1, _cnt1);

				_cnt1   = 0;
				_param  = 4;
				persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(1));
				Assert.That(persons[0].ID, Is.EqualTo(4));
				//Assert.AreEqual(1, _cnt1);

				_cnt1   = 0;
				_param  = 1;
				persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(1));
				Assert.That(persons[0].ID, Is.EqualTo(1));
				//Assert.AreEqual(1, _cnt1);
			}

			List<Person> Query(ITestDataContext db)
			{
				return db.Person
					.Where(_ => GetQuery4(db).Select(p => p.ID).Contains(_.ID))
					.ToList();
			}
		}

		private IQueryable<Person> GetQuery4(ITestDataContext db)
		{
			_cnt1++;
			var paramCopy = _param;
			return db.Person.Where(p => p.ID == paramCopy);
		}

		private int[] _params = new int[30];

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3450")]
		public void TestIQueryableParameterEvaluationMultiThreaded([IncludeDataSources(true, TestProvName.AllSqlServer)] string context)
		{
			using var _ = new DisableBaseline("multi-threading");

			var tasks = new Task[30];

			for (var i = 0; i < tasks.Length; i++)
			{
				var thread = i;
				tasks[i] = Task.Run(() => TestRunner(context, thread));
			}

			Task.WaitAll(tasks);
		}

		private void TestRunner(string context, int thread)
		{
			// don't use Assert.Multiple in multi-threading tests
#pragma warning disable NUnit2045 // Use Assert.Multiple
			using var db = GetDataContext(context);
			_params[thread] = 1;
			var persons = Query(db, thread);

			Assert.That(persons, Has.Count.EqualTo(1));
			Assert.That(persons[0].ID, Is.EqualTo(1));

			_params[thread] = 2;
			persons = Query(db, thread);

			Assert.That(persons, Has.Count.EqualTo(3));
			Assert.That(persons.Count(_ => _.ID == 1), Is.EqualTo(1));
			Assert.That(persons.Count(_ => _.ID == 2), Is.EqualTo(1));
			Assert.That(persons.Count(_ => _.ID == 4), Is.EqualTo(1));

			_params[thread] = 3;
			persons = Query(db, thread);

			Assert.That(persons, Has.Count.EqualTo(2));
			Assert.That(persons.Count(_ => _.ID == 2), Is.EqualTo(1));
			Assert.That(persons.Count(_ => _.ID == 3), Is.EqualTo(1));

			_params[thread] = 1;
			persons = Query(db, thread);

			Assert.That(persons, Has.Count.EqualTo(1));
			Assert.That(persons[0].ID, Is.EqualTo(1));

			_params[thread] = 3;
			persons = Query(db, thread);

			Assert.That(persons, Has.Count.EqualTo(2));
			Assert.That(persons.Count(_ => _.ID == 2), Is.EqualTo(1));
			Assert.That(persons.Count(_ => _.ID == 3), Is.EqualTo(1));

			_params[thread] = 2;
			persons = Query(db, thread);

			Assert.That(persons, Has.Count.EqualTo(3));
			Assert.That(persons.Count(_ => _.ID == 1), Is.EqualTo(1));
			Assert.That(persons.Count(_ => _.ID == 2), Is.EqualTo(1));
			Assert.That(persons.Count(_ => _.ID == 4), Is.EqualTo(1));

			List<Person> Query(ITestDataContext db, int thread)
			{
				return db.Person
					.Where(_ =>
					 GetQueryT1(db, thread).Select(p => p.ID).Contains(_.ID) &&
					(GetQueryT2(db, thread).Select(p => p.ID).Contains(_.ID) ||
					 GetQueryT3(db, thread).Select(p => p.ID).Contains(_.ID)))
					.ToList();
			}
#pragma warning restore NUnit2045 // Use Assert.Multiple
		}

		private IQueryable<Person> GetQueryT1(ITestDataContext db, int thread)
		{
			_cnt1++;
			var paramCopy = _params[thread];
			if (paramCopy == 1)
				return db.Person.Where(p => p.ID == paramCopy);

			return db.Person.Where(p => paramCopy + 1 != p.ID);
		}

		private IQueryable<Person> GetQueryT2(ITestDataContext db, int thread)
		{
			_cnt2++;
			var paramCopy = _params[thread];
			if (paramCopy == 2)
				return db.Person.Where(p => paramCopy == p.ID);

			return db.Person.Where(p => p.ID == paramCopy - 1);
		}

		private IQueryable<Person> GetQueryT3(ITestDataContext db, int thread)
		{
			_cnt3++;
			var paramCopy = _params[thread];
			if (paramCopy == 3)
				return db.Person.Where(p => p.ID == paramCopy);

			return db.Person.Where(p => paramCopy + 1 != p.ID);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3450")]
		public void TestSimpleParameterEvaluation([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				_cnt        = 0;
				_param      = 1;
				var persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(persons.All(p => p.ID != _param), Is.True);
					Assert.That(_cnt, Is.EqualTo(1));
				}

				_cnt    = 0;
				_param  = 2;
				persons = Query(db);

				Assert.That(persons, Has.Count.EqualTo(3));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(persons.All(p => p.ID != _param), Is.True);
					Assert.That(_cnt, Is.EqualTo(1));
				}
			}

			List<Person> Query(ITestDataContext db)
			{
				return db.Person.Where(_ => GetPersonsEnumerable().Contains(_.ID)).ToList();
			}
		}

		private IEnumerable<int> GetPersonsEnumerable()
		{
			_cnt++;
			return new[] { 1, 2, 3, 4 }.Where(_ => _ != _param);
		}

		[Test]
		public void Issue4052([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var data = new Person()
			{
				ID = 1,
			};

			var query1 = (from c in db.Person
						  where c.ID == data.ID
						  && (c.MiddleName != null ? c.MiddleName.Trim().ToLower() : string.Empty) == (data.MiddleName != null ? data.MiddleName.Trim().ToLower() : string.Empty)
						  select c).ToList();
		}

		int GetId(int id, int increment)
		{
			return id + increment;
		}

		/// <summary>
		/// Tests that we do not have cache hit for similar parameters
		/// </summary>
		/// <param name="context"></param>
		[Test]
		public void Caching([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var id = 1;

			var query1  = db.Parent.Where(x => x.ParentID == GetId(id, 0) || x.ParentID == GetId(id, 0));
			AssertQuery(query1);

			// check only one parameter generated
			if(!context.IsAnyOf(TestProvName.AllClickHouse))
				Assert.That(query1.ToSqlQuery().Parameters, Has.Count.EqualTo(1));

			id = 2;

			var query2  = db.Parent.Where(x => x.ParentID == GetId(id, 1) || x.ParentID == GetId(id, 0));
			AssertQuery(query2);

			id = 1;
			query1  = db.Parent.Where(x => x.ParentID == GetId(id, 0) || x.ParentID == GetId(id, 0));
			AssertQuery(query1);

			// check only one parameter generated (1+2+1=4)
			if (!context.IsAnyOf(TestProvName.AllClickHouse))
				Assert.That(query1.ToSqlQuery().Parameters, Has.Count.EqualTo(2));
		}

#if SUPPORTS_DATEONLY
		[Table]
		public sealed class Issue4371Table2
		{
			[Column(DataType = DataType.VarChar)] public DateOnly?       ColumnDO  { get; set; }
		}

		[Test]
		public void Issue4371TestDateOnly([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4371Table2>();

			var dt = TestData.DateOnly;
			db.Insert(new Issue4371Table2() { ColumnDO = dt });

			using var _ = new CultureRegion("fa-IR");
			Assert.That(tb.Where(r => r.ColumnDO == dt).Count(), Is.EqualTo(1));
		}

		[Test]
		public void Issue4371TestDateOnlyCrash([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4371Table2>();

			var dt = DateOnly.FromDateTime(new DateTime(50284592391540000));
			db.Insert(new Issue4371Table2() { ColumnDO = dt });

			using var _ = new CultureRegion("fa-IR");
			Assert.That(tb.Where(r => r.ColumnDO == dt).Count(), Is.EqualTo(1));
		}
#endif

		[Table]
		public sealed class Issue4371Table
		{
			[Column(DataType = DataType.VarChar)] public DateTime?       ColumnDT  { get; set; }
			[Column(DataType = DataType.VarChar)] public DateTimeOffset? ColumnDTO { get; set; }
			[Column(DataType = DataType.VarChar)] public TimeSpan?       ColumnTS  { get; set; }
		}

		[Test]
		public void Issue4371TestDateTimeOffset([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4371Table>();

			var dto = TestData.DateTimeOffset;
			db.Insert(new Issue4371Table() { ColumnDTO = dto });

			using var _ = new CultureRegion("fa-IR");
			Assert.That(tb.Where(r => r.ColumnDTO == dto).Count(), Is.EqualTo(1));
		}

		[Test]
		public void Issue4371TestDateTimeCrash([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4371Table>();

			var dt = new DateTime(50284592391540000);
			db.Insert(new Issue4371Table() { ColumnDT = dt });

			using var _ = new CultureRegion("fa-IR");
			Assert.That(tb.Where(r => r.ColumnDT == dt).Count(), Is.EqualTo(1));
		}

		[Test]
		public void Issue4371TestDateTimeOffsetCrash([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4371Table>();

			var dto = new DateTimeOffset(50284592391540000, default);
			db.Insert(new Issue4371Table() { ColumnDTO = dto });

			using var _ = new CultureRegion("fa-IR");
			Assert.That(tb.Where(r => r.ColumnDTO == dto).Count(), Is.EqualTo(1));
		}

		[Test]
		public void Issue4371TestTimeSpan([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue4371Table>();

			var ts = TestData.TimeOfDay;
			db.Insert(new Issue4371Table() { ColumnTS = ts });

			using var _ = new CultureRegion("fa-IR");
			Assert.That(tb.Where(r => r.ColumnTS == ts).Count(), Is.EqualTo(1));
		}

		[Test]
		public void Issue4359_PrimaryParameterName([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			// primary constructor parameter use reference to backing field with ugly name
			// we use field name to derive parameter name
			var result = new TestParameterNames("John").Test(db);

			Assert.That(result, Is.Not.Null);
			Assert.That(result!.ID, Is.EqualTo(1));
		}

		private class TestParameterNames(string Parameter)
		{
			public Person? Test(ITestDataContext db)
			{
				return db.Person.Where(p => p.FirstName == Parameter).SingleOrDefault();
			}
		}

		sealed class TestBool
		{
			public int Id { get; set; }
			[Column(Configuration = ProviderName.Sybase, CanBeNull = false)]
			public bool? Value { get; set; }
		}

		[Test]
		public void Issue_BooleanNullPreserved([DataSources] string context, [Values] bool inline, [Values] bool? value)
		{
			if (value == null && context.IsAnyOf(TestProvName.AllAccess, TestProvName.AllSybase))
				Assert.Ignore("Database doesn't support NULL as boolean");

			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<TestBool>();

			db.InlineParameters = inline;

			// test parameter
			tb.Insert(() => new TestBool()
			{
				Id = 1,
				Value = !value
			});

			var record = tb.Single();

			Assert.That(record.Value, Is.EqualTo(!value));

			// test field
			tb.Update(r => new TestBool()
			{
				Id = 1,
				Value = !r.Value
			});

			record = tb.Single();

			Assert.That(record.Value, Is.EqualTo(value));

			// disabled temporary due to
			// https://github.com/ClickHouse/ClickHouse/issues/73934
			if (!context.IsAnyOf(TestProvName.AllClickHouse))
			{
				// test parameter in update
				tb.Update(r => new TestBool()
				{
					Id = 1,
					Value = !value
				});

				record = tb.Single();

				Assert.That(record.Value, Is.EqualTo(!value));
			}
		}

		[ActiveIssue]
		[Test]
		public void Issue_NRE_InAccessor([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			Assert.That(db.Select(() => Sql.AsSql(Sql.PadLeft(null, 1, '.'))), Is.Null);
		}

		sealed class IssueDedup
		{
			public int Id { get; set; }
			public bool? Value1 { get; set; }
			public bool? Value2 { get; set; }
			public bool? Value3 { get; set; }
			public bool? Value4 { get; set; }
			public bool? Value5 { get; set; }
		}

		[Test]
		public void DedupOfParameters([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values(1, 2)] int iteration)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable([new IssueDedup()]);

			Test(new Wrap<bool>(true), new Wrap<bool>(false), new Wrap<bool>(true), new Wrap<bool>(false), new Wrap<bool>(false));
			Test(new Wrap<bool>(true), new Wrap<bool>(false), new Wrap<bool>(false), new Wrap<bool>(true), new Wrap<bool>(false));
			Test(new Wrap<bool>(false), new Wrap<bool>(true), new Wrap<bool>(false), new Wrap<bool>(true), new Wrap<bool>(false));
			Test(new Wrap<bool>(true), new Wrap<bool>(true), new Wrap<bool>(false), new Wrap<bool>(true), new Wrap<bool>(true));

			void Test(Wrap<bool> f1, Wrap<bool> f2, Wrap<bool> f3, Wrap<bool> f4, Wrap<bool> f5)
			{
				var cacheMissCount = tb.GetCacheMissCount();

				tb
					.Set(r => r.Value1, r => f1.Value)
					.Set(r => r.Value2, r => f2.Value)
					.Set(r => r.Value3, r => f3.Value)
					.Set(r => r.Value4, r => f4.Value)
					.Set(r => r.Value5, r => f5.Value)
					.Update();

				if (iteration > 1)
				{
					Assert.That(tb.GetCacheMissCount(), Is.EqualTo(cacheMissCount));
				}

				var record = tb.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(record.Value1, Is.EqualTo(f1.Value));
					Assert.That(record.Value2, Is.EqualTo(f2.Value));
					Assert.That(record.Value3, Is.EqualTo(f3.Value));
					Assert.That(record.Value4, Is.EqualTo(f4.Value));
					Assert.That(record.Value5, Is.EqualTo(f5.Value));
				}
			}
		}

		readonly struct Wrap<TValue>
		{
			public Wrap(TValue? value)
			{
				Value = value;
			}

			public TValue? Value { get; }
		}

		[Test]
		public void LambdaParameterTest([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var valueGetter = () => 1;

			AssertQuery(db.Parent.Where(r => r.ParentID == valueGetter()));
		}

		[Test]
		public void LambdaBodyInQuery([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);

			Expression<Func<Parent, int>> valueGetter = p => p.ParentID;

			var query = db.Parent
				.Select(p => (valueGetter.Body as MemberExpression)!.Member.Name);

			AssertQuery(query);
		}

		sealed class Issue4963Table
		{
			public byte Field { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4963")]
		public void Issue4963([DataSources] string context, [Values] bool inline)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(new[] { new Issue4963Table() { Field = 2 } });

			db.InlineParameters = inline;
			var offset = -1;

			tb.Update(r => new Issue4963Table()
			{
				Field = (byte)(r.Field + offset)
			});

			var record = tb.Single();

			Assert.That(record.Field, Is.EqualTo(1));
		}
	}
}
