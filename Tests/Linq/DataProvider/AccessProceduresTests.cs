using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class AccessProceduresTests : DataProviderTestBase
	{
		[ActiveIssue]
		[Test]
		public void TestT4ProcedureCallOleDb([IncludeDataSources(ProviderName.Access)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var id = PersonSelectByNameOLEDB(db, "Jürgen", "König");
				Assert.AreEqual(4, id);
			}
		}

		[ActiveIssue]
		[Test]
		public void TestT4ProcedureCallODBC([IncludeDataSources(ProviderName.Access)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var id = PersonSelectByNameODBC(db, "Jürgen", "König");
				Assert.AreEqual(4, id);
			}
		}

		public static int PersonSelectByNameOLEDB(DataConnection dataConnection, string? @firstName, string? @lastName)
		{
			return dataConnection.ExecuteProc("Person_SelectByName",
				new DataParameter("@firstName", @firstName, DataType.NText),
				new DataParameter("@lastName" , @lastName , DataType.NText));
		}

		public static int PersonSelectByNameODBC(DataConnection dataConnection, string? @firstName, string? @lastName)
		{
			return dataConnection.ExecuteProc("CALL Person_SelectByName(?, ?)",
				new DataParameter("@firstName", @firstName, DataType.NText),
				new DataParameter("@lastName", @lastName, DataType.NText));
		}
	}
}
