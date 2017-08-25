using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Linq;
using NUnit.Framework;
using System;
using System.Linq;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue825Tests : TestBase
	{
		[Test, DataContextSource]
		public void TestCount(string context)
		{
			using (var db = GetDataContext(context))
			{
				var grandChildId = 322;
				var childId = 32;

				var query = db.Parent
					.Where(p => p.GrandChildren.Any(g => g.GrandChildID == grandChildId))
					.SelectMany(parent => parent.Children)
					.Where(child => child.ChildID == childId)
					.Select(child => child.Parent);

				var result = query.ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(3, result[0].ParentID);

			}
		}
	}
}
