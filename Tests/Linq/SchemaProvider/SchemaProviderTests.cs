using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.SchemaProvider;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;

using NUnit.Framework;

namespace Tests.SchemaProvider
{
	[TestFixture]
	public class SchemaProviderTests : TestBase
	{
		// only tests that GetSchema call doesn't fail to detect incorrect calls to default implementation
		// or other failures
		// doesn't test that actual data returned
		[Test]
		public void TestApiImplemented([DataSources(false)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var options = new GetSchemaOptions()
				{
					GetProcedures = true,
					GetTables     = true
				};

				var p = db.DataProvider.GetSchemaProvider();
				p.GetSchema(db, options);
			}
		}

		// TODO: temporary disabled for oracle, as it takes 10 minutes for Oracle12 to process schema exceptions
		[Test]
		public void Test([DataSources(false, TestProvName.AllOracle12, ProviderName.SQLiteMS)]
			string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var sp         = conn.DataProvider.GetSchemaProvider();
				var schemaName = TestUtils.GetSchemaName(conn, context);
				var dbSchema   = sp.GetSchema(conn, new GetSchemaOptions()
				{
					IncludedSchemas = schemaName != TestUtils.NO_SCHEMA_NAME ?new[] { schemaName } : null
				});

				var tableNames = new HashSet<string>();
				foreach (var schemaTable in dbSchema.Tables)
				{
					var tableName = schemaTable.CatalogName + "." +
						(schemaTable.IsDefaultSchema ? schemaTable.TableName : schemaTable.SchemaName + "." + schemaTable.TableName);

					if (tableNames.Contains(tableName))
						Assert.Fail("Not unique table " + tableName);

					tableNames.Add(tableName);

					var columnNames = new HashSet<string>();
					foreach (var schemaColumn in schemaTable.Columns)
					{
						if(columnNames.Contains(schemaColumn.ColumnName))
							Assert.Fail($"Not unique column {schemaColumn.ColumnName} for table {schemaTable.SchemaName}.{schemaTable.TableName}");

						columnNames.Add(schemaColumn.ColumnName);
					}
				}

				//Get table from default schema and fall back to schema indifferent
				TableSchema getTable(string name) =>
								dbSchema.Tables.SingleOrDefault(t => t.IsDefaultSchema && t.TableName!.ToLowerInvariant() == name)
							??  dbSchema.Tables.SingleOrDefault(t => t.TableName!.ToLowerInvariant() == name)!;

				var table = getTable("parent");

				Assert.That(table,                                           Is.Not.Null);
				Assert.That(table.Columns.Count(c => c.ColumnName != "_ID"), Is.EqualTo(2));

				AssertType<Model.LinqDataTypes>(conn.MappingSchema, dbSchema);
				AssertType<Model.Parent>       (conn.MappingSchema, dbSchema);

				if (!context.IsAnyOf(TestProvName.AllAccessOdbc, TestProvName.AllClickHouse))
					Assert.That(getTable("doctor").ForeignKeys, Has.Count.EqualTo(1));
				else // no FK information for ACCESS ODBC, no FKs in CH
					Assert.That(dbSchema.Tables.Single(t => t.TableName!.ToLowerInvariant() == "doctor").ForeignKeys, Is.Empty);

				switch (context)
				{
					case string when context.IsAnyOf(TestProvName.AllSqlServer):
						{
							var indexTable = dbSchema.Tables.Single(t => t.TableName == "IndexTable");
							Assert.That(indexTable.ForeignKeys, Has.Count.EqualTo(1));
							Assert.That(indexTable.ForeignKeys[0].ThisColumns, Has.Count.EqualTo(2));
						}

						break;

					case string when context.IsAnyOf(TestProvName.AllInformix):
						{
							var indexTable = dbSchema.Tables.First(t => t.TableName == "testunique");
							using (Assert.EnterMultipleScope())
							{
								Assert.That(indexTable.Columns.Count(c => c.IsPrimaryKey), Is.EqualTo(2));
								Assert.That(indexTable.ForeignKeys, Has.Count.EqualTo(2));
							}
					}

						break;
				}

				switch (context)
				{
					case string when context.IsAnyOf(TestProvName.AllSqlServer2008Plus):
						{
							var tbl = dbSchema.Tables.Single(at => at.TableName == "AllTypes");
							var col = tbl.Columns.First(c => c.ColumnName == "datetimeoffset3DataType");
							using (Assert.EnterMultipleScope())
							{
								Assert.That(col.DataType, Is.EqualTo(DataType.DateTimeOffset));
								Assert.That(col.Length, Is.Null);
								Assert.That(col.Precision, Is.EqualTo(3));
								Assert.That(col.Scale, Is.Null);
							}
					}

						break;
				}
			}
		}

