using System.Linq;

using NUnit.Framework;

using Shouldly;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public class MySqlTests : NorthwindContextTestBase
	{
		[Test]
		public void SimpleProviderTest([EFDataSources] string provider)
		{
			using var db = CreateContext(provider);

			var items = db.Customers.Where(e => e.Address != null).ToLinqToDB().ToArray();
		}

		[Test(Description = "https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1801")]
		public void TestFunctionTranslation([EFDataSources] string provider)
		{
			using var db = CreateContext(provider);
			var items = db.Customers.Where(e => e.Address!.Contains("anything")).ToLinqToDB().ToArray();
		}

		[Test(Description = "https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1801")]
		public void TestFunctionTranslationParameter([EFDataSources] string provider)
		{
			using var db = CreateContext(provider);

			var value = "anything";
			var items = db.Customers.Where(e => e.Address!.Contains(value)).ToLinqToDB().ToArray();
		}

		[Test(Description = "https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1801")]
		public void TranslationAfterReaderContextDisposed([EFDataSources] string provider)
		{
			LinqToDBForEFTools.ClearCaches();

			try
			{
				using (var ctx = CreateContext(provider))
				using (ctx.CreateLinqToDBConnection())
				{
				}

				using var db = CreateContext(provider);

				var items = db.Customers
					.Where(e => e.Address!.Contains("a"))
					.ToLinqToDB()
					.ToArray();

				items.ShouldNotBeEmpty();
			}
			finally
			{
				LinqToDBForEFTools.ClearCaches();
			}
		}

		[Test(Description = "https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1801")]
		public void EfTranslationAfterReaderTranslationOnSameContext([EFDataSources] string provider)
		{
			LinqToDBForEFTools.ClearCaches();

			try
			{
				using var db = CreateContext(provider);

				var linqToDBItems = db.Customers
					.Where(e => e.Address!.Contains("a"))
					.ToLinqToDB()
					.ToArray();

				var efItems = db.Customers
					.Where(e => e.Address!.Contains("a"))
					.ToArray();

				linqToDBItems.ShouldNotBeEmpty();
				efItems.ShouldNotBeEmpty();
			}
			finally
			{
				LinqToDBForEFTools.ClearCaches();
			}
		}
	}
}
