using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2787Tests : TestBase
	{
		[Table]
		class Car
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column(CanBeNull = false)]
			public string? Name { get; set; }
		}

		[Table]
		class Tyre
		{
			[PrimaryKey]
			public int CarId { get; set; }
		}

		class MyProjection
		{
			public Car? Car { get; set; }
			public Tyre? Tyre { get; set; }
		}

		[Test]
		public void Issue2787Test([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<Car>(throwExceptionIfNotExists: false);
				db.DropTable<Tyre>(throwExceptionIfNotExists: false);
				using (var carTable = db.CreateLocalTable<Car>())
				using (var tyreTable = db.CreateLocalTable<Tyre>())
				{
					var query  = (from car in carTable
								  from tyre in tyreTable.Where(x => x.CarId == car.Id)
								  select new MyProjection
								  {
									  Car = car,
									  Tyre = tyre
								  });


					var res = query.Select(x => new {a = x.Car.Id == 3 ? x.Car.Name : string.Empty}).ToList();

					var lastQuery = ((DataConnection) db).LastQuery;

					Assert.IsTrue(lastQuery?.ToLower().Contains("case"));
				}
			}
		}
	}
}
