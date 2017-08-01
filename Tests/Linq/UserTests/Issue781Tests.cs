using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue781Tests : TestBase
	{
		[DataContextSource(false)]
		public void TestCount(string context)
		{
			using (var db = new DataConnection(context))
			{
				var cnt = db.GetTable<Person>()
					.GroupBy(_ => Sql.Concat("test", _.Patient.Diagnosis))
					.Count();

				Assert.AreEqual(2, cnt);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.InvariantCultureIgnoreCase) != -1);
			}
		}

		[DataContextSource(false)]
		public void TestLongCount(string context)
		{
			using (var db = new DataConnection(context))
			{
				var cnt = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.LongCount();

				Assert.AreEqual(2, cnt);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.InvariantCultureIgnoreCase) != -1);
			}
		}

		[DataContextSource(false)]
		public void TestHavingCount(string context)
		{
			using (var db = new DataConnection(context))
			{
				var cnt = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.Having(_ => _.Key != null)
					.Count();

				Assert.AreEqual(1, cnt);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.InvariantCultureIgnoreCase) != -1);
			}
		}

		[DataContextSource(false)]
		public void TestHavingLongCount(string context)
		{
			using (var db = new DataConnection(context))
			{
				var cnt = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.Having(_ => _.Key != null)
					.LongCount();

				Assert.AreEqual(1, cnt);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.InvariantCultureIgnoreCase) != -1);
			}
		}

		[DataContextSource(false)]
		public void TestCountWithSelect(string context)
		{
			using (var db = new DataConnection(context))
			{
				var cnt = db.GetTable<Person>().GroupBy(_ => "test" + _.Patient.Diagnosis).Select(_ => _.Key).Count();

				Assert.AreEqual(2, cnt);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.InvariantCultureIgnoreCase) != -1);
			}
		}

		[DataContextSource(false)]
		public void TestLongCountWithSelect(string context)
		{
			using (var db = new DataConnection(context))
			{
				var cnt = db.GetTable<Person>().GroupBy(_ => "test" + _.Patient.Diagnosis).Select(_ => _.Key).LongCount();

				Assert.AreEqual(2, cnt);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.InvariantCultureIgnoreCase) != -1);
			}
		}

		[DataContextSource(false)]
		public void TestHavingCountWithSelect(string context)
		{
			using (var db = new DataConnection(context))
			{
				var cnt = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.Having(_ => _.Key != null)
					.Select(_ => _.Key)
					.Count();

				Assert.AreEqual(1, cnt);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.InvariantCultureIgnoreCase) != -1);
			}
		}

		[DataContextSource(false)]
		public void TestHavingLongCountWithSelect(string context)
		{
			using (var db = new DataConnection(context))
			{
				var cnt = db.GetTable<Person>()
					.GroupBy(_ => "test" + _.Patient.Diagnosis)
					.Having(_ => _.Key != null)
					.Select(_ => _.Key)
					.LongCount();

				Assert.AreEqual(1, cnt);
				Assert.True(db.LastQuery.IndexOf("COUNT", StringComparison.InvariantCultureIgnoreCase) != -1);
			}
		}
	}
}
