using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Linq;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class ConcurrencyTests : TestBase
	{
		[Test]
		public void SequentialUsagesDoNotThrow([DataSources] string context)
		{
			using var db = GetDataContext(context);

			_ = db.Parent.ToList();
			_ = db.Parent.ToList();
		}

		[Test]
		public async Task ConcurrentUsageMultipleThreadsThrows([DataSources] string context)
		{
			using var db = GetDataContext(context);

			await using var enum1 = db.Parent.AsAsyncEnumerable().GetAsyncEnumerator();
			await enum1.MoveNextAsync();

			Assert.ThrowsAsync<LinqException>(async () =>
				await Task.Run(() => db.Parent.ToListAsync()));
		}

		[Test]
		public void ConcurrentUsageSameThreadThrows([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var enum1 = db.Parent.GetEnumerator();
			enum1.MoveNext();

			using var enum2 = db.Parent.GetEnumerator();

			Assert.Throws<LinqException>(
				() => enum2.MoveNext());
		}
	}
}
