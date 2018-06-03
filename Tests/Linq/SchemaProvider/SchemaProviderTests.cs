﻿using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Tests.SchemaProvider
{
	using LinqToDB;
	using LinqToDB.Data;
	using LinqToDB.DataProvider.SqlServer;
	using LinqToDB.Mapping;
	using LinqToDB.SchemaProvider;

	[TestFixture]
	public class SchemaProviderTests : TestBase
	{
		[Test, DataContextSource(false, ProviderName.SQLiteMS
#if NETSTANDARD2_0
			, ProviderName.MySql, TestProvName.MySql57
#endif
			)]
		public void Test(string context)
		{
			SqlServerTools.ResolveSqlTypes("");

			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);

				var tableNames = new HashSet<string>();
				foreach (var schemaTable in dbSchema.Tables)
				{
					var tableName = schemaTable.CatalogName + "." +
						(schemaTable.IsDefaultSchema ? schemaTable.TableName : schemaTable.SchemaName + "." + schemaTable.TableName);

					if (tableNames.Contains(tableName))
						Assert.Fail("Not unique table " + tableName);

					tableNames.Add(tableName);

					var columnNames = new HashSet<string>();
					foreach (var schemaColumm in schemaTable.Columns)
					{
						if(columnNames.Contains(schemaColumm.ColumnName))
							Assert.Fail("Not unique column {0} for table {1}.{2}", schemaColumm.ColumnName, schemaTable.SchemaName, schemaTable.TableName);

						columnNames.Add(schemaColumm.ColumnName);
					}
				}

				var table = dbSchema.Tables.SingleOrDefault(t => t.TableName.ToLower() == "parent");

				Assert.That(table,                                           Is.Not.Null);
				Assert.That(table.Columns.Count(c => c.ColumnName != "_ID"), Is.EqualTo(2));

				AssertType<Model.LinqDataTypes>(conn.MappingSchema, dbSchema);
				AssertType<Model.Parent>       (conn.MappingSchema, dbSchema);

				Assert.That(dbSchema.Tables.Single(t => t.TableName.ToLower() == "doctor").ForeignKeys.Count, Is.EqualTo(1));

				switch (context)
				{
					case ProviderName.SqlServer2000 :
					case ProviderName.SqlServer2005 :
					case ProviderName.SqlServer2008 :
					case ProviderName.SqlServer2012 :
					case ProviderName.SqlServer2014 :
					case TestProvName.SqlAzure      :
						{
							var indexTable = dbSchema.Tables.Single(t => t.TableName == "IndexTable");
							Assert.That(indexTable.ForeignKeys.Count,                Is.EqualTo(1));
							Assert.That(indexTable.ForeignKeys[0].ThisColumns.Count, Is.EqualTo(2));
						}
						break;

					case ProviderName.Informix      :
						{
							var indexTable = dbSchema.Tables.First(t => t.TableName == "testunique");
							Assert.That(indexTable.Columns.Count(c => c.IsPrimaryKey), Is.EqualTo(2));
							Assert.That(indexTable.ForeignKeys.Count(), Is.EqualTo(2));
						}
						break;
				}

				switch (context)
				{
					case ProviderName.SqlServer2008 :
					case ProviderName.SqlServer2012 :
					case ProviderName.SqlServer2014 :
					case TestProvName.SqlAzure      :
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

			var schemaTable = dbSchema.Tables.FirstOrDefault(_ => _.TableName.Equals(e.TableName, StringComparison.OrdinalIgnoreCase));
			Assert.IsNotNull(schemaTable, e.TableName);

			Assert.That(schemaTable.Columns.Count >= e.Columns.Count);

			foreach (var column in e.Columns)
			{
				var schemaColumn = schemaTable.Columns.FirstOrDefault(_ => _.ColumnName.Equals(column.ColumnName, StringComparison.InvariantCultureIgnoreCase));
				Assert.IsNotNull(schemaColumn, column.ColumnName);

				if (column.CanBeNull)
					Assert.AreEqual(column.CanBeNull, schemaColumn.IsNullable, column.ColumnName + " Nullable");

				Assert.AreEqual(column.IsPrimaryKey, schemaColumn.IsPrimaryKey, column.ColumnName + " PrimaryKey");
			}

			//Assert.That(schemaTable.ForeignKeys.Count >= e.Associations.Count);
		}

		[Test, NorthwindDataContext(false, true)]
		public void NorthwindTest(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);

				Assert.IsNotNull(dbSchema);
			}
		}

