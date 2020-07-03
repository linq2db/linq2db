﻿using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;
using Tests.Model;

namespace Tests.Data
{
	[TestFixture]
	public class DataExtensionsTests : TestBase
	{
		[Test]
		public void TestScalar1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query(rd => rd[0], "SELECT 1").ToList();

				Assert.That(new[] { 1 }, Is.EquivalentTo(list));
			}
		}

		[Test]
		public void TestScalar2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<int>("SELECT 1").ToList();

				Assert.That(new[] { 1 }, Is.EquivalentTo(list));
			}
		}

		[Test]
		public void TestScalar3([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<DateTimeOffset>("SELECT CURRENT_TIMESTAMP").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}

		class QueryObject
		{
			public int      Column1;
			public DateTime Column2;
		}

		[Test]
		public void TestObject1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<QueryObject>("SELECT 1 as Column1, CURRENT_TIMESTAMP as Column2").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void TestObject2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query(
					new
					{
						Column1 = 1,
						Column2 = DateTime.MinValue
					},
					"SELECT 1 as Column1, CURRENT_TIMESTAMP as Column2").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void TestObject3()
		{
			var arr1 = new byte[] { 48, 57 };
			var arr2 = new byte[] { 42 };

			using (var conn = new DataConnection())
			{
				Assert.That(conn.Execute<byte[]>("SELECT @p", new { p = arr1 }), Is.EqualTo(arr1));
				Assert.That(conn.Execute<byte[]>("SELECT @p", new { p = arr2 }), Is.EqualTo(arr2));
			}
		}

		[Test]
		public void TestObject4()
		{
			using (var conn = new DataConnection())
			{
				Assert.That(conn.Execute<int>("SELECT @p", new { p = 1 }), Is.EqualTo(1));
			}
		}

		[Test]
		public void TestObject5()
		{
			using (var conn = new DataConnection())
			{
				var res = conn.Execute<string>(
					"SELECT @p",
					new
					{
						p  = new DataParameter { DataType = DataType.VarChar, Value = "123" },
						p1 = 1
					});

				Assert.That(res, Is.EqualTo("123"));
			}
		}

		[Test]
		public void TestObject51([DataSources(false)] string context)
		{
			using (var conn = new TestDataConnection(context))
			{
				conn.InlineParameters = true;
				var sql = conn.Person.Where(p => p.ID == 1).Select(p => p.Name).Take(1).ToString()!;
				sql = string.Join(Environment.NewLine, sql.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
					.Where(line => !line.StartsWith("-- Access")));
				var res = conn.Execute<string>(sql);

				Assert.That(res, Is.EqualTo("John"));
			}
		}

		[Test]
		public void TestObjectProjection([DataSources(false)] string context)
		{
			using (var conn = new TestDataConnection(context))
			{
				var result = conn.Person.Where(p => p.ID == 1).Select(p => new { p.ID, p.Name })
					.Take(1)
					.ToArray();

				var expected = Person.Where(p => p.ID == 1).Select(p => new { p.ID, p.Name })
					.Take(1)
					.ToArray();

				AreEqual(expected, result);
			}
		}

		[Test]
		public void TestObjectLeftJoinProjection([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var conn = GetDataContext(context))
			{
				var result = 
					from p in conn.Person
					from pp in conn.Person.LeftJoin(pp => pp.ID + 1 == p.ID)
					select new { p.ID, pp.Name };

				var expected =
					from p in Person
					join pp in Person on p.ID equals pp.ID + 1 into j
					from pp in j.DefaultIfEmpty()
					select new { p.ID, pp?.Name };

				AreEqual(expected, result);
			}
		}


		[Test]
		public void TestObject6()
		{
			using (var conn = new DataConnection())
			{
				Assert.That(conn.Execute<string>(
					"SELECT @p",
					new
					{
						p1 = new DataParameter { Name = "p", DataType = DataType.Char, Value = "123" },
						p2 = 1
					}), Is.EqualTo("123"));
			}
		}

		[ScalarType(false)]
		struct QueryStruct
		{
			public int      Column1;
			public DateTime Column2;
		}

		[Test]
		public void TestStruct1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<QueryStruct>("SELECT 1 as Column1, CURRENT_TIMESTAMP as Column2").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void TestDataReader([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var conn   = new DataConnection(context))
			using (var reader = conn.ExecuteReader("SELECT 1; SELECT '2'"))
			{
				var n = reader.Execute<int>();

				Assert.AreEqual(1, n);

				var s = reader.Query<string>();

				Assert.AreEqual("2", s.First());
			}
		}

		[ScalarType]
		class TwoValues
		{
			public int Value1;
			public int Value2;
		}

		[Test]
		public void TestDataParameterMapping1()
		{
			var ms = new MappingSchema();

#pragma warning disable CS0675 // strange math here: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
			ms.SetConvertExpression<TwoValues,DataParameter>(tv => new DataParameter { Value = (long)tv.Value1 << 16 | tv.Value2 });
#pragma warning restore CS0675

			using (var conn = new DataConnection().AddMappingSchema(ms))
			{
				var n = conn.Execute<long>("SELECT @p", new { p = new TwoValues { Value1 = 1, Value2 = 2 } });

				Assert.AreEqual(1L << 16 | 2, n);
			}
		}

		[Test]
		public void TestDataParameterMapping2([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

#pragma warning disable CS0675 // strange math here: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
			ms.SetConvertExpression<TwoValues,DataParameter>(tv => new DataParameter { Value = (long)tv.Value1 << 32 | tv.Value2 });
#pragma warning restore CS0675

			using (var conn = (DataConnection)GetDataContext(context, ms))
			{
				var n = conn.Execute<long?>("SELECT @p", new { p = (TwoValues?)null });

				Assert.AreEqual(null, n);
			}
		}

		[Test]
		public void TestDataParameterMapping3([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<TwoValues,DataParameter>(tv =>
				new DataParameter
				{
#pragma warning disable CS0675 // strange math here: Bitwise-or operator used on a sign-extended operand; consider casting to a smaller unsigned type first
					Value = tv == null ? (long?)null : (long)tv.Value1 << 32 | tv.Value2,
#pragma warning restore CS0675
					DataType = DataType.Int64
				},
				false);

			using (var conn = (DataConnection)GetDataContext(context, ms))
			{
				var n = conn.Execute<long?>("SELECT @p", new { p = (TwoValues?)null });

				Assert.AreEqual(null, n);
			}
		}

		[Test]
		public void CacheTest([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			using (var dc= new DataConnection(context))
			{
				dc.Execute("CREATE TABLE #t1(v1 int not null)");
				dc.Execute("INSERT INTO #t1(v1) values (1)");
				var v1 = dc.Query<object>("SELECT v1 FROM #t1").ToList();
				dc.Execute("ALTER TABLE #t1 ALTER COLUMN v1 INT NULL");

				DataConnection.ClearObjectReaderCache();

				dc.Execute("INSERT INTO #t1(v1) VALUES (null)");
				var v2 = dc.Query<object>("SELECT v1 FROM #t1").ToList();
			}
		}
	}
}
