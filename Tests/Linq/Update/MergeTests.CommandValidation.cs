using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlProvider;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using Model;
#if NET46
	using System.ServiceModel;
#endif

	public partial class MergeTests
	{
		[Test]
		public void NotSupportedProviders([DataSources(
			ProviderName.DB2, TestProvName.AllFirebird,
			TestProvName.AllOracle,
			TestProvName.AllSybase,
			TestProvName.AllSqlServer,
			TestProvName.AllInformix,
			TestProvName.AllSapHana)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				var table = GetTarget(db);

				GetProviderName(context, out var isLinq);
				if (!isLinq)
					Assert.Throws<LinqToDBException>(() => table.Merge().Using(GetSource1(db)).OnTargetKey().InsertWhenNotMatched().Merge());
#if NET46
					else
						Assert.Throws<FaultException<ExceptionDetail>>(() => table.Merge().Using(GetSource1(db)).OnTargetKey().InsertWhenNotMatched().Merge());
#endif
			}
		}
	}
}
