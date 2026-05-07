using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Reflection;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

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
			Registration.RegisterMethod(() => Sql.Replace("", (char?)null, null), TranslateSqlReplace);
			Registration.RegisterMember(() => "".Length, TranslateLength);

			// ReSharper disable ReturnValueOfPureMethodIsNotUsed
			Registration.RegisterMethod(() => "".Replace("", ""), TranslateStringReplace);
			Registration.RegisterMethod(() => "".Replace(' ', ' '), TranslateStringReplace);

			Registration.RegisterMethod(() => "".PadLeft(0), TranslateStringPadLeft);
			Registration.RegisterMethod(() => "".PadLeft(0, ' '), TranslateStringPadLeft);

			Registration.RegisterMethod(() => string.Join(string.Empty, Enumerable.Empty<string?>()),  TranslateStringJoin);
			Registration.RegisterMethod(() => string.Join(string.Empty, Array.Empty<string?>()),       TranslateStringJoin);
#pragma warning disable RS0030 // Do not use banned APIs
			Registration.RegisterMethod(() => string.Join(string.Empty, Array.Empty<object?>()),       TranslateStringJoin);
			Registration.RegisterMethod(() => string.Join(string.Empty, Enumerable.Empty<int>()),      TranslateStringJoin, isGenericTypeMatch: true);
#pragma warning restore RS0030 // Do not use banned APIs
#if !NETSTANDARD2_0 && !NETFRAMEWORK
#pragma warning disable RS0030 // Do not use banned APIs
			Registration.RegisterMethod(() => string.Join(',', Array.Empty<object?>()),                TranslateStringJoin);
			Registration.RegisterMethod(() => string.Join(',', Enumerable.Empty<int>()),               TranslateStringJoin, isGenericTypeMatch: true);
#pragma warning restore RS0030 // Do not use banned APIs
			Registration.RegisterMethod(() => string.Join(',', Array.Empty<string?>()),                TranslateStringJoin);