		static void AssertType<T>(MappingSchema mappingSchema, DatabaseSchema dbSchema)
		{
			var e = mappingSchema.GetEntityDescriptor(typeof(T));

			var schemaTable = dbSchema.Tables.FirstOrDefault(_ => _.TableName!.Equals(e.TableName, StringComparison.OrdinalIgnoreCase))!;
			Assert.That(schemaTable, Is.Not.Null, e.TableName);

			Assert.That(schemaTable.Columns, Has.Count.GreaterThanOrEqualTo(e.Columns.Count));

			foreach (var column in e.Columns)
			{
				var schemaColumn = schemaTable.Columns.FirstOrDefault(_ => _.ColumnName.Equals(column.ColumnName, StringComparison.InvariantCultureIgnoreCase))!;
				Assert.That(schemaColumn, Is.Not.Null, column.ColumnName);

				if (column.CanBeNull)
					Assert.That(schemaColumn.IsNullable, Is.EqualTo(column.CanBeNull), column.ColumnName + " Nullable");

				Assert.That(schemaColumn.IsPrimaryKey, Is.EqualTo(column.IsPrimaryKey), column.ColumnName + " PrimaryKey");
			}

			//Assert.That(schemaTable.ForeignKeys.Count >= e.Associations.Count);
		}

