using System.Linq;

using Shouldly;

using LinqToDB;

using NUnit.Framework;

namespace Tests.xUpdate
{
	// tests for empty enumerable source
	public partial class MergeTests
	{
		[Test]
		public void EnumerableSourceQueryCaching([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var table = GetTarget(db);

				table.ClearCache();

				var source = new[]
				{
					new TestMapping1()
					{
						Id     = 0,
						Field1 = 1,
						Field2 = 2,
						Field4 = 4
					}
				};

				TestQuery(table, source);

				var cacheMissCount = table.GetCacheMissCount();

				source = new[]
				{
					new TestMapping1()
					{
						Id     = 10,
						Field1 = 11,
						Field2 = 12,
						Field4 = 14
					}
				};

				TestQuery(table, source);

				table.GetCacheMissCount().ShouldBe(cacheMissCount);

				source = new[]
				{
					new TestMapping1()
					{
						Id     = 20,
						Field1 = 21,
						Field2 = 22,
						Field4 = 24
					},
					new TestMapping1()
					{
						Id     = 30,
						Field1 = 31,
						Field2 = 32,
						Field4 = 34
					}
				};

				TestQuery(table, source);

				table.GetCacheMissCount().ShouldBe(cacheMissCount);
			}

			void TestQuery(ITable<TestMapping1> table, TestMapping1[] source)
			{
				table.Delete();

				var rows = table
							.Merge()
							.Using(source)
							.OnTargetKey()
							.InsertWhenNotMatched()
							.Merge();

				var result = table.OrderBy(_ => _.Id).ToArray();
				AssertRowCount(source.Length, rows, context);
				Assert.That(result, Has.Length.EqualTo(source.Length));

				for (var i = 0; i < source.Length; i++)
				{
					using (Assert.EnterMultipleScope())
					{
						Assert.That(result[i].Id, Is.EqualTo(source[i].Id));
						Assert.That(result[i].Field1, Is.EqualTo(source[i].Field1));
						Assert.That(result[i].Field2, Is.EqualTo(source[i].Field2));
						Assert.That(result[i].Field4, Is.EqualTo(source[i].Field4));
					}
				}
			}
		}
	}
}
