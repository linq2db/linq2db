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

				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[0], out var firstArg, out var firstArgError))
				{
					return firstArgError;
				}

				if (!translationContext.TranslateToSqlExpression(methodCall.Arguments[1], out var secondArg, out var secondArgError))
				{
					return secondArgError;
				}

				if (methodCall.Arguments is
				    [
					    _, _, ConstantExpression
					    {
						    Value: StringComparison
						    and (
						    StringComparison.CurrentCultureIgnoreCase
						    or StringComparison.InvariantCultureIgnoreCase
						    or StringComparison.OrdinalIgnoreCase)
					    }
				    ])
				{
					firstArg  = factory.ToLower(firstArg);
					secondArg = factory.ToLower(secondArg);
				}

				var firstArgJsonbToTextCast = factory.Cast(firstArg, new DbDataType(typeof(string), DataType.Text));
				var secondArgEscaped        = factory.Expression(new DbDataType(typeof(string)), "'%' || {0} || '%'", secondArg);

				var predicate       = factory.LikePredicate(firstArgJsonbToTextCast, false, secondArgEscaped);
				var searchCondition = factory.SearchCondition().Add(predicate);

				return translationContext.CreatePlaceholder(translationContext.CurrentSelectQuery, searchCondition, methodCall);
			}
		}

		[Table("TestClass5347")]
		class TestClass
		{
			[PrimaryKey]
			public int Id { get; set; }
			[Column(DataType = DataType.Text)]
			public string Value { get; set; } = null!;
		}

		[Test]
		public void TestContainsMemberTranslator([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context, o => o.UseMemberTranslator(new ContainsMemberTranslator()));

			using var table = db.CreateLocalTable<TestClass>(new TestClass[]
			{
				new TestClass { Id = 1, Value = "test" },
				new TestClass { Id = 2, Value = "EXAMPLE" },
				new TestClass { Id = 3, Value = "sample" }
			});

			var query = table.Where(t => t.Value.Contains("amp", StringComparison.OrdinalIgnoreCase));

			// simple test that translator is used and produces expected SQL
			query.ToSqlQuery().Sql.ShouldContain("||");
		}
	}
}