#endif

			Registration.RegisterMethod(() => Sql.ConcatStrings(string.Empty, Enumerable.Empty<string>()), TranslateConcatStrings);
			Registration.RegisterMethod(() => Sql.ConcatStrings(string.Empty, Array.Empty<string>()),      TranslateConcatStrings);

			Registration.RegisterMethod(() => Sql.ConcatStringsNullable(",", Enumerable.Empty<string>()), TranslateConcatStringsNullable);

			// CONCAT
			Registration.RegisterMethod(() => string.Concat((object?)null),                                              TranslateConcatWithoutNull);
			Registration.RegisterMethod(() => string.Concat((object?)null, (object?)null),                               TranslateConcatWithoutNull);
			Registration.RegisterMethod(() => string.Concat((object?)null, (object?)null, (object?)null),                TranslateConcatWithoutNull);
			Registration.RegisterMethod(() => string.Concat((string?)null, (string?)null),                               TranslateConcatWithoutNull);
			Registration.RegisterMethod(() => string.Concat((string?)null, (string?)null, (string?)null),                TranslateConcatWithoutNull);
			Registration.RegisterMethod(() => string.Concat((string?)null, (string?)null, (string?)null, (string?)null), TranslateConcatWithoutNull);
			Registration.RegisterMethod(() => string.Concat(Array.Empty<string?>()),                                     TranslateConcatWithoutNullList);
			Registration.RegisterMethod(() => string.Concat(Enumerable.Empty<string?>()),                                TranslateConcatWithoutNullList);
			Registration.RegisterMethod(() => string.Concat(Array.Empty<object?>()),                                     TranslateConcatWithoutNullList);
			Registration.RegisterMethod(() => string.Concat(Enumerable.Empty<int>()),                                    TranslateConcatWithoutNullList, isGenericTypeMatch: true);
			Registration.RegisterMethod(() => Sql.Concat(Array.Empty<string?>()),                                        TranslateConcatNullableList);
			Registration.RegisterMethod(() => Sql.Concat(Array.Empty<object?>()),                                        TranslateConcatNullableList);
		}

		/// <summary>
		/// Catches all string-typed binary `Add` / `AddChecked` expressions (`a + b` where the
		/// result type is string), regardless of operand types. C# `string + obj` treats null as
		/// empty string while SQL `||` propagates NULL — for each operand we rewrite non-string
		/// operands to `.ToString()` calls (so ordinary translation produces a string-typed SQL
		/// expression / CAST), translate to SQL, and wrap each side with `COALESCE(..., '')`.
		/// </summary>
		protected override Expression? TranslateOverrideHandler(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			if (memberExpression is BinaryExpression { Method: not null } binaryExpression
			    && binaryExpression.Type == typeof(string)
			    && binaryExpression.NodeType is ExpressionType.Add or ExpressionType.AddChecked)
			{
				return TranslateBinaryStringConcat(translationContext, binaryExpression, translationFlags);
			}

			return base.TranslateOverrideHandler(translationContext, memberExpression, translationFlags);
		}

		static Expression? TranslateBinaryStringConcat(ITranslationContext translationContext, BinaryExpression binaryExpression, TranslationFlags translationFlags)
		{
			if (translationContext.CanBeEvaluatedOnClient(binaryExpression))
				return null;

			var left  = ConvertOperandToString(binaryExpression.Left);
			var right = ConvertOperandToString(binaryExpression.Right);

			using var disposable = translationContext.UsingTypeFromExpression(left, right);

			// If an operand can't be SQL-translated (e.g. let-bound non-translatable expression),
			// bail out so VisitBinary falls back to the regular binary `+` handling which can
			// partition the projection for client-side evaluation.
			if (!translationContext.TranslateToSqlExpression(left, out var leftSql))
				return null;

			if (!translationContext.TranslateToSqlExpression(right, out var rightSql))
				return null;

			// C# string concatenation treats null as empty string. SQL `||` propagates NULL —
			// always wrap each operand with COALESCE(..., '') so the SQL matches the C# expectation.
			var factory = translationContext.ExpressionFactory;

			leftSql  = factory.Coalesce(leftSql,  factory.Value(factory.GetDbDataType(leftSql),  string.Empty));
			rightSql = factory.Coalesce(rightSql, factory.Value(factory.GetDbDataType(rightSql), string.Empty));

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, factory.Concat(leftSql, rightSql), binaryExpression);
		}

		static Expression ConvertOperandToString(Expression operand)
		{
			if (operand.Type == typeof(string))
				return operand;

			// C# `string + non-string` boxes the non-string operand to object via Convert<object>;
			// peel it so the underlying value reaches a normal ToString() translation.
			operand = operand.UnwrapConvertToObject();

			if (operand.Type == typeof(string))
				return operand;

			return Expression.Call(operand, Methods.System.Object_ToString);
		}

		Expression? TranslateConcatWithoutNullList(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			return TranslateStringJoin(translationContext, methodCall, translationFlags, nullValuesAsEmptyString: true, isNullableResult: false, withoutSeparator: true);
		}

		Expression? TranslateConcatNullableList(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			return TranslateStringJoin(translationContext, methodCall, translationFlags, nullValuesAsEmptyString: false, isNullableResult: true, withoutSeparator: true);
		}

		Expression? TranslateConcatWithoutNull(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			return TranslateConcat(translationContext, methodCall, translationFlags, nullValuesAsEmptyString: true);
		}

		Expression? TranslateConcat(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags _/*translationFlags*/, bool nullValuesAsEmptyString)
		{
			if (translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;

			// Rewrite non-string arguments to `.ToString()` calls so each operand reaches the
			// provider's Guid / numeric / DateTime → string translator (e.g. SQLite's hex-and-substr
			// pattern for Guid). Without this rewrite the (object, object[, object]) overload's
			// raw `CAST(arg AS VarChar(N))` fallback emits the binary representation for Guid on
			// SQLite, the wrong format on Oracle, etc. — diverging from C# `string.Concat` semantics.
			var arguments = new Expression[methodCall.Arguments.Count];
			for (var i = 0; i < arguments.Length; i++)
				arguments[i] = ConvertOperandToString(methodCall.Arguments[i]);

			var fragments = new ISqlExpression[arguments.Length];

			using var disposable = translationContext.UsingTypeFromExpression(arguments[0], arguments[1]);

			for (var i = 0; i < arguments.Length; i++)
			{
				if (!translationContext.TranslateToSqlExpression(arguments[i], out var translatedFragment))
					return translationContext.CreateErrorExpression(arguments[i], type: methodCall.Type);

				fragments[i] = translatedFragment;
			}

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, new SqlConcatExpression(!nullValuesAsEmptyString, fragments), methodCall);
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
			if (translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;
			return TranslateStringJoin(translationContext, methodCall, translationFlags, nullValuesAsEmptyString: true, isNullableResult : false, withoutSeparator: false);
		}

		Expression? TranslateConcatStrings(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;
			return TranslateStringJoin(translationContext, methodCall, translationFlags, nullValuesAsEmptyString: false, isNullableResult : false, withoutSeparator: false);
		}

		Expression? TranslateConcatStringsNullable(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (translationContext.CanBeEvaluatedOnClient(methodCall))
				return null;
			return TranslateStringJoin(translationContext, methodCall, translationFlags, nullValuesAsEmptyString: false, isNullableResult : true, withoutSeparator: false);
		}

		protected void ConfigureConcatWs(AggregateFunctionBuilder builder, bool nullValuesAsEmptyString, bool isNullableResult, Func<ISqlExpressionFactory, DbDataType, ISqlExpression, ISqlExpression[], ISqlExpression>? functionFactory = null, bool withoutSeparator = false)
		{
			builder
				.ConfigurePlain(c =>
				{
					if (withoutSeparator)
						c.HasSequenceIndex(0);
					else
						c.HasSequenceIndex(1).TranslateArguments(0);

					c.AllowFilter()
						.AllowNotNullCheck(true)
						.OnBuildFunction(composer =>
						{
							var info = composer.BuildInfo;
							if (info.Values.Length == 0 || (!withoutSeparator && info.Argument(0) == null))
							{
								composer.SetResult(info.Factory.Value(info.Factory.GetDbDataType(typeof(string)), string.Empty));
								return;
							}

							var factory   = info.Factory;
							var separator = withoutSeparator
								? factory.Value(factory.GetDbDataType(typeof(string)), string.Empty)
								: info.Argument(0)!;
							var dataType  = factory.GetDbDataType(info.Values[0]);

							if (info.Values is [var singleValue])
							{
								singleValue = isNullableResult && !nullValuesAsEmptyString ? singleValue : factory.Coalesce(singleValue, factory.Value(dataType, string.Empty));
								composer.SetResult(singleValue);
								return;
							}

							if (!composer.GetFilteredToNullValues(out ICollection<ISqlExpression>? values, out var error))
							{
								composer.SetError(error);
								return;
							}

							var items = !info.IsNullFiltered && nullValuesAsEmptyString
								? values.Select(i => factory.Coalesce(i, factory.Value(factory.GetDbDataType(i), ""))).ToArray()
								: values;

							ISqlExpression result;

							if (functionFactory != null)
							{
								result = functionFactory(factory, dataType, separator, items.ToArray());
							}
							else
							{
								result = factory.Function(dataType, "CONCAT_WS",
									parametersNullability : ParametersNullabilityType.SameAsFirstParameter,
									[separator, ..items]);
							}

							if (isNullableResult)
							{
								var condition = factory.SearchCondition();
								condition.AddRange(values.Select(v => factory.IsNull(v)));

								result = factory.Condition(condition, factory.Null(dataType), result);
							}

							composer.SetResult(result);
						});
				});
		}

		protected void ConfigureConcatWsEmulation(AggregateFunctionBuilder builder, bool nullValuesAsEmptyString, bool isNullResult, Func<ISqlExpressionFactory, DbDataType, ISqlExpression, ISqlExpression, ISqlExpression> substringFunc, bool withoutSeparator = false)
		{
			builder
				.ConfigurePlain(c =>
				{
					if (withoutSeparator)
						c.HasSequenceIndex(0);
					else
						c.HasSequenceIndex(1).TranslateArguments(0);

					c.AllowFilter()
						.AllowNotNullCheck(true)
						.OnBuildFunction(composer =>
						{
							var info = composer.BuildInfo;
							if (info.Values.Length == 0 || (!withoutSeparator && info.Argument(0) == null))
							{
								composer.SetResult(info.Factory.Value(info.Factory.GetDbDataType(typeof(string)), string.Empty));
								return;
							}

							var factory   = info.Factory;
							var dataType  = factory.GetDbDataType(info.Values[0]);
							var separator = withoutSeparator
								? factory.Value(dataType, string.Empty)
								: info.Argument(0)!;

							if (info.Values.Length == 1)
							{
								var singleValue = info.Values[0];
								singleValue = isNullResult ? singleValue : factory.Coalesce(singleValue, factory.Value(dataType, string.Empty));
								composer.SetResult(singleValue);
								return;
							}

							if (!composer.GetFilteredToNullValues(out var values, out var error))
							{
								composer.SetError(error);
								return;
							}

							if (withoutSeparator)
							{
								// No separator: just chain values directly with optional null-coalescing.
								var concatValues = values
									.Select(v => isNullResult ? v : factory.Coalesce(v, factory.Value(dataType, "")))
									.Aggregate((v1, v2) => factory.Concat(v1, v2));

								var result = isNullResult
									? concatValues
									: factory.Coalesce(concatValues, factory.Value(dataType, string.Empty));

								composer.SetResult(result);
							}
							else if (info.IsNullFiltered || isNullResult || !nullValuesAsEmptyString)
							{
								var concatValues = values
									.Select(v => factory.Coalesce(factory.Concat(separator, v), factory.Value(dataType, "")))
									.Aggregate((v1, v2) => factory.Concat(v1, v2));

								var substring = substringFunc(factory, dataType, separator, concatValues);

								var result = isNullResult ? substring : factory.Coalesce(substring, factory.Value(dataType, string.Empty));

								composer.SetResult(result);
							}
							else
							{
								var concatValues = values
									.Select(v => factory.Coalesce(v, factory.Value(dataType, "")))
									.Aggregate((v1, v2) => factory.Concat(v1, separator, v2));

								composer.SetResult(concatValues);
							}
						});
				});
		}

		protected virtual Expression? TranslateStringJoin(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, bool nullValuesAsEmptyString,
			bool                                                              isNullableResult,
			bool                                                              withoutSeparator)
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
