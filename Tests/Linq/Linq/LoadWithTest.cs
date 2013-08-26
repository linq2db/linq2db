using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class LoadWithTest : TestBase
	{
		[Test]
		public void LoadWith1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q =
					from p in db.GetTable<Northwind.Product>().LoadWith(p => p.Category.Products[0].OrderDetails)
					select p;

				q.ToList();
			}
		}
	}
}
