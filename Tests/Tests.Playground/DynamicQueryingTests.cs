using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class DynamicQueryingTests : TestBase
	{
		public class Car : BaseEntity
		{
			[Column]
			public string Brand { get; set; }

			[Column]
			public string Color { get; set; }

			[Column]
			public int TopSpeed { get; set; }
		}

		public class SomePerson : BaseEntity
		{
			[PrimaryKey]
			public int Id { get; set; }
			public string FirstName { get; set; }
			public string LastName { get; set; }

			public int Age { get; set; }

			public int? AddressId { get; set; }

			[Association(ThisKey = nameof(AddressId), OtherKey = nameof(SomeAddress.Id), CanBeNull = true)]
			public SomeAddress Address { get; set; }

			[Association(ThisKey = nameof(AddressId), OtherKey = nameof(SomeAddress.Id), CanBeNull = true)]
			public List<SomeAddress> Addresses { get; set; }
		}

		public class SomeAddress : BaseEntity
		{
			public int Id { get; set; }
			public string City { get; set; }
			public string Street { get; set; }
		}

		private static readonly MethodInfo _getTaTableMethodInfo =
			MemberHelper.MethodOf<IDataContext>(dc => dc.GetTable<object>()).GetGenericMethodDefinition();

		public static ITable<TBase> GetDynamicTable<TBase>(IDataContext dc, string kind)
		{
			Type type;
			switch (kind)
			{
				case "Car":
					type = typeof(Car);
					break;
				case "Person":
					type = typeof(SomePerson);
					break;
				default:
					throw new ArgumentException();
			}

			var method = _getTaTableMethodInfo.MakeGenericMethod(type);
			var result = (ITable<TBase>)method.Invoke(null, new[] {dc});
			return result;
		}

		[Test]
		public void SampleSelectTest([DataSources(false)] string context)
		{
			using (var db = new DynamicConnection(context))
			using (db.CreateLocalTable(new[]{new Car{ Brand = "Mercedes", Color = "Black", TopSpeed = 250} }))
			using (db.CreateLocalTable(new[]{new SomeAddress{Id = 1, City = "NY", Street = "4t Avenue"}}))
			using (db.CreateLocalTable(new[]{new SomePerson{Id = 33, AddressId = 1, Age = 40, FirstName = "John", LastName = "Smith"} }))
			{
//				ITable<BaseEntity> table = GetDynamicTable<BaseEntity>(db, "Car");
//				var query = table.Where(e =>
//					e["Brand"] == "Mercedes"
//					&& e["TopSpeed"] == 1
//					&& e["TopSpeed"] > 1
//					&& e["TopSpeed"] >= 1
//					&& e["TopSpeed"] <= 1
//					&& e["TopSpeed"] < 1);
//
//				var result = query.FirstOrDefault();
//
//				var result2 = table.Where(e =>
//					e["Brand"] == "Mercedes" && e["TopSpeed"] >= 1).First();

				ITable<BaseEntity> persons = GetDynamicTable<BaseEntity>(db, "Person");

//				var person1 = persons.Where(p => p["Address.City"] == "NY").First();
//
//				var person2 = persons.Where(p => Sql.Property<string>(Sql.Property<BaseEntity>(p, "Address"), "City") == "NY").First();
//
//				var person3 = persons.Where(p => p["FirstName"] == "John")
//					.FirstOrDefault(p => p["LastName"] == "Smith");
//
//				var person4 = persons
//					.Where(p => p["Address.City"] == "NY")
//					.FirstOrDefault(p => p["LastName"] == "Smith");
//
//				var person5 = persons
//					.Where(p => p["Address"]["City"] == "NY")
//					.FirstOrDefault();
//
//				var person6 = persons
//					.Where(p => p["Addresses"].Any())
//					.FirstOrDefault();
//
				var person7 = persons
					.Where(p => p["Addresses"].Any(a => a["City"] == "NY"))
					.FirstOrDefault();

				var addressQuery = 
					from p in persons.Where(p => p["Addresses"].Count() == 1)
					from a in p["Addresses"]
					where a["City"] == "NY"
					select new
					{
						PP = p,
						AA = a
					};

				var addreses = addressQuery.ToArray();

				var addressQuery2 = from p in persons.Where(p => p["Addresses"].Count() == 1) select p;
//				var addressQuery2 = persons.Where(p => p["Addresses"].Count() == 1).Select(p => p);

				var addressQueryUp = persons.Where(p => p["FirstName"] == "John")
					.Set(a => a["FirstName"], "Me")
					.Update();

				db.Into(persons)
					.Value(p => p["FirstName"], "Sarah")
					.Value(p => p["LastName"], "Connor")
					.Value(p => p["Age"], 10)
					.Value(p => p["Id"], 44)
					.Insert();

				var personU = persons.FirstOrDefault();

				personU["FirstName"].SetValue("Superman");

				db.Update(personU);

				var addreses2 = addressQuery2.ToArray();
			}
		}
	}
}