		[Test]
		public void NorthwindTest([NorthwindDataContext(false, true)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);

				Assert.That(dbSchema, Is.Not.Null);
			}
		}

		[Test]
		public void MySqlTest([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
				var table    = dbSchema.Tables.Single(t => t.TableName!.Equals("alltypes", StringComparison.OrdinalIgnoreCase));
				using (Assert.EnterMultipleScope())
				{
					Assert.That(table.Columns[0].MemberType, Is.Not.EqualTo("object"));

					Assert.That(table.Columns.Single(c => c.ColumnName.Equals("intUnsignedDataType", StringComparison.OrdinalIgnoreCase)).MemberType, Is.EqualTo("uint?"));
				}

				var view = dbSchema.Tables.Single(t => t.TableName!.Equals("personview", StringComparison.OrdinalIgnoreCase));

				Assert.That(view.Columns, Has.Count.EqualTo(1));
			}
		}

		[Test]
		public void MySqlPKTest([IncludeDataSources(TestProvName.AllMySql, TestProvName.AllClickHouse)]
			string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
				var table    = dbSchema.Tables.Single(t => t.TableName!.Equals("person", StringComparison.OrdinalIgnoreCase));
				var pk       = table.Columns.FirstOrDefault(t => t.IsPrimaryKey);

				Assert.That(pk, Is.Not.Null);
			}
		}

		sealed class PKTest
		{
			[PrimaryKey(1)] public int ID1;
			[PrimaryKey(2)] public int ID2;
		}

		sealed class ArrayTest
		{
			[Column(DbType = "text[]")]             public string[]  StrArray     { get; set; } = null!;
			[Column(DbType = "int[]")]              public int[]     IntArray     { get; set; } = null!;
			[Column(DbType = "int[][]")]            public int[][]   Int2dArray   { get; set; } = null!;
			[Column(DbType = "bigint[]")]           public long[]    LongArray    { get; set; } = null!;
			[Column(DbType = "double precision[]")] public double[]  DoubleArray  { get; set; } = null!;
			[Column(DbType = "numeric[]")]          public decimal[] DecimalArray { get; set; } = null!;
		}

		[Test]
		public void PostgreSQLTest([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using (var conn = GetDataConnection(context))
			using (conn.CreateLocalTable<ArrayTest>())
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
				var table    = dbSchema.Tables.Single(t => t.TableName == "ArrayTest");
			}
		}

		[Test]
		public void DB2Test([IncludeDataSources(ProviderName.DB2)] string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
				var table    = dbSchema.Tables.Single(t => t.TableName == "ALLTYPES");
				using (Assert.EnterMultipleScope())
				{
					Assert.That(table.Columns.Single(c => c.ColumnName == "BINARYDATATYPE").ColumnType, Is.EqualTo("CHAR (5) FOR BIT DATA"));
					Assert.That(table.Columns.Single(c => c.ColumnName == "VARBINARYDATATYPE").ColumnType, Is.EqualTo("VARCHAR (5) FOR BIT DATA"));
				}
			}
		}

		[Test]
		public void ToValidNameTest()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(SchemaProviderBase.ToValidName("1"), Is.EqualTo("_1"));
				Assert.That(SchemaProviderBase.ToValidName("    1   "), Is.EqualTo("_1"));
				Assert.That(SchemaProviderBase.ToValidName("\t1\t"), Is.EqualTo("_1"));
			}
		}

		[Test]
		public void IncludeExcludeCatalogTest([DataSources(false)]
			string context)
		{
			using (var conn = GetDataConnection(context))
			{
				var exclude = conn.DataProvider.GetSchemaProvider().GetSchema(conn).Tables.Select(_ => _.CatalogName).Distinct().ToList();
				exclude.Add(null);
				exclude.Add("");

				var schema1 = conn.DataProvider.GetSchemaProvider().GetSchema(conn, new GetSchemaOptions {ExcludedCatalogs = exclude.ToArray()});
				var schema2 = conn.DataProvider.GetSchemaProvider().GetSchema(conn, new GetSchemaOptions {IncludedCatalogs = new []{ "IncludeExcludeCatalogTest" }});
				using (Assert.EnterMultipleScope())
				{
					Assert.That(schema1.Tables, Is.Empty);
					Assert.That(schema2.Tables, Is.Empty);
				}
			}
		}

		[Test]
		public void IncludeExcludeSchemaTest([DataSources(false)]
			string context)
		{
			using (new DisableBaseline("TODO: exclude schema list is not stable, db2 schema provider needs refactoring", GetProviderName(context, out var _) == ProviderName.DB2))
			using (var conn = GetDataConnection(context))
			{
				var exclude = conn.DataProvider.GetSchemaProvider()
					.GetSchema(conn, new GetSchemaOptions {ExcludedSchemas = new string?[] { null }})
					.Tables.Select(_ => _.SchemaName)
					.Distinct()
					.ToList();
				exclude.Add(null);
				exclude.Add("");

				var schema1 = conn.DataProvider.GetSchemaProvider().GetSchema(conn, new GetSchemaOptions {ExcludedSchemas = exclude.ToArray()});
				var schema2 = conn.DataProvider.GetSchemaProvider().GetSchema(conn, new GetSchemaOptions {IncludedSchemas = new []{ "IncludeExcludeSchemaTest" } });
				using (Assert.EnterMultipleScope())
				{
					Assert.That(schema1.Tables, Is.Empty);
					Assert.That(schema2.Tables, Is.Empty);
				}
			}
		}

		[Test]
		public void SchemaProviderNormalizeName([IncludeDataSources(TestProvName.AllSQLiteClassic)]
			string context)
		{
			using (var db = new DataConnection(new DataOptions().UseConnectionString(context, "Data Source=:memory:;")))
			{
				db.Execute(
					@"create table Customer
					(
						ID int not null primary key,
						Name nvarchar(30) not null
					)");

				db.Execute(
					@"create table Purchase
					(
						ID int not null primary key,
						CustomerID int null references Customer (ID),
						Date datetime not null,
						Description varchar(30) not null,
						Price decimal not null
					)");

				db.Execute(
					@"create table PurchaseItem
					(
						ID int not null primary key,
						PurchaseID int not null references Purchase (ID),
						Detail varchar(30) not null,
						Price decimal not null
					)");

				var sp = db.DataProvider.GetSchemaProvider();
				var sc = sp.GetSchema(db);

				Assert.That(sc, Is.Not.Null);
				Assert.That(sc.Tables.SelectMany(_ => _.ForeignKeys).Where(_ => _.MemberName.Any(char.IsDigit)), Is.Empty);
			}
		}

		// TODO: temporary disabled for oracle, as it takes 10 minutes for Oracle12 to process schema exceptions
		// Access.Odbc: no FK information available for provider
		[Test]
		public void PrimaryForeignKeyTest([DataSources(false, TestProvName.AllOracle12, TestProvName.AllAccessOdbc, ProviderName.SQLiteMS)]
			string context)
		{
			var skipFK = context.IsAnyOf(TestProvName.AllClickHouse);
			using (var db = GetDataConnection(context))
			{
				var p = db.DataProvider.GetSchemaProvider();
				var schemaName = TestUtils.GetSchemaName(db, context);
				var s = p.GetSchema(db, new GetSchemaOptions()
				{
					IncludedSchemas = schemaName != TestUtils.NO_SCHEMA_NAME ? new[] { schemaName } : null
				});

				var fkCountDoctor = s.Tables.Single(_ => _.TableName!.Equals(nameof(Model.Doctor), StringComparison.OrdinalIgnoreCase)).ForeignKeys.Count;
				var pkCountDoctor = s.Tables.Single(_ => _.TableName!.Equals(nameof(Model.Doctor), StringComparison.OrdinalIgnoreCase)).Columns.Count(_ => _.IsPrimaryKey);

				if (!skipFK)
					Assert.That(fkCountDoctor, Is.EqualTo(1));
				Assert.That(pkCountDoctor, Is.EqualTo(1));

				var fkCountPerson = s.Tables.Single(_ => _.TableName!.Equals(nameof(Model.Person), StringComparison.OrdinalIgnoreCase) && !(_.SchemaName ?? "").Equals("MySchema", StringComparison.OrdinalIgnoreCase)).ForeignKeys.Count;
				var pkCountPerson = s.Tables.Single(_ => _.TableName!.Equals(nameof(Model.Person), StringComparison.OrdinalIgnoreCase) && !(_.SchemaName ?? "").Equals("MySchema", StringComparison.OrdinalIgnoreCase)).Columns.Count(_ => _.IsPrimaryKey);

				if (!skipFK)
					Assert.That(fkCountPerson, Is.EqualTo(2));
				Assert.That(pkCountPerson, Is.EqualTo(1));
			}
		}

		[ActiveIssue("Unstable, depends on metadata selection order")]
		/*
		 * Expected Was
		 * ! FK_TestSchemaY_OtherID <> FK_TestSchemaY_TestSchemaX
		 * ParentTestSchemaX == ParentTestSchemaX
		 * TestSchemaX == TestSchemaX
		 */
		[Test]
		public void ForeignKeyMemberNameTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = db.DataProvider.GetSchemaProvider();
				var s = p.GetSchema(db);

				var table = s.Tables.Single(t => t.TableName == "TestSchemaY");
				var fks   = table.ForeignKeys.Select(fk => fk.MemberName).ToArray();

				AreEqual(new[] { "TestSchemaX", "ParentTestSchemaX", "FK_TestSchemaY_OtherID" }, fks, _ => _.OrderBy(_ => _));

				table = s.Tables.Single(t => t.TableName == "TestSchemaB");
				fks   = table.ForeignKeys.Select(fk => fk.MemberName).ToArray();

				AreEqual(new[] { "OriginTestSchemaA", "TargetTestSchemaA", "Target_Test_Schema_A" }, fks, _ => _.OrderBy(_ => _));
			}
		}

		[Test]
		public void ForeignKeyMemberNameTest2([IncludeDataSources(TestProvName.AllNorthwind)]
			string context)
		{
			using (var db = GetDataConnection(context))
			{
				var p = db.DataProvider.GetSchemaProvider();
				var s = p.GetSchema(db);

				var table = s.Tables.Single(t => t.TableName == "Employees");
				var fks   = table.ForeignKeys.Select(fk => fk.MemberName).ToArray();

				Assert.That(fks, Is.EqualTo(new[] { "FK_Employees_Employees", "FK_Employees_Employees_BackReference", "Orders", "EmployeeTerritories" }));
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2348")]
		public void SchemaOnlyTestIssue2348([IncludeDataSources(TestProvName.AllSqlServer2012Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = (DataConnection)GetDataContext(context);

			var schema1 = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions
			{
				GetTables     = false,
				GetProcedures = true,
				UseSchemaOnly = true,
//					LoadProcedure = sp => sp.ProcedureName == "SelectImplicitColumn"
			});

			var schema2 = db.DataProvider.GetSchemaProvider().GetSchema(db, new GetSchemaOptions
			{
				GetTables     = false,
				GetProcedures = true,
				UseSchemaOnly = false,
//					LoadProcedure = sp => sp.ProcedureName == "SelectImplicitColumn"
			});

			Assert.That(schema1.Procedures, Has.Count.EqualTo(schema2.Procedures.Count));

			for (var i = 0; i < schema1.Procedures.Count; i++)
			{
				var p1 = schema1.Procedures[i];
				var p2 = schema2.Procedures[i];

				if (p1.ResultTable == null)
				{
					Assert.That(p2.ResultTable, Is.Null);
				}
				else
				{
					Assert.That(p2.ResultTable, Is.Not.Null);

					var t1 = p1.ResultTable;
					var t2 = p2.ResultTable!;
					using (Assert.EnterMultipleScope())
					{
						Assert.That(t1.ID, Is.EqualTo(t2.ID));
						Assert.That(t1.CatalogName, Is.EqualTo(t2.CatalogName));
						Assert.That(t1.SchemaName, Is.EqualTo(t2.SchemaName));
						Assert.That(t1.TableName, Is.EqualTo(t2.TableName));
						Assert.That(t1.Description, Is.EqualTo(t2.Description));
						Assert.That(t1.IsDefaultSchema, Is.EqualTo(t2.IsDefaultSchema));
						Assert.That(t1.IsView, Is.EqualTo(t2.IsView));
						Assert.That(t1.IsProcedureResult, Is.EqualTo(t2.IsProcedureResult));
						Assert.That(t1.TypeName, Is.EqualTo(t2.TypeName));
						Assert.That(t1.IsProviderSpecific, Is.EqualTo(t2.IsProviderSpecific));
						Assert.That(t1.Columns, Has.Count.EqualTo(t2.Columns.Count));
					}

					for (var j = 0; j < p1.ResultTable.Columns.Count; j++)
					{
						var c1 = t1.Columns[j];
						var c2 = t2.Columns[j];
						using (Assert.EnterMultipleScope())
						{
							Assert.That(c1.ColumnName, Is.EqualTo(c2.ColumnName));
							Assert.That(c1.ColumnType, Is.EqualTo(c2.ColumnType));
							Assert.That(c1.IsNullable, Is.EqualTo(c2.IsNullable));
							Assert.That(c1.IsIdentity, Is.EqualTo(c2.IsIdentity));
							Assert.That(c1.IsPrimaryKey, Is.EqualTo(c2.IsPrimaryKey));
							Assert.That(c1.PrimaryKeyOrder, Is.EqualTo(c2.PrimaryKeyOrder));
							Assert.That(c1.Description, Is.EqualTo(c2.Description));
							Assert.That(c1.MemberName, Is.EqualTo(c2.MemberName));
							Assert.That(c1.MemberType, Is.EqualTo(c2.MemberType));
							Assert.That(c1.ProviderSpecificType, Is.EqualTo(c2.ProviderSpecificType));
							Assert.That(c1.SystemType, Is.EqualTo(c2.SystemType));
							Assert.That(c1.DataType, Is.EqualTo(c2.DataType));
							Assert.That(c1.SkipOnInsert, Is.EqualTo(c2.SkipOnInsert));
							Assert.That(c1.SkipOnUpdate, Is.EqualTo(c2.SkipOnUpdate));
							Assert.That(c1.Length, Is.EqualTo(c2.Length));
							Assert.That(c1.Precision, Is.EqualTo(c2.Precision));
							Assert.That(c1.Scale, Is.EqualTo(c2.Scale));
						}
					}
				}
			}
		}

		[Test]
		public void ClickHouseDataTypeTest([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			using var conn     = GetDataConnection(context);
			var       sp       = conn.DataProvider.GetSchemaProvider();
			var       dbSchema = sp.GetSchema(conn);
			var       table    = dbSchema.Tables.Single(t => t.TableName!.Equals("alltypes", StringComparison.OrdinalIgnoreCase));
			var       pk       = table.Columns.FirstOrDefault(t => t.IsPrimaryKey);

			Assert.That(pk, Is.Not.Null);
		}
	}
}
