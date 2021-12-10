using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class QueryNameTests : TestBase
	{
		[Test]
		public void TableTest([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var q = db.Parent.Select(p => p).QueryName("PARENT");

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("SELECT /* PARENT */").Or.Contains("SELECT /*+ QB_NAME(PARENT) */"));
		}
	}
}
