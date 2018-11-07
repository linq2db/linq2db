using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Data
{
	using Model;
	using System.Data.SqlClient;

	[TestFixture]
	public class ProcedureTests : TestBase
	{
		public static IEnumerable<Person> PersonSelectByKey(DataConnection dataConnection, int? @id)
		{
			var databaseName = TestUtils.GetDatabaseName(dataConnection);
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
			var escapedTableName = new SqlCommandBuilder().QuoteIdentifier(databaseName);
#else
			var escapedTableName = "[" + databaseName + "]";
#endif
			return dataConnection.QueryProc<Person>(escapedTableName + "..[Person_SelectByKey]",
				new DataParameter("@id", @id));
		}

		[Test]
		public void Test([IncludeDataSources(
			ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
			string context)
		{
			using (var db = new DataConnection(context))
			{
				var p1 = PersonSelectByKey(db, 1).First();
				var p2 = db.Query<Person>("SELECT * FROM Person WHERE PersonID = @id", new { id = 1 }).First();

				Assert.AreEqual(p1, p2);
			}
		}
	}
}
