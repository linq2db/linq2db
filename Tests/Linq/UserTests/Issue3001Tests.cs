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
	public class Issue3001Tests : TestBase
	{
		public class House3001
		{
			[Column] [PrimaryKey] public int Id;

			[Column]
			public int Levels { get; set; }
		}

		public class Person3001
		{
			[Column] public int HouseId;

			[Column] [PrimaryKey] public int Id;

			[Association(ThisKey = nameof(HouseId), OtherKey = "Id", CanBeNull = true)]
			public House3001 House { get; set; } = null!;
		}

		public class Pet3001
		{
			[Column] [PrimaryKey] public int Id;

			[Column] public int PersonId;

			[Association(ThisKey = nameof(PersonId), OtherKey = "Id", CanBeNull = false)]
			public Person3001 Person { get; set; } = null!;

			[ExpressionMethod(nameof(HouseExpression))]
			public House3001 House { get; set; } = null!;

			[ExpressionMethod(nameof(IsHouseMultiLevelExpression), IsColumn = true)]
			public bool IsHouseMultiLevel { get; set; }

			private static Expression<Func<Pet3001, House3001>> HouseExpression()
			{
				return entity => entity.Person.House;
			}

			private static Expression<Func<Pet3001, bool>> IsHouseMultiLevelExpression()
			{
				// Replacing code below to entity.Parent.House.Levels > 1 will make column work
				return entity => entity.House.Levels > 1;
			}
		}

		[Test]
		public void TestExpressionAssociation([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)]
			string context)
		{
			using var db          = GetDataContext(context);
			using var houseTable  = db.CreateLocalTable(new[] {new House3001  {Id = 1, Levels   = 2}});
			using var personTable = db.CreateLocalTable(new[] {new Person3001 {Id = 1, HouseId  = 1}});
			using var petTable    = db.CreateLocalTable(new[] {new Pet3001    {Id = 1, PersonId = 1}});

			var data = petTable
				.Select(x => new {x.Id, x.IsHouseMultiLevel})
				.First();

			data.IsHouseMultiLevel.Should().BeTrue();
		}
	}
}
