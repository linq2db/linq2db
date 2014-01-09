using System;
using System.Linq;

using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

namespace Tests.SchemaProvider
{
	using LinqToDB;

	[TestFixture]
	public class SchemaProviderTest : TestBase
	{
		[Test, DataContextSource(false)]
		public void Test(string context)
		{
			SqlServerTools.ResolveSqlTypes("");

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

				switch (context)
				{
					case ProviderName.SqlServer2000 :
					case ProviderName.SqlServer2005 :
					case ProviderName.SqlServer2008 :
					case ProviderName.SqlServer2012 :
						var indexTable = dbSchema.Tables.Single(t => t.TableName == "IndexTable");
						Assert.That(indexTable.ForeignKeys.Count,                Is.EqualTo(1));
						Assert.That(indexTable.ForeignKeys[0].ThisColumns.Count, Is.EqualTo(2));
						break;
				}
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
