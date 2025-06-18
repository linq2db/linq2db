using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Linq.Builder;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	[Flags]
	public enum TranslationFlags
	{
		None       = 0,
		Expression = 1,
		Sql        = 1 << 1,
		Expand     = 1 << 2,
		Traverse   = 1 << 3
	}

	public interface ISqlExpressionTranslator
	{
		public bool TranslateExpression(Expression expression, [NotNullWhen(true)] out ISqlExpression? sql, out SqlErrorExpression? error);
	}

	public interface IAggregationContext : ISqlExpressionTranslator
	{
		public Expression?                              FilterExpression { get; }
		public Expression?                              ValueExpression  { get; }
		public ITranslationContext.OrderByInformation[] OrderBy          { get; }
		public bool                                     IsDistinct       { get; }
		public bool                                     IsGroupBy        { get;  }

		public bool      TranslateLambdaExpression(LambdaExpression lambdaExpression, [NotNullWhen(true)] out ISqlExpression? sql, out SqlErrorExpression? error);
		LambdaExpression SimplifyEntityLambda(LambdaExpression      lambda,           int                                     parameterIndex);
	}

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

		public Expression? BuildAggregationFunction(Expression                              methodsChain,
			Expression                                                                      functionExpression,
			AllowedAggregationOperators                                                     allowedOperations,
			Func<IAggregationContext, (ISqlExpression? sqlExpr, SqlErrorExpression? error)> functionFactory);

		/// <summary>
		/// Forces expression cache to compare expressions by value, not by reference.
		/// </summary>
		/// <param name="expression"></param>
		void MarkAsNonParameter(Expression expression, object? currentValue);

		IDisposable UsingColumnDescriptor(ColumnDescriptor? columnDescriptor);
		IDisposable UsingCurrentAggregationContext(Expression basedOn);
	}
}
