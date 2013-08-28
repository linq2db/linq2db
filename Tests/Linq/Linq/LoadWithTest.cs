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
					select p;

				q.ToList();
			}
		}
	}
}
