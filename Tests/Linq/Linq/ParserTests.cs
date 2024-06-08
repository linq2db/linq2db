﻿using System.Linq;

using LinqToDB;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class ParserTests : TestBase
	{
		[Test]
		public void Join6([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			{
				var actual =
					from g in db.GrandChild
					join p in db.Parent4 on g.Child!.ParentID equals p.ParentID
					select g;

				var expected =
					from g in GrandChild
					join p in Parent4 on g.Child!.ParentID equals p.ParentID
					select g;

				AreEqual(expected, actual);
			}
		}

		[Test]
		public void OracleXmlTable()
		{
			using (var db = new TestDataConnection())
			{
				Assert.IsNotNull(db.OracleXmlTable<Person>(() => "<xml/>"));
				Assert.IsNotNull(db.OracleXmlTable<Person>("<xml/>"));
				Assert.IsNotNull(db.OracleXmlTable(new[] { new Person() }));
			}
		}
	}
}
