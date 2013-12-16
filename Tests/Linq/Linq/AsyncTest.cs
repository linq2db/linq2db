using System;

namespace Tests.Linq
{
	using System.Linq;

	using LinqToDB;

	using NUnit.Framework;

	[TestFixture]
	public class AsyncTest : TestBase
	{
		[Test]
		public async void Test([DataContexts(ExcludeLinqService=true)] string context)
		{
			Test1(context);

			using (var db = GetDataContext(context + ".LinqService"))
			{
				var list = await db.Parent.ToArrayAsync();
				Assert.AreNotEqual(list.Length, Is.Not.EqualTo(0));
			}
		}
		public void Test1(string context)
		{
			using (var db = GetDataContext(context + ".LinqService"))
			{
				var list = db.Parent.ToArrayAsync().Result;
				Assert.AreNotEqual(list.Length, Is.Not.EqualTo(0));
			}
		}
	}
}
