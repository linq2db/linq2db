using System;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.Model;

namespace LinqToDB.Internal.Linq.Translation
{
	[Flags]
	public enum TranslationFlags
	{
		None = 0,
		Expression = 1,
		Sql        = 1 << 1,
	}

	public interface ITranslationContext
	{
		Expression Translate(Expression expression, TranslationFlags translationFlags = TranslationFlags.Sql);

		public MappingSchema MappingSchema { get; }
		public DataOptions   DataOptions   { get; }

		public ISqlExpressionFactory ExpressionFactory { get; }

		public ColumnDescriptor? CurrentColumnDescriptor { get; }
		public SelectQuery       CurrentSelectQuery      { get; }
		public string?           CurrentAlias            { get; }

		SqlPlaceholderExpression CreatePlaceholder(SelectQuery    selectQuery, ISqlExpression sqlExpression,  Expression basedOn);
		SqlErrorExpression       CreateErrorExpression(Expression basedOn,     string?        message = null, Type?      type = null);

		public bool CanBeEvaluatedOnClient(Expression expression);

		bool    CanBeEvaluated(Expression     expression);
		object? Evaluate(Expression           expression);
		bool    TryEvaluate(ISqlExpression    expression, out object? result);

		/// <summary>
		/// Forces expression cache to compare expressions by value, not by reference.
		/// </summary>
		/// <param name="expression"></param>
		void MarkAsNonParameter(Expression expression, object? currentValue);

		public IDisposable UsingColumnDescriptor(ColumnDescriptor? columnDescriptor);
	}
}
