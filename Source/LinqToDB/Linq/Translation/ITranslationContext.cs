using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Linq.Translation
{
	public interface ITranslationContext : ISqlExpressionTranslator
	{
		[Flags]
		public enum AllowedAggregationOperators
		{
			None     = 0,
			Filter   = 1 << 0,
			OrderBy  = 1 << 1,
			Distinct = 1 << 2,

			All = Filter | OrderBy | Distinct
		}

		public record OrderByInformation(Expression Expr, bool IsDescending, Sql.NullsPosition Nulls);

		Expression Translate(Expression expression, TranslationFlags translationFlags = TranslationFlags.Sql);

		MappingSchema MappingSchema { get; }
		DataOptions   DataOptions   { get; }

		ISqlExpressionFactory ExpressionFactory { get; }

		ColumnDescriptor? CurrentColumnDescriptor { get; }
		SelectQuery       CurrentSelectQuery      { get; }
		string?           CurrentAlias            { get; }

		SqlPlaceholderExpression CreatePlaceholder(SelectQuery    selectQuery, ISqlExpression sqlExpression,  Expression basedOn);
		SqlErrorExpression       CreateErrorExpression(Expression basedOn,     string?        message = null, Type?      type = null);

		Expression? GetAggregationContext(Expression     expression);
		SelectQuery GetAggregationSelectQuery(Expression enumerableContext);

		bool CanBeEvaluatedOnClient(Expression expression);

		bool    CanBeEvaluated(Expression     expression);
		object? Evaluate(Expression           expression);
		bool    TryEvaluate(ISqlExpression    expression, out object? result);

		Expression? BuildArrayAggregationFunction(
			Expression                                                                                                      methodsChain,
			Expression                                                                                                      functionExpression,
			AllowedAggregationOperators                                                                                     allowedOperations,
			Func<IAggregationContext, (ISqlExpression? sqlExpr, SqlErrorExpression? error, Expression? fallbackExpression)> functionFactory
		);

		Expression? BuildAggregationFunction(
			Expression                                                                                                      methodsChain,
			Expression                                                                                                      functionExpression,
			AllowedAggregationOperators                                                                                     allowedOperations,
			Func<IAggregationContext, (ISqlExpression? sqlExpr, SqlErrorExpression? error, Expression? fallbackExpression)> functionFactory
		);

		/// <summary>
		/// Forces expression cache to compare expressions by value, not by reference.
		/// </summary>
		/// <param name="expression"></param>
		void MarkAsNonParameter(Expression expression, object? currentValue);

		IDisposable UsingColumnDescriptor(ColumnDescriptor? columnDescriptor);
		IDisposable UsingCurrentAggregationContext(Expression basedOn);
	}
}
