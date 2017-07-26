using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

namespace Tests.UserTests
{
	using Model;

	[TestFixture]
	public class Issue445Tests : TestBase
	{ 
		class IssueContextSourceAttribute : IncludeDataContextSourceAttribute
		{
			public IssueContextSourceAttribute(bool includeLinqService = true)
				: base(includeLinqService, ProviderName.SQLite, ProviderName.SqlServer2008, ProviderName.SqlServer2012, TestProvName.SQLiteMs)
			{ }
		}

		[Test, IssueContextSourceAttribute]
		public void ConnectionClosed1(string context)
		{
			for   (var i = 0; i < 1000; i++)
			using (var db = GetDataContext(context))
			{
				AreEqual(Person.Where(_ => _.ID == 1), db.Person.Where(_ => _.ID == 1));
			}
		}

		[Test, IssueContextSourceAttribute(false)]
		public void ConnectionClosed2(string context)
		{
			for   (var i = 0; i < 1000; i++)
			using (var db = (DataConnection)GetDataContext(context))
			{
				AreEqual(Person.Where(_ => _.ID == 1), db.GetTable<Person>().Where(_ => _.ID == 1));
			}

		}

		[Test, IssueContextSourceAttribute(false)]
		public void ConnectionClosed3(string context)
		{
			for (var i = 0; i < 1000; i++)
			{
				var dc = new DataContext(context);
				AreEqual(Person.Where(_ => _.ID == 1), dc.GetTable<Person>().Where(_ => _.ID == 1));
			}
		}

		private IEnumerable<Person> GetPersonsFromDisposed1(string context)
		{
			using (var db = GetDataContext(context))
				return db.GetTable<Person>().Where(_ => _.ID == 1);
		}

		[Test, IssueContextSourceAttribute]
		public void ObjectDisposedException1(string context)
		{
			Assert.Throws<ObjectDisposedException>(() =>
			{
				AreEqual(Person.Where(_ => _.ID == 1), GetPersonsFromDisposed1(context));
			});
		}

		private IEnumerable<Person> GetPersonsFromDisposed3(string context)
		{
			using (var db = GetDataContext(context))
				return db.GetTable<Person>().Where(_ => _.ID == 1).AsEnumerable();
		}

		[Test, IssueContextSourceAttribute]
		public void ObjectDisposedException3(string context)
		{
			Assert.Throws<ObjectDisposedException>(() =>
			{
				AreEqual(Person.Where(_ => _.ID == 1), GetPersonsFromDisposed3(context));
			});
		}
		private IEnumerable<Person> GetPersonsFromDisposed2(string context)
		{
			using (var db = new DataContext(context))
				return db.GetTable<Person>().Where(_ => _.ID == 1);
		}

		[Test, IssueContextSourceAttribute(false)]
		public void CanDisposeDataContext(string context)
		{
			AreEqual(Person.Where(_ => _.ID == 1), GetPersonsFromDisposed2(context));
		}

		[Test, IncludeDataContextSource(false, ProviderName.SqlServer2008, ProviderName.SqlServer2012)]
		public void ConnectioonPoolException1(string context)
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
