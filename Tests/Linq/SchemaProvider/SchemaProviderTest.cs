using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class SchemaProviderTest : DataProviderTestBase
	{
		[Test]
		public void Test([DataContexts(
			ProviderName.Access,
			ProviderName.DB2,
			ProviderName.Firebird,
			ProviderName.Informix,
			ProviderName.MySql,
			ProviderName.Oracle,
			ProviderName.PostgreSQL,
			ProviderName.SqlCe,
			ProviderName.Sybase, ExcludeLinqService=true)] string context)
		{
			SqlServerFactory.ResolveSqlTypesPath("");

			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);

				dbSchema.Tables.ToDictionary(
					t => t.IsDefaultSchema ? t.TableName : t.SchemaName + "." + t.TableName,
					t => t.Columns.ToDictionary(c => c.ColumnName));

				var table = dbSchema.Tables.SingleOrDefault(t => t.TableName == "Parent");

				Assert.That(table,               Is.Not.Null);
				Assert.That(table.Columns.Count, Is.EqualTo(2));

				Assert.That(dbSchema.Tables.Single(t => t.TableName == "Doctor").ForeignKeys.Count, Is.EqualTo(1));
			}
		}

		[Test]
		public void NorthwindTest([IncludeDataContexts("Northwind")] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var sp       = conn.DataProvider.GetSchemaProvider();
				var dbSchema = sp.GetSchema(conn);
			}
		}
	}
}
