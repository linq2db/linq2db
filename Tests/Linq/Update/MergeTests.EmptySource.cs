﻿using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

using Tests.Model;

namespace Tests.xUpdate
{
	// tests for empty enumerable source
	public partial class MergeTests
	{
		[Test]
		public void MergeEmptyLocalSourceSameType([MergeDataContextSource(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(Array.Empty<TestMapping1>())
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(0, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
			}
		}

		[Test]
		public void MergeEmptyLocalSourceDifferentTypes([MergeDataContextSource(TestProvName.AllOracle)] string context)
		{
			using (var db = GetDataContext(context))
			{

				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(Array.Empty<Person>())
					.On((t, s) => t.Id == s.ID)
					.InsertWhenNotMatched(s => new TestMapping1()
					{
						Id = s.ID
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(0, rows, context);

				Assert.That(result, Has.Count.EqualTo(4));

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
			}
		}
	}
}
