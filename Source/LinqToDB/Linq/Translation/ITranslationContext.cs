using System;
using System.Linq.Expressions;

using LinqToDB.Expressions;
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
	}

	public interface ITranslationContext
	{
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

		/// <summary>
		/// Forces expression cache to compare expressions by value, not by reference.
		/// </summary>
		/// <param name="expression"></param>
		void MarkAsNonParameter(Expression expression, object? currentValue);

		IDisposable UsingColumnDescriptor(ColumnDescriptor? columnDescriptor);
	}
}
