using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class MultipleQueryTests : TestBase
	{
		//[Test]
		public void Test1([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children,
					from p in db.Parent select p.Children);
		}

		//[Test]
		public void Test2([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.ToList(),
					from p in db.Parent select p.Children.ToList());
		}

		[Test]
		public void Test3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child,
					from p in db.Parent select db.Child);
		}

		[Test]
		public void Test4([DataSources] string context)
		{
			using (new AllowMultipleQuery())
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from p in    Parent select p.Children.Select(c => c.ChildID),
					from p in db.Parent select p.Children.Select(c => c.ChildID));
			}
		}

		[Test]
		public void Test5([DataSources(ProviderName.Sybase)] string context)
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
