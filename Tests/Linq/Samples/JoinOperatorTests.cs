using System;
using System.Linq;

using NUnit.Framework;

namespace Tests.Samples
{
	using Model;

	[TestFixture]
	public class JoinOperatorTests : TestBase
	{
		[Test, NorthwindDataContext]
		public void InnerJoinOnSingleColumn(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var query =
					from c in db.Category
					join p in db.Product on c.CategoryID equals p.CategoryID
					where !p.Discontinued
					select c;

				foreach (var category in query)
				{
#if !APPVEYOR
					Console.WriteLine(category.CategoryID);
#endif
				}
			}
		}

		[Test, NorthwindDataContext]
		public void InnerJoinOnMultipleColumns(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var query =
					from p in db.Product
					from o in db.Order
					join d in db.OrderDetail
						on     new { p.ProductID, o.OrderID }
						equals new { d.ProductID, d.OrderID }
					select new
					{
						p.ProductID,
						o.OrderID,
					};

				var data = query.ToArray();
				Assert.IsNotEmpty(data);
			}
		}
	}
}
