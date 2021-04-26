using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB.Expressions
{
	using LinqToDB.Common;
	using LinqToDB.Extensions;
	using LinqToDB.Mapping;
	using LinqToDB.Reflection;
	using System.Diagnostics.CodeAnalysis;
	using System.Reflection;

	public static class Extensions
	{
		#region GetDebugView

		private static Func<Expression,string>? _getDebugView;

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
						ExpressionHelper.PropertyOrField(p, "DebugView"),
						p);

					_getDebugView = l.CompileExpression();
				}
				catch (ArgumentException)
				{
					var l = Expression.Lambda<Func<Expression,string>>(
						Expression.Call(p, MemberHelper.MethodOf<Expression>(e => e.ToString())),
						p);

					_getDebugView = l.CompileExpression();
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
		/// <param name="context">Expression-Tree visitor context.</param>
		/// <param name="func">Predicate which is used to test if the given expression should be counted.</param>
		public static int GetCount<TContext>(this Expression expr, TContext context, Func<TContext, Expression, bool> func)
		{
			var ctx = new CountContext<TContext>(context, func);

			expr.Visit(ctx, static (context, e) =>
			{
				if (context.Func(context.Context, e))
					context.Count++;
			});

			return ctx.Count;
		}

		private class CountContext<TContext>
		{
			public CountContext(TContext context, Func<TContext, Expression, bool> func)
			{
				Context = context;
				Func    = func;
			}

			public readonly TContext                         Context;
			public          int                              Count;
			public readonly Func<TContext, Expression, bool> Func;
		}

		#endregion

		#region Visit
		/// <summary>
		/// Calls the given <paramref name="func"/> for each child node of the <paramref name="expr"/>.
		/// </summary>
		public static void Visit<TContext>(this Expression expr, TContext context, Action<TContext, Expression> func)
		{
			if (expr == null)
				return;

			new VisitActionVisitor<TContext>(context, func).Visit(expr);
		}

		/// <summary>
		/// Calls the given <paramref name="func"/> for each child node of the <paramref name="expr"/>.
		/// </summary>
		public static void Visit(this Expression expr, Action<object?, Expression> func)
		{
			if (expr == null)
				return;

			new VisitActionVisitor<object?>(null, func).Visit(expr);
		}

		/// <summary>
		/// Calls the given <paramref name="func"/> for each node of the <paramref name="expr"/>.
		/// If the <paramref name="func"/> returns false, no childs of the tested expression will be enumerated.
		/// </summary>
		public static void Visit<TContext>(this Expression expr, TContext context, Func<TContext, Expression, bool> func)
		{
			if (expr == null || !func(context, expr))
				return;

			new VisitFuncVisitor<TContext>(context, func).Visit(expr);
		}

		/// <summary>
		/// Calls the given <paramref name="func"/> for each node of the <paramref name="expr"/>.
		/// If the <paramref name="func"/> returns false, no childs of the tested expression will be enumerated.
		/// </summary>
		public static void Visit(this Expression expr, Func<object?, Expression, bool> func)
		{
			if (expr == null || !func(null, expr))
				return;

			new VisitFuncVisitor<object?>(null, func).Visit(expr);
		}

		#endregion

		#region Find

		/// <summary>
		/// Enumerates the expression tree and returns the <paramref name="exprToFind"/> if it's
		/// contained within the <paramref name="expr"/>.
		/// </summary>
		public static Expression? Find(this Expression? expr, Expression exprToFind)
		{
			return expr.Find(exprToFind, (exprToFind, e) => e == exprToFind);
		}

		/// <summary>
		/// Enumerates the given <paramref name="expr"/> and returns the first sub-expression
		/// which matches the given <paramref name="func"/>. If no expression was found, null is returned.
		/// </summary>
		public static Expression? Find<TContext>(this Expression? expr, TContext context, Func<TContext,Expression, bool> func)
		{
			if (expr == null)
				return expr;

			return new FindVisitor<TContext>(context, func).Find(expr);
		}

		/// <summary>
		/// Enumerates the given <paramref name="expr"/> and returns the first sub-expression
		/// which matches the given <paramref name="func"/>. If no expression was found, null is returned.
		/// </summary>
		public static Expression? Find(this Expression? expr, Func<object?, Expression, bool> func)
		{
			if (expr == null)
				return expr;

			return new FindVisitor<object?>(null, func).Find(expr);
		}

		#endregion

		#region Transform

		public static Expression Replace(this Expression expression, Expression toReplace, Expression replacedBy)
		{
			return Transform(
				expression,
				new { toReplace, replacedBy },
				static (context, e) => e == context.toReplace ? context.replacedBy : e);
		}

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces the first parameter of that
		/// lambda expression with the <paramref name="exprToReplaceParameter"/> expression.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter)
		{
			return Transform(
				lambda.Body,
				new { lambda, exprToReplaceParameter },
				static (context, e) => e == context.lambda.Parameters[0] ? context.exprToReplaceParameter : e);
		}

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces the first two parameters of
		/// that lambda expression with the given replace expressions.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter1, Expression exprToReplaceParameter2)
		{
			return Transform(
				lambda.Body,
				new { lambda, exprToReplaceParameter1, exprToReplaceParameter2 },
				static (context, e) =>
					e == context.lambda.Parameters[0] ? context.exprToReplaceParameter1 :
					e == context.lambda.Parameters[1] ? context.exprToReplaceParameter2 : e);
		}

		/// <summary>
		/// Returns the body of <paramref name="lambda"/> but replaces the first three parameters of
		/// that lambda expression with the given replace expressions.
		/// </summary>
		public static Expression GetBody(this LambdaExpression lambda, Expression exprToReplaceParameter1, Expression exprToReplaceParameter2, Expression exprToReplaceParameter3)
		{
			return Transform(
				lambda.Body,
				new { lambda, exprToReplaceParameter1, exprToReplaceParameter2, exprToReplaceParameter3 },
				static (context, e) =>
					e == context.lambda.Parameters[0] ? context.exprToReplaceParameter1 :
					e == context.lambda.Parameters[1] ? context.exprToReplaceParameter2 :
					e == context.lambda.Parameters[2] ? context.exprToReplaceParameter3 : e);
		}

		/// <summary>
		/// Enumerates the expression tree of <paramref name="expr"/> and might
		/// replace expression with the returned value of the given <paramref name="func"/>.
		/// </summary>
		/// <returns>The modified expression.</returns>
		[return: NotNullIfNotNull("expr")]
		public static Expression? Transform<TContext>(this Expression? expr, TContext context, [InstantHandle] Func<TContext, Expression, Expression> func)
		{
			if (expr == null)
				return null;

			return new TransformVisitor<TContext>(context, func).Transform(expr);
		}

		/// <summary>
		/// Enumerates the expression tree of <paramref name="expr"/> and might
		/// replace expression with the returned value of the given <paramref name="func"/>.
		/// </summary>
		/// <returns>The modified expression.</returns>
		[return: NotNullIfNotNull("expr")]
		public static Expression? Transform(this Expression? expr, [InstantHandle] Func<object?, Expression, Expression> func)
		{
			if (expr == null)
				return null;

			return new TransformVisitor<object?>(null, func).Transform(expr);
		}

		#endregion

		#region Transform2
		[return: NotNullIfNotNull("expr")]
		public static Expression? Transform<TContext>(this Expression? expr, TContext context, Func<TContext, Expression, TransformInfo> func)
		{
			if (expr == null)
				return null;

			return new TransformInfoVisitor<TContext>(context, func).Transform(expr);
		}

		[return: NotNullIfNotNull("expr")]
		public static Expression? Transform(this Expression? expr, Func<object?, Expression, TransformInfo> func)
		{
			if (expr == null)
				return null;

			return new TransformInfoVisitor<object?>(null, func).Transform(expr);
		}
		#endregion

		public static Expression GetMemberGetter(MemberInfo mi, Expression obj)
		{
			if (mi is DynamicColumnInfo)
			{
				return Expression.Call(
					Methods.LinqToDB.SqlExt.Property.MakeGenericMethod(mi.GetMemberType()),
					obj,
					Expression.Constant(mi.Name));
			}
			else
				return Expression.MakeMemberAccess(obj, mi);
		}
	}
}
