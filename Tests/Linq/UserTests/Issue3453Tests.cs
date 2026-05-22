using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;

using Npgsql;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3453Tests : TestBase
	{
		[Table(Schema = "public", Name = "schedule")]
		public class Schedule
		{
			[Column("id"), PrimaryKey, Identity] public int Id { get; set; } // integer
			[Column("unit", DataType = DataType.Enum)] public TimeUnit Unit { get; set; } // USER-DEFINED
			[Column("unit_nullable", DataType = DataType.Enum)] public TimeUnit? UnitNullable { get; set; } // USER-DEFINED
			[Column("amount")] public int Amount { get; set; } // integer
		}

		[Table(Schema = "public", Name = "schedule")]
		public class ScheduleWithTypes
		{
			[Column("id"), PrimaryKey, Identity] public int Id { get; set; } // integer
			[Column("unit", DataType = DataType.Enum, DbType = "time_unit")] public TimeUnit Unit { get; set; } // USER-DEFINED
			[Column("unit_nullable", DataType = DataType.Enum, DbType = "time_unit")] public TimeUnit? UnitNullable { get; set; } // USER-DEFINED
			[Column("amount")] public int Amount { get; set; } // integer
		}

		public enum TimeUnit
		{
			[MapValue("hour")]
			Hour,
			[MapValue("day")]
			Day,
		}

		private DataOptions SetupEnums(string context, bool withTable)
		{
			var mappingSchema = new MappingSchema();

			const string initScript = @"
DROP TABLE IF EXISTS schedule;
DROP TYPE IF EXISTS time_unit;
CREATE TYPE time_unit AS ENUM ('hour', 'day');";

			const string createTableScript = @"
CREATE TABLE IF NOT EXISTS schedule
(
  id SERIAL CONSTRAINT schedule_pk PRIMARY KEY,
  unit                 time_unit   NOT NULL,
  unit_nullable        time_unit       NULL,
  amount               int         NOT NULL
);
INSERT INTO schedule(unit, unit_nullable,amount) VALUES ('day','day',1),('day','day',2),('day','day',3);";

			// executing separately, we have to reload just created types
			IDataProvider dataProvider;
			string?       connectionString;
			using (var db = GetDataConnection(context))
			{
				db.Execute(initScript);
				if (withTable)
					db.Execute(createTableScript);

				dataProvider     = db.DataProvider;
				connectionString = db.ConnectionString;
			}

			var builder = new NpgsqlDataSourceBuilder(connectionString);
			builder.MapEnum<TimeUnit>("time_unit");
			var dataSource = builder.Build();

			return new DataOptions()
				.UseConnectionFactory(dataProvider, _ => dataSource.CreateConnection())
				.UseMappingSchema(mappingSchema);
		}

		[Test]
		public void EnumMappingTest([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			var       unit         = TimeUnit.Day;
			TimeUnit? unitNullable = TimeUnit.Day;
			TimeUnit? unitNull     = null;

			using var db = GetDataConnection(SetupEnums(context, true));
			db.Insert(new Schedule { Unit = TimeUnit.Hour, Amount = 1 });

			db.GetTable<Schedule>().ToList().Count().ShouldBe(4);

			db.GetTable<Schedule>().Where(x => x.Unit == unit).ToList().Count().ShouldBe(3);
			db.GetTable<Schedule>().Where(x => x.UnitNullable == unit).ToList().Count().ShouldBe(3);
			db.GetTable<Schedule>().Where(x => x.UnitNullable == unitNullable).ToList().Count().ShouldBe(3);
			db.GetTable<Schedule>().Where(x => x.UnitNullable == TimeUnit.Day).ToList().Count().ShouldBe(3);

			db.GetTable<Schedule>().Where(x => x.UnitNullable == unitNull).ToList().Count().ShouldBe(1);

			var alItems = db.GetTable<Schedule>().ToArray();

			alItems[0].Unit.ShouldBe(TimeUnit.Day);
			alItems[1].Unit.ShouldBe(TimeUnit.Day);
			alItems[2].Unit.ShouldBe(TimeUnit.Day);
			alItems[3].Unit.ShouldBe(TimeUnit.Hour);

			alItems[0].UnitNullable.ShouldBe(TimeUnit.Day);
			alItems[1].UnitNullable.ShouldBe(TimeUnit.Day);
			alItems[2].UnitNullable.ShouldBe(TimeUnit.Day);
			alItems[3].UnitNullable.ShouldBeNull();

		}

		[Test]
		public void EnumMappingTestWithTypes([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context, [Values] BulkCopyType bcType)
		{
			var data = new[]
			{
				new ScheduleWithTypes { Unit = TimeUnit.Day, UnitNullable  = TimeUnit.Day, Amount = 1 },
				new ScheduleWithTypes { Unit = TimeUnit.Day, UnitNullable  = TimeUnit.Day, Amount = 2 },
				new ScheduleWithTypes { Unit = TimeUnit.Day, UnitNullable  = TimeUnit.Day, Amount = 3 },
				new ScheduleWithTypes { Unit = TimeUnit.Hour, UnitNullable = null, Amount         = 1 },
			};

			var       unit         = TimeUnit.Day;
			TimeUnit? unitNullable = TimeUnit.Day;
			TimeUnit? unitNull     = null;

			using (var db = GetDataConnection(SetupEnums(context, false)))
			using (db.CreateLocalTable<ScheduleWithTypes>())
			{
				db.BulkCopy(new BulkCopyOptions() { BulkCopyType = bcType }, data);

				db.GetTable<Schedule>().ToList().Count().ShouldBe(4);

				db.GetTable<Schedule>().Where(x => x.Unit         == unit).ToList().Count().ShouldBe(3);
				db.GetTable<Schedule>().Where(x => x.UnitNullable == unit).ToList().Count().ShouldBe(3);
				db.GetTable<Schedule>().Where(x => x.UnitNullable == unitNullable).ToList().Count().ShouldBe(3);
				db.GetTable<Schedule>().Where(x => x.UnitNullable == TimeUnit.Day).ToList().Count().ShouldBe(3);

				db.GetTable<Schedule>().Where(x => x.UnitNullable == unitNull).ToList().Count().ShouldBe(1);

				var alItems = db.GetTable<Schedule>().ToArray();

				alItems[0].Unit.ShouldBe(TimeUnit.Day);
				alItems[1].Unit.ShouldBe(TimeUnit.Day);
				alItems[2].Unit.ShouldBe(TimeUnit.Day);
				alItems[3].Unit.ShouldBe(TimeUnit.Hour);

				alItems[0].UnitNullable.ShouldBe(TimeUnit.Day);
				alItems[1].UnitNullable.ShouldBe(TimeUnit.Day);
				alItems[2].UnitNullable.ShouldBe(TimeUnit.Day);
				alItems[3].UnitNullable.ShouldBeNull();
			}
		}

		[Test]
		public async Task EnumMappingTestWithTypesAsync([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context, [Values] BulkCopyType bcType)
		{
			var data = new[]
			{
				new ScheduleWithTypes { Unit = TimeUnit.Day, UnitNullable  = TimeUnit.Day, Amount = 1 },
				new ScheduleWithTypes { Unit = TimeUnit.Day, UnitNullable  = TimeUnit.Day, Amount = 2 },
				new ScheduleWithTypes { Unit = TimeUnit.Day, UnitNullable  = TimeUnit.Day, Amount = 3 },
				new ScheduleWithTypes { Unit = TimeUnit.Hour, UnitNullable = null, Amount         = 1 },
			};

			var       unit         = TimeUnit.Day;
			TimeUnit? unitNullable = TimeUnit.Day;
			TimeUnit? unitNull     = null;

			using (var db = GetDataConnection(SetupEnums(context, false)))
			using (db.CreateLocalTable<ScheduleWithTypes>())
			{
				await db.BulkCopyAsync(new BulkCopyOptions() { BulkCopyType = bcType }, data);

				db.GetTable<Schedule>().ToList().Count().ShouldBe(4);

				db.GetTable<Schedule>().Where(x => x.Unit         == unit).ToList().Count().ShouldBe(3);
				db.GetTable<Schedule>().Where(x => x.UnitNullable == unit).ToList().Count().ShouldBe(3);
				db.GetTable<Schedule>().Where(x => x.UnitNullable == unitNullable).ToList().Count().ShouldBe(3);
				db.GetTable<Schedule>().Where(x => x.UnitNullable == TimeUnit.Day).ToList().Count().ShouldBe(3);

				db.GetTable<Schedule>().Where(x => x.UnitNullable == unitNull).ToList().Count().ShouldBe(1);

				var alItems = db.GetTable<Schedule>().ToArray();

				alItems[0].Unit.ShouldBe(TimeUnit.Day);
				alItems[1].Unit.ShouldBe(TimeUnit.Day);
				alItems[2].Unit.ShouldBe(TimeUnit.Day);
				alItems[3].Unit.ShouldBe(TimeUnit.Hour);

				alItems[0].UnitNullable.ShouldBe(TimeUnit.Day);
				alItems[1].UnitNullable.ShouldBe(TimeUnit.Day);
				alItems[2].UnitNullable.ShouldBe(TimeUnit.Day);
				alItems[3].UnitNullable.ShouldBeNull();
			}
		}
	}

}
