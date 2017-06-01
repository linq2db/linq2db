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
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using Tests.Model;

namespace Tests.Merge
{
	public partial class MergeTests
	{
		// ASE: ASE just don't like this query...
		[MergeDataContextSource(ProviderName.Sybase, ProviderName.Oracle, ProviderName.OracleManaged, ProviderName.OracleNative,
			ProviderName.Firebird, ProviderName.Informix, ProviderName.SapHana)]
		public void TestParameters1(string context)
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
					.From(GetSource2(db)
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
						}), (t, s) => t.Id == s.Id || t.Id == parameterValues.Val4)
					.Insert(s => s.Field7 == parameterValues.Val1 + s.Id, s => new TestMapping1()
					{
						Id = s.Id + parameterValues.Val5,
						Field1 = s.Field1
					})
					.Update((t, s) => s.Id == parameterValues.Val3, (t, s) => new TestMapping1()
					{
						Field4 = parameterValues.Val5
					})
					.Delete((t, s) => t.Field3 != parameterValues.Val2)
					.Merge();

				Assert.AreEqual(8, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[MergeBySourceDataContextSource]
		public void TestParameters2(string context)
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
					.From(GetSource2(db)
						.ToList()
						.Select(_ => new
						{
							Id = _.OtherId,
							Field1 = _.OtherField1,
							Field2 = _.OtherField2,
							Field3 = _.OtherField3,
							Field4 = _.OtherField4,
							Field5 = _.OtherField5
						}), (t, s) => t.Id == s.Id || t.Id == parameterValues.Val4)
					.UpdateBySource(t => t.Id == parameterValues.Val3, t => new TestMapping1()
					{
						Field4 = parameterValues.Val5
					})
					.DeleteBySource(t => t.Field3 != parameterValues.Val2)
					.Merge();

				Assert.AreEqual(4, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[MergeBySourceDataContextSource]
		public void TestParametersInListSourceProperty(string context)
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
						.From(GetSource2(db)
							.ToList()
							.Select(_ => new
							{
								Id = _.OtherId,
								Field = parameterValues.val
							}), (t, s) => t.Id == s.Id || t.Id == 2)
						.DeleteBySource(t => t.Field3 != 1)
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

		// Oracle: optimized by provider
		[MergeDataContextSource(ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged)]
		public void TestParametersInMatchCondition(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 4;

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db), (t, s) => t.Id == s.Id && t.Id == param)
					.Update()
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		private static char GetParameterToken(string context)
		{
			return context == ProviderName.Informix ? '?' : (context == ProviderName.SapHana ? ':' : '@');
		}

		// Oracle: optimized by provider
		[MergeDataContextSource(ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Firebird, ProviderName.Informix, ProviderName.SapHana)]
		public void TestParametersInUpdateCondition(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 4;

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Update((t, s) => t.Id == param)
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		// Oracle: optimized by provider
		[MergeDataContextSource(ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Firebird, ProviderName.Informix, ProviderName.SapHana)]
		public void TestParametersInInsertCondition(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 5;

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Insert(s => s.Id == param)
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[MergeDataContextSource(ProviderName.Firebird, ProviderName.Informix, ProviderName.SapHana)]
		public void TestParametersInDeleteCondition(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 4;

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Delete((t, s) => s.Id == param)
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[MergeBySourceDataContextSource]
		public void TestParametersInDeleteBySourceCondition(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 2;

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.DeleteBySource(t => t.Id == param)
					.Merge();

				Assert.AreEqual(1, rows);
				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[MergeBySourceDataContextSource]
		public void TestParametersInUpdateBySourceCondition(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 2;

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.UpdateBySource(t => t.Id == param, t => new TestMapping1()
					{
						Field1 = t.Field1
					})
					.Merge();

				Assert.AreEqual(1, rows);
				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		// Oracle, DB2, FB3: optimized by provider
		[MergeDataContextSource(ProviderName.DB2, ProviderName.DB2LUW, ProviderName.DB2zOS,
			ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.Firebird, TestProvName.Firebird3, ProviderName.Informix, ProviderName.SapHana)]
		public void TestParametersInInsertCreate(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = new { val = 123 };

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Insert(s => s.Id == 5, s => new TestMapping1()
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

		[MergeDataContextSource(ProviderName.Firebird, ProviderName.Informix, ProviderName.SapHana)]
		public void TestParametersInUpdateExpression(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 123;

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.Update((t, s) => s.Id == 4, (t, s) => new TestMapping1()
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

		[MergeBySourceDataContextSource]
		public void TestParametersInUpdateBySourceExpression(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 123;

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.UpdateBySource(t => t.Id == 1, t => new TestMapping1()
					{
						Field1 = param
					})
					.Merge();

				Assert.AreEqual(1, rows);
				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 1).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(param, result[0].Field1);
			}
		}

		// Oracle: optimized by provider
		[MergeDataContextSource(ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged)]
		public void TestParametersInSourceFilter(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 3;

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db).Where(_ => _.Id == param))
					.Update()
					.Merge();

				Assert.AreEqual(1, rows);
				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}

		[MergeDataContextSource]
		public void TestParametersInSourceSelect(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 3;

				var table = GetTarget(db);

				var rows = table
					.From(GetSource1(db).Select(_ => new { _.Id, Val = param }), (t, s) => t.Id == s.Id && t.Id == s.Val)
					.Update((t, s) => new TestMapping1()
					{
						Field1 = s.Val * 111
					})
					.Merge();

				AssertRowCount(1, rows, context);

				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));

				var result = GetTarget(db).Where(_ => _.Id == 3).ToList();

				Assert.AreEqual(1, result.Count);
				Assert.AreEqual(333, result[0].Field1);
			}
		}

		// Provider optimize scalar parameters
		//[IncludeDataContextSource(false, ProviderName.Oracle, ProviderName.OracleNative, ProviderName.OracleManaged)]
		public void TestParametersInUpdateWithDeleteDeleteCondition(string context)
		{
			using (var db = new TestDataConnection(context))
			{
				PrepareData(db);

				var param = 4;

				var table = GetTarget(db);

				var rows = table
					.FromSame(GetSource1(db))
					.UpdateWithDelete((t, s) => t.Id == param)
					.Merge();

				AssertRowCount(2, rows, context);

				Assert.AreEqual(1, db.LastQuery.Count(_ => _ == GetParameterToken(context)));
			}
		}
	}
}
