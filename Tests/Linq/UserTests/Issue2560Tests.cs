using System;
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
		class DataClass
		{
			[Column] public int Id    { get; set; }
			[Column] public LocalDateTime Value { get; set; }
		}

		[Test]
		public void TestNodaTimeInsert([IncludeDataSources(TestProvName.AllSqlServer2005Plus)] string context)
		{
			var ms = new MappingSchema();

			ms.SetDataType(typeof(LocalDateTime), new SqlDataType(new DbDataType(typeof(DateTime), DataType.DateTime)));

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

			ms.SetValueToSqlConverter(typeof(LocalDateTime), (sb, dt, v) =>
				{
					var d = (LocalDateTime)v;
					var d1 = new DateTime(d.Year, d.Month, d.Day, d.Hour,
						d.Minute, d.Second, d.Millisecond);

					sb.Append("'").Append(d1.ToString()).Append("'");
				}
			);

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<DataClass>())
			{
				var item = new DataClass
				{
					Value = LocalDateTime.FromDateTime(DateTime.Now),
				};

				db.Insert(item);
			}
		}
	}
}
