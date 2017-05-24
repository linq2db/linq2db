using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Linq;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Tests.Model;

namespace Tests.Merge
{
	public partial class MergeTests
	{
		private const int ROWS = 5000;

		[MergeDataContextSource]
		public void BigSource(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetBigSource())
					.Insert()
					.Merge();

				var result = table.OrderBy(_ => _.Id).ToList();

				Assert.AreEqual(ROWS, rows);

				Assert.AreEqual(ROWS + 4, result.Count);

				AssertRow(InitialTargetData[0], result[0], null, null);
				AssertRow(InitialTargetData[1], result[1], null, null);
				AssertRow(InitialTargetData[2], result[2], null, 203);
				AssertRow(InitialTargetData[3], result[3], null, null);

				for (var i = 4; i < ROWS + 4; i++)
				{
					Assert.AreEqual(i + 1, result[i].Id);
					Assert.AreEqual(i + 2, result[i].Field1);
					Assert.AreEqual(i + 3, result[i].Field2);
					Assert.IsNull(result[i].Field3);
					Assert.AreEqual(i + 5, result[i].Field4);
					Assert.IsNull(result[i].Field5);
				}
			}
		}

		private IEnumerable<TestMapping1> GetBigSource()
		{
			for (var i = 0; i < ROWS; i++)
			{
				yield return new TestMapping1()
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
