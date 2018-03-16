using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Linq;
using Tests.Model;

namespace Tests.Data
{
	[TestFixture]
	public class DataExtensionsTests : TestBase
	{

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL)]
		public void TestCNCreateTableAndInsertAndUpdateAndDeleteAndDrop(string context)
		{
			const string CNSchemaName = "linq2db";
			const string CNTableName = "xxcat";
			using (var conn = new DataConnection(context))
			{
				// Drop table if exists
				conn.DropTable<Cat>(tableName: CNTableName, schemaName: CNSchemaName, throwExceptionIfNotExists: false);
				// create table
				ITable<Cat> table = conn.CreateTable<Cat>(tableName: CNTableName, schemaName: CNSchemaName);
				Assert.IsNotNull(table);
				Assert.AreEqual(CNTableName, table.TableName);
				Assert.AreEqual(CNSchemaName, table.SchemaName);
				Assert.IsTrue(conn.LastQuery.StartsWith($"CREATE TABLE {CNSchemaName}.{CNTableName}", StringComparison.Ordinal));
				// insert a row into the table
				Cat cat = new Cat() { Name = "Tom", Age = 5, Color = "Blue" };
				int newId = conn.InsertWithInt32Identity<Cat>(cat, tableName: CNTableName, schemaName: CNSchemaName);
				var count = table.Count();
				Assert.AreEqual(count, 1);
				// update that row
				Cat catForUpdate = table.SingleOrDefault(c => c.CatID == newId);
				Assert.IsNotNull(catForUpdate);
				catForUpdate.Age = 3;
				conn.Update(catForUpdate, tableName: CNTableName, schemaName: CNSchemaName);
				count = table.Count();
				Assert.AreEqual(count, 1);
				Cat catUpdated = table.SingleOrDefault(c => c.CatID == newId);
				Assert.IsNotNull(catUpdated);
				Assert.AreEqual(3, catUpdated.Age);
				// insert an other row
				int newId2 = newId + 1;
				Cat cat2 = new Cat() { CatID = newId2, Name = "Thomas", Age = 8, Color = "Black" };
				conn.Insert<Cat>(cat2, tableName: CNTableName, schemaName: CNSchemaName);
				count = table.Count();
				Assert.AreEqual(count, 2);
				// Delete that row from table
				conn.Delete(catUpdated, tableName: CNTableName, schemaName: CNSchemaName);
				count = table.Count();
				Assert.AreEqual(count, 1);
				Cat catRemained = table.SingleOrDefault(c => c.CatID == newId2);
				Assert.IsNotNull(catRemained);
				// Cleanup, drop table
				conn.DropTable<Cat>(tableName: CNTableName, schemaName: CNSchemaName);
			}
		}


		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void TestScalar1(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query(rd => rd[0], "SELECT 1").ToList();

				Assert.That(new[] { 1 }, Is.EquivalentTo(list));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void TestScalar2(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<int>("SELECT 1").ToList();

				Assert.That(new[] { 1 }, Is.EquivalentTo(list));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void TestScalar3(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<DateTimeOffset>("SELECT CURRENT_TIMESTAMP").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}

		class QueryObject
		{
			public int Column1;
			public DateTime Column2;
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void TestObject1(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<QueryObject>("SELECT 1 as Column1, CURRENT_TIMESTAMP as Column2").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void TestObject2(string context)
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
						p = new DataParameter { DataType = DataType.VarChar, Value = "123" },
						p1 = 1
					});

				Assert.That(res, Is.EqualTo("123"));
			}
		}

		[Test, DataContextSource(false)]
		public void TestObject51(string context)
		{
			using (var conn = new TestDataConnection(context))
			{
				var sql = conn.Person.Where(p => p.ID == 1).Select(p => p.Name).Take(1).ToString().Replace("-- Access", "");
				var res = conn.Execute<string>(sql);

				Assert.That(res, Is.EqualTo("John"));
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
			public int Column1;
			public DateTime Column2;
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void TestStruct1(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var list = conn.Query<QueryStruct>("SELECT 1 as Column1, CURRENT_TIMESTAMP as Column2").ToList();

				Assert.That(list.Count, Is.EqualTo(1));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer)]
		public void TestDataReader(string context)
		{
			using (var conn = new DataConnection(context))
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

#pragma warning disable 675

		[Test]
		public void TestDataParameterMapping1()
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<TwoValues, DataParameter>(tv => new DataParameter { Value = (long)tv.Value1 << 16 | tv.Value2 });

			using (var conn = new DataConnection().AddMappingSchema(ms))
			{
				var n = conn.Execute<long>("SELECT @p", new { p = new TwoValues { Value1 = 1, Value2 = 2 } });

				Assert.AreEqual(1L << 16 | 2, n);
			}
		}

		[Test, IncludeDataContextSource(false, ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
		public void TestDataParameterMapping2(string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<TwoValues, DataParameter>(tv => new DataParameter { Value = (long)tv.Value1 << 32 | tv.Value2 });

			using (var conn = (DataConnection)GetDataContext(context, ms))
			{
				var n = conn.Execute<long?>("SELECT @p", new { p = (TwoValues)null });

				Assert.AreEqual(null, n);
			}
		}

		[Test, IncludeDataContextSource(false, ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
		public void TestDataParameterMapping3(string context)
		{
			var ms = new MappingSchema();

			ms.SetConvertExpression<TwoValues, DataParameter>(tv =>
				 new DataParameter
				 {
					 Value = tv == null ? (long?)null : (long)tv.Value1 << 32 | tv.Value2,
					 DataType = DataType.Int64
				 },
				false);

			using (var conn = (DataConnection)GetDataContext(context, ms))
			{
				var n = conn.Execute<long?>("SELECT @p", new { p = (TwoValues)null });

				Assert.AreEqual(null, n);
			}
		}

		[Test, IncludeDataContextSourceAttribute(TestProvName.Northwind)]
		public void CacheTest(string context)
		{
			using (var dc = new DataConnection(context))
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
