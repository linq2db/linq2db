using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class LoadWithTest : TestBase
	{
		[Test]
		public void LoadWith1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Parent.LoadWith(p => p.Children.First().GrandChildren[0].Child.Parent)
					select new
					{
						p.GrandChildren.Count,
						p
					};

				q.ToList();

				var q1 = q.Select(t => t.p).SelectMany(p => p.Children);

				q1.ToList();
			}
		}

		[Test]
		public void LoadWith2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.Child.LoadWith(p => p.GrandChildren[0].Child.Parent)
					select new
					{
						p.GrandChildren.Count,
						p.Parent
					};

				q.ToList();

				var q1 = q.Select(t => t.Parent).SelectMany(p => p.Children).Distinct();

				q1.ToList();
			}
		}
	}
}
