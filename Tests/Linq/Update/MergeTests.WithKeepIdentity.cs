using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		[Test]
		public void MergeWithKeepIdentity([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);

			PrepareData(db);

			var table = GetTarget(db);

			table
				.Merge()
				.Using(GetSource1(db).Where(_ => _.Id == 5))
				.OnTargetKey()
				.InsertWhenNotMatched()
				.WithKeepIdentity()
				.Merge();
		}
	}
}
