using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.ProviderSpecific
{
	using Model;

	[TestFixture]
	public class Firebird : TestBase
	{
		[Test]
		public void SequenceInsert()
		{
			using (var db = new TestDbManager(ProviderName.Firebird))
			{
				db.GetTable<FirebirdSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();
				db.Insert(new FirebirdSpecific.SequenceTest { Value = "SeqValue" });

				var id = db.GetTable<FirebirdSpecific.SequenceTest>().Single(_ => _.Value == "SeqValue").ID;

				db.GetTable<FirebirdSpecific.SequenceTest>().Where(_ => _.ID == id).Delete();

				Assert.AreEqual(0, db.GetTable<FirebirdSpecific.SequenceTest>().Count(_ => _.Value == "SeqValue"));
			}
		}

		[Test]
		public void SequenceInsertWithIdentity()
		{
			using (var db = new TestDbManager(ProviderName.Firebird))
			{
				db.GetTable<FirebirdSpecific.SequenceTest>().Where(_ => _.Value == "SeqValue").Delete();

				var id1 = Convert.ToInt32(db.InsertWithIdentity(new FirebirdSpecific.SequenceTest { Value = "SeqValue" }));
				var id2 = db.GetTable<FirebirdSpecific.SequenceTest>().Single(_ => _.Value == "SeqValue").ID;

				Assert.AreEqual(id1, id2);

				db.GetTable<FirebirdSpecific.SequenceTest>().Where(_ => _.ID == id1).Delete();

				Assert.AreEqual(0, db.GetTable<FirebirdSpecific.SequenceTest>().Count(_ => _.Value == "SeqValue"));
			}
		}
	}
}
