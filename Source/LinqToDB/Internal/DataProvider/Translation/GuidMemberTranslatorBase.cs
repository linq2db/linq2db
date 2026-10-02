using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Linq.Translation;

namespace LinqToDB.Internal.DataProvider.Translation
{
	[SuppressMessage("ReSharper", "ReturnValueOfPureMethodIsNotUsed")]
	public class GuidMemberTranslatorBase : MemberTranslatorBase
	{
		public GuidMemberTranslatorBase()
		{
			// Optional: the translation is a cast, NULL-strict on every provider, and the client rebuild guards on
			// the column, so a missed LeftJoin reads null either way (linq2db#5929).
			using (Registration.OptionalScope())
			{
				Registration.RegisterMethod(() => Guid.Empty.ToString(),          TranslateGuildToStringMethod);
				Registration.RegisterMethod(() => ((Guid?)Guid.Empty).ToString(), TranslateGuildToStringMethod);
			}
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
