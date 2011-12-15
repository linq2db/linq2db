using System;

using LinqToDB.Data.DataProvider;

using NUnit.Framework;

namespace Tests.ProviderSpecific
{
	using Model;

	[TestFixture]
	public class Access : TestBase
	{
		[Test]
		public void SqlTest()
		{
			using (var db = new TestDbManager(ProviderName.Access))
			{
				var res = db
					.SetCommand(@"
						UPDATE
							[Child] [c]
								LEFT JOIN [Parent] [t1] ON [c].[ParentID] = [t1].[ParentID]
						SET
							[ChildID] = @id
						WHERE
							[c].[ChildID] = @id1 AND [t1].[Value1] = 1
",
						db.Parameter("@id1", 1001),
						db.Parameter("@id", 1002))
					.ExecuteNonQuery();
			}
		}
	}
}
