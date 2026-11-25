using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Infrastructure
{
	[TestFixture]
	public class ReferenceDataTests : TestBase
	{
		sealed class Probe : TestBase
		{
			public int ParentCount => Parent.Count();
		}

		[Test, NonParallelizable]
		public void SnapshotIgnoresLaterWrites()
		{
			// read before the insert: under parallel lanes this waits for the default CreateDatabase capture,
			// so the insert below cannot overlap that ungated CreateDatabase
			var expected = new Probe().ParentCount;

			using var db = new TestDataConnection();

			try
			{
				db.Insert(new Parent { ParentID = 1001 });

				new Probe()
					.ParentCount
					.ShouldBe(expected);
			}
			finally
			{
				db.Parent.Delete(p => p.ParentID >= 1000);
			}
		}
	}
}
