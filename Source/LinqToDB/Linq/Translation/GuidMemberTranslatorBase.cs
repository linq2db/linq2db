using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.SqlQuery;

namespace LinqToDB.Linq.Translation
{
	[SuppressMessage("ReSharper", "ReturnValueOfPureMethodIsNotUsed")]
	public class GuidMemberTranslatorBase : MemberTranslatorBase
	{
		public GuidMemberTranslatorBase()
		{
			Registration.RegisterMethod(() => Guid.NewGuid().ToString(), TranslateGuildToStringMethod);
			Registration.RegisterMethod(() => ((Guid?)Guid.NewGuid()).ToString(), TranslateGuildToStringMethod);
		}

		Expression? TranslateGuildToStringMethod(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
		{
			var guidExpr = methodCall.Object;
			if (guidExpr == null || translationContext.CanBeEvaluatedOnClient(guidExpr))
				return null;

			if (!translationContext.TranslateToSqlExpression(guidExpr, out var sqlGuidExpr))
				return translationContext.CreateErrorExpression(guidExpr, type: methodCall.Type);

			var result = TranslateGuildToString(translationContext, methodCall, sqlGuidExpr, translationFlags);
			if (result == null)
				return null;

			return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, result, methodCall);
		}

		protected virtual ISqlExpression? TranslateGuildToString(ITranslationContext translationContext, MethodCallExpression methodCall, ISqlExpression guidExpr, TranslationFlags translationFlags)
		{
			return null;
		}
	}
}
