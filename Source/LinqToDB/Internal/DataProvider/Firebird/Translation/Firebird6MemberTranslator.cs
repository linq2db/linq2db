using System;
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

		protected override IMemberTranslator? CreateWindowFunctionsMemberTranslator()
		{
			return new Firebird6WindowFunctionsMemberTranslator();
		}

		// Firebird 6 adds a version argument to GEN_UUID (default 4); GEN_UUID(7) produces a
		// time-ordered RFC 9562 UUIDv7 server-side.
		protected override ISqlExpression? TranslateNewGuid7Method(ITranslationContext translationContext, TranslationFlags translationFlags)
		{
			var factory = translationContext.ExpressionFactory;
			return factory.NonPureFunction(factory.GetDbDataType(typeof(Guid)), "Gen_Uuid", factory.Value(factory.GetDbDataType(typeof(int)), 7));
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

		// Firebird 6 adds: the GROUPS frame unit and frame EXCLUDE clause (frames themselves date to FB 4),
		// and the SQL-standard ordered-set aggregates PERCENTILE_CONT/PERCENTILE_DISC (WITHIN GROUP form only —
		// Firebird has no windowed OVER form, so IsOrderedSetWindowedSupported stays off).
		protected class Firebird6WindowFunctionsMemberTranslator : Firebird5WindowFunctionsMemberTranslator
		{
			protected override bool IsFrameGroupsSupported    => true;
			protected override bool IsFrameExclusionSupported => true;
			protected override bool IsPercentileContSupported => true;
			protected override bool IsPercentileDiscSupported => true;
		}
	}
}
