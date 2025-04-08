using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB.Internal.Common;

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

			Assert.That(finalList, Is.EqualTo(new int[][] { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 9 } }));
		}

		[Test]
		public void BatchThrowsWhenEnumerating2ndTimeTest()
		{
			var countTo10   = Enumerable.Range(0, 10);
			var enumerables = EnumerableHelper.Batch(countTo10, 3);

			foreach (var enumerable2 in enumerables)
			{
				var array1 = enumerable2.ToList();
				Assert.That(array1, Is.EqualTo(new int[] { 0, 1, 2 }));
				Assert.Throws<InvalidOperationException>(() =>
				{
					var array2 = enumerable2.ToList();
				});
				return;
			}
		}

		[Test]
		public async Task BatchAsyncTest()
		{
			var countTo10   = AsyncEnumerableRange(10);
			var enumerables = EnumerableHelper.Batch(countTo10, 3);
			var finalList   = new List<List<int>>();

			await foreach (var enumerable2 in enumerables)
			{
				var array1 = new List<int>();
				await foreach (var elem in enumerable2)
					array1.Add(elem);
				finalList.Add(array1);
			}

			Assert.That(finalList, Is.EqualTo(new int[][] { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 9 } }));
		}

		[Test]
		public async Task BatchAsyncThrowsWhenEnumerating2ndTimeTest()
		{
			var countTo10   = AsyncEnumerableRange(10);
			var enumerables = EnumerableHelper.Batch(countTo10, 3);

			await foreach (var enumerable2 in enumerables)
			{
				var array1 = new List<int>();
				await foreach (var elem in enumerable2)
					array1.Add(elem);
				Assert.That(array1, Is.EqualTo(new int[] { 0, 1, 2 }));
				Assert.ThrowsAsync<InvalidOperationException>(async () =>
				{
					await foreach (var _ in enumerable2) { }
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
	}
}
