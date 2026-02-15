using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue445Tests : TestBase
	{
		[AttributeUsage(AttributeTargets.Parameter)]
		sealed class IssueContextSourceAttribute : IncludeDataSourcesAttribute
		{
			public IssueContextSourceAttribute(bool includeLinqService = true)
				: base(includeLinqService, TestProvName.AllSQLite, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)
			{ }
		}

		[Test]
		public void ConnectionClosed1([IssueContextSource] string context)
		{
			for (var i = 0; i < 1000; i++)
				using (var db = GetDataContext(context))
				{
					AreEqual(Person.Where(_ => _.ID == 1), db.Person.Where(_ => _.ID == 1));
				}
		}

		[Test]
		public void ConnectionClosed2([IssueContextSource(false)] string context)
		{
			for (var i = 0; i < 1000; i++)
				using (var db = (DataConnection)GetDataContext(context))
				{
					AreEqual(Person.Where(_ => _.ID == 1), db.GetTable<Person>().Where(_ => _.ID == 1));
				}
		}

		[Test]
		public void ConnectionClosed3([IssueContextSource(false)] string context)
		{
			for (var i = 0; i < 1000; i++)
			{
#pragma warning disable CA2000 // Dispose objects before losing scope
				var dc = new DataContext(context);
#pragma warning restore CA2000 // Dispose objects before losing scope
				AreEqual(Person.Where(_ => _.ID == 1), dc.GetTable<Person>().Where(_ => _.ID == 1));
			}
		}

		[Test]
		public void ConnectionClosed2Async([IssueContextSource(false)] string context)
		{
			for (var i = 0; i < 1000; i++)
				using (var db = (DataConnection)GetDataContext(context))
				{
					AreEqual(Person.Where(_ => _.ID == 1), db.GetTable<Person>().Where(_ => _.ID == 1).ToArrayAsync().Result);
				}
		}

		[Test]
		public void ConnectionClosed3Async([IssueContextSource(false)] string context)
		{
			for (var i = 0; i < 1000; i++)
			{
#pragma warning disable CA2000 // Dispose objects before losing scope
				var dc = new DataContext(context);
#pragma warning restore CA2000 // Dispose objects before losing scope
				AreEqual(Person.Where(_ => _.ID == 1), dc.GetTable<Person>().Where(_ => _.ID == 1).ToArrayAsync().Result);
			}
		}

		IEnumerable<Person> GetPersonsFromDisposed1(string context)
		{
			using var db = GetDataContext(context);
			return db.GetTable<Person>().Where(_ => _.ID == 1);
		}

		[Test]
		public void ObjectDisposedException1([IssueContextSource] string context)
		{
			Assert.Throws<ObjectDisposedException>(() =>
			{
				AreEqual(Person.Where(_ => _.ID == 1), GetPersonsFromDisposed1(context));
			});
		}

		IEnumerable<Person> GetPersonsFromDisposed3(string context)
		{
			using var db = GetDataContext(context);
			return db.GetTable<Person>().Where(_ => _.ID == 1).AsEnumerable();
		}

		[Test]
		public void ObjectDisposedException3([IssueContextSource] string context)
		{
			Assert.Throws<ObjectDisposedException>(() =>
			{
				AreEqual(Person.Where(_ => _.ID == 1), GetPersonsFromDisposed3(context));
			});
		}

		IEnumerable<Person> GetPersonsFromDisposed2(string context)
		{
			using var db = new DataContext(context);
			return db.GetTable<Person>().Where(_ => _.ID == 1);
		}

		// no u can't
		//[Test]
		//public void CanDisposeDataContext([IssueContextSource(false)] string context)
		//{
		//	AreEqual(Person.Where(_ => _.ID == 1), GetPersonsFromDisposed2(context));
		//}

		[Test]
		[Explicit("Too slow")]
		public void ConnectionPoolException1([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (new DisableBaseline("Output depends on pool size"))
			{
				Assert.Throws<InvalidOperationException>(() =>
				{
					for (var i = 0; i < 1000; i++)
					{
						var db = GetDataContext(context);
						AreEqual(Person.Where(_ => _.ID == 1), db.GetTable<Person>().Where(_ => _.ID == 1));
					}
				});
			}
		}
	}
}
