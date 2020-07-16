using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class SQLiteParameterTests : TestBase
	{
		[Table]
		class ClassWithIntDate
		{
			[Column] public int Id    { get; set; }
			[Column(DataType = DataType.Int64)] public DateTime Value { get; set; }
		}

		[Test]
		public void DateTimeConversion([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();
			
			ms.SetConverter<long, DateTime>(ticks => new DateTime(ticks, DateTimeKind.Unspecified));
			ms.SetConverter<DateTime, DataParameter>(d => new DataParameter("", d.Ticks, DataType.Long));

			using (var db = GetDataContext(context, ms))
			{
				db.InlineParameters = true;

				var query = from t in db.GetTable<ClassWithIntDate>()
					where t.Value > DateTime.Now
					select t;

				Assert.That(query.GetStatement().Parameters.Count, Is.EqualTo(0));

				Assert.That(query.ToString(), Does.Not.Contain("DateTime("));
			}
		}

	}
}
