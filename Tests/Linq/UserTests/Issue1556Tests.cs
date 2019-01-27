using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1556Tests : TestBase
	{
		[Test]
		public void Issue1556Test([DataSources(ProviderName.Sybase, ProviderName.OracleNative)] string context)
		{
			using (var db = GetDataContext(context))
			{
//				var records1 =
//				(
//					from p in db.Parent
//					from c in db.Child
//					where p.ParentID == c.ParentID || c.GrandChildren.Any(y => y.ParentID == p.ParentID)
//					select new { p, c }
//				)
//				.ToList();

				var records = db.Parent
					.InnerJoin(db.Child,
						(p,c) => p.ParentID == c.ParentID || c.GrandChildren.Any(y => y.ParentID == p.ParentID),
						(p,c) => new { p, c })
					.ToList();
			}
		}
	}
}
