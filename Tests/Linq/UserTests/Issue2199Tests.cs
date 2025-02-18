using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2199Tests : TestBase
	{

		[Table("Manufacturer")]
		public class Manufacturer
		{
			[Column]
			[PrimaryKey]
			public int ManufacturerId { get; set; }

			[Column]
			public string? Name { get; set; }

			[Column]
			public string? CountryCode { get; set; }

			[Association(ThisKey = nameof(CountryCode), OtherKey = nameof(Country.Code))]
			public Country? CountryEntity { get; set; }
		}

		[Table("Country")]
		public class Country
		{
			[Column(CanBeNull = false, Length = 10)]
			[PrimaryKey]
			public string? Code { get; set; }

			[Column]
			public string? Name { get; set; }
		}

		[Test]
		public void LeftJoinTests([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Manufacturer>())
			using (db.CreateLocalTable<Country>())
			{
				var man1 = from e in db.GetTable<Manufacturer>()
					join c in db.GetTable<Country>() on e.CountryCode equals c.Code into ce
					from co in ce.DefaultIfEmpty()
					where e.ManufacturerId == 1
					select new Manufacturer { ManufacturerId=e.ManufacturerId, CountryCode=e.CountryCode,Name=e.Name,CountryEntity=co};

				var man2 = from e in db.GetTable<Manufacturer>()
					join c in db.GetTable<Country>() on e.CountryCode equals c.Code into ce
					from co in ce.DefaultIfEmpty()
					where e.ManufacturerId == 2
					select new Manufacturer { ManufacturerId = e.ManufacturerId, CountryCode = e.CountryCode, Name = e.Name, CountryEntity = co };

				var query1 = from m1 in man1
					join m2 in man2 on 1 equals 1 into manjoin
					from m in manjoin.DefaultIfEmpty()
					select new Tuple<Manufacturer, Manufacturer>(m1, m);

				var result = query1.ToArray();
			}
		}

		[Test]
		public void LeftJoinTests2([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<Manufacturer>())
			using (db.CreateLocalTable<Country>())
			{
				var man1 = from e in db.GetTable<Manufacturer>()
					from co in db.GetTable<Country>().LeftJoin(co => co.Code == e.CountryCode)
					where e.ManufacturerId == 1
					select new Manufacturer { ManufacturerId=e.ManufacturerId, CountryCode=e.CountryCode,Name=e.Name,CountryEntity=co};

				var man2 = from e in db.GetTable<Manufacturer>()
					from co in db.GetTable<Country>().LeftJoin(co => co.Code == e.CountryCode)
					where e.ManufacturerId == 2
					select new Manufacturer { ManufacturerId = e.ManufacturerId, CountryCode = e.CountryCode, Name = e.Name, CountryEntity = co };

				var query1 = from m1 in man1
					join m2 in man2 on 1 equals 1 into manjoin
					from m in manjoin.DefaultIfEmpty()
					select new Tuple<Manufacturer, Manufacturer>(m1, m);

				var result = query1.ToArray();
			}
		}

	}
}
