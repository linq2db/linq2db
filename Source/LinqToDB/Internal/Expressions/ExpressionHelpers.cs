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
		/// Moves a <c>Sql.Constant</c> or <c>Sql.Parameter</c> request outward through the member accesses applied
		/// to its result: <c>Sql.Constant(x).A.B</c> becomes <c>Sql.Constant(x.A.B)</c>.
		/// </summary>
		/// <remarks>
		/// The request is written around the value the caller had in hand, but what a translator ends up sending is
		/// often a member of it - a duration's tick count, a date's year. Left where it was written, the request
		/// would sit inside an argument that nothing reads any more, and the caller's choice would be dropped in
		/// silence. Moving it keeps it on the value that actually reaches the statement.
		/// <para>
		/// Anything else is returned unchanged, so this is safe to apply to a member access whether or not one of
		/// the two requests is underneath it.
		/// </para>
		/// </remarks>
		public static Expression MoveValueMarkerOutside(Expression expression)
		{
			List<MemberInfo>? members = null;

			var current = expression;

			while (current is MemberExpression { Expression: not null } member)
			{
				(members ??= new()).Add(member.Member);
				current = member.Expression;
			}

			if (members == null)
				return expression;

			if (current.UnwrapConvert() is not MethodCallExpression call || !IsValueMarker(call.Method))
				return expression;

			var moved = call.Arguments[0];

			for (var i = members.Count - 1; i >= 0; i--)
				moved = Expression.MakeMemberAccess(moved, members[i]);

			return Expression.Call(call.Method.GetGenericMethodDefinition().MakeGenericMethod(moved.Type), moved);
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
