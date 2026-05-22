using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class SQLiteParameterTests : TestBase
	{
		[Table]
		sealed class ClassWithIntDate
		{
			[Column] public int Id             { get; set; }
			[Column(DataType = DataType.Int64)] public DateTime Value { get; set; }
			[Column] public double DoubleValue { get; set; }
			[Column] public float FloatValue   { get; set; }
		}

		[Table]
		sealed class ClassRealTypes
		{
			[Column] public int Id             { get; set; }
			[Column] public double DoubleValue { get; set; }
			[Column] public float FloatValue   { get; set; }

			public static ClassRealTypes[] Seed()
			{
				var result = new ClassRealTypes[]
				{
					new ClassRealTypes { Id = 1, DoubleValue = double.MaxValue, FloatValue = float.MaxValue, },
					new ClassRealTypes { Id = 1, DoubleValue = double.MinValue, FloatValue = float.MinValue, },
				};

				return result;
			}
		}

		[Test]
		public void DateTimeConversion([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			ms.SetConverter<long, DateTime>(ticks => new DateTime(ticks, DateTimeKind.Unspecified));
			ms.SetConverter<DateTime, DataParameter>(d => new DataParameter("", d.Ticks, DataType.Long));

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<ClassWithIntDate>();
			db.InlineParameters = true;

			var query = from t in db.GetTable<ClassWithIntDate>()
						where t.Value > TestData.NonReadonlyDateTime
						select t;

			query.ToArray();
			using (Assert.EnterMultipleScope())
			{
#pragma warning disable CS0618 // Type or member is obsolete
				Assert.That(query.GetStatement().CollectParameters(), Is.Empty);
#pragma warning restore CS0618 // Type or member is obsolete

				Assert.That(query.ToSqlQuery().Sql, Does.Not.Contain("DateTime("));
			}
		}

		[Test]
		public void DoubleParameterization([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var data = ClassRealTypes.Seed();

			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var actual = (
					from t1 in table
					where t1.DoubleValue == double.MaxValue && t1.FloatValue == float.MaxValue
					select t1
				).Concat(
					from t1 in table
					where t1.DoubleValue == double.MinValue && t1.FloatValue == float.MinValue
					select t1
				).ToArray();

			var expected = (
					from t1 in data
					where t1.DoubleValue == double.MaxValue && t1.FloatValue == float.MaxValue
					select t1
				).Concat(
					from t1 in data
					where t1.DoubleValue == double.MinValue && t1.FloatValue == float.MinValue
					select t1
				);

			AreEqualWithComparer(expected, actual);
		}

	}
}
