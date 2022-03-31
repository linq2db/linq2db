﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using Tests.Model;

using NUnit.Framework;

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
				Assert.AreEqual(0, db.Person.Where(_ => _.ID == id).Count());

				id = 1;
				Assert.AreEqual(1, db.Person.Where(_ => _.ID == id).Count());
			}
		}

		[Test]
		public void TestOptimizingParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;
				Assert.AreEqual(1, db.Person.Where(_ => _.ID == id || _.ID <= id || _.ID == id).Count());
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

				Assert.That(query.GetStatement().CollectParameters().Length,        Is.EqualTo(1));
				Assert.That(queryInlined.GetStatement().CollectParameters().Length, Is.EqualTo(0));
			}
		}

		[Test]
		public void CharAsSqlParameter1(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllPostgreSQL,
				TestProvName.AllInformix,
				ProviderName.DB2,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "0 \x0 ' 0";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test]
		public void CharAsSqlParameter2(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllSQLite,
				TestProvName.AllPostgreSQL,
				TestProvName.AllInformix,
				ProviderName.DB2,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x0 \x0 ' \x0";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test]
		public void CharAsSqlParameter3(
			[DataSources(
				ProviderName.SqlCe,
				TestProvName.AllPostgreSQL,
				TestProvName.AllInformix,
				ProviderName.DB2,
				ProviderName.SQLiteMS,
				TestProvName.AllSapHana)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x0";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test]
		public void CharAsSqlParameter4([DataSources] string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = "\x1-\x2-\x3";
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		[Test]
		public void CharAsSqlParameter5(
			[DataSources(
				TestProvName.AllPostgreSQL,
				TestProvName.AllInformix,
				ProviderName.DB2)]
			string context)
		{
			using (var  db = GetDataContext(context))
			{
				var s1 = '\x0';
				var s2 = db.Select(() => Sql.ToSql(s1));

				Assert.That(s2, Is.EqualTo(s1));
			}
		}

		class AllTypes
		{
			public decimal DecimalDataType;
			public byte[]? BinaryDataType;
			public byte[]? VarBinaryDataType;
			[Column(DataType = DataType.VarChar)]
			public string? VarcharDataType;
		}

		// Excluded providers inline such parameter
		[Test]
		public void ExposeSqlDecimalParameter([DataSources(false, ProviderName.DB2, TestProvName.AllInformix)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var p   = 123.456m;
				var sql = db.GetTable<AllTypes>().Where(t => t.DecimalDataType == p).ToString();

				TestContext.WriteLine(sql);

				Assert.That(sql, Contains.Substring("(6, 3)"));
			}
		}

		// DB2: see DB2SqlOptimizer.SetQueryParameter - binary parameters inlined for DB2
		[Test]
		public void ExposeSqlBinaryParameter([DataSources(false, ProviderName.DB2)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var p   = new byte[] { 0, 1, 2 };
				var sql = db.GetTable<AllTypes>().Where(t => t.BinaryDataType == p).ToString();

				TestContext.WriteLine(sql);

				Assert.That(sql, Contains.Substring("(3)").Or.Contains("Blob").Or.Contains("(8000)"));
			}
		}

		[Test]
		public void Test1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var dt = TestData.DateTime;

				if (context.Contains("Informix"))
					dt = new DateTime(dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);

				var _ = db.Types.Where(t => t.DateTimeValue == Sql.ToSql(dt)).ToList();
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
		public void TestQueryableCall([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Where(p => GetChildren(db).Select(c => c.ParentID).Contains(p.ParentID)).ToList();
			}
		}

		[Test]
		public void TestQueryableCallWithParameters([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Where(p => GetChildrenFiltered(db, c => c.ChildID != 5).Select(c => c.ParentID).Contains(p.ParentID)).ToList();
			}
		}

		[Test]
		public void TestQueryableCallWithParametersWorkaround([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Parent.Where(p => GetChildrenFiltered(db, ChildFilter).Select(c => c.ParentID).Contains(p.ParentID)).ToList();
			}
		}

		[ActiveIssue(Configuration = TestProvName.AllSybase, Details = "CI: sybase image needs utf-8 enabled")]
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

		// sequence evaluation fails in GetChildrenFiltered2
		[ActiveIssue("Unable to cast object of type 'System.Linq.Expressions.FieldExpression' to type 'System.Linq.Expressions.LambdaExpression'.")]
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
				Assert.Throws<LinqException>(()
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
		class Table404One
		{
			[Column] public int Id { get; set; }

			public static readonly Table404One[] Data = new[]
			{
				new Table404One() { Id = 1 },
				new Table404One() { Id = 2 }
			};
		}

		[Table]
		class Table404Two
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

		class FirstTable
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
					Assert.AreEqual(1, res1.Id);
					Assert.AreEqual(3, res1.Values!.Count);
					Assert.AreEqual(3, res1.Values.Where(v => v.FirstTableId == 1).Count());

					usage = Issue404.Value1;
					allUsages = false;
					var res2 = Test()!;
					Assert.AreEqual(1, res2.Id);
					Assert.AreEqual(2, res2.Values!.Count);
					Assert.AreEqual(2, res2.Values.Where(v => v.Usage == usage).Count());
					Assert.AreEqual(2, res2.Values.Where(v => v.FirstTableId == 1).Count());

					usage = Issue404.Value2;
					allUsages = false;
					var res3 = Test()!;
					Assert.AreEqual(1, res2.Id);
					Assert.AreEqual(1, res3.Values!.Count);
					Assert.AreEqual(1, res3.Values.Where(v => v.Usage == usage).Count());
					Assert.AreEqual(1, res3.Values.Where(v => v.FirstTableId == 1).Count());

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

		[ActiveIssue("SQL0418N", Configuration = ProviderName.DB2)]
		[Test]
		public void Issue1189Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable<Issue1189Customer>())
			{
				table.Where(k => k.ToDelete <= TestData.DateTime).ToList();
			}
		}

		[Table]
		class TestEqualsTable1
		{
			[Column]
			public int Id { get; set; }

			[Association(ThisKey = nameof(Id), OtherKey = nameof(TestEqualsTable2.FK), CanBeNull = true)]
			public IQueryable<TestEqualsTable2> Relation { get; } = null!;
		}

		[Table]
		class TestEqualsTable2
		{
			[Column]
			public int Id { get; set; }

			[Column]
			public int? FK { get; set; }
		}

		[Test]
		public void TestParameterInEquals([DataSources] string context)
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

				var cacheMiss = Query<ParameterDeduplication>.CacheMissCount;
				var sql       = db.LastQuery!;

				sql.Should().Contain("@id");
				sql.Should().Contain("@int1");
				sql.Should().Contain("@int2");
				sql.Should().Contain("@intN1");
				sql.Should().Contain("@intN2");
				sql.Should().Contain("@str1");
				sql.Should().Contain("@str2");
				sql.Should().Contain("@str3");

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

				Query<ParameterDeduplication>.CacheMissCount.Should().Be(cacheMiss);
				sql = db.LastQuery!;

				sql.Should().Contain("@id");
				sql.Should().Contain("@int1");
				sql.Should().Contain("@int2");
				sql.Should().Contain("@intN1");
				sql.Should().Contain("@intN2");
				sql.Should().Contain("@str1");
				sql.Should().Contain("@str2");
				sql.Should().Contain("@str3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Should().HaveCount(2);

				res[0].Id.Should().Be(1);
				res[0].Int1.Should().Be(2);
				res[0].Int2.Should().Be(2);
				res[0].IntN1.Should().Be(2);
				res[0].IntN2.Should().Be(2);
				res[0].String1.Should().Be("str");
				res[0].String2.Should().Be("str");
				res[0].String3.Should().Be("str");

				res[1].Id.Should().Be(2);
				res[1].Int1.Should().Be(3);
				res[1].Int2.Should().Be(4);
				res[1].IntN1.Should().Be(5);
				res[1].IntN2.Should().Be(6);
				res[1].String1.Should().Be("str1");
				res[1].String2.Should().Be("str2");
				res[1].String3.Should().Be("str3");
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

				var cacheMiss = Query<ParameterDeduplication>.CacheMissCount;
				var sql       = db.LastQuery!;

				sql.Should().Contain("@Id");
				sql.Should().Contain("@Int1");
				sql.Should().Contain("@Int2");
				sql.Should().Contain("@IntN1");
				sql.Should().Contain("@IntN2");
				sql.Should().Contain("@String1");
				sql.Should().Contain("@String2");
				sql.Should().Contain("@String3");

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

				Query<ParameterDeduplication>.CacheMissCount.Should().Be(cacheMiss);
				sql = db.LastQuery!;

				sql.Should().Contain("@Id");
				sql.Should().Contain("@Int1");
				sql.Should().Contain("@Int2");
				sql.Should().Contain("@IntN1");
				sql.Should().Contain("@IntN2");
				sql.Should().Contain("@String1");
				sql.Should().Contain("@String2");
				sql.Should().Contain("@String3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Should().HaveCount(2);

				res[0].Id.Should().Be(1);
				res[0].Int1.Should().Be(2);
				res[0].Int2.Should().Be(2);
				res[0].IntN1.Should().Be(2);
				res[0].IntN2.Should().Be(2);
				res[0].String1.Should().Be("str");
				res[0].String2.Should().Be("str");
				res[0].String3.Should().Be("str");

				res[1].Id.Should().Be(2);
				res[1].Int1.Should().Be(3);
				res[1].Int2.Should().Be(4);
				res[1].IntN1.Should().Be(5);
				res[1].IntN2.Should().Be(6);
				res[1].String1.Should().Be("str1");
				res[1].String2.Should().Be("str2");
				res[1].String3.Should().Be("str3");
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

				var cacheMiss = Query<ParameterDeduplication>.CacheMissCount;
				var sql       = db.LastQuery!;

				sql.Should().Contain("@Id");
				sql.Should().Contain("@Int1");
				sql.Should().Contain("@Int2");
				sql.Should().Contain("@IntN1");
				sql.Should().Contain("@IntN2");
				sql.Should().Contain("@String1");
				sql.Should().Contain("@String2");
				sql.Should().Contain("@String3");

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

				Query<ParameterDeduplication>.CacheMissCount.Should().Be(cacheMiss);
				sql = db.LastQuery!;

				sql.Should().Contain("@Id");
				sql.Should().Contain("@Int1");
				sql.Should().Contain("@Int2");
				sql.Should().Contain("@IntN1");
				sql.Should().Contain("@IntN2");
				sql.Should().Contain("@String1");
				sql.Should().Contain("@String2");
				sql.Should().Contain("@String3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Should().HaveCount(2);

				res[0].Id.Should().Be(1);
				res[0].Int1.Should().Be(2);
				res[0].Int2.Should().Be(2);
				res[0].IntN1.Should().Be(2);
				res[0].IntN2.Should().Be(2);
				res[0].String1.Should().Be("str");
				res[0].String2.Should().Be("str");
				res[0].String3.Should().Be("str");

				res[1].Id.Should().Be(2);
				res[1].Int1.Should().Be(3);
				res[1].Int2.Should().Be(4);
				res[1].IntN1.Should().Be(5);
				res[1].IntN2.Should().Be(6);
				res[1].String1.Should().Be("str1");
				res[1].String2.Should().Be("str2");
				res[1].String3.Should().Be("str3");
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

				var cacheMiss = Query<ParameterDeduplication>.CacheMissCount;
				var sql       = db.LastQuery!;

				sql.Should().Contain("@id");
				sql.Should().Contain("@int1");
				sql.Should().Contain("@int2");
				sql.Should().Contain("@intN1");
				sql.Should().Contain("@intN2");
				sql.Should().Contain("@str1");
				sql.Should().Contain("@str2");
				sql.Should().Contain("@str3");

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

				Query<ParameterDeduplication>.CacheMissCount.Should().Be(cacheMiss);

				sql = db.LastQuery!;

				sql.Should().Contain("@id");
				sql.Should().Contain("@int1");
				sql.Should().Contain("@int2");
				sql.Should().Contain("@intN1");
				sql.Should().Contain("@intN2");
				sql.Should().Contain("@str1");
				sql.Should().Contain("@str2");
				sql.Should().Contain("@str3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Should().HaveCount(2);

				res[0].Id.Should().Be(1);
				res[0].Int1.Should().Be(2);
				res[0].Int2.Should().Be(2);
				res[0].IntN1.Should().Be(2);
				res[0].IntN2.Should().Be(2);
				res[0].String1.Should().Be("str");
				res[0].String2.Should().Be("str");
				res[0].String3.Should().Be("str");

				res[1].Id.Should().Be(2);
				res[1].Int1.Should().Be(3);
				res[1].Int2.Should().Be(4);
				res[1].IntN1.Should().Be(5);
				res[1].IntN2.Should().Be(6);
				res[1].String1.Should().Be("str1");
				res[1].String2.Should().Be("str2");
				res[1].String3.Should().Be("str3");
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

				var cacheMiss = Query<ParameterDeduplication>.CacheMissCount;
				var sql       = db.LastQuery!;

				sql.Should().Contain("@id");
				sql.Should().Contain("@int1");
				sql.Should().Contain("@int2");
				sql.Should().Contain("@intN1");
				sql.Should().Contain("@intN2");
				sql.Should().Contain("@str1");
				sql.Should().Contain("@str2");
				sql.Should().Contain("@str3");

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

				Query<ParameterDeduplication>.CacheMissCount.Should().Be(cacheMiss);
				sql = db.LastQuery!;

				sql.Should().Contain("@id");
				sql.Should().Contain("@int1");
				sql.Should().Contain("@int2");
				sql.Should().Contain("@intN1");
				sql.Should().Contain("@intN2");
				sql.Should().Contain("@str1");
				sql.Should().Contain("@str2");
				sql.Should().Contain("@str3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Should().HaveCount(2);

				res[0].Id.Should().Be(1);
				res[0].Int1.Should().Be(2);
				res[0].Int2.Should().Be(2);
				res[0].IntN1.Should().Be(2);
				res[0].IntN2.Should().Be(2);
				res[0].String1.Should().Be("str");
				res[0].String2.Should().Be("str");
				res[0].String3.Should().Be("str");

				res[1].Id.Should().Be(2);
				res[1].Int1.Should().Be(3);
				res[1].Int2.Should().Be(4);
				res[1].IntN1.Should().Be(5);
				res[1].IntN2.Should().Be(6);
				res[1].String1.Should().Be("str1");
				res[1].String2.Should().Be("str2");
				res[1].String3.Should().Be("str3");
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

				var cacheMiss = Query<ParameterDeduplication>.CacheMissCount;
				var sql       = db.LastQuery!;

				sql.Should().Contain("@Id");
				sql.Should().Contain("@Int1");
				sql.Should().Contain("@Int2");
				sql.Should().Contain("@IntN1");
				sql.Should().Contain("@IntN2");
				sql.Should().Contain("@String1");
				sql.Should().Contain("@String2");
				sql.Should().Contain("@String3");

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

				Query<ParameterDeduplication>.CacheMissCount.Should().Be(cacheMiss);
				sql = db.LastQuery!;

				sql.Should().Contain("@Id");
				sql.Should().Contain("@Int1");
				sql.Should().Contain("@Int2");
				sql.Should().Contain("@IntN1");
				sql.Should().Contain("@IntN2");
				sql.Should().Contain("@String1");
				sql.Should().Contain("@String2");
				sql.Should().Contain("@String3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Should().HaveCount(2);

				res[0].Id.Should().Be(1);
				res[0].Int1.Should().Be(2);
				res[0].Int2.Should().Be(2);
				res[0].IntN1.Should().Be(2);
				res[0].IntN2.Should().Be(2);
				res[0].String1.Should().Be("str");
				res[0].String2.Should().Be("str");
				res[0].String3.Should().Be("str");

				res[1].Id.Should().Be(2);
				res[1].Int1.Should().Be(3);
				res[1].Int2.Should().Be(4);
				res[1].IntN1.Should().Be(5);
				res[1].IntN2.Should().Be(6);
				res[1].String1.Should().Be("str1");
				res[1].String2.Should().Be("str2");
				res[1].String3.Should().Be("str3");
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

				var cacheMiss = Query<ParameterDeduplication>.CacheMissCount;
				var sql       = db.LastQuery!;

				sql.Should().Contain("@id");
				sql.Should().Contain("@Int1");
				sql.Should().Contain("@Int2");
				sql.Should().Contain("@IntN1");
				sql.Should().Contain("@IntN2");
				sql.Should().Contain("@String1");
				sql.Should().Contain("@String2");
				sql.Should().Contain("@String3");

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

				Query<ParameterDeduplication>.CacheMissCount.Should().Be(cacheMiss);
				sql = db.LastQuery!;

				sql.Should().Contain("@id");
				sql.Should().Contain("@Int1");
				sql.Should().Contain("@Int2");
				sql.Should().Contain("@IntN1");
				sql.Should().Contain("@IntN2");
				sql.Should().Contain("@String1");
				sql.Should().Contain("@String2");
				sql.Should().Contain("@String3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Should().HaveCount(2);

				res[0].Id.Should().Be(1);
				res[0].Int1.Should().Be(2);
				res[0].Int2.Should().Be(2);
				res[0].IntN1.Should().Be(2);
				res[0].IntN2.Should().Be(2);
				res[0].String1.Should().Be("str");
				res[0].String2.Should().Be("str");
				res[0].String3.Should().Be("str");

				res[1].Id.Should().Be(2);
				res[1].Int1.Should().Be(3);
				res[1].Int2.Should().Be(4);
				res[1].IntN1.Should().Be(5);
				res[1].IntN2.Should().Be(6);
				res[1].String1.Should().Be("str1");
				res[1].String2.Should().Be("str2");
				res[1].String3.Should().Be("str3");
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

				var cacheMiss = Query<ParameterDeduplication>.CacheMissCount;
				var sql       = db.LastQuery!;

				sql.Should().Contain("@id");
				sql.Should().Contain("@int1");
				sql.Should().Contain("@int2");
				sql.Should().Contain("@intN1");
				sql.Should().Contain("@intN2");
				sql.Should().Contain("@str1");
				sql.Should().Contain("@str2");
				sql.Should().Contain("@str3");

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

				Query<ParameterDeduplication>.CacheMissCount.Should().Be(cacheMiss);

				sql = db.LastQuery!;

				sql.Should().Contain("@id");
				sql.Should().Contain("@int1");
				sql.Should().Contain("@int2");
				sql.Should().Contain("@intN1");
				sql.Should().Contain("@intN2");
				sql.Should().Contain("@str1");
				sql.Should().Contain("@str2");
				sql.Should().Contain("@str3");

				var res = table.OrderBy(_ => _.Id).ToArray();

				res.Should().HaveCount(2);

				res[0].Id.Should().Be(1);
				res[0].Int1.Should().Be(2);
				res[0].Int2.Should().Be(2);
				res[0].IntN1.Should().Be(2);
				res[0].IntN2.Should().Be(2);
				res[0].String1.Should().Be("str");
				res[0].String2.Should().Be("str");
				res[0].String3.Should().Be("str");

				res[1].Id.Should().Be(2);
				res[1].Int1.Should().Be(3);
				res[1].Int2.Should().Be(4);
				res[1].IntN1.Should().Be(5);
				res[1].IntN2.Should().Be(6);
				res[1].String1.Should().Be("str1");
				res[1].String2.Should().Be("str2");
				res[1].String3.Should().Be("str3");
			}
		}

		private int _cnt;
		private int _cnt1;
		private int _cnt2;
		private int _cnt3;
		private int _param;

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3450")]
		public void TestIQueryableParameterEvaluation([DataSources] string context)
		{
			// cached queries affect cnt values due to extra comparisons in cache
			LinqToDB.Linq.Query.ClearCaches();

			using (var db = GetDataContext(context))
			{
				_cnt1       = 0;
				_cnt2       = 0;
				_cnt3       = 0;
				_param      = 1;
				var persons = Query(db);

				Assert.AreEqual(1, persons.Count);
				Assert.AreEqual(1, persons[0].ID);
				Assert.AreEqual(1, _cnt1);
				Assert.AreEqual(1, _cnt2);
				Assert.AreEqual(1, _cnt3);

				_cnt1   = 0;
				_cnt2   = 0;
				_cnt3   = 0;
				_param  = 2;
				persons = Query(db);

				Assert.AreEqual(3, persons.Count);
				Assert.AreEqual(1, persons.Count(_ => _.ID == 1));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 2));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 4));
				Assert.AreEqual(3, _cnt1);
				Assert.AreEqual(1, _cnt2);
				Assert.AreEqual(1, _cnt3);

				_cnt1   = 0;
				_cnt2   = 0;
				_cnt3   = 0;
				_param  = 3;
				persons = Query(db);

				Assert.AreEqual(2, persons.Count);
				Assert.AreEqual(1, persons.Count(_ => _.ID == 2));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 3));
				Assert.AreEqual(5, _cnt1);
				Assert.AreEqual(3, _cnt2);
				Assert.AreEqual(1, _cnt3);

				_cnt1   = 0;
				_cnt2   = 0;
				_cnt3   = 0;
				_param  = 1;
				persons = Query(db);

				Assert.AreEqual(1, persons.Count);
				Assert.AreEqual(1, persons[0].ID);
				Assert.AreEqual(4, _cnt1);
				Assert.AreEqual(2, _cnt2);
				Assert.AreEqual(2, _cnt3);

				_cnt1   = 0;
				_cnt2   = 0;
				_cnt3   = 0;
				_param  = 3;
				persons = Query(db);

				Assert.AreEqual(2, persons.Count);
				Assert.AreEqual(1, persons.Count(_ => _.ID == 2));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 3));
				Assert.AreEqual(2, _cnt1);
				Assert.AreEqual(2, _cnt2);
				Assert.AreEqual(2, _cnt3);

				_cnt1   = 0;
				_cnt2   = 0;
				_cnt3   = 0;
				_param  = 2;
				persons = Query(db);

				Assert.AreEqual(3, persons.Count);
				Assert.AreEqual(1, persons.Count(_ => _.ID == 1));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 2));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 4));
				Assert.AreEqual(4, _cnt1);
				Assert.AreEqual(3, _cnt2);
				Assert.AreEqual(2, _cnt3);
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
		public void TestIQueryableParameterEvaluationCaching([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				_cnt1       = 0;
				_param      = 1;
				var persons = Query(db);

				Assert.AreEqual(1, persons.Count);
				Assert.AreEqual(1, persons[0].ID);
				//Assert.AreEqual(1, _cnt1);

				_cnt1   = 0;
				_param  = 2;
				persons = Query(db);

				Assert.AreEqual(1, persons.Count);
				Assert.AreEqual(2, persons[0].ID);
				//Assert.AreEqual(1, _cnt1);

				_cnt1   = 0;
				_param  = 3;
				persons = Query(db);

				Assert.AreEqual(1, persons.Count);
				Assert.AreEqual(3, persons[0].ID);
				//Assert.AreEqual(1, _cnt1);

				_cnt1   = 0;
				_param  = 4;
				persons = Query(db);

				Assert.AreEqual(1, persons.Count);
				Assert.AreEqual(4, persons[0].ID);
				//Assert.AreEqual(1, _cnt1);

				_cnt1   = 0;
				_param  = 1;
				persons = Query(db);

				Assert.AreEqual(1, persons.Count);
				Assert.AreEqual(1, persons[0].ID);
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

		public void TestRunner(string context, int thread)
		{
			using (var db = GetDataContext(context))
			{
				_params[thread] = 1;
				var persons = Query(db, thread);

				Assert.AreEqual(1, persons.Count);
				Assert.AreEqual(1, persons[0].ID);

				_params[thread] = 2;
				persons = Query(db, thread);

				Assert.AreEqual(3, persons.Count);
				Assert.AreEqual(1, persons.Count(_ => _.ID == 1));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 2));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 4));

				_params[thread] = 3;
				persons = Query(db, thread);

				Assert.AreEqual(2, persons.Count);
				Assert.AreEqual(1, persons.Count(_ => _.ID == 2));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 3));

				_params[thread] = 1;
				persons = Query(db, thread);

				Assert.AreEqual(1, persons.Count);
				Assert.AreEqual(1, persons[0].ID);

				_params[thread] = 3;
				persons = Query(db, thread);

				Assert.AreEqual(2, persons.Count);
				Assert.AreEqual(1, persons.Count(_ => _.ID == 2));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 3));

				_params[thread] = 2;
				persons = Query(db, thread);

				Assert.AreEqual(3, persons.Count);
				Assert.AreEqual(1, persons.Count(_ => _.ID == 1));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 2));
				Assert.AreEqual(1, persons.Count(_ => _.ID == 4));
			}

			List<Person> Query(ITestDataContext db, int thread)
			{
				return db.Person
					.Where(_ => 
					 GetQueryT1(db, thread).Select(p => p.ID).Contains(_.ID) &&
					(GetQueryT2(db, thread).Select(p => p.ID).Contains(_.ID) ||
					 GetQueryT3(db, thread).Select(p => p.ID).Contains(_.ID)))
					.ToList();
			}
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

				Assert.AreEqual(3, persons.Count);
				Assert.True(persons.All(p => p.ID != _param));
				Assert.AreEqual(1, _cnt);

				_cnt    = 0;
				_param  = 2;
				persons = Query(db);

				Assert.AreEqual(3, persons.Count);
				Assert.True(persons.All(p => p.ID != _param));
				Assert.AreEqual(1, _cnt);
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
	}
}
