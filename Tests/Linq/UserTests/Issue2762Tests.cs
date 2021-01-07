using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests.Issue2762Tests
{
	public class Base
	{
		public int Id { get; set; }
	}

	public class Alps : Base
	{
		public int TreeId { get; set; }
		public int RaceStartId { get; set; }
		public string? Issue { get; set; }
		public DateTime Date1 { get; set; }

	}

	public class Beach : Base
	{
		public int SamanthaId { get; set; }
		public string? Name { get; set; }
	}

	public class Samantha : Base
	{
		public int SamanthaNameId { get; set; }
	}

	public class SamanthaName : Base
	{
		public string? Name { get; set; }
	}

	public class Stone : Base
	{
		public int PowerId { get; set; }
		public DateTime Date2 { get; set; }
	}

	public class Apple : Base
	{
		public int FlintId { get; set; }
		public bool Flag { get; set; }
		public string? Identifier2 { get; set; }

		public static string Fastest = "FASTEST";
	}

	public class Flint : Base
	{
		public int BicycleId { get; set; }
		public int PowerVersionId { get; set; }
		public int CarId { get; set; }
		public bool? Flag { get; set; }
		public string? Name { get; set; }
	}

	public class Car : Base
	{
		public bool? Flag { get; set; }
		public string? Name { get; set; }
	}

	public class Bicycle : Base
	{
		public string? Name { get; set; }
	}

	public class PowerVersion : Base
	{

	}

	public class Power : Base
	{
		public string? Identifier { get; set; }
	}

	public class PowerHistory : Base
	{
		public int PowerId { get; set; }
		public Power? Power { get; set; }

		public int AppleId { get; set; }
		public Apple? Apple { get; set; }
		public DateTime InstallationDate { get; set; }
		public DateTime? DeinstallationDate { get; set; }
	}

	// no entity
	public class PowerHistoryApplePower
	{
		public PowerHistory? PowerHistory { get; set; }
		public Apple? Apple { get; set; }
		public Power? Power { get; set; }
	}

	// no entity
	public class FlintAppleHelper
	{
		public Flint? Flint { get; set; }
		public Apple? Apple { get; set; }
		public Power? Power { get; set; }
		public PowerHistory? PowerHistory { get; set; }
	}

	public class RankStone
	{
		public AlpsOverviewModel? Stone { get; set; }
		public long Rank { get; set; }
	}

	// no entity
	public class AlpsOverviewModel
	{
		public string? DisplayName { get; set; }
		public Apple? Apple { get; set; }
		public Car? Car { get; set; }
		public SamanthaName? SamanthaName { get; set; }
		public Power? PowerOfSnapshot { get; set; }
		public Power? PowerOfApple { get; set; }
		public PowerHistory? PowerHistoryPowerOfSnapshot { get; set; }
		public PowerHistory? PowerHistoryApple { get; set; }
		public PowerVersion? PowerVersion { get; set; }
		public Bicycle? Bicycle { get; set; }
		public Beach? Beach { get; set; }
		public Flint? Flint { get; set; }
		public Alps? Alps { get; set; }

	}

	public static class FlintExtensions
	{
		[ExpressionMethod(nameof(FilterFlagByExpression))]
		public static bool FilterFlagBy(this Flint flint, bool? flag)
		{
			return !flag.HasValue ||
			flag.Value == false && (!flint.Flag.HasValue || flint.Flag.Value == false) ||
			flag.Value && flint.Flag.HasValue && flint.Flag.Value;
		}

		public static Expression<Func<Flint, bool?, bool>> FilterFlagByExpression()
		{
			return (x, flag) => !flag.HasValue ||
			flag.Value == false && (!x.Flag.HasValue || x.Flag.Value == false) ||
			flag.Value && x.Flag.HasValue && x.Flag.Value;
		}
	}

	public static class CarExtensions
	{
		[ExpressionMethod(nameof(FilterFlagByExpression))]
		public static bool FilterFlagBy(this Car car, bool? flag)
		{
			return !flag.HasValue ||
			flag.Value == false && (!car.Flag.HasValue || car.Flag.Value == false) ||
			flag.Value && car.Flag.HasValue && car.Flag.Value;
		}

		public static Expression<Func<Car, bool?, bool>> FilterFlagByExpression()
		{
			return (x, flag) => !flag.HasValue ||
			flag.Value == false && (!x.Flag.HasValue || x.Flag.Value == false) ||
			flag.Value && x.Flag.HasValue && x.Flag.Value;
		}
	}

	public static class AppleExtensions
	{
		[ExpressionMethod(nameof(Hidden))]
		public static bool FilterFlagBy(this Apple apple, bool? flag = false)
		{
			return !flag.HasValue || apple.Flag == flag.Value;
		}

		public static Expression<Func<Apple, bool?, bool>> Hidden()
		{
			return (x, flag) => !flag.HasValue || x.Flag == flag.Value;
		}

		[ExpressionMethod(nameof(FilterIsInStockByNullableBool))]
		public static bool FilterIsInStockBy(this Apple apple, bool? isInStock)
		{
			return !isInStock.HasValue || isInStock.Value && apple.Identifier2 == Apple.Fastest ||
			!isInStock.Value && apple.Identifier2 != Apple.Fastest;
		}

		public static Expression<Func<Apple, bool?, bool>> FilterIsInStockByNullableBool()
		{
			return (x, isInStock) => !isInStock.HasValue || isInStock.Value && x.Identifier2 == Apple.Fastest ||
			!isInStock.Value && x.Identifier2 != Apple.Fastest;
		}
	}

	public static class QueryExtensions
	{
		public static IQueryable<Flint> GetFlints(CountContext context, bool? flag = false)
		{
			return context.Flints.Where(x => x.FilterFlagBy(flag));
		}

		public static IQueryable<FlintAppleHelper> GetFlintAppleHelpers(CountContext context, bool? flag = false, bool? isPowerStock = false)
		{
			return from flint in GetFlints(context, flag)
				   from apple in context.Apples.Where(x => x.FlintId == flint.Id && x.FilterIsInStockBy(isPowerStock) && x.FilterIsInStockBy(flag))
				   select new FlintAppleHelper { Apple = apple, Flint = flint };
		}

		public static IQueryable<PowerHistoryApplePower> GetHist(CountContext context, bool? flag = false, bool? isInStock = false)
		{
			var query = (
				from appleInfo in GetFlintAppleHelpers(context, flag, isInStock)
				from powerHistory in context.PowerHistories.Where(x => x.AppleId == appleInfo.Apple.Id)
				from power in context.Powers.Where(x => x.Id == powerHistory.PowerId)
				select new PowerHistoryApplePower
				{
					Apple = appleInfo.Apple,
					Power = power,
					PowerHistory = powerHistory
				});

			return query;
		}
	}

	public class CountContext : DataConnection
	{
		public CountContext(string configurationString, MappingSchema mappingSchema)
		: base(configurationString, mappingSchema) { }

		public ITable<Alps> Alpss => GetTable<Alps>();
		public ITable<Beach> Beachs => GetTable<Beach>();
		public ITable<Samantha> Assemblies => GetTable<Samantha>();
		public ITable<SamanthaName> SamanthaNames => GetTable<SamanthaName>();
		public ITable<Stone> Stones => GetTable<Stone>();
		public ITable<Apple> Apples => GetTable<Apple>();
		public ITable<Flint> Flints => GetTable<Flint>();
		public ITable<Car> Cars => GetTable<Car>();
		public ITable<Bicycle> Bicycles => GetTable<Bicycle>();
		public ITable<PowerVersion> PowerVersions => GetTable<PowerVersion>();
		public ITable<Power> Powers => GetTable<Power>();
		public ITable<PowerHistory> PowerHistories => GetTable<PowerHistory>();
	}

	[TestFixture]
	public class Issue2762Tests : TestBase
	{
		private IQueryable<AlpsOverviewModel> GetQueryable1(CountContext context, bool? flag = false)
		{
			return GetQueryable2(context, flag).Select(x => new RankStone
			{
				Stone = x,
				Rank = Sql.Ext.RowNumber().Over().PartitionBy(x.Alps.Id)
				.OrderBy(x.Car.Name)
				.ThenBy(x.Apple.Identifier2)
				.ThenBy(x.Flint.Name)
				.ThenBy(x.Alps.Date1)
				.ThenBy(x.Bicycle.Name)
				.ThenBy(x.PowerOfApple.Identifier)


				.ToValue()
			})
			.Where(x => x.Rank == 1)
			.Select(x => x.Stone);
		}

		private IQueryable<AlpsOverviewModel> GetQueryable2(CountContext context, bool? flag = false)
		{
			var hist = QueryExtensions.GetHist(context, flag, false);

			return (
				from alps in context.Alpss
				from beach in context.Beachs.Where(x => x.Id == alps.TreeId).DefaultIfEmpty()
				from samantha in context.Assemblies.Where(x => x.Id == beach.SamanthaId)
				from samanthaName in context.SamanthaNames.Where(x => x.Id == samantha.SamanthaNameId)
				from begin in context.Stones.Where(x => x.Id == alps.RaceStartId)
				from powerInstallationSnapshot in hist.Where(x =>
				x.Power.Id == begin.PowerId
				&& x.PowerHistory.InstallationDate <= begin.Date2
				&& (x.PowerHistory.DeinstallationDate == null || begin.Date2 < x.PowerHistory.DeinstallationDate)
				)
				from powerInstallationApple in hist.Where(x =>
				x.Apple.Id == powerInstallationSnapshot.Apple.Id
				&& x.PowerHistory.InstallationDate <= begin.Date2
				&& (x.PowerHistory.DeinstallationDate == null || begin.Date2 < x.PowerHistory.DeinstallationDate)

				)
				from apple in context.Apples.Where(x => x.Id == powerInstallationSnapshot.Apple.Id && x.FilterFlagBy(flag) && x.FilterIsInStockBy(flag))
				from flint in context.Flints.Where(x => x.Id == apple.FlintId && x.FilterFlagBy(flag))
				from car in context.Cars.Where(x => x.Id == flint.CarId && x.FilterFlagBy(flag))
				from bicycle in context.Bicycles.Where(x => x.Id == flint.BicycleId)
				from powerVersion in context.PowerVersions.Where(x => x.Id == flint.PowerVersionId).DefaultIfEmpty()

				select new AlpsOverviewModel
				{
					Apple = apple,
					Car = car,
					SamanthaName = samanthaName,
					PowerOfApple = powerInstallationApple.Power,
					PowerHistoryApple = powerInstallationApple.PowerHistory,
					PowerOfSnapshot = powerInstallationSnapshot.Power,
					PowerHistoryPowerOfSnapshot = powerInstallationSnapshot.PowerHistory,
					Bicycle = bicycle,
					PowerVersion = powerVersion,
					Beach = beach,
					Flint = flint,
					Alps = alps,
					DisplayName = string.IsNullOrEmpty(alps.Issue) && beach != null && samanthaName != null && !string.IsNullOrEmpty(beach.Name)
					&& !string.IsNullOrEmpty(samanthaName.Name) ? $"{beach.Name} {samanthaName.Name}" : alps.Issue
				}


			);
		}

		private void InitDb(CountContext context)
		{
			context.CreateLocalTable<Alps>();
			context.CreateLocalTable<Beach>();
			context.CreateLocalTable<Samantha>();
			context.CreateLocalTable<SamanthaName>();
			context.CreateLocalTable<Stone>();
			context.CreateLocalTable<Apple>();
			context.CreateLocalTable<Flint>();
			context.CreateLocalTable<Car>();
			context.CreateLocalTable<Bicycle>();
			context.CreateLocalTable<PowerVersion>();
			context.CreateLocalTable<Power>();
			context.CreateLocalTable<PowerHistory>();
		}

		private MappingSchema GetMappingSchema()
		{
			var mappingSchema = new MappingSchema();
			var fluentMappingBuilder = mappingSchema.GetFluentMappingBuilder();

			var aBuilder = fluentMappingBuilder.Entity<Alps>();
			aBuilder.Property(x => x.Id);
			aBuilder.Property(x => x.TreeId);
			aBuilder.Property(x => x.Issue);
			aBuilder.Property(x => x.RaceStartId);

			var bBuilder = fluentMappingBuilder.Entity<Beach>().HasTableName("Issue2762Beach");
			bBuilder.Property(x => x.Id);
			//bBuilder.Property(x => x.Name);

			var cBuilder = fluentMappingBuilder.Entity<Samantha>().HasTableName("Issue2762Samantha");
			cBuilder.Property(x => x.Id);
			//cBuilder.Property(x => x.Name);

			var dBuilder = fluentMappingBuilder.Entity<SamanthaName>().HasTableName("Issue2762SamanthaName");
			dBuilder.Property(x => x.Id);
			//dBuilder.Property(x => x.Name);

			var eBuilder = fluentMappingBuilder.Entity<Stone>().HasTableName("Issue2762Stone");
			eBuilder.Property(x => x.Id);
			//eBuilder.Property(x => x.Name);

			var fBuilder = fluentMappingBuilder.Entity<Apple>().HasTableName("Issue2762Apple");
			fBuilder.Property(x => x.Id);
			//fBuilder.Property(x => x.Name);

			var gBuilder = fluentMappingBuilder.Entity<Flint>().HasTableName("Issue2762Flint");
			gBuilder.Property(x => x.Id);
			//gBuilder.Property(x => x.Name);

			var hBuilder = fluentMappingBuilder.Entity<Car>().HasTableName("Issue2762Car");
			hBuilder.Property(x => x.Id);
			//hBuilder.Property(x => x.Name);

			var iBuilder = fluentMappingBuilder.Entity<Bicycle>().HasTableName("Issue2762Bicycle");
			iBuilder.Property(x => x.Id);
			//iBuilder.Property(x => x.Name);

			var jBuilder = fluentMappingBuilder.Entity<PowerVersion>().HasTableName("Issue2762PowerVersion");
			jBuilder.Property(x => x.Id);
			// jBuilder.Property(x => x.Name);

			var kBuilder = fluentMappingBuilder.Entity<Power>().HasTableName("Issue2762Power");
			kBuilder.Property(x => x.Id);
			// kBuilder.Property(x => x.Name);

			var lBuilder = fluentMappingBuilder.Entity<PowerHistory>().HasTableName("Issue2762PowerHistory");
			lBuilder.Property(x => x.Id);
			// lBuilder.Property(x => x.Name);

			return mappingSchema;
		}

		[Test]
		public void Issue2762Test([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllOracleManaged)] string context)
		{
			using (var db = new CountContext(context, GetMappingSchema()))
			{
				InitDb(db);
				var count = GetQueryable1(db).Count();
				var sw = new Stopwatch();


				sw.Start();
				count = GetQueryable1(db).Count();
				sw.Stop();


				var elapsed = sw.Elapsed;
				var lastQuery = db.LastQuery;

				//File.WriteAllText("ResultingSql.txt", lastQuery);
			}
		}
	}
}
