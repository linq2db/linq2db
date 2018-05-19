using System;
using System.Linq;
using JetBrains.Annotations;
using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;

	[TestFixture]
	public class TruncateTableTests : TestBase
	{
		[Table]
		[UsedImplicitly]
		class TestTrun
		{
			[Column, PrimaryKey] public int     ID;
			[Column]             public decimal Field1;
		}

		[Test, DataContextSource(ProviderName.OracleNative)]
		public void TruncateTableTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<TestTrun>(throwExceptionIfNotExists:false);

				var table = db.CreateTable<TestTrun>();
				table.Truncate();
				table.Drop();
			}
		}

		[Table]
		class TestIdTrun
		{
			[Column, Identity, PrimaryKey] public int     ID;
			[Column]                       public decimal Field1;
		}

		[Test, DataContextSource(ProviderName.OracleNative, ProviderName.Informix)]
		public void TruncateIdentityTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<TestIdTrun>(throwExceptionIfNotExists:false);

				var table = db.CreateTable<TestIdTrun>();

				table.Insert(() => new TestIdTrun { Field1 = 1m });
				table.Insert(() => new TestIdTrun { Field1 = 1m });

				var id = table.OrderBy(t => t.ID).Skip(1).Single().ID;

				table.Truncate();

				db.Close();

				table.Insert(() => new TestIdTrun { Field1 = 1m });
				table.Insert(() => new TestIdTrun { Field1 = 1m });

				var r = table.OrderBy(t => t.ID).Skip(1).Single();

				Assert.That(r.ID, Is.EqualTo(id));

				table.Drop();
			}
		}

		[Test, DataContextSource(ProviderName.OracleNative)]
		public void TruncateIdentityNoResetTest(string context)
		{
			using (var db = GetDataContext(context))
			{
				db.DropTable<TestIdTrun>(throwExceptionIfNotExists:false);

				var table = db.CreateTable<TestIdTrun>();

				table.Insert(() => new TestIdTrun { Field1 = 1m });
				table.Insert(() => new TestIdTrun { Field1 = 1m });

				var id = table.OrderBy(t => t.ID).Skip(1).Single().ID;

				table.Truncate(false);

				table.Insert(() => new TestIdTrun { Field1 = 1m });
				table.Insert(() => new TestIdTrun { Field1 = 1m });

				var r = table.OrderBy(t => t.ID).Skip(1).Single();

				Assert.That(r.ID, Is.EqualTo(id + 2));

				table.Drop();
			}
		}
	}
}
