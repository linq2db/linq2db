using System;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.Access;

using NUnit.Framework;

namespace Tests.Extensions
{
	[TestFixture]
	public class AccessTests : TestBase
	{
		[Test]
		public void QueryHintTest([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from p in db.Parent
					select p
				)
				.QueryHint(AccessHints.Query.WithOwnerAccessOption);

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH OWNERACCESS OPTION"));
		}

		[Test]
		public void WithOwnerAccessOptionTest([IncludeDataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				(
					from p in db.Parent
					select p
				)
				.AsAccess()
				.WithOwnerAccessOption();

			_ = q.ToList();

			Assert.That(LastQuery, Contains.Substring("WITH OWNERACCESS OPTION"));
		}
	}
}
