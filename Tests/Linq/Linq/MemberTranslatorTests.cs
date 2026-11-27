using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

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

		public class JsonbContainsMemberTranslator : MemberTranslatorBase
		{
			public JsonbContainsMemberTranslator()
			{
				Registration.RegisterMethod(() => "".Contains(""),                           TranslateContains);
				Registration.RegisterMethod(() => "".Contains("", StringComparison.Ordinal), TranslateContains);
			}

			private static Expression? TranslateContains(ITranslationContext translationContext, MethodCallExpression methodCall, TranslationFlags translationFlags)
			{
				var factory = translationContext.ExpressionFactory;

				// For the instance string.Contains the haystack is the receiver (methodCall.Object) and
				// the needle is Arguments[0]; Arguments[1] (when present) is the StringComparison.
				if (methodCall.Object == null)
					return null;

				if (!translationContext.TranslateToSqlExpression(methodCall.Object, out var haystack, out var haystackError))
				{
					return haystackError;
				}

				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var needle, out var needleError))
				{
					return needleError;
				}

				// Cast jsonb -> text first, so the optional LOWER and the LIKE operate on text
				// (LOWER(jsonb) doesn't exist on PostgreSQL).
				var haystackText = factory.Cast(haystack, new DbDataType(typeof(string), DataType.Text));

				if (methodCall.Arguments is
				    [
					    _, ConstantExpression
					    {
						    Value: StringComparison
						    and (
						    StringComparison.CurrentCultureIgnoreCase
						    or StringComparison.InvariantCultureIgnoreCase
						    or StringComparison.OrdinalIgnoreCase)
					    }
				    ])
				{
					haystackText = factory.ToLower(haystackText);
					needle       = factory.ToLower(needle);
				}

				var needleEscaped   = factory.Expression(new DbDataType(typeof(string)), "'%' || {0} || '%'", needle);
				var predicate       = factory.LikePredicate(haystackText, false, needleEscaped);
				var searchCondition = factory.SearchCondition().Add(predicate);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, searchCondition, methodCall);
			}
		}

		[Table("JsonbContainsEntity")]
		class JsonbContainsEntity
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column(DataType = DataType.BinaryJson)]
			public string Value { get; set; } = null!;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5347")]
		// PostgreSQL 9.4+ (jsonb)
		public void CustomContainsTranslatorOnJsonbColumn([IncludeDataSources(TestProvName.AllPostgreSQL95Plus)] string context)
		{
			using var db = GetDataContext(context, o => o.UseMemberTranslator(new JsonbContainsMemberTranslator()));

			using var table = db.CreateLocalTable<JsonbContainsEntity>(new JsonbContainsEntity[]
			{
				new JsonbContainsEntity { Id = 1, Value = "\"test\""    },
				new JsonbContainsEntity { Id = 2, Value = "\"EXAMPLE\"" },
				new JsonbContainsEntity { Id = 3, Value = "\"sample\""  }
			});

			var query = table.Where(t => t.Value.Contains("amp", StringComparison.OrdinalIgnoreCase));

			// The custom translator must win over the built-in predicate path (#5347): the built-in
			// LIKE on the jsonb column fails with like_escape(jsonb, ...). Case-insensitive "amp"
			// matches rows 2 (EXAMPLE) and 3 (sample).
			var ids = query.OrderBy(t => t.Id).Select(t => t.Id).ToArray();

			ids.ShouldBe(new[] { 2, 3 });
		}
	}
}
