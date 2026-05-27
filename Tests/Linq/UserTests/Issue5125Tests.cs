using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5125Tests : TestBase
	{
		// Repro of issue #5125 (https://github.com/linq2db/linq2db/issues/5125).
		// The original repro (non-nullable string + [AllowNull] + [ExpressionMethod] using
		// `string.IsNullOrEmpty` + interpolated string, projected via `new T { ... }` after
		// OrderBy on a calculated expression) emitted an invalid SQL where Length(...) inside
		// the subquery referenced the outer alias `x_1` instead of `x`. That was fixed by #5126.
		//
		// The v6.2.0+ follow-up regression reported in the same issue is reproduced here: when
		// the calling code installs an IExpressionPreprocessor that wraps OrderBy lambdas in
		// Sql.Expr<T>($"{0} NULLS FIRST"), the optimizer extracts the wrapped expression as a
		// subquery column but keeps the NULLS FIRST directive inside the column expression,
		// producing PostgreSQL like:
		//     CASE WHEN ... THEN ... ELSE ... END NULLS FIRST as c1
		// which fails with `42601: syntax error at or near "NULLS"`. The NULLS FIRST directive
		// must stay in the outer ORDER BY clause, not inside the subquery column.
		[Table("InterpolatedTest5125", IsColumnAttributeRequired = false)]
		public class InterpolatedTest5125
		{
			public int     Id     { get; set; }
			public string? StrVal { get; set; }
			public int     IntVal { get; set; }

			[AllowNull]
			[ExpressionMethod(nameof(Expr1Impl))]
			public string Expr1 { get; set; } = null!;

			private static Expression<Func<InterpolatedTest5125, IDataContext, string>> Expr1Impl() =>
				(e, ctx) => !string.IsNullOrEmpty(e.StrVal) ? e.StrVal! : e.IntVal.ToString();

			[AllowNull]
			[ExpressionMethod(nameof(Expr2Impl))]
			public string Expr2 { get; set; } = null!;

			private static Expression<Func<InterpolatedTest5125, IDataContext, string>> Expr2Impl() =>
				(e, ctx) => $"{(!string.IsNullOrEmpty(e.StrVal) ? e.StrVal : e.IntVal.ToString())}";
		}

		sealed class OrderByNullsFirstVisitor : ExpressionVisitor
		{
			static readonly MethodInfo SqlExprMethod =
				typeof(Sql).GetMethods(BindingFlags.Public | BindingFlags.Static)
					.First(m => m.Name == nameof(Sql.Expr) && m.IsGenericMethodDefinition
						&& m.GetParameters().Length == 1
						&& m.GetParameters()[0].ParameterType == typeof(FormattableString));

			static readonly MethodInfo CreateFormattable =
				typeof(FormattableStringFactory).GetMethod(
					nameof(FormattableStringFactory.Create),
					new[] { typeof(string), typeof(object[]) })!;

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (node.Method.DeclaringType == typeof(Queryable)
					&& node.Method.Name == nameof(Queryable.OrderBy))
				{
					var lambda = (LambdaExpression)((UnaryExpression)node.Arguments[1]).Operand;
					var body   = lambda.Body;

					var sqlExprCall = Expression.Call(
						SqlExprMethod.MakeGenericMethod(body.Type),
						Expression.Call(
							CreateFormattable,
							Expression.Constant("{0} NULLS FIRST"),
							Expression.NewArrayInit(typeof(object), Expression.Convert(body, typeof(object)))
						)
					);

					var newLambda = Expression.Lambda(sqlExprCall, lambda.Parameters);
					return Expression.Call(node.Method, Visit(node.Arguments[0]), newLambda);
				}

				return base.VisitMethodCall(node);
			}
		}

		sealed class InterpolatedDataConnection : DataConnection, IExpressionPreprocessor
		{
			public InterpolatedDataConnection(string configurationString) : base(configurationString)
			{
			}

			public Expression ProcessExpression(Expression expression)
				=> new OrderByNullsFirstVisitor().Visit(expression);
		}

		[Test]
		public void TestIssue5125_NullsFirst([IncludeDataSources(false, TestProvName.AllPostgreSQL)] string context)
		{
			var data = new[]
			{
				new InterpolatedTest5125 { Id = 1, StrVal = null,     IntVal = 11 },
				new InterpolatedTest5125 { Id = 2, StrVal = "",       IntVal = 12 },
				new InterpolatedTest5125 { Id = 3, StrVal = "Value3", IntVal = 13 },
			};

			using var db = new InterpolatedDataConnection(context);
			using var tb = db.CreateLocalTable(data);

			var query = tb
				.OrderBy(x => x.Expr1)
				.Select(x => new InterpolatedTest5125
				{
					Id    = x.Id,
					Expr1 = x.Expr1,
					Expr2 = x.Expr2
				});

			var list = query.ToList();

			Assert.That(list, Has.Count.EqualTo(3));
		}
	}
}
