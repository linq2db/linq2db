using System.Linq.Expressions;

namespace LinqToDB.DataProvider.SqlServer.Translation
{
	using Linq.Translation;
	using SqlQuery;

	public class SqlServer2022MemberTranslator : SqlServer2012MemberTranslator
	{
		protected class SqlServer2022MathMemberTranslator : SqlServerMathMemberTranslator
		{
			protected override ISqlExpression? TranslateMaxMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var dbType = factory.GetDbDataType(xValue);

				return factory.Function(dbType, "GREATEST", xValue, yValue);
			}

			protected override ISqlExpression? TranslateMinMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;

				var dbType = factory.GetDbDataType(xValue);

				return factory.Function(dbType, "LEAST", xValue, yValue);
			}
		}

		protected override IMemberTranslator CreateMathMemberTranslator()
		{
			return new SqlServer2022MathMemberTranslator();
		}
	}
}
