using System.Collections.Generic;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		[Test]
		public void BigSource([MergeDataContextSource] string context)
		{
			var batchSize = context switch
			{
				// ASE: you may need to increase memory procedure cache sizes like that:
				// exec sp_configure 'max memory', NEW_MEMORY_SIZE
				// exec sp_configure 'procedure cache size', NEW_CACHE_SIZE
				string when context.IsAnyOf (TestProvName.AllSybase)                         => 500,
				// hard limit around 100 records
				// also big queries could kill connection with server
				string when context.IsAnyOf (ProviderName.Firebird25)                        => 100,
				// hard limit around 250 records
				string when context.IsAnyOf (TestProvName.AllFirebird3Plus)                  => 250,
				// takes too long
				string when context.IsAnyOf (TestProvName.AllInformix)                       => 500,
				// original 2500 actually works, but sometimes fails with
				// "cannot allocate enough memory: please check traces for further information"
				// as HANA virtual machine is PITA to configure, we just use smaller data set
				string when context.IsAnyOf (TestProvName.AllSapHana)                        => 50,
				// big batches leads to a lot of memory use by oracle, which could mess with testing environment
				string when context.IsAnyOf (TestProvName.AllOracle)                         => 1000,
				_                                                                            => 2500,
			};
		
			RunTest(context, batchSize);
		}

		private void RunTest(string context, int size)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetBigSource(size))
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(size, rows, context);

				Assert.That(result, Has.Count.EqualTo(size + 4));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				for (var i = 4; i < size + 4; i++)
				{
					using (Assert.EnterMultipleScope())
					{
						Assert.That(result[i].Id, Is.EqualTo(i + 1));
						Assert.That(result[i].Field1, Is.EqualTo(i + 2));
						Assert.That(result[i].Field2, Is.EqualTo(i + 3));
						Assert.That(result[i].Field3, Is.Null);
						Assert.That(result[i].Field4, Is.EqualTo(i + 5));
						Assert.That(result[i].Field5, Is.Null);
					}
				}
			}
		}

		private IEnumerable<TestMapping1> GetBigSource(int size)
		{
			for (var i = 0; i < size; i++)
			{
				yield return new TestMapping1
				{
					Id = i + 5,
					Field1 = i + 6,
					Field2 = i + 7,
					Field3 = i + 8,
					Field4 = i + 9,
					Field5 = i + 10,
				};
			}
		}
	}
}
