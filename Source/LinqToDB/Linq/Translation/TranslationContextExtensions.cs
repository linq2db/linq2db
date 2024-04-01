using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Translation
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
			translated = null;
			var result = translationContext.Translate(expression, TranslationFlags.Sql);

			if (result is not SqlPlaceholderExpression placeholder)
				return false;

			translated = placeholder.Sql;

			return true;
		}
	}
}
