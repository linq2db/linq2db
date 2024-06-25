using System.Linq;
using FluentAssertions;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;
using NUnit.Framework;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Collections.Generic;
using System;
using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using System.Data.SQLite;
using System.Data.Common;
using System.Linq.Expressions;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue698Tests : TestBase
	{
		[Table]
		public class InfeedAdvicePositionDTO
		{
			[Column] public int Id { get; set; }
			[Column] public string? Text { get; set; }
		}

		[Test]
		public void Test698([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable<InfeedAdvicePositionDTO>())
			{
				SqlRegex.EnableSqliteRegex((DataConnection)db);
				SqlRegex.AddRegexSupport();
				
				db.Insert(new InfeedAdvicePositionDTO() { Id = 1, Text = "abcd" });
				db.Insert(new InfeedAdvicePositionDTO() { Id = 2, Text = "aabbcc" });
				var qryA = from infeed in db.GetTable<InfeedAdvicePositionDTO>()
						   where (new Regex("aa.*")).IsMatch(infeed.Text!)
						   select new
						   {
							   InfeedAdvicePosition = infeed,
						   };

				var l = qryA.Single();
			}
		}
	}
}
