using System.Linq;
using LinqToDB;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Linq;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class IsNullTests : TestBase
	{
		[Test]
		public void TestExceptionThrown_IsNull_ServerSideOnly()
		{
			Assert.Throws<LinqException>(() => Sql.Ext.SqlServer().IsNull(10, 1000));
		}

		[Test]
		public void TestProperty_IsNull_ServerSideOnly(
		[IncludeDataSources(TestProvName.Northwind)] string context)
		{
			string defaultCategory = "test";
			using (var db = new NorthwindDB(context))
			{
				var query = (from c in db.Category
							 select Sql.Ext.SqlServer().IsNull(c.CategoryName, defaultCategory));

				Assert.That(query.ToString().Contains("ISNULL([c_1].[CategoryName], @defaultCategory)"));
			}
		}

		[Test]
		public void TestStatementWithOrderBy_IsNull_ServerSideOnly(
		[IncludeDataSources(TestProvName.Northwind)] string context)
		{
			int  categoryId = 1;
			using (var db = new NorthwindDB(context))
			{
				var supplierQuery = GetSupplierIdWithMaxUnitPrice(categoryId, db);
				Assert.That(supplierQuery.ToString().Contains("ISNULL([p].[UnitPrice], 10)"));
			}
		}

		private IQueryable<int?> GetSupplierIdWithMaxUnitPrice(int categoryId, NorthwindDB db)
		{
			return (from p in db.Product
					where p.CategoryID == categoryId
					orderby Sql.Ext.SqlServer().IsNull(p.UnitPrice, 10) descending
					select p.SupplierID);
		}
	}
}
