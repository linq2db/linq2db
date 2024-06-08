﻿using System;
using System.Linq.Expressions;

using LinqToDB.Mapping;

namespace LinqToDB.Linq.Translation
{
	using LinqToDB.Expressions;

	using SqlQuery;

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

		SqlPlaceholderExpression CreatePlaceholder(SelectQuery selectQuery, ISqlExpression sqlExpression, Expression basedOn);
		SqlErrorExpression CreateErrorExpression(Expression basedOn, string message);

		public bool CanBeCompiled(Expression      expression, TranslationFlags translationFlags);
		public bool IsServerSideOnly(Expression   expression, TranslationFlags translationFlags);
		public bool IsPreferServerSide(Expression expression, TranslationFlags translationFlags);

		bool    CanBeEvaluated(Expression  expression);
		object? Evaluate(Expression        expression);
		bool    TryEvaluate(ISqlExpression expression, out object? result);
	}
}
