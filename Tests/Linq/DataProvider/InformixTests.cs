﻿using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;

using IBM.Data.Informix;
using LinqToDB.DataProvider.Informix;
using NUnit.Framework;

namespace Tests.DataProvider
{
	using Model;

	[TestFixture]
	public class InformixTests : DataProviderTestBase
	{
		const string CurrentProvider = ProviderName.Informix;

		public InformixTests()
		{
			PassNullSql  = null;
			PassValueSql = "SELECT ID FROM {1} WHERE {0} = ?";
		}

		[Test]
		public void TestDataTypes([IncludeDataSources(CurrentProvider)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(TestType<long?>       (conn, "bigintDataType",   DataType.Int64),     Is.EqualTo(1000000L));
				Assert.That(TestType<long?>       (conn, "int8DataType",     DataType.Int64),     Is.EqualTo(1000001L));
				Assert.That(TestType<int?>        (conn, "intDataType",      DataType.Int32),     Is.EqualTo(7777777));
				Assert.That(TestType<short?>      (conn, "smallintDataType", DataType.Int16),     Is.EqualTo(100));
				Assert.That(TestType<decimal?>    (conn, "decimalDataType",  DataType.Decimal),   Is.EqualTo(9999999m));
				Assert.That(TestType<IfxDecimal?> (conn, "decimalDataType",  DataType.Decimal),   Is.EqualTo(new IfxDecimal(9999999m)));
				Assert.That(TestType<decimal?>    (conn, "moneyDataType",    DataType.Money),     Is.EqualTo(8888888m));
				Assert.That(TestType<float?>      (conn, "realDataType",     DataType.Single),    Is.EqualTo(20.31f));
				Assert.That(TestType<double?>     (conn, "floatDataType",    DataType.Double),    Is.EqualTo(16.2d));

				Assert.That(TestType<bool?>       (conn, "boolDataType",     DataType.Boolean),   Is.EqualTo(true));

				Assert.That(TestType<string>      (conn, "charDataType",     DataType.Char),      Is.EqualTo("1"));
				Assert.That(TestType<string>      (conn, "varcharDataType",  DataType.VarChar),   Is.EqualTo("234"));
				Assert.That(TestType<string>      (conn, "ncharDataType",    DataType.NChar),     Is.EqualTo("55645"));
				Assert.That(TestType<string>      (conn, "nvarcharDataType", DataType.NVarChar),  Is.EqualTo("6687"));
				Assert.That(TestType<string>      (conn, "lvarcharDataType", DataType.NVarChar),  Is.EqualTo("AAAAA"));

				Assert.That(TestType<DateTime?>   (conn, "dateDataType",     DataType.Date),      Is.EqualTo(new DateTime(2012, 12, 12)));
				Assert.That(TestType<DateTime?>   (conn, "datetimeDataType", DataType.DateTime2), Is.EqualTo(new DateTime(2012, 12, 12, 12, 12, 12)));
				Assert.That(TestType<IfxDateTime?>(conn, "datetimeDataType", DataType.DateTime),  Is.EqualTo(new IfxDateTime(new DateTime(2012, 12, 12, 12, 12, 12))));
				Assert.That(TestType<TimeSpan?>   (conn, "intervalDataType", DataType.Time),      Is.EqualTo(new TimeSpan(12, 12, 12)));
				Assert.That(TestType<IfxTimeSpan?>(conn, "intervalDataType", DataType.Time),      Is.EqualTo(new IfxTimeSpan(new TimeSpan(12, 12, 12))));

				Assert.That(TestType<string>      (conn, "textDataType",     DataType.Text,      skipPass:true), Is.EqualTo("BBBBB"));
				Assert.That(TestType<string>      (conn, "textDataType",     DataType.NText,     skipPass:true), Is.EqualTo("BBBBB"));
				Assert.That(TestType<byte[]>      (conn, "byteDataType",     DataType.Binary,    skipPass:true), Is.EqualTo(new byte[] { 1, 2 }));
				Assert.That(TestType<byte[]>      (conn, "byteDataType",     DataType.VarBinary, skipPass:true), Is.EqualTo(new byte[] { 1, 2 }));
			}
		}

		[Test]
		public void BulkCopyLinqTypes([IncludeDataSources(CurrentProvider)] string context)
		{
			InformixTools.ResolveInformix(typeof(IBM.Data.Informix.IfxConnection).Assembly);

			foreach (var bulkCopyType in new[] { BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific })
			{
				using (var db = new DataConnection(context))
				{
					db.BulkCopy(
						new BulkCopyOptions { BulkCopyType = bulkCopyType },
						Enumerable.Range(0, 10).Select(n =>
							new LinqDataTypes
							{
								ID            = 4000 + n,
								MoneyValue    = 1000m + n,
								DateTimeValue = new DateTime(2001,  1,  11,  1, 11, 21, 100),
								BoolValue     = true,
								GuidValue     = Guid.NewGuid(),
								SmallIntValue = (short)n
							}
						));

					db.GetTable<LinqDataTypes>().Delete(p => p.ID >= 4000);
				}
			}
		}

//		[Test]
		public void Driver([IncludeDataSources(CurrentProvider)] string context)
		{
//			InformixTools.ResolveInformix(typeof(IBM.Data.Informix.IfxConnection).Assembly);
//
//			var dr = null as IfxDataReader;
//
//			var _ = dr.GetBigInt(0);

			var tm = new IfxTimeSpan(0);
			var _ = IfxTimeSpan.Null;
		}
	}
}
