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
			_ = await enum1.MoveNextAsync();

			_ = Assert.ThrowsAsync<LinqException>(async () =>
				await Task.Run(() => db.Parent.ToListAsync()));
		}

		[Test]
		public void ConcurrentUsageSameThreadDoesNotThrow([DataSources] string context)
		{
			using var db = GetDataContext(context);

			using var enum1 = db.Parent.GetEnumerator();
			_ = enum1.MoveNext();

			using var enum2 = db.Parent.GetEnumerator();
			_ = enum2.MoveNext();
		}
	}
}
