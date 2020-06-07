﻿using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue817Tests : TestBase
	{
		[ActiveIssue(SkipForNonLinqService = true, Details = "SELECT * query")]
		[Test]
		public void TestUnorderedTake([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Person>().Take(1).Select(_ => new { }).ToList();

				Assert.AreEqual(1, result.Count);
			}
		}

		[ActiveIssue(
			Configurations = new[]
			{
				TestProvName.AllAccess,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllSapHana,
				TestProvName.AllSqlServer2008Minus,
				TestProvName.AllSybase
			},
			SkipForNonLinqService = true,
			Details = "SELECT * query")]
		[Test]
		public void TestUnorderedSkip([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var cnt = db.GetTable<Person>().Count();

				var result = db.GetTable<Person>().Skip(1).Select(_ => new { }).ToList();

				Assert.AreEqual(cnt - 1, result.Count);
			}
		}

		[ActiveIssue(
			Configurations = new[]
			{
				TestProvName.AllAccess,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllSapHana,
				TestProvName.AllSqlServer2008Minus,
				TestProvName.AllSybase
			},
			SkipForNonLinqService = true,
			Details = "SELECT * query")]
		[Test]
		public void TestUnorderedTakeSkip([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Person>().Skip(1).Take(1).Select(_ => new { }).ToList();

				Assert.AreEqual(1, result.Count);
			}
		}


		[ActiveIssue(
			Configurations = new[]
			{
				TestProvName.AllAccess,
				ProviderName.DB2,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllSapHana,
				TestProvName.AllSqlServer2008Minus,
				TestProvName.AllSybase
			},
			SkipForNonLinqService = true,
			Details = "SELECT * query")]
		[Test]
		public void TestUnorderedTakeSkipZero([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var result = db.GetTable<Person>().Skip(0).Take(1).Select(_ => new { }).ToList();

				Assert.AreEqual(1, result.Count);
			}
		}
	}
}
