using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq.Translation
{
	public static class TranslationContextExtensions
	{
		public static bool TryEvaluate<T>(this ITranslationContext translationContext, Expression expression, out T result)
		{
			if (translationContext.CanBeEvaluated(expression))
			{
				var value = translationContext.Evaluate(expression);
				if (value is T t)
				{
					result = t;
					return true;
				}
			}

			result = default!;
			return false;
		}

		public static DbDataType GetDbDataType(this ITranslationContext translationContext, ISqlExpression sqlExpression)
		{
			return QueryHelper.GetDbDataType(sqlExpression, translationContext.MappingSchema);
		}

		public static SqlPlaceholderExpression CreatePlaceholder(this ITranslationContext translationContext, ISqlExpression sqlExpression, Expression basedOn)
		{
			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, sqlExpression, basedOn);
		}

		public static bool TranslateToSqlExpression(this ITranslationContext translationContext, Expression expression, [NotNullWhen(true)] out ISqlExpression? translated)
		{
			var result = translationContext.Translate(expression, TranslationFlags.Sql);

			if (result is not SqlPlaceholderExpression placeholder)
			{
				translated = null;
				return false;
			}

			translated = placeholder.Sql;
			return true;
		}

		public static IDisposable? UsingTypeFromExpression(this ITranslationContext translationContext, ISqlExpression? fromExpression)
		{
			if (fromExpression is null)
				return null;

			var columnDescriptor = QueryHelper.GetColumnDescriptor(fromExpression);

			if (columnDescriptor == null)
				return null;

			return translationContext.UsingColumnDescriptor(columnDescriptor);
		}

		public static IDisposable? UsingTypeFromExpression(this ITranslationContext translationContext, params Expression[] fromExpressions)
		{
			for (var i = 0; i < fromExpressions.Length; i++)
			{
				var translated = translationContext.Translate(fromExpressions[i], TranslationFlags.Sql);
				if (translated is not SqlPlaceholderExpression placeholder)
					continue;
				var found = translationContext.UsingTypeFromExpression(placeholder.Sql);
				if (found != null)
					return found;
			}

			return null;
		}
	}
}
