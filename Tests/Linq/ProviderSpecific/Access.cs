using System;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.ProviderSpecific
{
	[TestFixture]
	public class Access : TestBase
	{
		[Test]
		public void SqlTest([IncludeDataContexts(ProviderName.Access)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var res = db.Execute(@"
					UPDATE
						[Child] [c]
							LEFT JOIN [Parent] [t1] ON [c].[ParentID] = [t1].[ParentID]
					SET
						[ChildID] = @id
					WHERE
						[c].[ChildID] = @id1 AND [t1].[Value1] = 1",
					new { id1 = 1001, id = 1002 });
			}
		}
	}
}
