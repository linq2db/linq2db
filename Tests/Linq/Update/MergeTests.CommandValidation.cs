using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using LinqToDB.SchemaProvider;
using LinqToDB.SqlProvider;

using NUnit.Framework;

namespace Tests.xUpdate
{
	using LinqToDB.Linq;
	using Model;

	public partial class MergeTests
	{
		[Test]
		public void NotSupportedProviders([DataSources(false,
			ProviderName.DB2, TestProvName.AllFirebird,
			TestProvName.AllOracle,
			TestProvName.AllSybase,
			TestProvName.AllSqlServer,
			ProviderName.Informix,
			ProviderName.SapHana)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var table = GetTarget(db);

				Assert.Throws<LinqToDBException>(() => table.Merge().Using(GetSource1(db)).OnTargetKey().InsertWhenNotMatched().Merge());
			}
		}
	}
}
