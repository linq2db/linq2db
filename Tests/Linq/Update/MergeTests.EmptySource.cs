﻿
using NUnit.Framework;

namespace Tests.xUpdate
{
	using LinqToDB;
	using LinqToDB.Common;
	using Model;
	using System;
	using System.Linq;

	// tests for empty enumerable source
	public partial class MergeTests
	{
		[Test, MergeDataContextSource]
		public void MergeEmptyLocalSourceSameType(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{

				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(Array<TestMapping1>.Empty)
					.OnTargetKey()
					.InsertWhenNotMatched()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(0, rows, context);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
			}
		}

		[Test, MergeDataContextSource]
		public void MergeEmptyLocalSourceDifferentTypes(string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.BeginTransaction())
			{

				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(Array<Person>.Empty)
					.On((t, s) => t.Id == s.ID)
					.InsertWhenNotMatched(s => new TestMapping1()
					{
						Id = s.ID
					})
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				AssertRowCount(0, rows, context);

				Assert.AreEqual(4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);
			}
		}
	}
}
