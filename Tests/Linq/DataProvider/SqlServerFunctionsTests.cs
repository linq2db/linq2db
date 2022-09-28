﻿using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Tools;
using LinqToDB.Tools.DataProvider.SqlServer.Schemas;

using NUnit.Framework;

namespace Tests.DataProvider
{
	[TestFixture]
	public class SqlServerFunctionsTests : TestBase
	{
		#region Configuration

		[Test]
		public void DbTSTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DbTS);
			Console.WriteLine(result.ToDiagnosticString());
			Assert.That(result.Length, Is.EqualTo(8));
		}

		[Test]
		public void LangIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.LangID);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0));
		}

		[Test]
		public void LanguageTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Language);
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void LockTimeoutTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			db.Execute("SET LOCK_TIMEOUT 1000");
			var result = db.Select(() => SqlFn.LockTimeout);
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1000));
		}

		[Test]
		public void MaxConnectionsTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.MaxConnections);
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(32767));
		}

		[Test]
		public void MaxPrecisionTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.MaxPrecision);
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(38));
		}

		[Test]
		public void NestLevelTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.NestLevel);
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void OptionsTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Options);
			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void RemServerTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.RemServer);
			Console.WriteLine(result);
			Assert.That(result, Is.Null);
		}

		[Test]
		public void ServerNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ServerName);
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void ServiceNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			if (context == "SqlAzure")
				return;

			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ServiceName);
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void SpIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SpID);
			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void TextSizeTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			db.Execute("SET TEXTSIZE 2048");
			var result = db.Select(() => SqlFn.TextSize);
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2048));
		}

		[Test]
		public void VersionTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Version);
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		#endregion

		#region Conversion

		[Test]
		public void CastTest1([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Cast("10:10:10", SqlType.Time));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new TimeSpan(10, 10, 10)));
		}

		[Test]
		public void CastTest2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Cast("10:10:10", SqlType.Time(3)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new TimeSpan(10, 10, 10)));
		}

		[Test]
		public void CastTest3([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Cast<string>(123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void ConvertTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert(SqlType.VarChar(4), 123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void ConvertTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert(SqlType.Decimal, 123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123m));
		}

		[Test]
		public void ConvertTest3([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert(SqlType.NVarChar(10), 123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void ConvertTest4([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert(SqlType.VarCharMax, 123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void ConvertTest5([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert(SqlType.Decimal(30, 0), 123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123m));
		}

		[Test]
		public void ConvertTest6([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert<string>(123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void ConvertWithStyleTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert(SqlType.VarChar(4), 123, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void ConvertWithStyleTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert(SqlType.Decimal, 123, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123m));
		}

		[Test]
		public void ConvertWithStyleTest3([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert(SqlType.NVarChar(10), new DateTime(2022, 02, 22), 105));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("22-02-2022"));
		}

		[Test]
		public void ConvertWithStyleTest4([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert(SqlType.VarCharMax, 123, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void ConvertWithStyleTest5([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert(SqlType.Decimal(30, 0), 123, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123m));
		}

		[Test]
		public void ConvertWithStyleTest6([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Convert<string>(new DateTime(2022, 02, 22), 5));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("22-02-22"));
		}

		[Test]
		public void ParseTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Parse("Monday, 13 December 2010", SqlType.Date));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTime(2010, 12, 13)));
		}

		[Test]
		public void ParseTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Parse("123", SqlType.Decimal(30)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123m));
		}

		[Test]
		public void ParseTest3([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Parse<int>("123"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123));
		}

		[Test]
		public void ParseWithCultureTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Parse("€345,98", SqlType.Money, "de-DE"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(345.98m));
		}

		[Test]
		public void ParseWithCultureTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Parse("345,98", SqlType.Decimal(30,2), "de-DE"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(345.98m));
		}

		[Test]
		public void ParseWithCultureTest3([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Parse<decimal>("345,98", "de-DE"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(346m));
		}

		[Test]
		public void TryCastTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db     = new SystemDB(context);
			var       result = db.Select(() => SqlFn.TryCast("10:10:10", SqlType.Time));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new TimeSpan(10, 10, 10)));
		}

		[Test]
		public void TryCastTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db     = new SystemDB(context);
			var       result = db.Select(() => SqlFn.TryCast("10:10:10", SqlType.Time(3)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new TimeSpan(10, 10, 10)));
		}

		[Test]
		public void TryCastTest3([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db     = new SystemDB(context);
			var       result = db.Select(() => SqlFn.TryCast<string>(123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void TryConvertTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert(SqlType.VarChar(4), 123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void TryConvertTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert(SqlType.Decimal, 123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123m));
		}

		[Test]
		public void TryConvertTest3([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert(SqlType.NVarChar(10), 123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void TryConvertTest4([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert(SqlType.VarCharMax, 123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void TryConvertTest5([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert(SqlType.Decimal(30, 0), 123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123m));
		}

		[Test]
		public void TryConvertTest6([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert<string>(123));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void TryConvertWithStyleTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert(SqlType.VarChar(4), 123, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void TryConvertWithStyleTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert(SqlType.Decimal, 123, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123m));
		}

		[Test]
		public void TryConvertWithStyleTest3([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert(SqlType.NVarChar(10), new DateTime(2022, 02, 22), 105));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("22-02-2022"));
		}

		[Test]
		public void TryConvertWithStyleTest4([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert(SqlType.VarCharMax, 123, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123"));
		}

		[Test]
		public void TryConvertWithStyleTest5([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert(SqlType.Decimal(30, 0), 123, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123m));
		}

		[Test]
		public void TryConvertWithStyleTest6([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryConvert<string>(new DateTime(2022, 02, 22), 5));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("22-02-22"));
		}

		[Test]
		public void TryParseTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryParse("Monday, 13 December 2010", SqlType.Date));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTime(2010, 12, 13)));
		}

		[Test]
		public void TryParseTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryParse("123", SqlType.Decimal(30)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123m));
		}

		[Test]
		public void TryParseTest3([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryParse<int>("123"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(123));
		}

		[Test]
		public void TryParseWithCultureTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryParse("€345,98", SqlType.Money, "de-DE"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(345.98m));
		}

		[Test]
		public void TryParseWithCultureTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryParse("345,98", SqlType.Decimal(30,2), "de-DE"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(345.98m));
		}

		[Test]
		public void TryParseWithCultureTest3([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TryParse<decimal>("345,98", "de-DE"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(346m));
		}

		#endregion

		#region Data type

		[Test]
		public void DataLengthTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DataLength("123"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(6));
		}

		[Test]
		public void DataLengthLTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DataLengthBig("123"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(6));
		}

		[Test]
		public void IdentityCurrentTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IdentityCurrent("Person"));
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0m));
		}

		[Test]
		public void IdentityIncrementTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IdentityIncrement("Person"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1m));
		}

		[Test]
		public void IdentitySeedTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IdentitySeed("Person"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1m));
		}

		#endregion

		#region Date and Time

		[Test]
		public void DateFirstTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateFirst);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void CurrentTimestampTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.CurrentTimestamp);
			Console.WriteLine(result);
			Assert.That(result.Year, Is.EqualTo(DateTime.Today.Year));
		}

		[Test]
		public void CurrentTimezoneTest([IncludeDataSources(TestProvName.AllSqlServer2019Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.CurrentTimezone());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void CurrentTimezoneIDTest([IncludeDataSources(TestProvName.SqlAzure)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.CurrentTimezoneID());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void DateAddTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateAdd(SqlFn.DateParts.Month, -1, "2022-02-22"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTime(2022, 01, 22)));
		}

		[Test]
		public void DateAddTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateAdd(SqlFn.DateParts.Day, 1, TestData.Date));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(TestData.Date.AddDays(1)));
		}

		[Test]
		public void DateAddTest3([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateAdd(SqlFn.DateParts.Year, 1, TestData.DateTimeOffset));
			Console.WriteLine(result);
			Assert.That(result?.Date, Is.EqualTo(TestData.DateTimeOffset.Date.AddYears(1)));
		}

		[Test]
		public void DateAddTest4([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateAdd(SqlFn.DateParts.Hour, 1, TimeSpan.FromHours(2)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new TimeSpan(3, 0, 0)));
		}

		[Test]
		public void DateDiffTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateDiff(SqlFn.DateParts.Day, "2022-02-22", "2022-02-24"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public void DateDiffTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateDiff(SqlFn.DateParts.Month, TestData.Date, TestData.Date.AddYears(1)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(12));
		}

		[Test]
		public void DateDiffTest3([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateDiff(SqlFn.DateParts.Month, TestData.DateTimeOffset, TestData.DateTimeOffset.AddYears(1)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(12));
		}

		[Test]
		public void DateDiffTest4([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateDiff(SqlFn.DateParts.Hour, TimeSpan.FromHours(2), TimeSpan.FromHours(3)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void DateDiffBigTest1([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateDiffBig(SqlFn.DateParts.Day, "2022-02-22", "2022-02-24"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public void DateDiffBigTest2([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateDiffBig(SqlFn.DateParts.Month, TestData.Date, TestData.Date.AddYears(1)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(12));
		}

		[Test]
		public void DateDiffBigTest3([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateDiffBig(SqlFn.DateParts.Month, TestData.DateTimeOffset, TestData.DateTimeOffset.AddYears(1)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(12));
		}

		[Test]
		public void DateDiffBigTest4([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateDiffBig(SqlFn.DateParts.Hour, TimeSpan.FromHours(2), TimeSpan.FromHours(3)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void TimeFromPartsTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TimeFromParts(1, 1, 1, 0, 0));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new TimeSpan(1, 1, 1)));
		}

		[Test]
		public void TimeFromPartsTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TimeFromParts(1, 1, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new TimeSpan(1, 1, 1)));
		}

		[Test]
		public void DateFromPartsTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateFromParts(2022, 2, 22));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTime(2022, 2, 22)));
		}

		[Test]
		public void SmallDateTimeFromPartsTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SmallDateTimeFromParts(2022, 2, 22, 0, 0));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTime(2022, 2, 22)));
		}

		[Test]
		public void DateTimeFromPartsTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateTimeFromParts(2022, 2, 22, 0, 0, 0, 0));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTime(2022, 2, 22)));
		}

		[Test]
		public void DateTimeFromPartsTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateTimeFromParts(2022, 2, 22, 0, 0, 0));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTime(2022, 2, 22)));
		}

		[Test]
		public void DateTimeFromPartsTest3([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateTimeFromParts(2022, 2, 22));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTime(2022, 2, 22)));
		}

		[Test]
		public void DateTime2FromPartsTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateTime2FromParts(2022, 2, 22, 0, 0, 0, 0, 0));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTime(2022, 2, 22)));
		}

		[Test]
		public void DateTime2FromPartsTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateTime2FromParts(2022, 2, 22, 0, 0, 0));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTime(2022, 2, 22)));
		}

		[Test]
		public void DateTime2FromPartsTest3([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateTime2FromParts(2022, 2, 22));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTime(2022, 2, 22)));
		}

		[Test]
		public void DateTimeOffsetFromPartsTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateTimeOffsetFromParts(2022, 2, 22, 0, 0, 0, 0, 0, 0, 0));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTimeOffset(new DateTime(2022, 2, 22), TimeSpan.Zero)));
		}

		[Test]
		public void DateTimeOffsetFromPartsTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateTimeOffsetFromParts(2022, 2, 22, 0, 0, 0));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTimeOffset(new DateTime(2022, 2, 22), TimeSpan.Zero)));
		}

		[Test]
		public void DateTimeOffsetFromPartsTest3([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateTimeOffsetFromParts(2022, 2, 22));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(new DateTimeOffset(new DateTime(2022, 2, 22), TimeSpan.Zero)));
		}

		[Test]
		public void DateNameTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateName(SqlFn.DateParts.Day, "2022-02-24"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("24"));
		}

		[Test]
		public void DateNameTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateName(SqlFn.DateParts.Month, new DateTime(2022, 03, 22)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("March"));
		}

		[Test]
		public void DateNameTest3([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateName(SqlFn.DateParts.Month, new DateTimeOffset(new DateTime(2022, 03, 22), TimeSpan.Zero)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("March"));
		}

		[Test]
		public void DateNameTest4([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DateName(SqlFn.DateParts.Hour, TimeSpan.FromHours(2)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("2"));
		}

		[Test]
		public void DatePartTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DatePart(SqlFn.DateParts.Day, "2022-02-24"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(24));
		}

		[Test]
		public void DatePartTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DatePart(SqlFn.DateParts.Month, new DateTime(2022, 02, 22)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public void DatePartTest3([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DatePart(SqlFn.DateParts.Month, new DateTimeOffset(new DateTime(2022, 02, 22), TimeSpan.Zero)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public void DatePartTest4([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DatePart(SqlFn.DateParts.Hour, TimeSpan.FromHours(2)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public void DayTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Day("2022-02-24"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(24));
		}

		[Test]
		public void DayTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Day(new DateTime(2022, 02, 22)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(22));
		}

		[Test]
		public void DayTest3([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Day(new DateTimeOffset(new DateTime(2022, 02, 22), TimeSpan.Zero)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(22));
		}

		[Test]
		public void EndOfMonthTest1([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.EndOfMonth("2022-02-24"));
			Console.WriteLine(result);
			Assert.That(result?.Day, Is.EqualTo(28));
		}

		[Test]
		public void EndOfMonthTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.EndOfMonth("2022-02-24", 1));
			Console.WriteLine(result);
			Assert.That(result?.Day, Is.EqualTo(31));
		}

		[Test]
		public void EndOfMonthTest21([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var date   = "2022-02-24";
			var result = db.Select(() =>  SqlFn.EndOfMonth(date, 1));
			Console.WriteLine(result);
			Assert.That(result?.Day, Is.EqualTo(31));
		}

		[Test]
		public void EndOfMonthTest3([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.EndOfMonth(new DateTime(2022, 02, 22)));
			Console.WriteLine(result);
			Assert.That(result?.Day, Is.EqualTo(28));
		}

		[Test]
		public void EndOfMonthTest4([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.EndOfMonth(new DateTime(2022, 02, 22), 1));
			Console.WriteLine(result);
			Assert.That(result?.Day, Is.EqualTo(31));
		}

		[Test]
		public void GetDateTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.GetDate());
			Console.WriteLine(result);
			Assert.That(result.Year, Is.EqualTo(DateTime.Today.Year));
		}

		[Test]
		public void GetUtcDateTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.GetUtcDate());
			Console.WriteLine(result);
			Assert.That(result.Year, Is.EqualTo(DateTime.UtcNow.Year));
		}

		[Test]
		public void SysDatetimeTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SysDatetime());
			Console.WriteLine(result);
			Assert.That(result.Year, Is.EqualTo(DateTime.Now.Year));
		}

		[Test]
		public void SysDatetimeOffsetTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SysDatetimeOffset());
			Console.WriteLine(result);
			Assert.That(result.Year, Is.EqualTo(DateTime.Now.Year));
		}

		[Test]
		public void SysUtcDatetimeTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SysUtcDatetime());
			Console.WriteLine(result);
			Assert.That(result.Year, Is.EqualTo(DateTime.Now.Year));
		}

		[Test]
		public void IsDateTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IsDate("2022-02-22"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void MonthTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var date   = "2022-02-24";
			var result = db.Select(() => SqlFn.Month(date));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public void MonthTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Month(new DateTime(2022, 02, 22)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public void MonthTest3([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Month(new DateTimeOffset(new DateTime(2022, 02, 22), TimeSpan.Zero)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public void SwitchOffsetTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SwitchOffset(new DateTimeOffset(new DateTime(2022, 02, 22), TimeSpan.Zero), "-04:00"));
			Console.WriteLine(result);
			Assert.That(result?.Year, Is.EqualTo(2022));
		}

		[Test]
		public void ToDatetimeOffsetTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ToDatetimeOffset(new DateTime(2022, 02, 22), "-04:00"));
			Console.WriteLine(result);
			Assert.That(result?.Year, Is.EqualTo(2022));
		}

		[Test]
		public void YearTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Year("2022-02-24"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2022));
		}

		[Test]
		public void YearTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db     = new SystemDB(context);
			var       result = db.Select(() => SqlFn.Year(new DateTime(2022, 02, 22)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2022));
		}

		[Test]
		public void YearTest3([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db     = new SystemDB(context);
			var       result = db.Select(() => SqlFn.Year(new DateTimeOffset(new DateTime(2022, 02, 22), TimeSpan.Zero)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2022));
		}

		#endregion

		#region Json

		[Test]
		public void IsJson([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IsJson("{ \"test\" : 1 }"));
			Console.WriteLine(result);
			Assert.That(result, Is.True);
		}

		[Test]
		public void JsonValue([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.JsonValue("{ \"test\" : 1 }", "$.test"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("1"));
		}

		[Test]
		public void JsonQuery([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.JsonQuery("{ \"test\" : 1 }", "$"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("{ \"test\" : 1 }"));
		}

		[Test]
		public void JsonModify([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.JsonModify("{ \"test\" : 1 }", "$.test", "2"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("{ \"test\" : \"2\" }"));
		}

		[Test]
		public void OpenJson1([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.GetTable<SqlFn.JsonData>(null, LinqToDB.Linq.MethodHelper.GetMethodInfo(SqlFn.OpenJson, string.Empty), "{ \"test\" : 1 }").ToArray();
			Console.WriteLine(result);

			var expected = new[]
			{
				new SqlFn.JsonData { Key = "test", Value = "1", Type = 2, },
			};

			AreEqual(expected, result);
		}

		[Test]
		public void OpenJson2([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.GetTable<SqlFn.JsonData>(null, LinqToDB.Linq.MethodHelper.GetMethodInfo(SqlFn.OpenJson, string.Empty, string.Empty), "{ \"test\" : [ 10, 20 ] }", "$.test").ToArray();
			Console.WriteLine(result);

			var expected = new[]
			{
				new SqlFn.JsonData { Key = "0", Value = "10", Type = 2, },
				new SqlFn.JsonData { Key = "1", Value = "20", Type = 2, },
			};

			AreEqual(expected, result);
		}

		[Test]
		public void OpenJson3([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.GetTable<SqlFn.JsonData>(null, LinqToDB.Linq.MethodHelper.GetMethodInfo(SqlFn.OpenJson, string.Empty), "[ 10, 20, 30, 40, 50, 60, 70 ]")
				.Where(jd => jd.Key != "2")
				.Where(jd => jd.Value != "60")
				.Select(jd => jd.Value)
				.ToArray();
			Console.WriteLine(result);

			var expected = new[] { "10", "20", "40", "50", "70" };

			AreEqual(expected, result);
		}

#if !NET45
		[Test]
		public void OpenJson4([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.OpenJson("{ \"test\" : 1 }").ToArray();
			Console.WriteLine(result);

			var expected = new[]
			{
				new SqlFn.JsonData { Key = "test", Value = "1", Type = 2, },
			};

			AreEqual(expected, result);
		}

		// SQL Server 2016 doesn't support @var for path
		[Test]
		public void OpenJson5([IncludeDataSources(TestProvName.AllSqlServer2017Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.OpenJson("{ \"test\" : [ 10, 20 ] }", "$.test").ToArray();
			Console.WriteLine(result);

			var expected = new[]
			{
				new SqlFn.JsonData { Key = "0", Value = "10", Type = 2, },
				new SqlFn.JsonData { Key = "1", Value = "20", Type = 2, },
			};

			AreEqual(expected, result);
		}

		[Test]
		public void OpenJson6([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.OpenJson("[ 10, 20, 30, 40, 50, 60, 70 ]")
				.Where(jd => jd.Key != "2")
				.Where(jd => jd.Value != "60")
				.Select(jd => jd.Value)
				.ToArray();
			Console.WriteLine(result);

			var expected = new[] { "10", "20", "40", "50", "70" };

			AreEqual(expected, result);
		}
#endif
		#endregion

		#region Mathematical

		[Test]
		public void AbsTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Abs("-10"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("10"));
		}

		[Test]
		public void AbsTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Abs(-10.01m));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(10.01m));
		}

		[Test]
		public void AcosTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Acos(0.5));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(Math.Acos(0.5)));
		}

		[Test]
		public void AsinTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Asin(0.5));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(Math.Asin(0.5)));
		}

		[Test]
		public void AtanTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Atan(0.5));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(Math.Atan(0.5)));
		}

		[Test]
		public void Atn2Test([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Atn2(10, 100));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void CeilingTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Ceiling(123.45));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(124));
		}

		[Test]
		public void CosTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Cos(0));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(Math.Cos(0)));
		}

		[Test]
		public void CotTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Cot(1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void DegreesTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Degrees(1.5));
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void ExpTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Exp(10));
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void FloorTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Floor(10.11));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(10));
		}

		[Test]
		public void LogTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Log(SqlFn.Exp(10)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(10));
		}

		[Test]
		public void LogTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Log(10.0, 2));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(Math.Log(10, 2)));
		}

		[Test]
		public void Log10Test([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Log10(SqlFn.Exp(10)));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(10));
		}

		[Test]
		public void PITest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.PI());
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(Math.PI));
		}

		[Test]
		public void PowerTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Power(2m, 2));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(Math.Pow(2, 2)));
		}

		[Test]
		public void RadiansTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Round(SqlFn.Radians(-45.01m), 4));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(-0.7856m));
		}

		[Test]
		public void RandTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Random());
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(1));
		}

		[Test]
		public void RandTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Random(10));
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0).And.LessThanOrEqualTo(1));
		}

		[Test]
		public void RoundTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Round(12345, -3));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(12000));
		}

		[Test]
		public void RoundTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Round(1.999, 2, 0));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public void RoundTest3([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Round(0.999, 1, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0.9));
		}

		[Test]
		public void SignTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Sign(1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void SqrtTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Sqrt(4));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(2));
		}

		[Test]
		public void SquareTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Square(4));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(16));
		}

		[Test]
		public void TanTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Round(SqlFn.Tan(SqlFn.PI() / 2), -12));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(16331000000000000.0d));
		}

		#endregion

		#region Logical

		[Test]
		public void ChooseTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			var b = "B";

			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Choose(2, "A", b, "C"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("B"));
		}

		[Test]
		public void IifTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Iif(Sql.AsSql(1) > 2, "A", "B"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("B"));
		}

		#endregion

		#region Metadata

		[Test]
		public void AppNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.AppName());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void ColumnLengthTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var result = db.Select(() => SqlFn.ColumnLength("Person", "PersonID"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(4));

			result = db.Select(() => SqlFn.ColumnLength("Person", "ID"));
			Console.WriteLine(result);
			Assert.That(result, Is.Null);
		}

		[Test]
		public void ColumnNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ColumnName(SqlFn.ObjectID("dbo.Person", "U"), 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("PersonID"));
		}

		[Test]
		public void ColumnPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			foreach (var item in new[]
			{
				new { Parameter = SqlFn.ColumnPropertyName.AllowsNull, Result =  0 },
				new { Parameter = SqlFn.ColumnPropertyName.IsIdentity, Result =  1 },
				new { Parameter = SqlFn.ColumnPropertyName.Precision,  Result = 10 },
				new { Parameter = SqlFn.ColumnPropertyName.Scale,      Result =  0 },
			})
			{
				var result = db.Select(() => SqlFn.ColumnProperty(SqlFn.ObjectID("dbo.Person"), "PersonID", item.Parameter));
				Console.WriteLine(result);
				Assert.That(result, Is.EqualTo(item.Result));
			}
		}

		[Test]
		public void DatabasePropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DatabasePropertyEx(SqlFn.DbName(), SqlFn.DatabasePropertyName.Version));
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(600));
		}

		[Test]
		public void DbIDTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DbID(SqlFn.DbName()));
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void DbIDTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DbID());
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void DbNameTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DbName(SqlFn.DbID()));
			Console.WriteLine(result);
			Assert.That(result, Contains.Substring("TestData"));
		}

		[Test]
		public void DbNameTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.DbName());
			Console.WriteLine(result);
			Assert.That(result, Contains.Substring("TestData"));
		}

		[Test]
		public void FileIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => SqlFn.FileID(file.Name));

			Console.WriteLine(result);

			Assert.That(result, Is.EqualTo(file.FileID));
		}

		[Test]
		public void FileIDExTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => SqlFn.FileIDEx(file.Name));

			Console.WriteLine(result);

			Assert.That(result, Is.EqualTo(file.FileID));
		}

		[Test]
		public void FileNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => SqlFn.FileName(file.FileID));

			Console.WriteLine(result);

			Assert.That(result, Is.EqualTo(file.Name));
		}

		[Test]
		public void FileGroupIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.FileGroupID("PRIMARY"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void FileGroupNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.FileGroupName(1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("PRIMARY"));
		}

		[Test]
		public void FileGroupPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.FileGroupProperty("PRIMARY", SqlFn.FileGroupPropertyName.IsReadOnly));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void FilePropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => SqlFn.FileProperty(file.Name, SqlFn.FilePropertyName.IsPrimaryFile));

			Console.WriteLine(result);

			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void FilePropertyExTest([IncludeDataSources(TestProvName.SqlAzure)] string context)
		{
			using var db = new SystemDB(context);

			var file   = db.DatabasesAndFiles.DatabaseFiles.First();
			var result = db.Select(() => SqlFn.FilePropertyEx(file.Name, SqlFn.FilePropertyExName.AccountType));

			Console.WriteLine(result);

			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void FullTextServicePropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.FullTextServiceProperty(SqlFn.FullTextServicePropertyName.IsFulltextInstalled));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0).Or.EqualTo(1));
		}

		[Test]
		public void IndexColumnTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IndexColumn("Person", 1, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("PersonID"));
		}

		[Test]
		public void IndexKeyPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IndexKeyProperty(SqlFn.ObjectID("Person", "U"), 1, 1, SqlFn.IndexKeyPropertyName.ColumnId));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void IndexPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IndexProperty(SqlFn.ObjectID("dbo.Person"), "PK_Person", SqlFn.IndexPropertyName.IsClustered));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void NextValueForTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.NextValueFor("dbo.TestSequence"));
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void NextValueForOverTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person
				select new
				{
					Sequence = SqlFn.NextValueForOver("dbo.TestSequence").OrderBy(p.ID).ThenByDesc(p.FirstName).ToValue(),
					p.ID
				};

			var l = q.ToList();

			Assert.That(l.Count, Is.GreaterThan(0));
		}

		[Test]
		public void ObjectDefinitionTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.ObjectDefinition(SqlFn.ObjectID("PersonSearch")));
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void ObjectNameTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectName(SqlFn.ObjectID("dbo.Person")));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("Person"));
		}

		[Test]
		public void ObjectNameTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectName(SqlFn.ObjectID("dbo.Person"), SqlFn.DbID()));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("Person"));
		}

		[Test]
		public void ObjectSchemaNameTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectSchemaName(SqlFn.ObjectID("dbo.Person")));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("dbo"));
		}

		[Test]
		public void ObjectSchemaNameTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectSchemaName(SqlFn.ObjectID("dbo.Person"), SqlFn.DbID()));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("dbo"));
		}

		[Test]
		public void ObjectPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectProperty(SqlFn.ObjectID("dbo.Person"), SqlFn.ObjectPropertyName.HasDeleteTrigger));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void ObjectPropertyExTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ObjectPropertyEx(SqlFn.ObjectID("dbo.Person"), SqlFn.ObjectPropertyExName.IsTable));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void OriginalDbNameTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.OriginalDbName());
			Console.WriteLine(result);
			Assert.That(result, Contains.Substring("TestData"));
		}

		[Test]
		public void ParseNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ParseName("dbo.Person", 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("Person"));
		}

		[Test]
		public void SchemaIDNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SchemaName(SqlFn.SchemaID("sys")));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("sys"));
		}

		[Test]
		public void SchemaNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SchemaName());
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("dbo"));
		}

		[Test]
		public void SchemaIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SchemaName(SqlFn.SchemaID()));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("dbo"));
		}

		[Test]
		public void ScopeIdentityTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ScopeIdentity());
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void ServerPropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ServerProperty(SqlFn.ServerPropertyName.Edition));
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void StatsDateTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.StatsDate(SqlFn.ObjectID("dbo.Person"), 1));
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null.Or.Null);
		}

		[Test]
		public void TypeNameIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TypeName(SqlFn.TypeID("int")));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("int"));
		}

		[Test]
		public void TypePropertyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TypeProperty("int", SqlFn.TypePropertyName.Precision));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(10));
		}

		#endregion

		#region Replication

		[Test]
		public void PublishingServerNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.PublishingServerName());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		#endregion

		#region String

		[Test]
		public void AsciiTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Ascii('A'));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(65));
		}

		[Test]
		public void AsciiTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Ascii("ABC"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(65));
		}

		[Test]
		public void CharTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Char(65));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo('A'));
		}

		[Test]
		public void CharIndexTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.CharIndex("34", "123456"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(3));
		}

		[Test]
		public void CharIndexTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.CharIndex("34", "123456340", 4));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(7));
		}

		[Test]
		public void CharIndexTest3([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.CharIndexBig("34", "123456"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(3));
		}

		[Test]
		public void CharIndexTest4([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.CharIndexBig("34", "123456340", 4));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(7));
		}

		[Test]
		public void CharIndexTest5([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.CharIndex("34", "123456340", 4L));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(7));
		}

		[Test]
		public void ConcatTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Concat("34", "123456", "abc"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("34123456abc"));
		}

		[Test]
		public void ConcatWithSeparatorTest([IncludeDataSources(TestProvName.AllSqlServer2017Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.ConcatWithSeparator("-", "34", "123456", "abc"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("34-123456-abc"));
		}

		[Test]
		public void DifferenceTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Difference("Green", "Greene"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(4));
		}

		[Test]
		public void FormatTest([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Format(123456789, "###-##-####"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("123-45-6789"));
		}

		[Test]
		public void LeftTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Left("1234", 2));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("12"));
		}

		[Test]
		public void LenTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Len("1234"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(4));
		}

		[Test]
		public void LenBigTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.LenBig("1234"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(4L));
		}

		[Test]
		public void LowerTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Lower("AbC"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("abc"));
		}

		[Test]
		public void LeftTrimTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.LeftTrim("  ABC  "));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("ABC  "));
		}

		[Test]
		public void NCharTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.NChar(248));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo('ø'));
		}

		[Test]
		public void PatIndexTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.PatIndex("%ter%", "interesting data"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(3));
		}

		[Test]
		public void PatIndexBigTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.PatIndexBig("%ter%", "interesting data"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(3));
		}

		[Test]
		public void QuoteNameTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.QuoteName("abc[]def"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("[abc[]]def]"));
		}

		[Test]
		public void QuoteNameTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.QuoteName("abc def", "><"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("<abc def>"));
		}

		[Test]
		public void ReplaceTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Replace("abcdefghicde", "cde", "xxx"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("abxxxfghixxx"));
		}

		[Test]
		public void ReplicateTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Replicate("ab", 2));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("abab"));
		}

		[Test]
		public void ReplicateTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Replicate(new byte[] { (int)'a' }, 2));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("aa"));
		}

		[Test]
		public void ReverseTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Reverse("abc"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("cba"));
		}

		[Test]
		public void RightTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Right("12345", 2));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("45"));
		}

		[Test]
		public void RightTrimTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.RightTrim("  123  "));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("  123"));
		}

		[Test]
		public void SoundExTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.SoundEx("Bambardu"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("B516"));
		}

		[Test]
		public void SpaceTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => "'" + SqlFn.Space(3) + "'");
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("'   '"));
		}

		[Test]
		public void StrTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Str(10.101));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("        10"));
		}

		[Test]
		public void StrTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Str(10.101, 3));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(" 10"));
		}

		[Test]
		public void StrTest3([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Str(10.101, 5, 1));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(" 10.1"));
		}

		[Test]
		public void StringEscapeTest([IncludeDataSources(TestProvName.AllSqlServer2017Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.StringEscape("\\  /  \n\\\\    \"", "json"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("\\\\  \\/  \\n\\\\\\\\    \\\""));
		}

		[Test]
		public void StuffTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Stuff("abcdef", 2, 3, "ijklmn"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("aijklmnef"));
		}

		[Test]
		public void SubstringTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Substring("abcdef", 2, 3));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("bcd"));
		}

		[Test]
		public void TranslateTest([IncludeDataSources(TestProvName.AllSqlServer2017Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Translate("2*[3+4]/{7-2}", "[]{}", "()()"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("2*(3+4)/(7-2)"));
		}

		[Test]
		public void TrimTest1([IncludeDataSources(TestProvName.AllSqlServer2017Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Trim("     test    "));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("test"));
		}

		[Test]
		public void TrimTest2([IncludeDataSources(TestProvName.AllSqlServer2017Plus)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Trim(".,! ", "     #     test    ."));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("#     test"));
		}

		[Test]
		public void UnicodeTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Unicode("Åkergatan 24"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(197));
		}

		[Test]
		public void UpperTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Upper("AbC"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("ABC"));
		}

		[Test]
		public void CollateTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Collate("AbC", "Latin1_General_CI_AS"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("AbC"));
		}

		#endregion

		#region System

		[Test]
		public void IdentityTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Identity);
			Console.WriteLine(result);
			Assert.That(result, Is.Null);
		}

		[Test]
		public void PackReceivedTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.PackReceived);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void TransactionCountTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TransactionCount);
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0));
		}

		[Test]
		public void BinaryCheckSumTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person
				select SqlFn.BinaryCheckSum();

			var result = q.First();

			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void BinaryCheckSumTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person
				where p.ID == 1
				select SqlFn.BinaryCheckSum(p.ID, p.FirstName);

			var result = q.First();

			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void CheckSumTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person
				select SqlFn.CheckSum();

			var result = q.First();

			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void CheckSumTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);

			var q =
				from p in db.Person
				where p.ID == 1
				select SqlFn.CheckSum(p.ID, p.FirstName);

			var result = q.First();

			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void CompressTest1([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.Compress("ABC"));
			Console.WriteLine(result.ToDiagnosticString());
			Assert.That(result[0], Is.EqualTo(31));
		}

		[Test]
		public void CompressTest2([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.Compress(new byte[] { 1, 2, 3 }));
			Console.WriteLine(result.ToDiagnosticString());
			Assert.That(result[0], Is.EqualTo(31));
		}

		[Test]
		public void ConnectionPropertyTest([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.ConnectionProperty(SqlFn.ConnectionPropertyName.Net_Transport));
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void CurrentRequestIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.CurrentRequestID());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void CurrentTransactionIDTest([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.CurrentTransactionID());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.EqualTo(0));
		}

		[Test]
		public void DecompressTest([IncludeDataSources(TestProvName.AllSqlServer2016Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => Sql.ConvertTo<string>.From(SqlFn.Decompress(new byte[]
			{
				31, 139, 8, 0, 0, 0, 0, 0, 4, 0, 115, 100, 112, 98, 112, 102, 0, 0, 26, 244, 143, 159, 6, 0, 0, 0

			})));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("ABC"));
		}

		[Test]
		public void FormatMessageTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.FormatMessage(20009, "ABC", "CBA"));
			Console.WriteLine(result);
			Assert.That(result, Contains.Substring("ABC").And.Contains("CBA"));
		}

		[Test]
		public void FormatMessageTest2([IncludeDataSources(TestProvName.AllSqlServer2012Plus)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.FormatMessage("- %i %s -", 1, "A"));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo("- 1 A -"));
		}

		[Test]
		public void GetAnsiNullTest1([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.GetAnsiNull());
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void GetAnsiNullTest2([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.GetAnsiNull(SqlFn.DbName()));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void HostIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.HostID());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void HostNameTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.HostName());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void IsNullTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			int? p = null;

			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.IsNull(p, 10));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(10));
		}

		[Test]
		public void IsNumericTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.IsNumeric(10));
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));
		}

		[Test]
		public void MinActiveRowVersionTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.MinActiveRowVersion());
			Console.WriteLine(result);
			Assert.That(result.Length, Is.EqualTo(8));
		}

		[Test]
		public void NewIDTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.NewID());
			Console.WriteLine(result);
			Assert.That(result, Is.Not.Null);
		}

		[Test]
		public void RowCountTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.RowCount);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0));
		}

		[Test]
		public void RowCountBigTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = GetDataContext(context);
			var result = db.Select(() => SqlFn.RowCountBig());
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0));
		}

		[Test]
		public void XactStateTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = (DataConnection)GetDataContext(context);

			db.BeginTransaction();
			var result = db.Select(() => SqlFn.XactState());
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(1));

			db.RollbackTransaction();
			result = db.Select(() => SqlFn.XactState());
			Console.WriteLine(result);
			Assert.That(result, Is.EqualTo(0));
		}

		#endregion

		#region System Statistical

		[Test]
		public void ConnectionsTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Connections);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void CpuBusyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.CpuBusy);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void IdleTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.Idle);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0).Or.Not.GreaterThan(0));
		}

		[Test]
		public void IOBusyTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.IOBusy);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void PackSentTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.PackSent);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThan(0));
		}

		[Test]
		public void PacketErrorsTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.PacketErrors);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0));
		}

		[Test]
		public void TimeTicksTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TimeTicks);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0));
		}

		[Test]
		public void TotalErrorsTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TotalErrors);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0));
		}

		[Test]
		public void TotalReadTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TotalRead);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0));
		}

		[Test]
		public void TotalWriteTest([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using var db = new SystemDB(context);
			var result = db.Select(() => SqlFn.TotalWrite);
			Console.WriteLine(result);
			Assert.That(result, Is.GreaterThanOrEqualTo(0));
		}

		#endregion
	}
}
