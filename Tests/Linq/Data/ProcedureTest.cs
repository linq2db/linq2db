using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Data
{
	using Model;

	[TestFixture]
	public class ProcedureTest : TestBase
	{
		public static IEnumerable<Person> PersonSelectByKey(DataConnection dataConnection, int? @id)
		{
			return dataConnection.QueryProc<Person>("[TestData]..[Person_SelectByKey]",
				new DataParameter("@id", @id));
		}

		[Test]
		public void Test([IncludeDataContexts(ProviderName.SqlServer2008)] string context)
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
