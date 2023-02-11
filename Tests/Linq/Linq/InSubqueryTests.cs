using System;
using System.Linq;

using LinqToDB.Tools;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class InSubqueryTests : TestBase
	{
		[Test]
		public void InTest([DataSources] string context)
		{
			using var db  = GetDataContext(context);

			var q =
				from c in db.Child
				where c.ParentID.In(db.Parent.Select(p => p.ParentID))
				select c;

			_ = q.ToList();
		}

		[Test]
		public void InWithTakeTest([DataSources] string context)
		{
			using var db  = GetDataContext(context);

			var q =
				from c in db.Child
				where c.ParentID.In(db.Parent.Select(p => p.ParentID).Take(100))
				select c;

			_ = q.ToList();
		}

		[Test]
		public void ObjectInTest([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db  = GetDataContext(context);

			var q =
				from c in db.Child
				where new { c.ParentID, Value = c.ParentID }.In(db.Parent.Select(p => new { p.ParentID, p.Value1!.Value }))
				select c;

			_ = q.ToList();
		}

		[Test]
		public void ObjectInWithTakeTest([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db  = GetDataContext(context);

			var q =
				from c in db.Child
				where new { c.ParentID, Value = c.ParentID }.In(db.Parent.Select(p => new { p.ParentID, p.Value1!.Value }).Take(100))
				select c;

			_ = q.ToList();
		}
	}
}
