using System;
using System.Data.SqlClient;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

// ReSharper disable once CheckNamespace
namespace Tests.xUpdate
{
	using Model;

	public partial class MergeTests
	{
		// ASE: just fails
		[Test]
		public void TestParameters1([MergeDataContextSource(
			ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			ProviderName.SapHana, ProviderName.Firebird)]
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

				var parametersCount = 8;

				if (context == ProviderName.DB2)
					parametersCount = 0;
				else if (context == ProviderName.Firebird || context == TestProvName.Firebird3)
					parametersCount = 4;

				Assert.AreEqual(parametersCount, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		// ASE: just fails
		[Test]
		public void TestParameters3([MergeDataContextSource(
			ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			ProviderName.SapHana, ProviderName.Firebird)]
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

				var parametersCount = 7;

				if (context == ProviderName.DB2)
					parametersCount = 0;
				else if (context == ProviderName.Firebird || context == TestProvName.Firebird3)
					parametersCount = 3;

				Assert.AreEqual(parametersCount, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void TestParameters2([MergeBySourceDataContextSource] string context)
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

				Assert.AreEqual(4, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void TestParametersInListSourceProperty([MergeBySourceDataContextSource] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var parameterValues = new
				{
					// TODO: find type that cannot be converted to literal but will be accepted by server
					val = new object()
				};

				var table = GetTarget(db);

				try
				{
					var rows = table
						.Merge()
						.Using(GetSource2(db)
							.ToList()
							.Select(_ => new
							{
								Id = _.OtherId,
								Field = parameterValues.val
							}))
						.On((t, s) => t.Id == s.Id || t.Id == 2)
						.DeleteWhenNotMatchedBySourceAnd(t => t.Field3 != 1)
						.Merge();

					Assert.Fail();
				}
				catch (SqlException ex)
				{
					Assert.AreEqual(4, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
					Assert.AreEqual(8011, ex.Number);
				}
			}
		}

		[Test]
		public void TestParametersInMatchCondition([MergeDataContextSource] string context)
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

				var paramcount = 1;
				if (context == ProviderName.DB2 || context == ProviderName.Informix)
					paramcount = 0;

				Assert.AreEqual(paramcount, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		private static char GetParameterToken([MergeDataContextSource] string context)
		{
			switch (context)
			{
				case ProviderName.Informix:
					return '?';
				case ProviderName.SapHana:
				case ProviderName.Oracle:
				case ProviderName.OracleManaged:
				case ProviderName.OracleNative:
					return ':';
			}

			return '@';
		}

		[Test]
		public void TestParametersInUpdateCondition([MergeDataContextSource(
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
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

				var paramcount = 1;
				if (context == ProviderName.DB2)
					paramcount = 0;

				Assert.AreEqual(paramcount, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test]
		public void TestParametersInInsertCondition([MergeDataContextSource(
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Firebird)]
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

				var paramcount = 1;
				if (context == ProviderName.DB2)
					paramcount = 0;

				Assert.AreEqual(paramcount, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test]
		public void TestParametersInDeleteCondition([MergeDataContextSource(
			ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Sybase, ProviderName.SybaseManaged, ProviderName.Informix,
			ProviderName.SapHana, ProviderName.Firebird)]
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

				var paramcount = 1;
				if (context == ProviderName.DB2)
					paramcount = 0;

				Assert.AreEqual(paramcount, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void TestParametersInDeleteBySourceCondition([MergeBySourceDataContextSource] string context)
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
				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void TestParametersInUpdateBySourceCondition([MergeBySourceDataContextSource] string context)
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
				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		// excluded providers use literal instead of parameter
		[Test]
		public void TestParametersInInsertCreate([MergeDataContextSource(
			ProviderName.DB2, ProviderName.Firebird, TestProvName.Firebird3,
			ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Informix, ProviderName.SapHana)]
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

				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 5).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(param.val, result[0].Field1);
			}
		}

		// excluded providers use literal instead of parameter
		[Test]
		public void TestParametersInUpdateExpression([MergeDataContextSource(
			ProviderName.DB2, ProviderName.Firebird, TestProvName.Firebird3,
			ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Informix, ProviderName.SapHana)]
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

				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 4).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(param, result[0].Field1);
			}
		}

		[Test, Parallelizable(ParallelScope.None)]
		public void TestParametersInUpdateBySourceExpression([MergeBySourceDataContextSource] string context)
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

				Assert.AreEqual(paramcount, db.LastQuery.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 1).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(param, result[0].Field1);
			}
		}

		// FB, INFORMIX: supports this parameter, but for now we disable all parameters in source for them
		[Test]
		public void TestParametersInSourceFilter([MergeDataContextSource(
			ProviderName.Firebird, TestProvName.Firebird3, ProviderName.Informix)]
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

				var paramcount = 1;
				if (context == ProviderName.DB2)
					paramcount = 0;

				Assert.AreEqual(paramcount, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		// FB, INFORMIX, Oracle: doesn't support parameters in source select list
		[Test]
		public void TestParametersInSourceSelect([MergeDataContextSource(
			ProviderName.Firebird, TestProvName.Firebird3, ProviderName.Informix,
			ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged)]
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

				var paramcount = 1;
				if (context == ProviderName.DB2)
					paramcount = 0;

				Assert.AreEqual(paramcount, db.LastQuery.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 3).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(114, result[0].Field1);
			}
		}

		// Provider optimize scalar parameters
		[Test]
		public void TestParametersInUpdateWithDeleteDeleteCondition([IncludeDataSources(false,
			ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged)]
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
					.UpdateWhenMatchedThenDelete((t, s) => t.Id == param)
					.Merge();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}
	}
}
