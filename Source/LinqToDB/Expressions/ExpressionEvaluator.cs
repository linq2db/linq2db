using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToDB.Expressions
{
	using Extensions;
	using Common;
	using Common.Internal;
	using Reflection;


	/// <summary>
	/// Internal API.
	/// </summary>
	public static class ExpressionEvaluator
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T? EvaluateExpression<T>(this Expression? expr)
			where T : class
		{
			return expr.EvaluateExpression() as T;
		}

		static bool IsSimpleEvaluatable(Expression? expr)
		{
			if (expr == null)
				return true;

			switch (expr.NodeType)
			{
				case ExpressionType.Default:
					return true;

				case ExpressionType.Constant:
					return true;

				case ExpressionType.MemberAccess:
				{
					var member = (MemberExpression) expr;

					if (member.Member.MemberType == MemberTypes.Property)
					{
						return IsSimpleEvaluatable(member.Expression);
					}

					return false;
				}

				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)expr;
					return IsSimpleEvaluatable(mc.Object) && mc.Arguments.All(IsSimpleEvaluatable);
				}
			}

			return false;
		}


		public static object? EvaluateExpression(this Expression? expr)
		{
			if (expr == null)
				return null;

			if (IsSimpleEvaluatable(expr))
			{
				switch (expr.NodeType)
				{
					case ExpressionType.Default:
						return !expr.Type.IsNullableType() ? TypeAccessor.GetAccessor(expr.Type).CreateInstanceEx() : null;

					case ExpressionType.Constant:
						return ((ConstantExpression)expr).Value;

					case ExpressionType.MemberAccess:
					{
						var member = (MemberExpression) expr;

						if (member.Member.IsFieldEx())
							return ((FieldInfo)member.Member).GetValue(member.Expression.EvaluateExpression());

						if (member.Member is PropertyInfo propertyInfo)
						{
							var obj = member.Expression.EvaluateExpression();
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
						var arguments = mc.Arguments.Select(a => a.EvaluateExpression()).ToArray();
						var instance  = mc.Object.EvaluateExpression();

						if (instance == null && mc.Method.IsNullableGetValueOrDefault())
							return null;

						return mc.Method.Invoke(instance, arguments);
					}
				}
			}

			var value = Expression.Lambda(expr).CompileExpression().DynamicInvoke();
			return value;
		}
	}
}
