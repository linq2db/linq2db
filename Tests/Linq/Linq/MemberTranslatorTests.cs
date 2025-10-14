using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Linq.Translation;

using NUnit.Framework;

namespace Tests.Linq
{
	public class MemberTranslatorTests : TestBase
	{
		class RegExprMemberTranslator : MemberTranslatorBase
		{
			public RegExprMemberTranslator()
			{
				Registration.RegisterMethod(() => Regex.IsMatch("", ""), TranslateRegexMatch);
			}

			Expression? TranslateRegexMatch(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out var pattern))
				{
					return null;
				}

				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var expression))
				{
					return null;
				}

				var factory   = translationContext.ExpressionFactory;
				var sqlExpr   = factory.Binary(factory.GetDbDataType(typeof(bool)), expression, "~" , pattern);
				var predicate = factory.ExprPredicate(sqlExpr);
				var sc        = factory.SearchCondition();

				sc.Add(predicate);

				return translationContext.CreatePlaceholder(sc, methodCall);
			}
		}

		[Test]
		public void MemberTranslatorTest([IncludeDataSources(false, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context, o => o.UseMemberTranslator(new RegExprMemberTranslator()));

			AssertQuery(db.Person.Where(p => Regex.IsMatch(p.FirstName, "Jo.*")));
		}

	}
}
