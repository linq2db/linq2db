using System.Linq;

using FluentAssertions;

using JetBrains.Annotations;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	[Order(10000)]
	public class TruncateTableTests : TestBase
	{
		[Table]
		[UsedImplicitly]
		sealed class TestTrun
		{
			[Column, PrimaryKey] public int     ID;
			[Column]             public decimal Field1;
		}

		[Test]
		public void TruncateTableTest([DataSources] string context)
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
		sealed class TestIdTrun
		{
			[Column, Identity, PrimaryKey] public int     ID;
			[Column]                       public decimal Field1;
		}

		[Test]
		public void TruncateIdentityTest([DataSources(TestProvName.AllInformix, TestProvName.AllSapHana)]
			string context)
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

		[Test]
		public void TruncateIdentityNoResetTest([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);

			using var table = db.CreateLocalTable<TestIdTrun>("test_temp");

			table.Truncate(false);

			table.Insert(() => new TestIdTrun { Field1 = 1m });
			table.Insert(() => new TestIdTrun { Field1 = 1m });

			var id = table.OrderBy(t => t.ID).Skip(1).Single().ID;

			table.Truncate(false);

			table.Insert(() => new TestIdTrun { Field1 = 1m });
			table.Insert(() => new TestIdTrun { Field1 = 1m });

			var r = table.OrderBy(t => t.ID).Skip(1).Single();

			// Oracle sequence is not guaranted to be sequential
			// (in short sequence values generated in batches that could be discarded for whatever reason leading to gaps)
			if (context.IsAnyOf(TestProvName.AllOracle))
				Assert.That(r.ID, Is.GreaterThanOrEqualTo(id + 2));
			else
				Assert.That(r.ID, Is.EqualTo(id + 2));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/2847")]
		public void Issue2847Test([IncludeDataSources(TestProvName.AllOracle)] string context, [Values] bool withIdentity)
		{
			using var db = GetDataConnection(context);

			using var table = db.CreateLocalTable<TestIdTrun>();

			table.Insert(() => new TestIdTrun { Field1 = 1m });

			db.Execute("DROP SEQUENCE \"SIDENTITY_TestIdTrun\"");

			if (withIdentity)
			{
				Assert.That(() => table.Truncate(withIdentity), Throws.Exception.With.Message.Contain("ORA-02289"));
			}
			else
			{
				table.Truncate(withIdentity);
			}
		}
	}
}
