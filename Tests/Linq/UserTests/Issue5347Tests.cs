using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.DataProvider.Translation;
using LinqToDB.Linq.Translation;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5347Tests : TestBase
	{
		public class ContainsMemberTranslator : MemberTranslatorBase
		{
			public ContainsMemberTranslator()
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

		[Table("TestClass5347")]
		class TestClass
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column(DataType = DataType.BinaryJson)]
			public string Value { get; set; } = null!;
		}

		[Test]
		public void TestContainsMemberTranslator([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context, o => o.UseMemberTranslator(new ContainsMemberTranslator()));

			using var table = db.CreateLocalTable<TestClass>(new TestClass[]
			{
				new TestClass { Id = 1, Value = "\"test\""    },
				new TestClass { Id = 2, Value = "\"EXAMPLE\"" },
				new TestClass { Id = 3, Value = "\"sample\""  }
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
