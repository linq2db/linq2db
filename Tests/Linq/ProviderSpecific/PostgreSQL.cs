using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.ProviderSpecific
{
	using Model;

	[TestFixture]
	public class PostgreSQL : TestBase
	{
		[Test]
		public void SequenceInsert1()
		{
			using (var db = new TestDbManager(ProviderName.PostgreSQL))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.Value == "SeqValue").Delete();
				db.Insert(new PostgreSQLSpecific.SequenceTest1 { Value = "SeqValue" });

				var id = db.GetTable<PostgreSQLSpecific.SequenceTest1>().Single(_ => _.Value == "SeqValue").ID;

				db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.ID == id).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest1>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test]
		public void SequenceInsert2()
		{
			using (var db = new TestDbManager(ProviderName.PostgreSQL))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.Value == "SeqValue").Delete();
				db.Insert(new PostgreSQLSpecific.SequenceTest2 { Value = "SeqValue" });

				var id = db.GetTable<PostgreSQLSpecific.SequenceTest2>().Single(_ => _.Value == "SeqValue").ID;

				db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.ID == id).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest2>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test]
		public void SequenceInsert3()
		{
			using (var db = new TestDbManager(ProviderName.PostgreSQL))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.Value == "SeqValue").Delete();
				db.Insert(new PostgreSQLSpecific.SequenceTest3 { Value = "SeqValue" });

				var id = db.GetTable<PostgreSQLSpecific.SequenceTest3>().Single(_ => _.Value == "SeqValue").ID;

				db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.ID == id).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest3>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test]
		public void SequenceInsertWithIdentity1()
		{
			using (var db = new TestDbManager(ProviderName.PostgreSQL))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.Value == "SeqValue").Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.SequenceTest1 { Value = "SeqValue" }));
				var id2 = db.GetTable<PostgreSQLSpecific.SequenceTest1>().Single(_ => _.Value == "SeqValue").ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<PostgreSQLSpecific.SequenceTest1>().Where(_ => _.ID == id1).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest1>().Count(_ => _.Value == "SeqValue"));
			}
		}

			[Test]
		public void SequenceInsertWithIdentity2()
		{
			using (var db = new TestDbManager(ProviderName.PostgreSQL))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.Value == "SeqValue").Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.SequenceTest2 { Value = "SeqValue" }));
				var id2 = db.GetTable<PostgreSQLSpecific.SequenceTest2>().Single(_ => _.Value == "SeqValue").ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<PostgreSQLSpecific.SequenceTest2>().Where(_ => _.ID == id1).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest2>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test]
		public void SequenceInsertWithIdentity3()
		{
			using (var db = new TestDbManager(ProviderName.PostgreSQL))
			{
				db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.Value == "SeqValue").Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new PostgreSQLSpecific.SequenceTest3 { Value = "SeqValue" }));
				var id2 = db.GetTable<PostgreSQLSpecific.SequenceTest3>().Single(_ => _.Value == "SeqValue").ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<PostgreSQLSpecific.SequenceTest3>().Where(_ => _.ID == id1).Delete();

				Assert.AreEqual(0, db.GetTable<PostgreSQLSpecific.SequenceTest3>().Count(_ => _.Value == "SeqValue"));
			}
		}
	}
}
