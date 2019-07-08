﻿using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue737Tests : TestBase
	{
		[Test]
		public void Test([IncludeDataSources(TestProvName.AllSqlServer)] string context)
		{
			using (var db = new DataConnection(context))
			{
				var one = new QueryOne(db).Query().ToArray();
				var two = new QueryTwo(db).Query().ToArray();
			}
		}

		class QueryOne
		{
			readonly DataConnection _db;

			public QueryOne(DataConnection db)
			{
				_db = db;
			}

			public IQueryable<Person> Query()
				=> _db.GetTable<Person>().SelectMany(x => _db.GetTable<Person>().Where(y => false), (x, y) => x);
		}

		class QueryTwo
		{
			readonly DataConnection _db;

			public QueryTwo(DataConnection db)
			{
				_db = db;
			}

			public IQueryable<Person> Query()
				=> _db.GetTable<Person>().SelectMany(x => _db.GetTable<Person>().Where(y => false), (x, y) => x);
		}
	}
}
