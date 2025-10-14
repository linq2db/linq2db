using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.Translation
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
			Registration.RegisterMember(() => "".Length, TranslateLength);

			// ReSharper disable ReturnValueOfPureMethodIsNotUsed
			Registration.RegisterMethod(() => "".Replace("", ""), TranslateStringReplace);
			Registration.RegisterMethod(() => "".Replace(' ', ' '), TranslateStringReplace);

			Registration.RegisterMethod(() => "".PadLeft(0), TranslateStringPadLeft);
			Registration.RegisterMethod(() => "".PadLeft(0, ' '), TranslateStringPadLeft);

			Registration.RegisterMethod(() => string.Join(",", Enumerable.Empty<string>()), TranslateStringAggregate);
		}

		protected virtual Expression? TranslateLike(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
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

			var predicate       = translationContext.ExpressionFactory.LikePredicate(translatedField, false, translatedValue, escape);
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

		private Expression? TranslateStringPadLeft(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (methodCall.Object == null)
				return null;

			if (translationFlags.HasFlag(TranslationFlags.Expression) && translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			using var disposable = translationContext.UsingTypeFromExpression(methodCall.Object);

			if (!translationContext.TranslateToSqlExpression(methodCall.Object, out var translatedField))
				return translationContext.CreateErrorExpression(methodCall.Object, type: methodCall.Type);

			using var descriptorScope = translationContext.UsingColumnDescriptor(null);

			if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var translatedPadding))
				return translationContext.CreateErrorExpression(methodCall.Arguments[0], type: methodCall.Type);

			ISqlExpression? translatedPaddingChar = null;
			if (methodCall.Arguments.Count > 1)
			{
				using var d = translationContext.UsingTypeFromExpression(translatedField);

				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out translatedPaddingChar))
					return translationContext.CreateErrorExpression(methodCall.Arguments[1], type: methodCall.Type);
			}

			var resultSql = TranslatePadLeft(translationContext, methodCall, translationFlags, translatedField, translatedPadding, translatedPaddingChar);

			if (resultSql == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, resultSql, methodCall);
		}

		Expression? TranslateLength(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			if (memberExpression.Expression == null)
				return null;

			if (translationContext.CanBeEvaluatedOnClient(memberExpression.Expression))
				return null;

			if (!translationContext.TranslateToSqlExpression(memberExpression.Expression, out var value))
				return null;

			var translated = TranslateLength(translationContext, translationFlags, value);
			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translated, memberExpression);
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
					SqlSearchCondition? filterCondition = null;
					bool IsNullFiltered = false;

					var factory = translationContext.ExpressionFactory;

					var stringDataType = factory.GetDbDataType(translatedValue);

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

					if (info.FilterExpression != null)
					{
						if (!info.TranslateExpression(info.FilterExpression, out var filterExpr, out error))
						{
							return (null, error);
						}

						filterCondition = filterExpr as SqlSearchCondition;

						if (filterCondition is { IsAnd: true })
						{
							var isNotNull = filterCondition.Predicates.FirstOrDefault(p => p is SqlPredicate.IsNull { IsNot: true } isNull && isNull.Expr1.Equals(translatedValue));
							if (isNotNull != null)
							{
								IsNullFiltered = true;
								filterCondition.Predicates.Remove(isNotNull);
								if (filterCondition.Predicates.Count == 0)
								{
									filterCondition = null;
								}
							}
						}
					}

					if (!IsNullFiltered)
					{
						// string.Join(", ", ["1", null, "3"]]) generates "1, , 3" , so we need to coalesce nulls to empty strings
						translatedValue = factory.Coalesce(translatedValue, factory.Value(stringDataType, string.Empty));
					}

					if (filterCondition != null && !filterCondition.IsTrue())
					{
						var caseExpr = factory.Condition(
							filterCondition,
							translatedValue,
							factory.Null(stringDataType));

						translatedValue = caseExpr;
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
			return factory.Replace(value, oldValue, newValue);
		}

		public virtual ISqlExpression? TranslateStringFormat(ITranslationContext translationContext, MethodCallExpression methodCall, string format, IReadOnlyList<ISqlExpression> arguments, TranslationFlags translationFlags)
		{
			return QueryHelper.ConvertFormatToConcatenation(format, arguments);
		}

		public virtual ISqlExpression? TranslateLength(ITranslationContext translationContext, TranslationFlags translationFlags, ISqlExpression value)
		{
			var factory = translationContext.ExpressionFactory;
			return factory.Length(value);
		}

		public virtual ISqlExpression? TranslateLPad(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression padding, ISqlExpression paddingChar)
		{
			var factory = translationContext.ExpressionFactory;
			var valueTypeString = factory.GetDbDataType(value);
			return factory.Function(valueTypeString, "LPAD", value, padding, paddingChar);
		}

		public virtual ISqlExpression? TranslatePadLeft(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression padding, ISqlExpression? paddingChar)
		{
			var factory = translationContext.ExpressionFactory;

			var valueType = factory.GetDbDataType(value);

			/*
			 * CASE WHEN strValue IS NULL OR LEN(strValue) >= 2 
			 *		THEN strValue
			 *		ELSE LPad(strValue, 2) END
			 */

			var valueLen = TranslateLength(translationContext, translationFlags, value);
			if (valueLen == null)
				return null;

			paddingChar ??= factory.Value(valueType, ' ');

			var passingExpr = TranslateLPad(translationContext, methodCall, translationFlags, value, padding, paddingChar);
			if (passingExpr == null)
				return null;

			var condition = factory.SearchCondition(true)
				.Add(factory.IsNull(valueLen))
				.Add(factory.GreaterOrEqual(valueLen, padding));

			return factory.Condition(condition, value, passingExpr);
		}
	}
}
