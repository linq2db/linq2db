using System;
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

			Registration.RegisterMethod(() => string.Join(",", Enumerable.Empty<string>()), TranslateStringJoin);
			Registration.RegisterMethod(() => string.Join(",", Array.Empty<string>()), TranslateStringJoin);
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

		Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			return TranslateStringJoin(translationContext, methodCall, translationFlags, ignoreNulls: false);
		}

		protected void ConfigureConcatWs(AggregateFunctionBuilder builder, Func<ISqlExpressionFactory, DbDataType, ISqlExpression, ISqlExpression[], ISqlExpression>? functionFactory = null)
		{
			builder
				.ConfigurePlain(c => c
					.HasSequenceIndex(1)
					.TranslateArguments(0)
					.AllowFilter()
					.AllowNotNullCheck(true)
					.OnBuildFunction(composer =>
					{
						var info = composer.BuildInfo;
						if (info.Values.Length == 0 || info.Argument(0) == null)
						{
							composer.SetResult(info.Factory.Value(info.Factory.GetDbDataType(typeof(string)), string.Empty));
							return;
						}

						var factory   = info.Factory;
						var separator = info.Argument(0)!;
						var dataType  = factory.GetDbDataType(info.Values[0]);

						if (info.Values.Length == 1)
						{
							var singleValue = info.Values[0];
							singleValue = factory.Coalesce(singleValue, factory.Value(dataType, string.Empty));
							composer.SetResult(singleValue);
							return;
						}

						if (!composer.GetFilteredToNullValues(out IEnumerable<ISqlExpression>? values, out var error))
						{
							composer.SetError(error);
							return;
						}

						var items = info.IsNullFiltered
							? values
							: values.Select(i => factory.Coalesce(i, factory.Value(factory.GetDbDataType(i), ""))).ToArray();

						if (functionFactory != null)
						{
							var customResult = functionFactory(factory, dataType, separator, items.ToArray());
							composer.SetResult(customResult);
						}
						else
						{
							var function = factory.Function(dataType, "CONCAT_WS",
								parametersNullability : ParametersNullabilityType.IfAllParametersNullable,
								[separator, ..items]);

							composer.SetResult(function);
						}

					}));
		}

		protected void ConfigureConcatWsEmulation(AggregateFunctionBuilder builder, Func<ISqlExpressionFactory, DbDataType, ISqlExpression, ISqlExpression, ISqlExpression> substringFunc)
		{
			builder
				.ConfigurePlain(c => c
					.HasSequenceIndex(1)
					.TranslateArguments(0)
					.AllowFilter()
					.AllowNotNullCheck(true)
					.OnBuildFunction(composer =>
					{
						var info = composer.BuildInfo;
						if (info.Values.Length == 0 || info.Argument(0) == null)
						{
							composer.SetResult(info.Factory.Value(info.Factory.GetDbDataType(typeof(string)), string.Empty));
							return;
						}

						var factory   = info.Factory;
						var separator = info.Argument(0)!;
						var dataType  = factory.GetDbDataType(info.Values[0]);

						if (info.Values.Length == 1)
						{
							var singleValue = info.Values[0];
							singleValue = factory.Coalesce(singleValue, factory.Value(dataType, string.Empty));
							composer.SetResult(singleValue);
							return;
						}

						if (!composer.GetFilteredToNullValues(out var values, out var error))
						{
							composer.SetError(error);
							return;
						}

						if (info.IsNullFiltered)
						{
							var concatValues = values
								.Select(v => factory.Coalesce(factory.Concat(dataType, separator, v), factory.Value(dataType, "")))
								.Aggregate((v1, v2) => factory.Concat(dataType, v1, v2));

							var substring = substringFunc(factory, dataType, separator, concatValues);

							composer.SetResult(substring);
						}
						else
						{
							var concatValues = values
								.Select(v => factory.Coalesce(v, factory.Value(dataType, "")))
								.Aggregate((v1, v2) => factory.Concat(v1, separator, v2));

							composer.SetResult(concatValues);
						}
					}));
		}

		protected virtual Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool ignoreNulls)
		{
			return null;
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
