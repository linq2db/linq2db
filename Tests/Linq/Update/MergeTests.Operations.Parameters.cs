using LinqToDB;
using LinqToDB.Data;
using NUnit.Framework;
using System;
using System.Linq;
using Tests.Model;

// ReSharper disable once CheckNamespace
namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		// ASE: just fails
		[Test]
		public void TestParameters1([MergeDataContextSource(
			false,
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, TestProvName.AllFirebird25Minus)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var parameterValues = new
				{
					Val1 = 1,
					Val2 = 2,
					Val3 = 3,
					Val4 = 34,
					Val5 = 5
				};

				var table = GetTarget(db);

				table
					.Merge()
					.Using(GetSource2(db)
						.Where(_ => _.OtherId != parameterValues.Val5)
						.Select(_ => new
						{
							Id = _.OtherId,
							Field1 = _.OtherField1,
							Field2 = _.OtherField2,
							Field3 = _.OtherField3,
							Field4 = _.OtherField4,
							Field5 = _.OtherField5,
							Field7 = parameterValues.Val2
						}))
					.On((t, s) => t.Id == s.Id || t.Id == parameterValues.Val4)
					.InsertWhenNotMatchedAnd(
						s => s.Field7 == parameterValues.Val1 + s.Id,
						s => new TestMapping1()
						{
							Id = s.Id + parameterValues.Val5,
							Field1 = s.Field1
						})
					.UpdateWhenMatchedAnd(
						(t, s) => s.Id == parameterValues.Val3,
						(t, s) => new TestMapping1()
						{
							Field4 = parameterValues.Val5
						})
					.DeleteWhenMatchedAnd((t, s) => t.Field3 == parameterValues.Val2 + 123)
					.Merge();

				Assert.AreEqual(8, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		// ASE: just fails
		[Test]
		public void TestParameters3([MergeDataContextSource(
			false,
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, TestProvName.AllFirebird25Minus)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var parameterValues = new
				{
					Val1 = 1,
					Val2 = 2,
					Val3 = 3,
					Val4 = 4,
					Val5 = 5
				};

				var table = GetTarget(db);

				table
					.Merge()
					.Using(GetSource2(db)
						.Where(_ => _.OtherId != parameterValues.Val5)
						.Select(_ => new
						{
							Id = _.OtherId,
							Field1 = _.OtherField1,
							Field2 = _.OtherField2,
							Field3 = _.OtherField3,
							Field4 = _.OtherField4,
							Field5 = _.OtherField5,
							Field7 = parameterValues.Val2
						}))
					.On((t, s) => t.Id == s.Id)
					.InsertWhenNotMatchedAnd(
						s => s.Field7 == parameterValues.Val1 + s.Id,
						s => new TestMapping1()
						{
							Id = s.Id + parameterValues.Val5,
							Field1 = s.Field1
						})
					.UpdateWhenMatchedAnd(
						(t, s) => s.Id == parameterValues.Val3,
						(t, s) => new TestMapping1()
						{
							Field4 = parameterValues.Val5
						})
					.DeleteWhenMatchedAnd((t, s) => t.Field3 != parameterValues.Val2)
					.Merge();

				Assert.AreEqual(7, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test]
		public void TestParameters2([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var parameterValues = new
				{
					Val1 = 1,
					Val2 = 2,
					Val3 = 3,
					Val4 = 4,
					Val5 = 5
				};

				var table = GetTarget(db);

				table
					.Merge()
					.Using(GetSource2(db)
						.ToList()
						.Select(_ => new
						{
							Id = _.OtherId,
							Field1 = _.OtherField1,
							Field2 = _.OtherField2,
							Field3 = _.OtherField3,
							Field4 = _.OtherField4,
							Field5 = _.OtherField5
						}))
					.On((t, s) => t.Id == s.Id || t.Id == parameterValues.Val4)
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Id == parameterValues.Val3,
						t => new TestMapping1()
						{
							Field4 = parameterValues.Val5
						})
					.DeleteWhenNotMatchedBySourceAnd(t => t.Field3 != parameterValues.Val2)
					.Merge();

				Assert.AreEqual(4, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test]
		public void TestParametersInListSourceProperty([IncludeDataSources(ProviderName.DB2)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var parameterValues = new
				{
					// must be type that cannot be converted to literal but will be accepted by server
					// for now we don't generate literals for provider-specific types
					val = new IBM.Data.DB2Types.DB2Time(TimeSpan.FromMinutes(12))
				};

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource2(db)
						.ToList()
						.Select(_ => new
						{
							Id    = _.OtherId,
							Field = parameterValues.val
						}))
					.On((t, s) => t.Id == s.Id)
					.DeleteWhenMatchedAnd((t, s) => t.Field3 != 1 || Sql.ToNullable(s.Field) != null)
					.Merge();

				Assert.AreEqual(4, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test]
		public void TestParametersInMatchCondition([MergeDataContextSource(false)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 4;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.On((t, s) => t.Id == s.Id && t.Id == param)
					.UpdateWhenMatched()
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test]
		public void TestParametersInUpdateCondition([MergeDataContextSource(
			false,
			TestProvName.AllInformix, TestProvName.AllSapHana, TestProvName.AllFirebird25Minus)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 4;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatchedAnd((t, s) => t.Id == param)
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test]
		public void TestParametersInInsertCondition([MergeDataContextSource(
			false,
			TestProvName.AllInformix, TestProvName.AllSapHana, TestProvName.AllFirebird25Minus)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 5;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatchedAnd(s => s.Id == param)
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test]
		public void TestParametersInDeleteCondition([MergeDataContextSource(
			false,
			TestProvName.AllOracle,
			TestProvName.AllSybase, TestProvName.AllInformix,
			TestProvName.AllSapHana, TestProvName.AllFirebird25Minus)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 4;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.DeleteWhenMatchedAnd((t, s) => s.Id == param)
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test]
		public void TestParametersInDeleteBySourceCondition([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 2;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.DeleteWhenNotMatchedBySourceAnd(t => t.Id == param)
					.Merge();

				Assert.AreEqual(1, rows);
				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test]
		public void TestParametersInUpdateBySourceCondition([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 2;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Id == param,
						t => new TestMapping1()
						{
							Field1 = t.Field1
						})
					.Merge();

				Assert.AreEqual(1, rows);
				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		// excluded providers use literal instead of parameter
		[Test]
		public void TestParametersInInsertCreate([MergeDataContextSource(
			false,
			ProviderName.DB2, TestProvName.AllFirebird3Minus,
			TestProvName.AllOracle,
			TestProvName.AllInformix, TestProvName.AllSapHana)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = new { val = 123 };

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.InsertWhenNotMatchedAnd(
						s => s.Id == 5,
						s => new TestMapping1()
						{
							Id = s.Id,
							Field1 = param.val
						})
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 5).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(param.val, result[0].Field1);
			}
		}

		// excluded providers use literal instead of parameter
		[Test]
		public void TestParametersInUpdateExpression([MergeDataContextSource(
			false,
			ProviderName.DB2, TestProvName.AllFirebird3Minus,
			TestProvName.AllOracle,
			TestProvName.AllInformix, TestProvName.AllSapHana)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 123;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatchedAnd(
						(t, s) => s.Id == 4,
						(t, s) => new TestMapping1()
						{
							Field1 = param
						})
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 4).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(param, result[0].Field1);
			}
		}

		[Test]
		public void TestParametersInUpdateBySourceExpression([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 123;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenNotMatchedBySourceAnd(
						t => t.Id == 1,
						t => new TestMapping1()
						{
							Field1 = param
						})
					.Merge();

				Assert.AreEqual(1, rows);

				var paramcount = 1;
				if (context == ProviderName.DB2)
					paramcount = 0;

				Assert.AreEqual(paramcount, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 1).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(param, result[0].Field1);
			}
		}

		// FB, INFORMIX: supports this parameter, but for now we disable all parameters in source for them
		[Test]
		public void TestParametersInSourceFilter([MergeDataContextSource(
			false,
			TestProvName.AllFirebird3Minus, TestProvName.AllInformix)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 3;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Where(_ => _.Id == param))
					.OnTargetKey()
					.UpdateWhenMatched()
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		// FB, INFORMIX, Oracle: doesn't support parameters in source select list
		[Test]
		public void TestParametersInSourceSelect([MergeDataContextSource(
			false,
			TestProvName.AllFirebird3Minus, TestProvName.AllInformix,
			TestProvName.AllOracle)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 3;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Select(_ => new { _.Id, Val = param }))
					.On((t, s) => t.Id == s.Id && t.Id == s.Val)
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field1 = s.Val + 111
					})
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 3).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(114, result[0].Field1);
			}
		}

		// Provider optimize scalar parameters
		[Test]
		public void TestParametersInUpdateWithDeleteDeleteCondition([IncludeDataSources(TestProvName.AllOracle)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 4;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db))
					.OnTargetKey()
					.UpdateWhenMatchedThenDelete((t, s) => t.Id == param)
					.Merge();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test]
		public void TestParametersInSourceQueryFirebird([IncludeDataSources(false, TestProvName.AllFirebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = TestData.DateTime;

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).Select(_ => new { _.Id, Val = (DateTime?)param }))
					.On((t, s) => t.Id == s.Id && s.Val != null)
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field1 = 111,
					})
					.Merge();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(1, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 3).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(111, result[0].Field1);
			}
		}

		[Test]
		public void TestParametersInSourceEnumerableFirebird([IncludeDataSources(false, TestProvName.AllFirebird)]
			string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = TimeSpan.FromMinutes(5);

				var table = GetTarget(db);

				var rows = table
					.Merge()
					.Using(GetSource1(db).ToList().Select(_ => new { _.Id, Val = (TimeSpan?)param }))
					.On((t, s) => t.Id == s.Id && s.Val != null)
					.UpdateWhenMatched((t, s) => new TestMapping1()
					{
						Field1 = 111,
					})
					.Merge();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(4, db.LastQuery!.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 3).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(111, result[0].Field1);
			}
		}
	}
}
