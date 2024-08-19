using System.Linq;

using NUnit.Framework;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public class PomeloMySqlTests : NorthwindContextTestBase
	{
		[Test]
		public void SimpleProviderTest([EFDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var db = CreateContext(provider);

			var items = db.Customers.Where(e => e.Address != null).ToLinqToDB().ToArray();
		}

		[Test(Description = "https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1801")]
		public void TestFunctionTranslation([EFDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var db = CreateContext(provider);
			var items = db.Customers.Where(e => e.Address!.Contains("anything")).ToLinqToDB().ToArray();
		}

		[Test(Description = "https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/issues/1801")]
		public void TestFunctionTranslationParameter([EFDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var db = CreateContext(provider);

			var value = "anything";
			var items = db.Customers.Where(e => e.Address!.Contains(value)).ToLinqToDB().ToArray();
		}
	}
}
