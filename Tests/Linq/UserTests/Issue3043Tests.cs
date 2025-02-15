using System;
using System.Linq;
using System.Linq.Expressions;

using FluentAssertions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3043Tests : TestBase
	{
		public class City3043
		{
			[Column, PrimaryKey]
			public int Id;

			[Column]
			public string Name { get; set; } = null!;

			[Column]
			public int Population { get; set; }
		}

		public class Street3043
		{
			[Column, PrimaryKey]
			public int Id;

			[Column]
			public int CityId { get; set; }
	
			[Association(ThisKey = nameof(CityId), OtherKey = "Id", CanBeNull = true)]
			public City3043? City { get; set; }
	
			[ExpressionMethod(nameof(CityInfoExpression))]
			public string? CityInfo { get; set; }

			private static Expression<Func<Street3043, string>> CityInfoExpression()
			{
				return entity => entity.City!.Name + " " + entity.City.Population;
			}
		}

		public class House3043
		{
			[Column, PrimaryKey]
			public int Id;

			[Column]
			public int StreetId { get; set; }
	
			[Association(ThisKey = nameof(StreetId), OtherKey = "Id", CanBeNull = true)]
			public Street3043? Street { get; set; }
		}

		public class Person3043
		{
			[Column, PrimaryKey]
			public int Id;

			[Column]
			public int HouseId;

			[Association(ThisKey = nameof(HouseId), OtherKey = "Id", CanBeNull = true)]
			public House3043? House { get; set; }
		}

		public class Pet3043
		{
			[Column, PrimaryKey]
			public int Id;

			[Column]
			public int PersonId;

			[Association(ThisKey = nameof(PersonId), OtherKey = "Id", CanBeNull = false)]
			public Person3043 Person { get; set; } = null!;

			[ExpressionMethod(nameof(HouseExpression))]
			public House3043? House { get; set; }

			private static Expression<Func<Pet3043, House3043>> HouseExpression()
			{
				return entity => entity.Person.House!;
			}
		}

		[Test]
		public void TestExpressionExposing([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db          = GetDataContext(context);
			using var cityTable   = db.CreateLocalTable(new[] {new City3043 {Id   = 1, Name   = "City", Population = 100}});
			using var streetTable = db.CreateLocalTable(new[] {new Street3043 {Id = 1, CityId = 1}});
			using var houseTable  = db.CreateLocalTable(new[] {new House3043 {Id  = 1, StreetId = 1}});
			using var personTable = db.CreateLocalTable(new[] {new Person3043 {Id = 1, HouseId = 1}});
			using var petTable    = db.CreateLocalTable(new[] {new Pet3043 {Id = 1, PersonId = 1}});

			var data = petTable
				.Select(x => new
				{
					x.Id,
					x.House!.Street!.CityInfo
				})
				.First();

			data.CityInfo.Should().Be("City 100");
		}
	}
}
