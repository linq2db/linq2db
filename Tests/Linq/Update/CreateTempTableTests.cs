using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	public class CreateTempTableTests : TestBase
	{
		class IDTable
		{
			public int ID;
		}

		[Test, DataContextSource(ProviderName.OracleNative)]
		public void CreateTable1(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists:false);

				using (var tmp = db.CreateTempTable(
					"TempTable",
					db.Parent.Select(p => new IDTable { ID = p.ParentID })))
				{
					var list =
					(
						from t in tmp
						select t
					).ToList();
				}
			}
		}

		[Test, DataContextSource(ProviderName.OracleNative)]
		public void CreateTable2(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<int>("TempTable", throwExceptionIfNotExists:false);

				using (var tmp = db.CreateTempTable(
					"TempTable",
					db.Parent.Select(p => new { ID = p.ParentID })))
				{
					var list =
					(
						from t in tmp
						select t
					).ToList();
				}
			}
		}
	}
}
