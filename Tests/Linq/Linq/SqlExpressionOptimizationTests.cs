using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	// Here we test the optimization of the SQL expression tree.
	// You can find which expressions are optimized and which still not.

	[TestFixture]
	public class SqlExpressionOptimizationTests : TestBase
	{
		[Test(Description = "https://github.com/linq2db/linq2db/issues/6459")]
		public void NestedBooleanConditionPreservesResult(
			[Values] bool innerTrueValue,
			[Values(true, false, null)] bool? outerTrueValue,
			[Values(true, false, null)] bool? outerFalseValue,
			[Values] bool conditionValue,
			[Values] bool compareToTrue,
			[Values] bool allowModify)
		{
			var parameter = new SqlParameter(new DbDataType(typeof(bool)), "condition", null);
			var condition = new SqlPredicate.Expr(parameter);
			var inner     = new SqlConditionExpression(condition, new SqlValue(innerTrueValue), new SqlValue(!innerTrueValue));
			ISqlPredicate outerCondition = compareToTrue
				? new SqlPredicate.ExprExpr(inner, SqlPredicate.Operator.Equal, new SqlValue(true), null)
				: new SqlPredicate.Expr(inner);
			var outer = new SqlConditionExpression(outerCondition, new SqlValue(typeof(bool?), outerTrueValue), new SqlValue(typeof(bool?), outerFalseValue));

			var optimized = new SqlExpressionOptimizerVisitor(allowModify).Optimize(
				new EvaluationContext(), NullabilityContext.NonQuery, new DataOptions(), new MappingSchema(), outer, false);
			var evaluation = new EvaluationContext();
			evaluation.Register(parameter, false, conditionValue);

			optimized.TryEvaluateExpression(evaluation, out var actual)
				.ShouldBeTrue();
			actual.ShouldBe((conditionValue ? innerTrueValue : !innerTrueValue) ? outerTrueValue : outerFalseValue);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/6459")]
		public void NestedDynamicConditionPreservesResult(
			[Values] bool innerTrueValue,
			[Values] bool innerFalseValue,
			[Values] bool conditionValue,
			[Values] bool swapOuterValues,
			[Values] bool compareToTrue,
			[Values] bool allowModify)
		{
			var parameter  = new SqlParameter(new DbDataType(typeof(bool)), "condition", null);
			var trueValue  = new SqlParameter(new DbDataType(typeof(bool)), "trueValue", null);
			var falseValue = new SqlParameter(new DbDataType(typeof(bool)), "falseValue", null);
			var condition  = new SqlPredicate.Expr(parameter);
			var inner      = new SqlConditionExpression(condition, trueValue, falseValue);
			ISqlPredicate outerCondition = compareToTrue
				? new SqlPredicate.ExprExpr(inner, SqlPredicate.Operator.Equal, new SqlValue(true), null)
				: new SqlPredicate.Expr(inner);
			var outer = new SqlConditionExpression(outerCondition,
				swapOuterValues ? falseValue : trueValue,
				swapOuterValues ? trueValue : falseValue);

			var optimized = new SqlExpressionOptimizerVisitor(allowModify).Optimize(
				new EvaluationContext(), NullabilityContext.NonQuery, new DataOptions(), new MappingSchema(), outer, false);
			var evaluation = new EvaluationContext();
			evaluation.Register(parameter, false, conditionValue);
			evaluation.Register(trueValue, false, innerTrueValue);
			evaluation.Register(falseValue, false, innerFalseValue);

			var innerResult = conditionValue ? innerTrueValue : innerFalseValue;
			optimized.TryEvaluateExpression(evaluation, out var actual)
				.ShouldBeTrue();
			actual.ShouldBe(innerResult != swapOuterValues ? innerTrueValue : innerFalseValue);
		}

		[Test]
		public void ConditionalWithBinary([DataSources] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(OptimizationData.Seed());

			CheckPredicate(table, x => (x.IntVlaue == 1 ? 3 : 4) == 3);

			CheckPredicate(table, x => (x.IntVlaue == 1 ? null : false) == true);
			CheckPredicate(table, x => (x.IntVlaue == 1 ? null : true) == true);

			CheckPredicate(table, x => (x.BoolValue ? true : false) == true);
			CheckPredicate(table, x => (x.BoolValue == true ? null : true) == true);
			CheckPredicate(table, x => (x.BoolValue == true ? true : false) == true);

			CheckPredicate(table, x => (x.StringValueNullable == null ? 1 : x.StringValueNullable != null ? 2 : 3) == 2);

			CheckPredicate(table, x => (x.StringValueNullable == null ? 2 : x.StringValueNullable != null ? 1 : 3) == 2);

			// Full optimization
			CheckPredicate(table, x => (x.StringValueNullable == null ? 2 : x.StringValueNullable != null ? 1 : 3) > 3);
			CheckPredicate(table, x => (x.StringValueNullable == null ? 2 : x.StringValueNullable != null ? 1 : 3) >= 1);

			CheckPredicate(table, x => (x.StringValueNullable == null ? 2 : x.StringValueNullable != null ? 1 : 3) > 1);

			CheckPredicate(table, x => (x.StringValueNullable == null ? 1 : x.StringValueNullable != null ? 2 : 3) != 2);

			CheckPredicate(table, x => ((x.StringValueNullable != null) ? (x.StringValueNullable == "2" ? 2 : 10) : (x.StringValueNullable == null) ? 3 : 1) == 2);

			CheckPredicate(table, x => (x.IntVlaue < 4 ? 4 : x.IntVlaue) == 0, false, false);
			CheckPredicate(table, x => (x.IntVlaue < 4 ? 4 : x.IntVlaue) != 0);

			CheckPredicate(table, x => (x.IntVlaue < 4 ? 4 : x.IntVlaue) > 0);

			CheckPredicate(table, x => (x.IntVlaue < 4 ? 4 : x.IntVlaue) < 0);
			CheckPredicate(table, x => (x.IntVlaue < 4 ? 4 : x.IntVlaue) >= 0);
			CheckPredicate(table, x => (x.IntVlaue < 4 ? 4 : x.IntVlaue) <= 0);

			CheckPredicate(table, x => (x.IntVlaue >= 4 ? x.IntVlaue : 4) == 0);
			CheckPredicate(table, x => (x.IntVlaue >= 4 ? x.IntVlaue : 4) != 0);
			CheckPredicate(table, x => (x.IntVlaue >= 4 ? x.IntVlaue : 4) > 0);
			CheckPredicate(table, x => (x.IntVlaue >= 4 ? x.IntVlaue : 4) < 0);
			CheckPredicate(table, x => (x.IntVlaue >= 4 ? x.IntVlaue : 4) >= 0);
			CheckPredicate(table, x => (x.IntVlaue >= 4 ? x.IntVlaue : 4) <= 0);
		}

		#region Helpers

		class OptimizationData
		{
			[PrimaryKey] public int Id { get; set; }

			public int IntVlaue { get; set; }
			[Nullable]
			public int? IntVlaueNullable { get; set; }

			public bool BoolValue { get; set; }

			[NotColumn(Configuration = ProviderName.Sybase)]
			[Nullable]
			public bool? BoolValueNullable { get; set; }

			public string StringValue { get; set; } = default!;

			[Nullable]
			public string? StringValueNullable { get; set; }

			public static OptimizationData[] Seed()
			{
				return
				[
					new OptimizationData { Id = 1, IntVlaue = 1, IntVlaueNullable = 0, BoolValue = true, BoolValueNullable  = true, StringValue  = "1", StringValueNullable = "1" },
					new OptimizationData { Id = 2, IntVlaue = 2, IntVlaueNullable = 1, BoolValue = false, BoolValueNullable = null, StringValue  = "0", StringValueNullable = "0" },
					new OptimizationData { Id = 3, IntVlaue = 4, IntVlaueNullable = 4, BoolValue = false, BoolValueNullable = null, StringValue  = "1", StringValueNullable = "1" },
					new OptimizationData { Id = 4, IntVlaue = 0, IntVlaueNullable = 1, BoolValue = true, BoolValueNullable  = true, StringValue  = "0", StringValueNullable = null },
					new OptimizationData { Id = 5, IntVlaue = 1, IntVlaueNullable = 3, BoolValue = true, BoolValueNullable  = true, StringValue  = "1", StringValueNullable = null },
					new OptimizationData { Id = 6, IntVlaue = 3, IntVlaueNullable = 0, BoolValue = false, BoolValueNullable = false, StringValue = "0", StringValueNullable = "0" },
					new OptimizationData { Id = 7, IntVlaue = 1, IntVlaueNullable = 4, BoolValue = false, BoolValueNullable = false, StringValue = "1", StringValueNullable = "1" },
					new OptimizationData { Id = 8, IntVlaue = 3, IntVlaueNullable = 2, BoolValue = true, BoolValueNullable  = true, StringValue  = "0", StringValueNullable = "0" }
				];
			}
		}

		private string PrintExpression(Expression expression)
		{
			var printer = new ExpressionPrinter();
			return printer.PrintExpression(expression);
		}

		private void CheckPredicate<T>(IQueryable<T> query, Expression<Func<T, bool>> predicate, bool includeNot = true, bool withPermutations = true)
		{
			var originalParameter = predicate.Parameters[0];

			AssertQuery(query.Where(predicate).TagQuery(PrintExpression(predicate)));

			if (includeNot)
			{
				var parameter = Expression.Parameter(typeof(T), originalParameter.Name + "_with_not");
				var body      = predicate.GetBody(parameter);

				var notPredicate = Expression.Lambda<Func<T, bool>>(Expression.Not(body), parameter);

				var notQuery = query.Where(notPredicate);
				AssertQuery(notQuery.TagQuery(PrintExpression(notPredicate)));
			}

			if (withPermutations)
			{
				if (predicate.Body is BinaryExpression binary)
				{
					if (binary.NodeType == ExpressionType.Equal || binary.NodeType == ExpressionType.NotEqual)
					{
						var parameter        = Expression.Parameter(typeof(T), "swap");
						var swappedPredicate = Expression.Lambda<Func<T, bool>>(Expression.MakeBinary(binary.NodeType, binary.Right.Replace(originalParameter, parameter), binary.Left.Replace(originalParameter, parameter)), parameter);
						CheckPredicate(query, swappedPredicate, includeNot, false);
					}
				}
			}
		}

		#endregion Helpers
	}
}
