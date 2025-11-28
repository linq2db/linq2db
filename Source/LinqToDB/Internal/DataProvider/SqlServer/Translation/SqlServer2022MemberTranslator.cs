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
	}
}
