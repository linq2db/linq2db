using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Expressions
{
	using LinqToDB.Extensions;

	public static class Extensions
	{
		#region GetDebugView

		private static Func<Expression,string> _getDebugView;

		/// <summary>
		/// Gets the DebugView internal property value of provided expression.
		/// </summary>
		/// <param name="expression">Expression to get DebugView.</param>
		/// <returns>DebugView value.</returns>
		public static string GetDebugView(this Expression expression)
		{
			if (_getDebugView == null)
			{
				var p = Expression.Parameter(typeof(Expression));

				try
				{
					var l = Expression.Lambda<Func<Expression,string>>(
						Expression.PropertyOrField(p, "DebugView"),
						p);

					_getDebugView = l.Compile();
				}
				catch (ArgumentException)
				{
					var l = Expression.Lambda<Func<Expression,string>>(
						Expression.Call(p, MemberHelper.MethodOf<Expression>(e => e.ToString())),
						p);

					_getDebugView = l.Compile();
				}
			}

			return _getDebugView(expression);
		}

		#endregion

		#region GetCount

		/// <summary>
		/// Returns the total number of expression items which are matching the given.
		/// <paramref name="func"/>.
		/// </summary>
		/// <param name="expr">Expression-Tree which gets counted.</param>
		/// <param name="func">Predicate which is used to test if the given expression should be counted.</param>
		public static int GetCount(this Expression expr, Func<Expression,bool> func)
		{
			var n = 0;

			expr.Visit(e =>
			{
				if (func(e))
					n++;
			});

			return n;
		}

		#endregion

		#region Visit

		/// <summary>
		/// Calls the given <paramref name="func"/> for each child node of the <paramref name="expr"/>.
		/// </summary>
		public static void Visit(this Expression expr, Action<Expression> func)
		{
			new LinqExpressionVisitor().Visit(expr, func);
		}

		/// <summary>
		/// Calls the given <paramref name="func"/> for each node of the <paramref name="expr"/>.
		/// If the <paramref name="func"/> returns false, no childs of the tested expression will be enumerated.
		/// </summary>
		public static void Visit(this Expression expr, Func<Expression,bool> func)
		{
			new LinqExpressionVisitor().Visit(expr, func);
		}

		#endregion

		#region Find

		/// <summary>
		/// Enumerates the expression tree and returns the <paramref name="exprToFind"/> if it's
		/// contained within the <paramref name="expr"/>.
		/// </summary>
		public static Expression Find(this Expression expr, Expression exprToFind)
		{
			return new LinqExpressionVisitor().Find(expr, e => e == exprToFind);
		}

		/// <summary>
		/// Enumerates the given <paramref name="expr"/> and returns the first sub-expression
		/// which matches the given <paramref name="func"/>. If no expression was found, null is returned.
		/// </summary>
		public static Expression Find(this Expression expr, Func<Expression,bool> func)
		{
			return new LinqExpressionVisitor().Find(expr, func);
		}

		#endregion

		#region Transform

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces the first parameter of that
		/// lambda expression with the <paramref name="exprToReplaceParameter"/> expression.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter)
		{
			return new LinqExpressionVisitor().Transform(lambda.Body, e => e == lambda.Parameters[0] ? exprToReplaceParameter : e);
		}

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces the first two parameters of
		/// that lambda expression with the given replace expressions.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter1, Expression exprToReplaceParameter2)
		{
			return new LinqExpressionVisitor().Transform(lambda.Body, e =>
				e == lambda.Parameters[0] ? exprToReplaceParameter1 :
				e == lambda.Parameters[1] ? exprToReplaceParameter2 : e);
		}

		/// <summary>
		/// Enumerates the expression tree of <paramref name="expr"/> and might
		/// replace expression with the returned value of the given <paramref name="func"/>.
		/// </summary>
		/// <returns>The modified expression.</returns>
		public static Expression Transform(this Expression expr, [InstantHandle] Func<Expression,Expression> func)
		{
			return new LinqExpressionVisitor().Transform(expr, func);
		}

		public static Expression Transform(this Expression expr, Func<Expression,TransformInfo> func)
		{
			return new LinqExpressionVisitor().Transform(expr, func);
		}

		#endregion
	}
}
