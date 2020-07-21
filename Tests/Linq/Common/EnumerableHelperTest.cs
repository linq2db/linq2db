using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB.Async;
using LinqToDB.Common;

using NUnit.Framework;

namespace Tests.Common
{
	[TestFixture]
	public class EnumerableHelperTest
	{
		[Test]
		public void BatchTest()
		{
			var countTo10   = Enumerable.Range(0, 10);
			var enumerables = EnumerableHelper.Batch(countTo10, 3);
			var finalList   = new List<List<int>>();

			foreach (var enumerable2 in enumerables) 
			{
				finalList.Add(enumerable2.ToList());
			}
			Assert.AreEqual(new int[][] { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 9 } }, finalList);
		}

		[Test]
		public void BatchThrowsWhenEnumerating2ndTimeTest()
		{
			var countTo10   = Enumerable.Range(0, 10);
			var enumerables = EnumerableHelper.Batch(countTo10, 3);

			foreach (var enumerable2 in enumerables)
			{
				var array1 = enumerable2.ToList();
				Assert.AreEqual(new int[] { 0, 1, 2 }, array1);
				Assert.Throws<InvalidOperationException>(() =>
				{
					var array2 = enumerable2.ToList();
				});
				return;
			}
		}

#if !NET45 && !NET46
		[Test]
		public async Task BatchAsyncTest()
		{
			var countTo10   = AsyncEnumerableRange(10);
			var enumerables = EnumerableHelper.Batch(countTo10, 3);
			var finalList   = new List<List<int>>();

			await foreach (var enumerable2 in enumerables)
			{
				finalList.Add(await enumerable2.ToListAsync());
			}
			Assert.AreEqual(new int[][] { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 9 } }, finalList);
		}

		[Test]
		public async Task BatchAsyncThrowsWhenEnumerating2ndTimeTest()
		{
			var countTo10   = AsyncEnumerableRange(10);
			var enumerables = EnumerableHelper.Batch(countTo10, 3);

			await foreach (var enumerable2 in enumerables)
			{
				var array1 = await enumerable2.ToListAsync();
				Assert.AreEqual(new int[] { 0, 1, 2 }, array1);
				Assert.ThrowsAsync<InvalidOperationException>(async () =>
				{
					var array2 = await enumerable2.ToListAsync();
				});
				return;
			}
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		private async IAsyncEnumerable<int> AsyncEnumerableRange(int count)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			for (var i = 0; i < count; i++)
			{
				yield return i;
			}
		}
#endif
	}
}
