using System.Linq;
using System.Threading.Tasks;
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
		public async Task TestProperty_IsNull_ServerSideOnly(
		[IncludeDataSources(TestProvName.Northwind)] string context)
		{
			string defaultCategory = "test";
			string statement = "ISNULL([c_1].[CategoryName], @defaultCategory)";
			using (var db = new NorthwindDB(context))
			{
				var query = (from c in db.Category
							 select Sql.Ext.SqlServer().IsNull(c.CategoryName, defaultCategory));
				Assert.That(query.ToString()!.Contains(statement));

				var results = await query.ToListAsync();
				Assert.IsTrue(results.Any());
				Assert.That(db.LastQuery!.Contains(statement));
			}
		}

		[Test]
		public async Task TestStatementWithOrderBy_IsNull_ServerSideOnly(
		[IncludeDataSources(TestProvName.Northwind)] string context)
		{
			int  categoryId = 1;
			string statement = "ISNULL([p].[UnitPrice], 10)";
			using (var db = new NorthwindDB(context))
			{
				var supplierQuery =  GetSupplierIdWithMaxUnitPrice(categoryId, db);
				Assert.That(supplierQuery.ToString()!.Contains(statement));

				var results = await supplierQuery.ToListAsync();
				Assert.IsTrue(results.Any());
				Assert.That(db.LastQuery!.Contains(statement));
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
