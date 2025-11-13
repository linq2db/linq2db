using System;
using System.Collections.Generic;
using System.Linq.Expressions;

using LinqToDB.Expressions;

namespace LinqToDB.Internal.Expressions
{
	public static class ExpressionHelpers
	{
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
