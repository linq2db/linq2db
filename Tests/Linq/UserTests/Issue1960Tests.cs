using System;
using System.Globalization;
using System.Linq;
using System.Text;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1960Tests : TestBase
	{
		[Table]
		public class Issue1960Table
		{
			[Column(Precision = 28, Scale = 4)]
			[Column(Configuration = ProviderName.SQLite)]
			public decimal Decimal1 { get; set; }

			[Column(Precision = 28, Scale = 4, Configuration = ProviderName.SqlServer)]
			[Column]
			public decimal Decimal2 { get; set; }

			[Column]
			public decimal Decimal3 { get; set; }

			[Column]
			public DateTime DateTime { get; set; }
		}

		[Test]
		public void TestDecimalColumn([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();

			ms.SetDataType(typeof(decimal), new DbDataType(typeof(string), DataType.Text));
			ms.SetConvertExpression((string s) => decimal.Parse(s, CultureInfo.InvariantCulture));
			ms.SetConvertExpression((decimal d) => d.ToString(CultureInfo.InvariantCulture));

			ms.SetDataType(typeof(DateTime), new DbDataType(typeof(long), DataType.Int64));
			ms.SetConvertExpression((long l) => new DateTime(l, DateTimeKind.Utc));
			ms.SetConvertExpression((DateTime t) => t.Ticks);
			ms.SetConvertExpression((DateTime t) => t.Ticks.ToString(CultureInfo.InvariantCulture));
			ms.SetValueToSqlConverter(typeof(DateTime), (StringBuilder sb, DbDataType dt, object v) => sb.Append(((DateTime)v).Ticks.ToString(CultureInfo.InvariantCulture)));
			ms.SetConvertExpression<DateTime, DataParameter>(dt => new DataParameter() { Value = dt.Ticks, DataType = DataType.Int64 });

			using (var db = GetDataContext(context, ms))
			using (var t = db.CreateLocalTable<Issue1960Table>())
			{
				var decValue = 12345.6789m;
				var dtValue = new DateTime(123456789, DateTimeKind.Utc);
				t.Insert(() => new Issue1960Table()
				{
					Decimal1 = decValue,
					Decimal2 = decValue,
					Decimal3 = decValue,
					DateTime = dtValue
				});
				var record = t.Single();

				Assert.Multiple(() =>
				{
					Assert.That(record.Decimal1, Is.EqualTo(decValue));
					Assert.That(record.Decimal2, Is.EqualTo(decValue));
					Assert.That(record.Decimal3, Is.EqualTo(decValue));
					Assert.That(record.DateTime, Is.EqualTo(dtValue));
				});
			}
		}
	}
}
