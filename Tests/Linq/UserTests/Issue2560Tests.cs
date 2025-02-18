using System;
using System.Text;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NodaTime;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue2560Tests : TestBase
	{
		[Table]
		sealed class DataClass
		{
			[Column] public int Id    { get; set; }
			[Column] public LocalDateTime Value { get; set; }
		}

		[Test]
		public void TestNodaTimeInsert([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			var ms = new MappingSchema();

			ms.SetDataType(typeof(LocalDateTime), new DbDataType(typeof(DateTime), DataType.DateTime));

			ms.SetConverter<LocalDateTime, DataParameter>(timeStamp =>
				new DataParameter
				{
					Value = new DateTime(timeStamp.Year, timeStamp.Month, timeStamp.Day, timeStamp.Hour,
						timeStamp.Minute, timeStamp.Second, timeStamp.Millisecond),
					DataType = DataType.DateTime
				});

			ms.SetConverter<LocalDateTime, DateTime>(timeStamp =>
				new DateTime(timeStamp.Year, timeStamp.Month, timeStamp.Day, timeStamp.Hour,
					timeStamp.Minute, timeStamp.Second, timeStamp.Millisecond));

			ms.SetValueToSqlConverter(typeof(LocalDateTime), (StringBuilder sb, DbDataType dt, object v) =>
				{
					var d = (LocalDateTime)v;
					var d1 = new DateTime(d.Year, d.Month, d.Day, d.Hour,
						d.Minute, d.Second, d.Millisecond);

					sb.Append($"'{d1}'");
				}
			);

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DataClass>())
			{
				var item = new DataClass
				{
					Value = LocalDateTime.FromDateTime(TestData.DateTime),
				};

				db.Insert(item);
			}
		}
	}
}
