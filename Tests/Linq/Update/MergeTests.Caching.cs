using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Common;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;

	// tests for empty enumerable source
	public partial class MergeTests
	{
		[Test]
		public void EnumerableSourceQueryCaching([MergeDataContextSource] string context)
		{
			using (var db = GetDataContext(context))
			{
				var table = GetTarget(db);

				table.Delete();

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

				source[0] = new TestMapping1()
				{
					Id     = 10,
					Field1 = 11,
					Field2 = 12,
					Field4 = 14
				};

				TestQuery(table, source);
			}

			void TestQuery(ITable<TestMapping1> table, TestMapping1[] source)
			{
				var rows = table
							.Merge()
							.Using(source)
							.OnTargetKey()
							.InsertWhenNotMatched()
							.Merge();

				var result = table.Single();
				AssertRowCount(1, rows, context);
				Assert.AreEqual(source[0].Id,     result.Id);
				Assert.AreEqual(source[0].Field1, result.Field1);
				Assert.AreEqual(source[0].Field2, result.Field2);
				Assert.AreEqual(source[0].Field4, result.Field4);
			}
		}
	}
}
