#if NETFRAMEWORK
using System.ServiceModel;
#endif

using LinqToDB;

using NUnit.Framework;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		[Test]
		public void NotSupportedProviders([DataSources(
			ProviderName.DB2,
			TestProvName.AllFirebird,
			TestProvName.AllOracle,
			TestProvName.AllSybase,
			TestProvName.AllSqlServer,
			TestProvName.AllInformix,
			TestProvName.AllSapHana,
			TestProvName.AllPostgreSQL15Plus)]
			string context)
		{
			using (var db = GetDataContext(context, testLinqService : false))
			{
				var table = GetTarget(db);

				GetProviderName(context, out var isLinq);
				if (!isLinq)
					Assert.Throws<LinqToDBException>(() => table.Merge().Using(GetSource1(db)).OnTargetKey().InsertWhenNotMatched().Merge());
#if NETFRAMEWORK
					else
						Assert.Throws<FaultException>(() => table.Merge().Using(GetSource1(db)).OnTargetKey().InsertWhenNotMatched().Merge());
#endif
			}
		}
	}
}
