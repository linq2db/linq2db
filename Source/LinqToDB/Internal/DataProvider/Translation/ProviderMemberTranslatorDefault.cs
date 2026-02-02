using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	public abstract class ProviderMemberTranslatorDefault : MemberTranslatorBase
	{
		protected virtual IMemberTranslator CreateSqlTypesTranslator()
		{
			return new SqlTypesTranslationDefault();
		}

		protected abstract IMemberTranslator CreateDateMemberTranslator();

		protected virtual IMemberTranslator CreateMathMemberTranslator()
		{
			return new MathMemberTranslatorBase();
		}

		protected virtual IMemberTranslator CreateStringMemberTranslator()
		{
			return new StringMemberTranslatorBase();
		}

		protected virtual IMemberTranslator CreateGuidMemberTranslator()
		{
			return new GuidMemberTranslatorBase();
		}

		protected virtual IMemberTranslator CreateSqlFunctionsMemberTranslator()
		{
			return new SqlFunctionsMemberTranslatorBase();
		}

		protected virtual IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new WindowFunctionsMemberTranslator();
		}

		protected virtual IMemberTranslator CreateAggregateFunctionsMemberTranslator()
		{
			return new AggregateFunctionsMemberTranslatorBase();
		}

		protected virtual IMemberTranslator CreateConvertMemberTranslator()
		{
			return new ConvertMemberTranslatorDefault();
		}

		protected ProviderMemberTranslatorDefault()
		{
			InitDefaultTranslators();

			Registration.RegisterMethod(() => Sql.NewGuid(),  TranslateNewGuidMethod);
			Registration.RegisterMethod(() => Guid.NewGuid(), TranslateNewGuidMethod);
		}

		Expression? TranslateNewGuidMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var translated = TranslateNewGuidMethod(translationContext, translationFlags);
			if (translated == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translated, methodCall);
		}

		void InitDefaultTranslators()
		{
			CombinedMemberTranslator.Add(CreateSqlTypesTranslator());
			CombinedMemberTranslator.Add(CreateDateMemberTranslator());
			CombinedMemberTranslator.Add(CreateMathMemberTranslator());
			CombinedMemberTranslator.Add(CreateStringMemberTranslator());
			CombinedMemberTranslator.Add(CreateGuidMemberTranslator());
			CombinedMemberTranslator.Add(CreateSqlFunctionsMemberTranslator());
			CombinedMemberTranslator.Add(CreateAggregateFunctionsMemberTranslator());
			CombinedMemberTranslator.Add(CreateConvertMemberTranslator());

			var windowFunctionsTranslator = CreateWindowFunctionsMemberTranslator();
			if (windowFunctionsTranslator != null)
				CombinedMemberTranslator.Add(windowFunctionsTranslator);
		}

		protected virtual Expression? ConvertToString(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (translationFlags.HasFlag(TranslationFlags.Expression))
				return null;

			var obj = methodCall.Object!;

			var objPlaceholder = translationContext.TranslateNoRequiredObjectExpression(obj);

			if (objPlaceholder == null)
				return null;

			var fromType = translationContext.ExpressionFactory.GetDbDataType(objPlaceholder.Sql);
			DbDataType toType;

			if (translationContext.CurrentColumnDescriptor != null)
				toType = translationContext.CurrentColumnDescriptor.GetDbDataType(true);
			else
				toType = translationContext.MappingSchema.GetDbDataType(typeof(string));

			// ToString called on custom type already mapped to text-based db type or string
			if (fromType.IsTextType())
			{
				if (fromType.SystemType.IsEnum)
				{
					var enumValues = translationContext.MappingSchema.GetMapValues(fromType.SystemType)!;

					List<SqlCaseExpression.CaseItem>? cases = null;

					foreach (var field in enumValues)
					{
						if (field.MapValues.Length > 0)
						{
							var cond = field.MapValues.Length == 1
								? translationContext.ExpressionFactory.Equal(
									objPlaceholder.Sql,
									translationContext.ExpressionFactory.Value(fromType, field.MapValues[0].Value))
								: translationContext.ExpressionFactory
									.SearchCondition(isOr: true)
									.AddRange(
									field.MapValues.Select(
										v => translationContext.ExpressionFactory.Equal(
											objPlaceholder.Sql,
											translationContext.ExpressionFactory.Value(fromType, v.Value))));

							(cases ??= []).Add(
								new SqlCaseExpression.CaseItem(
									cond,
									translationContext.ExpressionFactory.Value(toType, FormattableString.Invariant($"{field.OrigValue}"))));
						}
					}

					var defaultSql = objPlaceholder.Sql;

					var expr = cases == null ? defaultSql : new SqlCaseExpression(toType, cases, defaultSql);

					return translationContext.CreatePlaceholder(
						translationContext.CurrentSelectQuery,
						expr,
						methodCall);
				}

				return objPlaceholder.WithType(typeof(string));
			}

			return translationContext.CreatePlaceholder(
				translationContext.CurrentSelectQuery,
				translationContext.ExpressionFactory.Cast(objPlaceholder.Sql, toType),
				methodCall);
		}

		protected bool ProcessToString(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, out Expression? translated)
		{
			translated = null;

			if (methodCall.Object != null && methodCall.Method.Name == nameof(ToString))
			{
				var parameters = methodCall.Method.GetParameters();
				if (parameters.Length > 1)
					return true;

				if (parameters.Length == 1)
				{
					if (parameters[0].ParameterType != typeof(IFormatProvider))
						return true;

					var cultureExpression = methodCall.Arguments[0];

					if (!translationContext.CanBeEvaluated(cultureExpression))
						return true;

					var culture = translationContext.Evaluate(cultureExpression);
					if (culture is not IFormatProvider formatProvider)
						return true;

					if (formatProvider != CultureInfo.InvariantCulture)
						return true;
				}

				if (translationFlags.HasFlag(TranslationFlags.Expression) && translationContext.CanBeEvaluatedOnClient(methodCall.Object))
					return true;

				translated = ConvertToString(translationContext, methodCall, translationFlags);

				if (translated == null)
					return false;
				
				return true;
			}

			return false;
		}

		protected bool ProcessSqlConvert(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, out Expression? translated)
		{
			translated = null;

			if (methodCall.Method.DeclaringType != typeof(Sql))
				return false;

			if (methodCall.Method.Name != nameof(Sql.Convert))
				return false;

			if (methodCall.Arguments.Count == 1)
				//TODO: Implement conversion
				return true;

			if (methodCall.Arguments.Count == 2)
			{
				if (!translationContext.TranslateExpression(methodCall.Arguments[1], out var argument, out _))
				{
					return false;
				}

				ISqlExpression? translatedSqlExpression;

				if (!translationContext.TranslateExpression(methodCall.Arguments[0], out var typeExpression, out _))
				{
					return false;
				}

				translatedSqlExpression = TranslateConvert(translationContext, typeExpression, argument, translationFlags);

				if (translatedSqlExpression == null)
					return false;

				translated = translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translatedSqlExpression, methodCall);
				return true;
			}

			return false;
		}

		protected bool ProcessConvertToBoolean(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, out Expression? translated)
		{
			translated = null;

			if (methodCall.Method.DeclaringType != typeof(Convert))
				return false;

			if (methodCall.Method.Name != nameof(Convert.ToBoolean))
				return false;

			if (methodCall.Arguments.Count != 1)
				return false;

			var argumentPlaceholder = translationContext.TranslateNoRequiredObjectExpression(methodCall.Arguments[0]);

			if (argumentPlaceholder == null)
				return true;

			var translatedSqlExpression = TranslateConvertToBoolean(translationContext, argumentPlaceholder.Sql, translationFlags);

			if (translatedSqlExpression == null)
				return true;

			translated = translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, translatedSqlExpression, methodCall);

			return true;
		}

		protected bool ProcessGetValueOrDefault(ITranslationContext translationContext, MethodCallExpression methodCall, out Expression? translated)
		{
			translated = null;

			var nullableType = methodCall.Method.DeclaringType;
			if (nullableType == null || !typeof(Nullable<>).IsSameOrParentOf(nullableType))
				return false;

			if (methodCall.Method.Name != nameof(Nullable<>.GetValueOrDefault))
				return false;

			var argumentPlaceholder = translationContext.TranslateNoRequiredObjectExpression(methodCall.Object);

			if (argumentPlaceholder == null)
				return true;

			var factory = translationContext.ExpressionFactory;

			var sqlExpression = argumentPlaceholder.Sql;
			var argumentType  = factory.GetDbDataType(sqlExpression);

			ISqlExpression? defaultValueExpression;

			if (methodCall.Arguments.Count == 1)
			{
				var defaulTranslation = translationContext.Translate(methodCall.Arguments[0]);
				if (defaulTranslation is not SqlPlaceholderExpression defaultValuePlaceholder)
					return true;

				defaultValueExpression = defaultValuePlaceholder.Sql;
			}
			else
			{
				defaultValueExpression = factory.Value(argumentType, translationContext.MappingSchema.GetDefaultValue(argumentType.SystemType.UnwrapNullableType()));
			}

			var caseExpression = factory.Condition(factory.IsNull(sqlExpression, true), sqlExpression, defaultValueExpression);

			translated = translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, caseExpression, methodCall);

			return true;
		}

		protected virtual ISqlExpression? TranslateConvert(ITranslationContext translationContext, ISqlExpression typeExpression, ISqlExpression sqlExpression, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;

			if (typeExpression.SystemType == typeof(bool))
			{
				return TranslateConvertToBoolean(translationContext, sqlExpression, translationFlags);
			}

			var toDataType = QueryHelper.GetDbDataType(typeExpression, translationContext.MappingSchema);
			return factory.Cast(sqlExpression, toDataType);
		}

		protected virtual ISqlExpression? TranslateConvertToBoolean(ITranslationContext translationContext, ISqlExpression sqlExpression, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;

			var sc = factory.SearchCondition();
			var predicate = factory.Equal(
					sqlExpression,
					factory.Value(0),
					translationContext.DataOptions.LinqOptions.CompareNulls == CompareNulls.LikeClr ? true : null)
				.MakeNot();

			sc.Add(predicate);

			return sc;
		}

		public virtual Expression? TranslateMethodCall(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			Expression? translated;

			if (ProcessToString(translationContext, methodCall, translationFlags, out translated))
				return translated;

			if (ProcessSqlConvert(translationContext, methodCall, translationFlags, out translated))
				return translated;

			if (ProcessConvertToBoolean(translationContext, methodCall, translationFlags, out translated))
				return translated;

			if (ProcessGetValueOrDefault(translationContext, methodCall, out translated))
				return translated;

			return null;
		}

		protected virtual Expression? TranslateMemberExpression(ITranslationContext translationContext, MemberExpression memberExpression, TranslationFlags translationFlags)
		{
			return null;
		}

		protected override Expression? TranslateOverrideHandler(ITranslationContext translationContext, Expression memberExpression, TranslationFlags translationFlags)
		{
			if (memberExpression is MethodCallExpression methodCallExpression)
			{
				var translated = TranslateMethodCall(translationContext, methodCallExpression, translationFlags);
				if (translated != null)
					return translated;

			}
			else if (memberExpression is MemberExpression member)
			{
				var translated = TranslateMemberExpression(translationContext, member, translationFlags);
				if (translated != null)
					return translated;
			}

			return base.TranslateOverrideHandler(translationContext, memberExpression, translationFlags);
		}

		#region Methods to override

		protected virtual ISqlExpression? TranslateNewGuidMethod(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			return null;
		}
		
		#endregion
	}
}
