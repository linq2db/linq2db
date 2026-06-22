using System.Linq.Expressions;

using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Firebird.Translation
{
	public class Firebird6MemberTranslator : Firebird5MemberTranslator
	{
		protected override IMemberTranslator CreateMathMemberTranslator()
		{
			return new Firebird6MathMemberTranslator();
		}

		protected override IMemberTranslator CreateStringMemberTranslator()
		{
			return new Firebird6StringMemberTranslator();
		}

		protected class Firebird6MathMemberTranslator : MathMemberTranslatorBase
		{
			// Firebird 6 adds the SQL-standard GREATEST/LEAST functions (aliases of MAXVALUE/MINVALUE);
			// emit them instead of the CASE WHEN fallback used by earlier versions.
			protected override ISqlExpression? TranslateMaxMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.Function(factory.GetDbDataType(xValue), "GREATEST", xValue, yValue);
			}

			protected override ISqlExpression? TranslateMinMethod(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression xValue, ISqlExpression yValue)
			{
				var factory = translationContext.ExpressionFactory;
				return factory.Function(factory.GetDbDataType(xValue), "LEAST", xValue, yValue);
			}
		}

		// Firebird 6: LIST is an alias for the SQL-standard LISTAGG, which supports ordered aggregation
		// via WITHIN GROUP (ORDER BY ...). Enable it so ordered string aggregation translates server-side.
		protected class Firebird6StringMemberTranslator : Firebird5StringMemberTranslator
		{
			protected override bool IsWithinGroupSupported => true;
		}
	}
}
