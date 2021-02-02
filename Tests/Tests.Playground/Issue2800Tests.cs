using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class Issue2800Tests : TestBase
	{
		public class Car
		{
			public int Id { get; set; }
			public string Name { get; set; } = null!;
			public bool IsSet { get; set; }
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
		public void TestExpressionMethod([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values(null, true, false)] bool? isSet)
		{
			var fluentMappingBuilder = new MappingSchema().GetFluentMappingBuilder();

			var carBuilder = fluentMappingBuilder.Entity<Car>();
			carBuilder.Property(x => x.Id).IsPrimaryKey();
			carBuilder.Property(x => x.Name).HasLength(50);

			using (var db = GetDataContext(context, fluentMappingBuilder.MappingSchema))
			using (var table = db.CreateLocalTable<Car>())
			{

				//db.Insert(new Car { Id = 1, Name = "MyCar", IsSet = true });

				var carTable = db.GetTable<Car>();

				var result = carTable.Where(x => CarExtensions.FilterBySpecialString(x, isSet)).ToList();

				
			}
		}
	}
}
