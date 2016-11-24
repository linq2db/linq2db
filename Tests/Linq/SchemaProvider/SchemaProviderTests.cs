using System;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.SchemaProvider
{
	using System.Collections.Generic;

	using LinqToDB;
	using LinqToDB.SchemaProvider;

	[TestFixture]
	public class SchemaProviderTests : TestBase
	{
		[Test, DataContextSource(false)]
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
					var tableName = schemaTable.CatalogName + "."
									+ (schemaTable.IsDefaultSchema
										? schemaTable.TableName
										: schemaTable.SchemaName + "." + schemaTable.TableName);

					if (tableNames.Contains(tableName))
						Assert.Fail("Not unique table " + tableName);

					tableNames.Add(tableName);

					var columnNames = new HashSet<string>();
					foreach (var schemaColumm in schemaTable.Columns)
					{
						if(columnNames.Contains(schemaColumm.ColumnName))
							Assert.Fail("Not unique column {0} for table {1}.{2}", schemaColumm.ColumnName, schemaTable.SchemaName, schemaTable.TableName);
					}
				}

				var table = dbSchema.Tables.SingleOrDefault(t => t.TableName.ToLower() == "parent");

				Assert.That(table,                                           Is.Not.Null);
				Assert.That(table.Columns.Count(c => c.ColumnName != "_ID"), Is.EqualTo(2));

				AssertType<Model.LinqDataTypes>(conn.MappingSchema, dbSchema);
				AssertType<Model.Parent>       (conn.MappingSchema, dbSchema);

//				Assert.That(dbSchema.Tables.Single(t => t.TableName.ToLower() == "doctor").ForeignKeys.Count, Is.EqualTo(1));

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

		[Test, NorthwindDataContext]
		public void NorthwindTest(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57)]
		public void MySqlTest(string context)
		{
			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
				var table    = dbSchema.Tables.Single(t => t.TableName == "alltypes");

				Assert.That(table.Columns[0].MemberType, Is.Not.EqualTo("object"));

				Assert.That(table.Columns.Single(c => c.ColumnName == "intUnsignedDataType").MemberType, Is.EqualTo("uint?"));

				var view = dbSchema.Tables.Single(t => t.TableName == "personview");

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
				var table    = dbSchema.Tables.Single(t => t.TableName == "person");
				var pk       = table.Columns.FirstOrDefault(t => t.IsPrimaryKey);

				Assert.That(pk, Is.Not.Null);
			}
		}

		class PKTest
		{
#pragma warning disable 0649
			[PrimaryKey(1)] public int ID1;
			[PrimaryKey(2)] public int ID2;
#pragma warning restore 0649
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
	}
}
