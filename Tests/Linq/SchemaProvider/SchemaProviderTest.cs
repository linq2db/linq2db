using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

namespace Tests.SchemaProvider
{
	[TestFixture]
	public class SchemaProviderTest : TestBase
	{
		[Test]
		public void Test([DataContexts(
			ProviderName.DB2,
			ProviderName.Firebird,
			ProviderName.Informix,
			ProviderName.Oracle,
			ProviderName.PostgreSQL,
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

				var table = dbSchema.Tables.SingleOrDefault(t => t.TableName.ToLower() == "parent");

				Assert.That(table,               Is.Not.Null);
				Assert.That(table.Columns.Count, Is.EqualTo(2));

//				Assert.That(dbSchema.Tables.Single(t => t.TableName.ToLower() == "doctor").ForeignKeys.Count, Is.EqualTo(1));
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
