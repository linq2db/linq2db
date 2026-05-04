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

		static Expression UnwrapEnumBoxing(Expression expression)
		{
			// Enum.HasFlag is declared on System.Enum, so call sites carry a Convert(x, typeof(Enum))
			// wrapper around an enum-typed receiver/argument. The SQL translator can't lower that
			// abstract-type boxing for parameter/closure-captured locals, so strip it.
			while (expression is UnaryExpression { NodeType: ExpressionType.Convert } unary
				&& unary.Type == typeof(Enum)
				&& unary.Operand.Type.IsEnum)
			{
				expression = unary.Operand;
			}

			return expression;
		}

		protected bool ProcessHasFlag(ITranslationContext translationContext, MethodCallExpression methodCall, out Expression? translated)
		{
			translated = null;

			if (methodCall.Method.DeclaringType != typeof(Enum) ||
				!string.Equals(methodCall.Method.Name, nameof(Enum.HasFlag), StringComparison.Ordinal) ||
				methodCall.Arguments.Count != 1 ||
				methodCall.Object == null)
			{
				return false;
			}

			var valueExpr = UnwrapEnumBoxing(methodCall.Object);
			var flagExpr  = UnwrapEnumBoxing(methodCall.Arguments[0]);

			// Bitwise-AND translation is only valid when the enum is stored as an integer.
			// Enums mapped to non-integer types (e.g. [MapValue("foo")] → NVarChar) must fall through.
			var enumType = valueExpr.Type.UnwrapNullableType();
			if (!enumType.IsEnum)
				return false;

			var underlyingDataType = translationContext.MappingSchema.GetUnderlyingDataType(enumType, out _);
			if (!underlyingDataType.Type.SystemType.IsIntegerType)
				return false;

			if (!translationContext.TranslateToSqlExpression(valueExpr, out var valueSql))
			{
				translated = translationContext.CreateErrorExpression(valueExpr, type: methodCall.Type);
				return true;
			}

			if (!translationContext.TranslateToSqlExpression(flagExpr, out var flagSql))
			{
				translated = translationContext.CreateErrorExpression(flagExpr, type: methodCall.Type);
				return true;
			}

			var factory   = translationContext.ExpressionFactory;
			var dbType    = factory.GetDbDataType(valueSql);
			var andExpr   = factory.Binary(dbType, valueSql, "&", flagSql);
			var equalPred = factory.Equal(andExpr, flagSql);

			var sc = factory.SearchCondition();
			sc.Add(equalPred);

			translated = translationContext.CreatePlaceholder(sc, methodCall);
			return true;
		}

		public virtual Expression? TranslateMethodCall(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			Expression? translated;

			if (ProcessGetValueOrDefault(translationContext, methodCall, out translated))
				return translated;

			if (ProcessHasFlag(translationContext, methodCall, out translated))
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
