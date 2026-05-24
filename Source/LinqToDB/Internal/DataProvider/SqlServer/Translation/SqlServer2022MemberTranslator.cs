using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.SqlServer.Translation
{
	public class SqlServer2022MemberTranslator : SqlServer2017MemberTranslator
	{
		protected override IMemberTranslator CreateMathMemberTranslator()
		{
			return new SqlServer2022MathMemberTranslator();
		}

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new SqlServer2022StringMemberTranslator();
		}

		protected class SqlServer2022MathMemberTranslator : SqlServerMathMemberTranslator
		{
			protected override ISqlExpression? TranslateMaxMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var dbType = factory.GetDbDataType(xValue);

				return factory.Function(dbType, "GREATEST", ParametersNullabilityType.IfAllParametersNullable, xValue, yValue);
			}

			protected override ISqlExpression? TranslateMinMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var dbType = factory.GetDbDataType(xValue);

				return factory.Function(dbType, "LEAST", ParametersNullabilityType.IfAllParametersNullable, xValue, yValue);
			}
		}

		protected class SqlServer2022StringMemberTranslator : SqlServer2017StringMemberTranslator
		{
			public override ISqlExpression? TranslateTrimStart(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				if (trimChars == null)
					return base.TranslateTrimStart(translationContext, methodCall, translationFlags, value, trimChars);

				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				return factory.Function(valueType, "LTRIM", value, trimChars);
			}

			public override ISqlExpression? TranslateTrimEnd(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags, ISqlExpression value, ISqlExpression? trimChars)
			{
				if (trimChars == null)
					return base.TranslateTrimEnd(translationContext, methodCall, translationFlags, value, trimChars);

				var factory   = translationContext.ExpressionFactory;
				var valueType = factory.GetDbDataType(value);

				return factory.Function(valueType, "RTRIM", value, trimChars);
			}
		}
	}
}
