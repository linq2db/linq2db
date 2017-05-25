using System.Diagnostics;
using System.Linq;
using LinqToDB.Data;
using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue676Tests : TestBase
	{
		[Test, NorthwindDataContext]
		public void Test(string context)
		{
			using (var db = new NorthwindDB(context))
			{
				DataConnection.TurnTraceSwitchOn(TraceLevel.Verbose);
				DataConnection.WriteTraceLine += (a, b) => { System.Diagnostics.Debug.WriteLine(a + " -> " + b); };

				var lstOrder = db.Order.ToList();
				var lstCustomer = db.Customer.ToList();

				var ljj = from o in lstOrder
					join c in lstCustomer on o.CustomerID equals c.CustomerID into cg
					from c in cg.DefaultIfEmpty().Take(1)
					select new {o, c};

				var lres = ljj.ToList();

				var ljj2 = from o in lstOrder
					join c in lstCustomer.Take(1) on o.CustomerID equals c.CustomerID into cg
					from c in cg.DefaultIfEmpty()
					select new {o, c};

				var lres2 = ljj2.ToList();

				var ljj3 = from o in lstOrder
					from c in lstCustomer.Where(x => x.CustomerID == o.CustomerID).DefaultIfEmpty().Take(1)
					select new {o, c};

				var lres3 = ljj3.ToList();

				var ljj4 = from o in lstOrder
					from c in lstCustomer.Take(1).Where(x => x.CustomerID == o.CustomerID).DefaultIfEmpty()
					select new {o, c};

				var lres4 = ljj4.ToList();

				var ljj5 = from o in lstOrder
					join c in lstCustomer on o.CustomerID equals c.CustomerID into cg
					from c in cg.Take(1).DefaultIfEmpty()
					select new { o, c };

				var lres5 = ljj5.ToList();





				var jj = from o in db.Order
					join c in db.Customer on o.CustomerID equals c.CustomerID into cg
					from c in cg.DefaultIfEmpty().Take(1)
					select new {o, c};

				var res = jj.ToList();

				var jj2 = from o in db.Order
					join c in db.Customer.Take(1) on o.CustomerID equals c.CustomerID into cg
					from c in cg.DefaultIfEmpty()
					select new {o, c};

				var res2 = jj2.ToList();

				var jj3 = from o in db.Order
					from c in db.Customer.Where(x => x.CustomerID == o.CustomerID).DefaultIfEmpty().Take(1)
					select new {o, c};

				var res3 = jj3.ToList();

				var jj4 = from o in db.Order
					from c in db.Customer.Take(1).Where(x => x.CustomerID == o.CustomerID).DefaultIfEmpty()
					select new {o, c};

				var res4 = jj4.ToList();

				var jj5 = from o in db.Order
					join c in db.Customer on o.CustomerID equals c.CustomerID into cg
					from c in cg.Take(1).DefaultIfEmpty()
					select new { o, c };

				var res5 = jj5.ToList();
			}
		}
	}
}
