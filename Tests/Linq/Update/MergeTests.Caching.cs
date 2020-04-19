using System.Linq;

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
				Assert.AreEqual(source.Length,     result.Length);

				for (var i = 0; i < source.Length; i++)
				{
					Assert.AreEqual(source[i].Id,     result[i].Id);
					Assert.AreEqual(source[i].Field1, result[i].Field1);
					Assert.AreEqual(source[i].Field2, result[i].Field2);
					Assert.AreEqual(source[i].Field4, result[i].Field4);
				}
			}
		}
	}
}
