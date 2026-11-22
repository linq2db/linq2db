using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

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

			[ExpressionMethod(nameof(HasNoLevelsExpression), IsColumn = true)]
			public bool HasNoLevels { get; set; }

			private static Expression<Func<Pet3001, House3001>> HouseExpression()
			{
				return entity => entity.Person.House;
			}

			private static Expression<Func<Pet3001, bool>> IsHouseMultiLevelExpression()
			{
				// Replacing code below to entity.Parent.House.Levels > 1 will make column work
				return entity => entity.House.Levels > 1;
			}

			private static Expression<Func<Pet3001, bool>> HasNoLevelsExpression() => entity => entity.House.Levels < 1;
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

			data.IsHouseMultiLevel.ShouldBeTrue();
		}

		[Test]
		public void TestExpressionAssociationMissingHouse([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context, [Values] bool preferClient)
		{
			using var db          = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var houseTable  = db.CreateLocalTable(new[] {new House3001  {Id = 1, Levels   = 2}});
			using var personTable = db.CreateLocalTable(new[] {new Person3001 {Id = 1, HouseId  = 1}, new Person3001 {Id = 2, HouseId  = 99}});
			using var petTable    = db.CreateLocalTable(new[] {new Pet3001    {Id = 1, PersonId = 1}, new Pet3001    {Id = 2, PersonId = 2}});

			var data = petTable
				.OrderBy(x => x.Id)
				.Select(x => new {x.Id, x.IsHouseMultiLevel, x.HasNoLevels})
				.ToList();

			data.Select(x => (x.Id, x.IsHouseMultiLevel, x.HasNoLevels)).ShouldBe(new[] {(1, true, false), (2, false, true)});

			// A predicate keeps SQL NULL, so a missing house satisfies neither filter, unlike the projection.
			petTable.Where(x => x.HasNoLevels).Select(x => x.Id).ToList().ShouldBeEmpty();
			petTable.Where(x => x.IsHouseMultiLevel).Select(x => x.Id).ToList().ShouldBe(new[] {1});
		}
	}
}
