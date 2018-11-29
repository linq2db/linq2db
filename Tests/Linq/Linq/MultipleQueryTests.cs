using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class MultipleQueryTests : TestBase
	{
		//[Test, DataContextSource]
		public void Test1(string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children,
					from p in db.Parent select p.Children);
		}

		//[Test, DataContextSource]
		public void Test2(string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.ToList(),
					from p in db.Parent select p.Children.ToList());
		}

		[Test, DataContextSource]
		public void Test3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child,
					from p in db.Parent select db.Child);
		}

		[Test, DataContextSource]
		public void Test4(string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in    Parent.OrderBy(p => p.ParentID) select p.Children.Select(c => c.ChildID),
					from p in db.Parent.OrderBy(p => p.ParentID) select p.Children.Select(c => c.ChildID));
			}
		}

		[Test, DataContextSource(ProviderName.Sybase)]
		public void Test5(string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child
					orderby ch.ChildID
					select    Parent.Where(p => p.ParentID == ch.Parent.ParentID).Select(p => p)
					,
					from ch in db.Child
					orderby ch.ChildID
					select db.Parent.Where(p => p.ParentID == ch.Parent.ParentID).Select(p => p));
		}
	}
}