#if !NETSTANDARD2_0

		[Test, IncludeDataContextSource(ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57)]
		public void MySqlTest(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
				var table    = dbSchema.Tables.Single(t => t.TableName.Equals("alltypes", StringComparison.OrdinalIgnoreCase));

				Assert.That(table.Columns[0].MemberType, Is.Not.EqualTo("object"));

				Assert.That(table.Columns.Single(c => c.ColumnName.Equals("intUnsignedDataType", StringComparison.OrdinalIgnoreCase)).MemberType, Is.EqualTo("uint?"));

				var view = dbSchema.Tables.Single(t => t.TableName.Equals("personview", StringComparison.OrdinalIgnoreCase));

				Assert.That(view.Columns.Count, Is.EqualTo(1));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57)]
		public void MySqlPKTest(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
				var table    = dbSchema.Tables.Single(t => t.TableName.Equals("person", StringComparison.OrdinalIgnoreCase));
				var pk       = table.Columns.FirstOrDefault(t => t.IsPrimaryKey);

				Assert.That(pk, Is.Not.Null);
			}
		}

#endif

		class PKTest
		{
			[PrimaryKey(1)] public int ID1;
			[PrimaryKey(2)] public int ID2;
		}

		[Test, IncludeDataContextSource(ProviderName.PostgreSQL)]
		public void PostgreSQLTest(string context)
		{
			using (var conn = new DataConnection(context))
			{
				try { conn.DropTable<PKTest>(); } catch {}

				conn.CreateTable<PKTest>();

				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
				var table    = dbSchema.Tables.Single(t => t.TableName == "PKTest");

				Assert.That(table.Columns[0].PrimaryKeyOrder, Is.EqualTo(1));
				Assert.That(table.Columns[1].PrimaryKeyOrder, Is.EqualTo(2));

				conn.DropTable<PKTest>();
			}
		}

		[Test, IncludeDataContextSource(ProviderName.DB2)]
		public void DB2Test(string context)
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

		[Test, DataContextSource(false, ProviderName.SQLiteMS
#if NETSTANDARD2_0
			, ProviderName.MySql, TestProvName.MySql57
#endif
			)]
		public void IncludeExcludeCatalogTest(string context)
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

		[Test, DataContextSource(false, ProviderName.SQLiteMS
#if NETSTANDARD2_0
			, ProviderName.MySql, TestProvName.MySql57
#endif
			)]
		public void IncludeExcludeSchemaTest(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var exclude = conn.DataProvider.GetSchemaProvider()
					.GetSchema(conn, new GetSchemaOptions {ExcludedSchemas = new string[] { null }})
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

		[Test, IncludeDataContextSource(ProviderName.SQLiteClassic)]
		public void SchemaProviderNormalizeName(string context)
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

		[Test, DataContextSource(false, ProviderName.SQLiteMS
#if NETSTANDARD2_0
			, ProviderName.MySql, TestProvName.MySql57
#endif
			)]
		public void PrimaryForeignKeyTest(string context)
		{
			using (var db = new DataConnection(context))
			{
				var p = db.DataProvider.GetSchemaProvider();
				var s = p.GetSchema(db);

				var fkCountDoctor = s.Tables.Single(_ => _.TableName.Equals(nameof(Model.Doctor), StringComparison.OrdinalIgnoreCase)).ForeignKeys.Count;
				var pkCountDoctor = s.Tables.Single(_ => _.TableName.Equals(nameof(Model.Doctor), StringComparison.OrdinalIgnoreCase)).Columns.Count(_ => _.IsPrimaryKey);

				Assert.AreEqual(1, fkCountDoctor);
				Assert.AreEqual(1, pkCountDoctor);

				var fkCountPerson = s.Tables.Single(_ => _.TableName.Equals(nameof(Model.Person), StringComparison.OrdinalIgnoreCase) && !(_.SchemaName ?? "").Equals("MySchema", StringComparison.OrdinalIgnoreCase)).ForeignKeys.Count;
				var pkCountPerson = s.Tables.Single(_ => _.TableName.Equals(nameof(Model.Person), StringComparison.OrdinalIgnoreCase) && !(_.SchemaName ?? "").Equals("MySchema", StringComparison.OrdinalIgnoreCase)).Columns.Count(_ => _.IsPrimaryKey);

				Assert.AreEqual(2, fkCountPerson);
				Assert.AreEqual(1, pkCountPerson);
			}
		}

		[Test, IncludeDataContextSource(false,
			ProviderName.SqlServer2005, ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void ForeignKeyMemberNameTest1(string context)
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

				Assert.That(fks, Is.EqualTo(new[] { "OriginTestSchemaA", "TargetTestSchemaA", "Target_Test_Schema_A" }));
			}
		}

		[Test, IncludeDataContextSource(false, TestProvName.Northwind)]
		public void ForeignKeyMemberNameTest2(string context)
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
	}
}
