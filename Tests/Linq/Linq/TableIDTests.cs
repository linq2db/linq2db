using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class TableIDTests : TestBase
	{
		//[Test]
		public void TableTest([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var q = db.Parent.TableID("pp");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /* PARENT */"));
		}
	}
}
