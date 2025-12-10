using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2800Tests : TestBase
	{
		public class Car
		{
			public int Id { get; set; }
			public string Name { get; set; } = null!;
		}

		public static class CarExtensions
		{
			[ExpressionMethod(nameof(FilterBySpecialStringExpression))]
			public static bool FilterBySpecialString(Car car, bool? isSpecial = false)
			{
				return !isSpecial.HasValue || isSpecial.Value && car.Name == "Special" || !isSpecial.Value && car.Name != "Special";
			}

			public static Expression<Func<Car, bool?, bool>> FilterBySpecialStringExpression()
			{
				return (x, isSpecial) => isSpecial == null || isSpecial.Value && x.Name == "Special" || !isSpecial.Value && x.Name != "Special";
			}
		}

		[Test]
		public void TestExpressionMethod([DataSources] string context)
		{
			var fluentMappingBuilder = new FluentMappingBuilder(new MappingSchema());

			var carBuilder = fluentMappingBuilder.Entity<Car>();
			carBuilder.Property(x => x.Id).IsPrimaryKey();
			carBuilder.Property(x => x.Name).HasLength(50);

			fluentMappingBuilder.Build();

			var records = new Car[]
			{
				new Car { Id = 1, Name = "Special" },
				new Car { Id = 2, Name = "NoSpecial" }
			};

			using var db = GetDataContext(context, fluentMappingBuilder.MappingSchema);
			using var carTable = db.CreateLocalTable<Car>(records);
			var isSpecial = (bool?)null;
			AssertQuery(carTable.Where(x => CarExtensions.FilterBySpecialString(x, isSpecial)));
			isSpecial = false;
			AssertQuery(carTable.Where(x => CarExtensions.FilterBySpecialString(x, isSpecial)));
			isSpecial = true;
			AssertQuery(carTable.Where(x => CarExtensions.FilterBySpecialString(x, isSpecial)));

			AssertQuery(carTable.Where(x => CarExtensions.FilterBySpecialString(x, null)));
			AssertQuery(carTable.Where(x => CarExtensions.FilterBySpecialString(x, false)));
			AssertQuery(carTable.Where(x => CarExtensions.FilterBySpecialString(x, true)));
		}
	}
}
