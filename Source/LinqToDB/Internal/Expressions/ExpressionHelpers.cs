using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Expressions
{
	public static class ExpressionHelpers
	{
		/// <summary>
		/// Moves a <c>Sql.Constant</c> or <c>Sql.Parameter</c> request outward through whatever
		/// <paramref name="build"/> makes of its result.
		/// </summary>
		/// <remarks>
		/// The request is written around the value the caller had in hand, but what a translator ends up sending is
		/// something made from it - a duration's tick count, a duration rounded down to whole seconds. Built inside
		/// the request, that work would leave the request wrapping an argument nothing reads any more, and the
		/// caller's choice of how the value should travel would be dropped in silence. Rewriting the request around
		/// the built value keeps it on what actually reaches the statement.
		/// <para>
		/// An expression carrying no such request is handed to <paramref name="build"/> unchanged, so this is safe to
		/// apply whether or not one of the two is underneath.
		/// </para>
		/// </remarks>
		public static Expression MoveValueMarkerOutside(Expression expression, Func<Expression, Expression> build)
		{
			if (expression.UnwrapConvert() is MethodCallExpression call && IsValueMarker(call.Method))
			{
				var moved = build(call.Arguments[0]);

				return Expression.Call(call.Method.GetGenericMethodDefinition().MakeGenericMethod(moved.Type), moved);
			}

			return build(expression);
		}

		/// <summary>
		/// Whether the method is one of the two that say how a value should reach the statement rather than what it
		/// is - <c>Sql.Constant</c> and <c>Sql.Parameter</c>, both of which return their argument unchanged.
		/// </summary>
		static bool IsValueMarker(MethodInfo method)
		{
			return method.IsGenericMethod
				&& method.DeclaringType == typeof(Sql)
				&& (string.Equals(method.Name, nameof(Sql.Constant),  StringComparison.Ordinal)
					|| string.Equals(method.Name, nameof(Sql.Parameter), StringComparison.Ordinal));
		}

		public static Expression EnsureObject(Expression expr)
		{
			return expr.Type == typeof(object)
				? expr
				: Expression.Convert(expr, typeof(object));
		}

		public static IEnumerable<Expression> CollectMembers(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.New:
				{
					var ne = (NewExpression)expr;

					for (int i = 0; i < ne.Arguments.Count; i++)
					{
						yield return ne.Arguments[i];
					}

					break;
				}

				default:
					yield return expr;
					break;
			}
		}

		#region MakeCall

		public static Expression MakeCall<TParam1, TResult>(Expression<Func<TParam1, TResult>> func, Expression param1)
			=> MakeCallInternal(func, param1);

		public static Expression MakeCall<TParam1, TParam2, TResult>(Expression<Func<TParam1, TParam2, TResult>> func, Expression param1, Expression param2)
			=> MakeCallInternal(func, param1, param2);

		public static Expression MakeCall<TParam1, TParam2, TParam3, TResult>(Expression<Func<TParam1, TParam2, TParam3, TResult>> func, Expression param1, Expression param2, Expression param3)
			=> MakeCallInternal(func, param1, param2, param3);

		static Expression MakeCallInternal(LambdaExpression lambda, params Expression[] parameters)
		{
			var body = lambda.Body;

			for (int i = 0; i < lambda.Parameters.Count; i++)
			{
				var param = lambda.Parameters[i];
				var arg   = parameters[i];

				if (param.Type != arg.Type)
				{
					if (param.Type.IsAssignableFrom(arg.Type))
					{
						arg = Expression.Convert(arg, param.Type);
					}
					else
					{
						throw new InvalidOperationException($"Cannot assign {arg.Type} to {param.Type}");
					}
				}

				body = body.Replace(param, arg);
			}

			return body;
		}

		#endregion

	}
}
