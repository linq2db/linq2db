using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class DataContextTests : TestBase
	{
		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014, ProviderName.SapHana)]
		public void TestContext(string context)
		{
			var ctx = new DataContext(context);

			ctx.GetTable<Person>().ToList();

			ctx.KeepConnectionAlive = true;

			ctx.GetTable<Person>().ToList();
			ctx.GetTable<Person>().ToList();

			ctx.KeepConnectionAlive = false;

			using (var tran = new DataContextTransaction(ctx))
			{
				ctx.GetTable<Person>().ToList();

				tran.BeginTransaction();

				ctx.GetTable<Person>().ToList();
				ctx.GetTable<Person>().ToList();

				tran.CommitTransaction();
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014, ProviderName.SapHana)]
		public void TestContextToString(string context)
		{
			using (var ctx = new DataContext(context))
			{
				Console.WriteLine(ctx.GetTable<Person>().ToString());

				var q =
					from s in ctx.GetTable<Person>()
					select s.FirstName;

				Console.WriteLine(q.ToString());
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
		public void Issue210(string context)
		{
			using (var ctx = new DataContext(context))
			{
				ctx.KeepConnectionAlive = true;
				ctx.KeepConnectionAlive = false;
			}
		}
	}
}
