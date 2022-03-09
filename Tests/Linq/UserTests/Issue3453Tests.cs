using System;
using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using Npgsql;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue3453Tests : TestBase
	{
		[Table(Schema = "public", Name = "schedule")]
		public class Schedule
		{
			[Column("id"), PrimaryKey, Identity]                 public int       Id           { get; set; } // integer
			[Column("unit", DataType = DataType.Enum)]           public TimeUnit  Unit         { get; set; } // USER-DEFINED        
			[Column("unit_nullable", DataType = DataType.Enum)]  public TimeUnit? UnitNullable { get; set; } // USER-DEFINED        
			[Column("amount")]                                   public int       Amount       { get; set; } // integer
		}

		public enum TimeUnit
		{
			[MapValue("hour")]
			Hour,
			[MapValue("day")]
			Day,		
		}

		[Test]
		public void EnumMappingTest([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			NpgsqlConnection.GlobalTypeMapper.MapEnum<TimeUnit>("time_unit");
			var mappingSchema = new MappingSchema();

			const string initScript = @"DROP TABLE IF EXISTS schedule;
DROP TYPE IF EXISTS time_unit;
CREATE TYPE time_unit AS ENUM ('hour', 'day');
CREATE TABLE IF NOT EXISTS schedule
(
  id SERIAL CONSTRAINT schedule_pk PRIMARY KEY,
  unit         time_unit NOT NULL,
  unit_nullable time_unit NULL,
  amount INT NOT NULL
);
INSERT INTO schedule(unit, unit_nullable,amount) VALUES ('day','day',1),('day','day',2),('day','day',3);";

			// executing separately, we have to reload just created types
			using (var db = (DataConnection)GetDataContext(context, mappingSchema))
			{
				db.Execute(initScript);

				((NpgsqlConnection)db.Connection).ReloadTypes();
			}

			var       unit         = TimeUnit.Day;
			TimeUnit? unitNullable = TimeUnit.Day;
			TimeUnit? unitNull     = null;

			using (var db = (DataConnection)GetDataContext(context, mappingSchema))
			{
				db.Insert(new Schedule { Unit = TimeUnit.Hour, Amount = 1 });

				db.GetTable<Schedule>().Where(x => x.UnitNullable == TimeUnit.Day).Should().HaveCount(3);

				db.GetTable<Schedule>().Should().HaveCount(4);

				db.GetTable<Schedule>().Where(x => x.Unit == unit).Should().HaveCount(3);
				db.GetTable<Schedule>().Where(x => x.UnitNullable == unit).Should().HaveCount(3);
				db.GetTable<Schedule>().Where(x => x.UnitNullable == unitNullable).Should().HaveCount(3);

				db.GetTable<Schedule>().Where(x => x.UnitNullable == unitNull).Should().HaveCount(1);

				var alItems = db.GetTable<Schedule>().ToArray();

				alItems[0].Unit.Should().Be(TimeUnit.Day);
				alItems[1].Unit.Should().Be(TimeUnit.Day);
				alItems[2].Unit.Should().Be(TimeUnit.Day);
				alItems[3].Unit.Should().Be(TimeUnit.Hour);

				alItems[0].UnitNullable.Should().Be(TimeUnit.Day);
				alItems[1].UnitNullable.Should().Be(TimeUnit.Day);
				alItems[2].UnitNullable.Should().Be(TimeUnit.Day);
				alItems[3].UnitNullable.Should().BeNull();
			}

		}
	}
}
