using System;
using System.Data.SqlTypes;
using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider;
using LinqToDB.Internal.DataProvider;

using NUnit.Framework;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Tests.Mapping
{
	[TestFixture]
	public class DecimalOverflowTests : TestBase
	{
		[Test]
		public void SqlDecimalTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db  = GetDataContext(context);
			using var tmp = db.CreateTempTable("#dtmp",
				[new { Value = new SqlDecimal(0.1m) }],
				ed => ed
					.Property(p => p.Value)
						.HasPrecision(38)
						.HasScale    (37));

			var data = tmp.ToList();

			Assert.That(data[0].Value.Scale, Is.EqualTo(37));
		}

		[Test]
		public void DecimalNegativeTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db  = GetDataConnection(context);
			using var tmp = db.CreateTempTable("#dtmp",
				[new { Value = 0.1m }],
				ed => ed
					.Property(p => p.Value)
						.HasPrecision(38)
						.HasScale    (37));

			var dataProvider = (DataProviderBase)db.DataProvider;

			dataProvider.SetFieldReaderExpression<System.Data.SqlClient.SqlDataReader,    decimal>(true, (r, i) => r.GetDecimal(i));
			dataProvider.SetFieldReaderExpression<Microsoft.Data.SqlClient.SqlDataReader, decimal>(true, (r, i) => r.GetDecimal(i));

			Assert.That(() => _ = tmp.ToList(), Throws.TypeOf<OverflowException>());
		}

		[Test]
		public void DecimalPositiveTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataConnection(context);

			var dataProvider = (DataProviderBase)db.DataProvider;

			dataProvider.SetFieldReaderExpression<System.Data.SqlClient.SqlDataReader,    decimal>(true, (r, i) => GetDecimal(r, i));
			dataProvider.SetFieldReaderExpression<Microsoft.Data.SqlClient.SqlDataReader, decimal>(true, (r, i) => GetDecimal(r, i));

			using var tmp = db.CreateTempTable("#dtmp",
				[new { Value = 0.1m }],
				ed => ed
					.Property(p => p.Value)
						.HasPrecision(38)
						.HasScale    (37));

			_ = tmp.ToList();

			dataProvider.SetFieldReaderExpression<System.Data.SqlClient.SqlDataReader,    decimal>(true, (r, i) => r.GetDecimal(i));
			dataProvider.SetFieldReaderExpression<Microsoft.Data.SqlClient.SqlDataReader, decimal>(true, (r, i) => r.GetDecimal(i));
		}

		static decimal GetDecimal(System.Data.SqlClient.SqlDataReader rd, int index)
		{
			var value = rd.GetSqlDecimal(index);

			if (value.Precision > 29)
			{
				var str = value.ToString();
				var val = decimal.Parse(str);
				return val;
			}

			return value.Value;
		}

		static decimal GetDecimal(Microsoft.Data.SqlClient.SqlDataReader rd, int index)
		{
			var value = rd.GetSqlDecimal(index);

			if (value.Precision > 29)
			{
				var str = value.ToString();
				var val = decimal.Parse(str);
				return val;
			}

			return value.Value;
		}
	}
}
