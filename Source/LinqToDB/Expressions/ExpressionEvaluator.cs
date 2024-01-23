using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToDB.Expressions
{
	using Extensions;

	using Common;

	/// <summary>
	/// Internal API.
	/// </summary>
	public static class ExpressionEvaluator
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T? EvaluateExpression<T>(this Expression? expr, IDataContext? dataContext = null)
			where T : class
		{
			return expr.EvaluateExpression(dataContext) as T;
		}

		public static object? EvaluateExpression(this Expression? expr, IDataContext? dataContext = null)
		{
			if (expr == null)
				return null;

			if (dataContext != null)
				expr = expr.Transform(
					dataContext,
					static (dc, e) => e is ConstantExpression { Value : null } ce && ce.Type == typeof(IDataContext) ? Expression.Constant(dc) : e);

			return Evaluate(expr, dataContext);

			static object? Evaluate(Expression? expr, IDataContext? dataContext)
			{
				if (expr == null)
					return null;

				switch (expr.NodeType)
				{
					case ExpressionType.Default:
						return expr.Type.GetDefaultValue();

					case ExpressionType.Constant:
					{
						var c = ((ConstantExpression)expr);
						return c.Type == typeof(IDataContext) && c.Value == null ? dataContext : c.Value;
					}

					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
					{
						var unary = (UnaryExpression)expr;
						var operand = Evaluate(unary.Operand, dataContext);
						if (operand == null)
							return null;
						break;
					}

					case ExpressionType.MemberAccess:
					{
						var member = (MemberExpression) expr;

						if (member.Member.IsFieldEx())
							return ((FieldInfo)member.Member).GetValue(Evaluate(member.Expression, dataContext));

						if (member.Member is PropertyInfo propertyInfo)
						{
							var obj = Evaluate(member.Expression, dataContext);
							if (obj == null)
							{
								if (propertyInfo.IsNullableValueMember())
									return null;
								if (propertyInfo.IsNullableHasValueMember())
									return false;
							}
							return propertyInfo.GetValue(obj, null);
						}

						break;
					}

					case ExpressionType.Call:
					{
						var mc        = (MethodCallExpression)expr;
						var arguments = mc.Arguments.Select(a => Evaluate(a, dataContext)).ToArray();
						var instance  = Evaluate(mc.Object, dataContext);

						if (instance == null && mc.Method.IsNullableGetValueOrDefault())
							return null;

						return mc.Method.Invoke(instance, arguments);
					}
				}

				var value = Expression.Lambda(expr).CompileExpression().DynamicInvoke();
				return value;
			}
		}
	}
}
