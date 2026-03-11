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

			// Add Convert translator at the end to avoid conflicts with other translators
			CombinedMemberTranslator.Add(CreateConvertMemberTranslator());

			var windowFunctionsTranslator = CreateWindowFunctionsMemberTranslator();
			if (windowFunctionsTranslator != null)
				CombinedMemberTranslator.Add(windowFunctionsTranslator);
		}

		protected bool ProcessGetValueOrDefault(ITranslationContext translationContext, MethodCallExpression methodCall, out Expression? translated)
		{
			translated = null;

			var nullableType = methodCall.Method.DeclaringType;
			if (nullableType == null || !typeof(Nullable<>).IsSameOrParentOf(nullableType))
				return false;

			if (!string.Equals(methodCall.Method.Name, nameof(Nullable<>.GetValueOrDefault), StringComparison.Ordinal))
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

		public virtual Expression? TranslateMethodCall(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			Expression? translated;

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
