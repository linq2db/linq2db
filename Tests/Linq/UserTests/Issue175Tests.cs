using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue175Tests : TestBase
	{
		public new class Parent
		{
			public int? ParentID;
		}

		public new class Child
		{
			public int? ParentID;
			public int? ChildID;
		}

		[Test]
		public void Test([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			var q = from c in db.GetTable<Child>()
					join p in db.GetTable<Parent>() on c.ParentID equals p.ParentID
					select c;

			Assert.That(q, Is.Not.Empty);
		}
	}
}
