using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
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
			using (var db = new DataConnection(context))
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
		public void Test([DataSources(false, ProviderName.SQLiteMS, ProviderName.MySqlConnector, TestProvName.AllOracle12)]
			string context)
		{
			using (var conn = new DataConnection(context))
			{
				var sp         = conn.DataProvider.GetSchemaProvider();
				var schemaName = TestUtils.GetSchemaName(conn);
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
							Assert.Fail("Not unique column {0} for table {1}.{2}", schemaColumn.ColumnName, schemaTable.SchemaName, schemaTable.TableName);

						columnNames.Add(schemaColumn.ColumnName);
					}
				}

				//Get table from default schema and fall back to schema indifferent
				TableSchema getTable(string name) =>
								dbSchema.Tables.SingleOrDefault(t => t.IsDefaultSchema && t.TableName!.ToLower() == name)
							??  dbSchema.Tables.SingleOrDefault(t => t.TableName!.ToLower() == name)!;

				var table = getTable("parent");

				Assert.That(table,                                           Is.Not.Null);
				Assert.That(table.Columns.Count(c => c.ColumnName != "_ID"), Is.EqualTo(2));

				AssertType<Model.LinqDataTypes>(conn.MappingSchema, dbSchema);
				AssertType<Model.Parent>       (conn.MappingSchema, dbSchema);

				if (context != ProviderName.AccessOdbc)
					Assert.That(getTable("doctor").ForeignKeys.Count, Is.EqualTo(1));
				else // no FK information for ACCESS ODBC
					Assert.That(dbSchema.Tables.Single(t => t.TableName!.ToLower() == "doctor").ForeignKeys.Count, Is.EqualTo(0));

				switch (context)
				{
					case ProviderName.SqlServer2000                       :
					case ProviderName.SqlServer2005                       :
					case ProviderName.SqlServer2008                       :
					case ProviderName.SqlServer2012                       :
					case ProviderName.SqlServer2014                       :
					case TestProvName.SqlServer2016                       :
					case ProviderName.SqlServer2017                       :
					case TestProvName.SqlServer2019                       :
					case TestProvName.SqlServer2019SequentialAccess       :
					case TestProvName.SqlServer2019FastExpressionCompiler :
					case TestProvName.SqlAzure                            :
						{
							var indexTable = dbSchema.Tables.Single(t => t.TableName == "IndexTable");
							Assert.That(indexTable.ForeignKeys.Count,                Is.EqualTo(1));
							Assert.That(indexTable.ForeignKeys[0].ThisColumns.Count, Is.EqualTo(2));
						}
						break;

					case ProviderName.Informix      :
					case ProviderName.InformixDB2   :
						{
							var indexTable = dbSchema.Tables.First(t => t.TableName == "testunique");
							Assert.That(indexTable.Columns.Count(c => c.IsPrimaryKey), Is.EqualTo(2));
							Assert.That(indexTable.ForeignKeys.Count, Is.EqualTo(2));
						}
						break;
				}

				switch (context)
				{
					case ProviderName.SqlServer2008                       :
					case ProviderName.SqlServer2012                       :
					case ProviderName.SqlServer2014                       :
					case TestProvName.SqlServer2016                       :
					case ProviderName.SqlServer2017                       :
					case TestProvName.SqlServer2019                       :
					case TestProvName.SqlServer2019SequentialAccess       :
					case TestProvName.SqlServer2019FastExpressionCompiler :
					case TestProvName.SqlAzure                            :
						{
							var tbl = dbSchema.Tables.Single(at => at.TableName == "AllTypes");
							var col = tbl.Columns.First(c => c.ColumnName == "datetimeoffset3DataType");
							Assert.That(col.DataType,  Is.EqualTo(DataType.DateTimeOffset));
							Assert.That(col.Length,    Is.Null);
							Assert.That(col.Precision, Is.EqualTo(3));
							Assert.That(col.Scale,     Is.Null);
						}
						break;
				}
			}
		}

		static void AssertType<T>(MappingSchema mappingSchema, DatabaseSchema dbSchema)
		{
			var e = mappingSchema.GetEntityDescriptor(typeof(T));

			var schemaTable = dbSchema.Tables.FirstOrDefault(_ => _.TableName!.Equals(e.TableName, StringComparison.OrdinalIgnoreCase))!;
			Assert.IsNotNull(schemaTable, e.TableName);

			Assert.That(schemaTable.Columns.Count >= e.Columns.Count);

			foreach (var column in e.Columns)
			{
				var schemaColumn = schemaTable.Columns.FirstOrDefault(_ => _.ColumnName.Equals(column.ColumnName, StringComparison.InvariantCultureIgnoreCase))!;
				Assert.IsNotNull(schemaColumn, column.ColumnName);

				if (column.CanBeNull)
					Assert.AreEqual(column.CanBeNull, schemaColumn.IsNullable, column.ColumnName + " Nullable");

				Assert.AreEqual(column.IsPrimaryKey, schemaColumn.IsPrimaryKey, column.ColumnName + " PrimaryKey");
			}

			//Assert.That(schemaTable.ForeignKeys.Count >= e.Associations.Count);
		}

		[Test]
		public void NorthwindTest([NorthwindDataContext(false, true)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);

				Assert.IsNotNull(dbSchema);
			}
		}

		[Test]
		public void MySqlTest([IncludeDataSources(TestProvName.AllMySql)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
				var table    = dbSchema.Tables.Single(t => t.TableName!.Equals("alltypes", StringComparison.OrdinalIgnoreCase));

				Assert.That(table.Columns[0].MemberType, Is.Not.EqualTo("object"));

				Assert.That(table.Columns.Single(c => c.ColumnName.Equals("intUnsignedDataType", StringComparison.OrdinalIgnoreCase)).MemberType, Is.EqualTo("uint?"));

				var view = dbSchema.Tables.Single(t => t.TableName!.Equals("personview", StringComparison.OrdinalIgnoreCase));

				Assert.That(view.Columns.Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void MySqlPKTest([IncludeDataSources(TestProvName.AllMySql)]
			string context)
		{
			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
				var table    = dbSchema.Tables.Single(t => t.TableName!.Equals("person", StringComparison.OrdinalIgnoreCase));
				var pk       = table.Columns.FirstOrDefault(t => t.IsPrimaryKey);

				Assert.That(pk, Is.Not.Null);
			}
		}

		class PKTest
		{
			[PrimaryKey(1)] public int ID1;
			[PrimaryKey(2)] public int ID2;
		}

		class ArrayTest
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
			using (var conn = new DataConnection(context))
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
			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
				var table    = dbSchema.Tables.Single(t => t.TableName == "ALLTYPES");

				Assert.That(table.Columns.Single(c => c.ColumnName == "BINARYDATATYPE").   ColumnType, Is.EqualTo("CHAR (5) FOR BIT DATA"));
				Assert.That(table.Columns.Single(c => c.ColumnName == "VARBINARYDATATYPE").ColumnType, Is.EqualTo("VARCHAR (5) FOR BIT DATA"));
			}
		}

		[Test]
		public void ToValidNameTest()
		{
			Assert.AreEqual("_1", SchemaProviderBase.ToValidName("1"));
			Assert.AreEqual("_1", SchemaProviderBase.ToValidName("    1   "));
			Assert.AreEqual("_1", SchemaProviderBase.ToValidName("\t1\t"));
		}

		[Test]
		public void IncludeExcludeCatalogTest([DataSources(false, ProviderName.SQLiteMS, ProviderName.MySqlConnector)]
			string context)
		{
			using (var conn = new DataConnection(context))
			{
				var exclude = conn.DataProvider.GetSchemaProvider().GetSchema(conn).Tables.Select(_ => _.CatalogName).Distinct().ToList();
				exclude.Add(null);
				exclude.Add("");

				var schema1 = conn.DataProvider.GetSchemaProvider().GetSchema(conn, new GetSchemaOptions {ExcludedCatalogs = exclude.ToArray()});
				var schema2 = conn.DataProvider.GetSchemaProvider().GetSchema(conn, new GetSchemaOptions {IncludedCatalogs = new []{ "IncludeExcludeCatalogTest" }});

				Assert.IsEmpty(schema1.Tables);
				Assert.IsEmpty(schema2.Tables);
			}
		}

		[Test]
		public void IncludeExcludeSchemaTest([DataSources(false, ProviderName.SQLiteMS, ProviderName.MySqlConnector)]
			string context)
		{
			using (new DisableBaseline("TODO: exclude schema list is not stable, db2 schema provider needs refactoring", GetProviderName(context, out var _) == ProviderName.DB2))
			using (var conn = new DataConnection(context))
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

				Assert.IsEmpty(schema1.Tables);
				Assert.IsEmpty(schema2.Tables);
			}
		}

		[Test]
		public void SchemaProviderNormalizeName([IncludeDataSources(TestProvName.AllSQLiteClassic)]
			string context)
		{
			using (var db = new DataConnection(context, "Data Source=:memory:;"))
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

				Assert.IsNotNull(sc);
				Assert.IsEmpty(sc.Tables.SelectMany(_ => _.ForeignKeys).Where(_ => _.MemberName.Any(char.IsDigit)));
			}
		}

		// TODO: temporary disabled for oracle, as it takes 10 minutes for Oracle12 to process schema exceptions
		// Access.Odbc: no FK information available for provider
		[Test]
		public void PrimaryForeignKeyTest([DataSources(false, ProviderName.SQLiteMS, ProviderName.MySqlConnector, TestProvName.AllOracle12, ProviderName.AccessOdbc)]
			string context)
		{
			using (var db = new DataConnection(context))
			{
				var p = db.DataProvider.GetSchemaProvider();
				var schemaName = TestUtils.GetSchemaName(db);
				var s = p.GetSchema(db, new GetSchemaOptions()
				{
					IncludedSchemas = schemaName != TestUtils.NO_SCHEMA_NAME ? new[] { schemaName } : null
				});

				var fkCountDoctor = s.Tables.Single(_ => _.TableName!.Equals(nameof(Model.Doctor), StringComparison.OrdinalIgnoreCase)).ForeignKeys.Count;
				var pkCountDoctor = s.Tables.Single(_ => _.TableName!.Equals(nameof(Model.Doctor), StringComparison.OrdinalIgnoreCase)).Columns.Count(_ => _.IsPrimaryKey);

				Assert.AreEqual(1, fkCountDoctor);
				Assert.AreEqual(1, pkCountDoctor);

				var fkCountPerson = s.Tables.Single(_ => _.TableName!.Equals(nameof(Model.Person), StringComparison.OrdinalIgnoreCase) && !(_.SchemaName ?? "").Equals("MySchema", StringComparison.OrdinalIgnoreCase)).ForeignKeys.Count;
				var pkCountPerson = s.Tables.Single(_ => _.TableName!.Equals(nameof(Model.Person), StringComparison.OrdinalIgnoreCase) && !(_.SchemaName ?? "").Equals("MySchema", StringComparison.OrdinalIgnoreCase)).Columns.Count(_ => _.IsPrimaryKey);

				Assert.AreEqual(2, fkCountPerson);
				Assert.AreEqual(1, pkCountPerson);
			}
		}

		[Test]
		public void ForeignKeyMemberNameTest1([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var p = db.DataProvider.GetSchemaProvider();
				var s = p.GetSchema(db);

				var table = s.Tables.Single(t => t.TableName == "TestSchemaY");
				var fks   = table.ForeignKeys.Select(fk => fk.MemberName).ToArray();

				Assert.That(fks, Is.EqualTo(new[] { "TestSchemaX", "ParentTestSchemaX", "FK_TestSchemaY_OtherID" }));

				table = s.Tables.Single(t => t.TableName == "TestSchemaB");
				fks   = table.ForeignKeys.Select(fk => fk.MemberName).ToArray();

				AreEqual(fks, new[] { "OriginTestSchemaA", "TargetTestSchemaA", "Target_Test_Schema_A" }, _ => _);
			}
		}

		[Test]
		public void ForeignKeyMemberNameTest2([IncludeDataSources(TestProvName.Northwind)]
			string context)
		{
			using (var db = new DataConnection(context))
			{
				var p = db.DataProvider.GetSchemaProvider();
				var s = p.GetSchema(db);

				var table = s.Tables.Single(t => t.TableName == "Employees");
				var fks   = table.ForeignKeys.Select(fk => fk.MemberName).ToArray();

				Assert.That(fks, Is.EqualTo(new[] { "FK_Employees_Employees", "FK_Employees_Employees_BackReference", "Orders", "EmployeeTerritories" }));
			}
		}

		[Test]
		public void SetForeignKeyMemberNameTest()
		{
			var thisTable  = new TableSchema { TableName = "Xxx", };
			var otherTable = new TableSchema { TableName = "Zzz", };

			var key = new ForeignKeySchema
			{
				KeyName      = "FK_Xxx_YyyZzz",
				MemberName   = "FK_Xxx_YyyZzz",
				ThisColumns  = new List<ColumnSchema>
				{
					new ColumnSchema { MemberName = "XxxID", IsPrimaryKey = true },
					new ColumnSchema { MemberName = "YyyZzzID" },
				},
				OtherColumns = new List<ColumnSchema>
				{
					new ColumnSchema { MemberName = "ZzzID" },
				},
				ThisTable    = thisTable,
				OtherTable   = otherTable,
			};

			var key1 = new ForeignKeySchema
			{
				KeyName      = "FK_Xxx_Zzz",
				MemberName   = "FK_Xxx_Zzz",
				ThisColumns  = new List<ColumnSchema>
				{
					new ColumnSchema { MemberName = "XxxID", IsPrimaryKey = true },
					new ColumnSchema { MemberName = "ZzzID" },
				},
				OtherColumns = new List<ColumnSchema>
				{
					new ColumnSchema { MemberName = "ZzzID" },
				},
				ThisTable    = thisTable,
				OtherTable   = otherTable,
			};

			key.ThisTable.ForeignKeys = new List<ForeignKeySchema> { key, key1 };
			key.ThisTable.Columns     = key.ThisColumns;

			key.BackReference = new ForeignKeySchema
			{
				KeyName         = key.KeyName    + "_BackReference",
				MemberName      = key.MemberName + "_BackReference",
				AssociationType = AssociationType.Auto,
				OtherTable      = key.ThisTable,
				ThisColumns     = key.OtherColumns,
				OtherColumns    = key.ThisColumns,
			};

			SchemaProviderBase.SetForeignKeyMemberName(new GetSchemaOptions {}, key.ThisTable, key);

			Assert.That(key.MemberName, Is.EqualTo("YyyZzz"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2348")]
		public void SchemaOnlyTestIssue2348([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
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

			Assert.That(schema1.Procedures.Count, Is.EqualTo(schema2.Procedures.Count));

			for (var i = 0; i < schema1.Procedures.Count; i++)
			{
				var p1 = schema1.Procedures[i];
				var p2 = schema2.Procedures[i];

				if (p1.ResultTable == null)
				{
					Assert.IsNull(p2.ResultTable);
				}
				else
				{
					Assert.IsNotNull(p2.ResultTable);

					var t1 = p1.ResultTable;
					var t2 = p2.ResultTable!;

					Assert.That(t1.ID,                 Is.EqualTo(t2.ID));
					Assert.That(t1.CatalogName,        Is.EqualTo(t2.CatalogName));
					Assert.That(t1.SchemaName,         Is.EqualTo(t2.SchemaName));
					Assert.That(t1.TableName,          Is.EqualTo(t2.TableName));
					Assert.That(t1.Description,        Is.EqualTo(t2.Description));
					Assert.That(t1.IsDefaultSchema,    Is.EqualTo(t2.IsDefaultSchema));
					Assert.That(t1.IsView,             Is.EqualTo(t2.IsView));
					Assert.That(t1.IsProcedureResult,  Is.EqualTo(t2.IsProcedureResult));
					Assert.That(t1.TypeName,           Is.EqualTo(t2.TypeName));
					Assert.That(t1.IsProviderSpecific, Is.EqualTo(t2.IsProviderSpecific));
					Assert.That(t1.Columns.Count,      Is.EqualTo(t2.Columns.Count));

					for (var j = 0; j < p1.ResultTable.Columns.Count; j++)
					{
						var c1 = t1.Columns[j];
						var c2 = t2.Columns[j];

						Assert.That(c1.ColumnName,           Is.EqualTo(c2.ColumnName));
						Assert.That(c1.ColumnType,           Is.EqualTo(c2.ColumnType));
						Assert.That(c1.IsNullable,           Is.EqualTo(c2.IsNullable));
						Assert.That(c1.IsIdentity,           Is.EqualTo(c2.IsIdentity));
						Assert.That(c1.IsPrimaryKey,         Is.EqualTo(c2.IsPrimaryKey));
						Assert.That(c1.PrimaryKeyOrder,      Is.EqualTo(c2.PrimaryKeyOrder));
						Assert.That(c1.Description,          Is.EqualTo(c2.Description));
						Assert.That(c1.MemberName,           Is.EqualTo(c2.MemberName));
						Assert.That(c1.MemberType,           Is.EqualTo(c2.MemberType));
						Assert.That(c1.ProviderSpecificType, Is.EqualTo(c2.ProviderSpecificType));
						Assert.That(c1.SystemType,           Is.EqualTo(c2.SystemType));
						Assert.That(c1.DataType,             Is.EqualTo(c2.DataType));
						Assert.That(c1.SkipOnInsert,         Is.EqualTo(c2.SkipOnInsert));
						Assert.That(c1.SkipOnUpdate,         Is.EqualTo(c2.SkipOnUpdate));
						Assert.That(c1.Length,               Is.EqualTo(c2.Length));
						Assert.That(c1.Precision,            Is.EqualTo(c2.Precision));
						Assert.That(c1.Scale,                Is.EqualTo(c2.Scale));
					}
				}
			}
		}
	}
}
