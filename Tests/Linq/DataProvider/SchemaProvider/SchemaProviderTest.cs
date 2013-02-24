using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.DataProvider.SchemaProvider
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
			ProviderName.SQLite,
			ProviderName.Sybase, ExcludeLinqService=true)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				var dbSchema = conn.DataProvider.GetSchema(conn);

				switch (context)
				{
					case ProviderName.SqlServer2005 :
					case ProviderName.SqlServer2008 :
					case ProviderName.SqlServer2012 :
						Assert.That(dbSchema.Tables.Single(t => t.TableName == "Parent"), Is.Not.Null);
						break;
				}
			}
		}
	}
}
