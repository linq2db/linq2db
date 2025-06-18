using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Common.Internal;
using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	public class StringMemberTranslatorBase : MemberTranslatorBase
	{
		public StringMemberTranslatorBase()
		{
			Registration.RegisterMethod(() => Sql.Like(null, null), TranslateLike);
			Registration.RegisterMethod(() => Sql.Like(null, null, null), TranslateLike);
#if NETFRAMEWORK
			Registration.RegisterMethod(() => System.Data.Linq.SqlClient.SqlMethods.Like(null, null), TranslateLike);
			Registration.RegisterMethod(() => System.Data.Linq.SqlClient.SqlMethods.Like(null, null, '~'), TranslateLike);
#endif
			Registration.RegisterMethod(() => Sql.Replace("", "", ""), TranslateSqlReplace);

			// ReSharper disable ReturnValueOfPureMethodIsNotUsed
			Registration.RegisterMethod(() => "".Replace("", ""), TranslateStringReplace);
			Registration.RegisterMethod(() => "".Replace(' ', ' '), TranslateStringReplace);

			Registration.RegisterMethod(() => string.Join(",", Enumerable.Empty<string>()), TranslateStringAggregate);
		}

		Expression? TranslateLike(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			using var disposable = translationContext.UsingTypeFromExpression(methodCall.Arguments[0], methodCall.Arguments[1]);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedField))
				return translationContext.CreateErrorExpression(methodCall.Arguments[0], type: methodCall.Type);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out var translatedValue))
				return translationContext.CreateErrorExpression(methodCall.Arguments[1], type: methodCall.Type);

			ISqlExpression? escape = null;

			if (methodCall.Arguments.Count == 3)
			{
				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[2], out escape))
					return translationContext.CreateErrorExpression(methodCall.Arguments[2], type: methodCall.Type);
			}

			var predicate       = new SqlPredicate.Like(translatedField, false, translatedValue, escape);
			var searchCondition = translationContext.ExpressionFactory.SearchCondition().Add(predicate);

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, searchCondition, methodCall);
		}

		Expression? TranslateSqlReplace(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			using var disposable = translationContext.UsingTypeFromExpression(methodCall.Arguments[0], methodCall.Arguments[1]);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedField))
				return translationContext.CreateErrorExpression(methodCall.Arguments[0], type: methodCall.Type);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out var translatedOldValue))
				return translationContext.CreateErrorExpression(methodCall.Arguments[1], type: methodCall.Type);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[2], out var translatedNevValue))
				return translationContext.CreateErrorExpression(methodCall.Arguments[2], type: methodCall.Type);

			var resultSql = TranslateReplace(translationContext, methodCall, translationFlags, translatedField, translatedOldValue, translatedNevValue);

			if (resultSql == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, resultSql, methodCall);
		}

		Expression? TranslateStringReplace(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (methodCall.Object == null)
				return null;

			if (translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			using var disposable = translationContext.UsingTypeFromExpression(methodCall.Object);

			if (!translationContext.TranslateToSqlExpression(methodCall.Object, out var translatedField))
				return translationContext.CreateErrorExpression(methodCall.Object, type: methodCall.Type);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedOldValue))
				return translationContext.CreateErrorExpression(methodCall.Arguments[0], type: methodCall.Type);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out var translatedNevValue))
				return translationContext.CreateErrorExpression(methodCall.Arguments[1], type: methodCall.Type);

			var resultSql = TranslateReplace(translationContext, methodCall, translationFlags, translatedField, translatedOldValue, translatedNevValue);

			if (resultSql == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, resultSql, methodCall);
		}

		Expression? TranslateStringAggregate(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var result = translationContext.BuildAggregationFunction(methodCall.Arguments[1], methodCall, ITranslationContext.AllowedAggregationOperators.Filter | ITranslationContext.AllowedAggregationOperators.OrderBy,
				(info) =>
				{
					if (info.ValueExpression == null)
						return (null, null);

					if (!info.TranslateExpression(info.ValueExpression, out var translatedValue, out var error))
						return (null, error);

					if (!info.TranslateExpression(methodCall.Arguments[0], out var separator, out error))
						return (null, error);

					ISqlExpression? suffix = null;

					var factory = translationContext.ExpressionFactory;

					var stringDataType = factory.GetDbDataType(translatedValue);

					// string.Join(", ", ["1", null, "3"]]) generates "1, , 3" , so we need to coalesce nulls to empty strings
					translatedValue = factory.Coalesce(translatedValue, factory.Value(stringDataType, string.Empty));

					if (info.OrderBy.Length > 0)
					{
						using var sb = Pools.StringBuilder.Allocate();

						var arguments = new ISqlExpression[info.OrderBy.Length];

						sb.Value.Append("ORDER BY ");

						for (var i = 0; i < info.OrderBy.Length; i++)
						{
							if (!info.TranslateExpression(info.OrderBy[i].Expr, out var argument, out error))
							{
								return (null, error);
							}

							arguments[i] = argument;

							if (i > 0)
								sb.Value.Append(", ");
							sb.Value.Append('{').Append(i).Append('}');
							if (info.OrderBy[i].IsDescending)
								sb.Value.Append(" DESC");

							if (info.OrderBy[i].Nulls != Sql.NullsPosition.None)
							{
								sb.Value.Append(" NULLS ");
								sb.Value.Append(info.OrderBy[i].Nulls == Sql.NullsPosition.First ? "FIRST" : "LAST");
							}
						}

						suffix = factory.Fragment(stringDataType, sb.Value.ToString(), arguments);
					}

					var function = factory.WindowFunction(stringDataType, "STRING_AGG",
						[new SqlFunctionArgument(translatedValue), new SqlFunctionArgument(separator, suffix : suffix)],
						[true, true], isAggregate: true);

					var coalesce = factory.Coalesce(function, factory.Value(stringDataType, string.Empty));

					return (coalesce, null);
				});

			return result;
		}

		Expression? TranslateStringAggregateOld(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (!translationContext.GetAggregationInfo(methodCall.Arguments[1],
				    TranslationContextExtensions.AllowedAggregationOperators.Filter | TranslationContextExtensions.AllowedAggregationOperators.OrderBy, out var info))
				{ return null; }


			if (!info.TranslateValue(translationContext, out var translatedValue, out var error))
				return error;

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var separator))
				return translationContext.CreateErrorExpression(methodCall.Arguments[0], type: methodCall.Type);

			var factory = translationContext.ExpressionFactory;

			var stringDataType = factory.GetDbDataType(translatedValue);

			if (info.OrderBy.Length > 0)
			{
				using var sb = Pools.StringBuilder.Allocate();

				var arguments = new ISqlExpression[info.OrderBy.Length + 1];
				arguments[0] = separator;

				sb.Value.Append("{0} ORDER BY ");

				for (var i = 0; i < info.OrderBy.Length; i++)
				{
					if (!translationContext.TranslateToSqlExpression(info.OrderBy[i].Expr, out var argument))
					{
						return translationContext.CreateErrorExpression(info.OrderBy[i].Expr, type: methodCall.Type);
					}

					arguments[i + 1] = argument;

					if (i > 0)
						sb.Value.Append(", ");
					sb.Value.Append('{').Append(i + 1).Append('}');
					if (info.OrderBy[i].IsDescending)
						sb.Value.Append(" DESC");

					if (info.OrderBy[i].Nulls != Sql.NullsPosition.None)
					{
						sb.Value.Append(" NULLS ");
						sb.Value.Append(info.OrderBy[i].Nulls == Sql.NullsPosition.First ? "FIRST" : "LAST");
					}
				}

				separator = factory.Fragment(stringDataType, sb.Value.ToString(), arguments);
			}

			var function = factory.WindowFunction(stringDataType, "STRING_AGG", [new SqlFunctionArgument(translatedValue), new SqlFunctionArgument(separator)], [true, true]);

			var coalesce = factory.Coalesce(function, factory.Value(stringDataType, string.Empty));

			var placeholder = info.CreatePlaceholder(translationContext, coalesce, methodCall);

			return placeholder;
		}

		public virtual ISqlExpression? TranslateReplace(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression oldValue, ISqlExpression newValue)
		{
			var factory = translationContext.ExpressionFactory;

			var valueType = factory.GetDbDataType(value);

			return factory.Function(valueType, "REPLACE", value, factory.EnsureType(oldValue, valueType), factory.EnsureType(newValue, valueType));
		}

		public virtual ISqlExpression? TranslateStringFormat(ITranslationContext translationContext, MethodCallExpression methodCall, string format, IList<ISqlExpression> arguments, TranslationFlags translationFlags)
		{
			return QueryHelper.ConvertFormatToConcatenation(format, arguments);
		}
	}
}
