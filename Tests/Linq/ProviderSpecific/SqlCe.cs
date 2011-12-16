using System;
using System.Transactions;

using LinqToDB;

namespace Tests.ProviderSpecific
{
	using Model;

	//[TestFixture]
	public class SqlCe : TestBase
	{
		//[Test]
		public void SqlTest()
		{
			using (new TransactionScope())
			{
				using (var db = new TestDbManager(ProviderName.SqlCe))
				{
					var list = db
						.SetCommand(@"

UPDATE
	[Parent]
SET
	[Value1] = 1
WHERE
	[Parent].[ParentID] = 100;

INSERT INTO [Parent] 
(
	[ParentID],
	[Value1]
)
VALUES
(
	100,
	NULL
)


")
						.ExecuteScalar();

					list = db
						.SetCommand(@"SELECT @@IDENTITY")
						.ExecuteScalar();
				}
			}
		}
	}
}
