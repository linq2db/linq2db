using System;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB;

	[TestFixture]
	public class AsyncTest : TestBase
	{
		[Test, DataContextSource(false)]
		public async void Test(string context)
		{
			Test1(context);

			using (var db = GetDataContext(context + ".LinqService"))
			{
				var list = await db.Parent.ToArrayAsync();
				Assert.AreNotEqual(list.Length, Is.Not.EqualTo(0));
			}
		}

		[Test, DataContextSource(false)]
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
