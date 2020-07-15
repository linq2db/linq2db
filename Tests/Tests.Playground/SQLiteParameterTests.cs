using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Playground
{
	[TestFixture]
	public class SQLiteParameterTests : TestBase
	{
		[Table]
		class SampleClass
		{
			[Column] public int Id    { get; set; }
			[Column(DataType = DataType.Long)] public DateTime Value { get; set; }
		}

		[Test]
		public void SampleSelectTest([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();
			
			ms.SetConverter<long, DateTime>(ticks => new DateTime(ticks, DateTimeKind.Unspecified));
			ms.SetConverter<DateTime, DataParameter>(d => new DataParameter("", d.Ticks, DataType.Long));
			ms.SetValueToSqlConverter(typeof(long), (builder, dt, value) => builder.Append((long)value).ToString());

			using (var db = GetDataContext(context, ms))
			using (var table = db.CreateLocalTable<SampleClass>())
			{
				db.InlineParameters = true;

				var query = from t in table
					where t.Value > DateTime.Now
					select t;
				var result = query.ToArray();
			}
		}
	}
}
