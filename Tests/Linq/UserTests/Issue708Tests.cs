using System;
using System.Linq;
using LinqToDB;
using NUnit.Framework;

namespace Tests.UserTests
{

	[TestFixture]
	public class Issue708Tests : TestBase
	{
		[Test, DataContextSource]
		public void Test2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var id = 1;

				var parents1 =    Parent.Where(_ => _.ParentID == id).Select(_ => _.ParentID);
				var parents2 = db.Parent.Where(_ => _.ParentID == id).Select(_ => _.ParentID);

				var query1 =    Child.Where(_ => parents1.Contains(_.ChildID) && _.ChildID >= 0 && _.ChildID <= 100);
				var query2 = db.Child.Where(_ => parents2.Contains(_.ChildID) && _.ChildID >= 0 && _.ChildID <= 100);

				AreEqual(query1, query2);
			}
		}
	}
}