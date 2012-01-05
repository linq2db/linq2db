using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class MultipleQuery : TestBase
	{
		//[Test]
		public void Test1([DataContexts] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children,
					from p in db.Parent select p.Children);

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		//[Test]
		public void Test2([DataContexts] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.ToList(),
					from p in db.Parent select p.Children.ToList());

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test]
		public void Test3([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select    Child,
					from p in db.Parent select db.Child);
		}

		[Test]
		public void Test4([DataContexts] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Parent select p.Children.Select(c => c.ChildID),
					from p in db.Parent select p.Children.Select(c => c.ChildID));

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}

		[Test]
		public void Test5([DataContexts] string context)
		{
			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = true;

			using (var db = GetDataContext(context))
				AreEqual(
					from ch in    Child
					orderby ch.ChildID
					select    Parent.Where(p => p.ParentID == ch.Parent.ParentID).Select(p => p),
					from ch in db.Child
					orderby ch.ChildID
					select db.Parent.Where(p => p.ParentID == ch.Parent.ParentID).Select(p => p));

			LinqToDB.Common.Configuration.Linq.AllowMultipleQuery = false;
		}
	}
}
