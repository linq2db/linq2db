using System;
using System.Collections;
using System.Globalization;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;
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

			var windowFunctionsTranslator = CreateWindowFunctionsMemberTranslator();
			if (windowFunctionsTranslator != null)
				CombinedMemberTranslator.Add(windowFunctionsTranslator);
		}

		protected SqlPlaceholderExpression? TranslateNoRequiredObjectExpression(ITranslationContext translationContext, Expression? objExpression)
		{
			if (objExpression == null)
				return null;

			if (translationContext.CanBeEvaluatedOnClient(objExpression))
				return null;

			var obj = translationContext.Translate(objExpression);

			if (obj is not SqlPlaceholderExpression objPlaceholder)
				return null;

			return objPlaceholder;
		}

		protected virtual Expression? ConvertToString(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			if (translationFlags.HasFlag(TranslationFlags.Expression))
				return null;

			var obj = methodCall.Object!;

			var objPlaceholder = TranslateNoRequiredObjectExpression(translationContext, obj);

			if (objPlaceholder == null)
				return null;

			var fromType = translationContext.ExpressionFactory.GetDbDataType(objPlaceholder.Sql);

			// ToString called on custom type already mapped to text-based db type or string
			if (fromType.IsTextType())
			{
				return objPlaceholder.WithType(typeof(string));
			}

			DbDataType toType;

			if (translationContext.CurrentColumnDescriptor != null)
			{
				toType = translationContext.CurrentColumnDescriptor.GetDbDataType(true);
			}
			else
			{
				toType = translationContext.MappingSchema.GetDbDataType(typeof(string));
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
				if (methodCall.Arguments[0].Type != typeof(bool))
					return false;
				
				var argumentPlaceholder = TranslateNoRequiredObjectExpression(translationContext, methodCall.Arguments[1]);

				if (argumentPlaceholder == null)
					return false;

				var translatedSqlExpression = TranslateConvertToBoolean(translationContext, argumentPlaceholder.Sql, translationFlags);

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

			var argumentPlaceholder = TranslateNoRequiredObjectExpression(translationContext, methodCall.Arguments[0]);

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

			if (methodCall.Method.Name != nameof(Nullable<int>.GetValueOrDefault))
				return false;

			var argumentPlaceholder = TranslateNoRequiredObjectExpression(translationContext, methodCall.Object);

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
				defaultValueExpression = factory.Value(argumentType, translationContext.MappingSchema.GetDefaultValue(argumentType.SystemType.ToNullableUnderlying()));
			}

			var caseExpression = factory.Condition(factory.IsNull(sqlExpression, true), sqlExpression, defaultValueExpression);

			translated = translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, caseExpression, methodCall);

			return true;
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
