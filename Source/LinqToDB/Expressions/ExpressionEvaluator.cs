using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Expressions
{
	using Extensions;
	using Reflection;
	using Linq;
	using Linq.Builder;
	using Mapping;
	using Common;
	using Common.Internal;

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

			switch (expr.NodeType)
			{
				case ExpressionType.Default:
					return !expr.Type.IsNullableType() ? Activator.CreateInstance(expr.Type) : null;

				case ExpressionType.Constant:
					return ((ConstantExpression)expr).Value;

				case ExpressionType.Parameter:
					if (expr == ExpressionConstants.DataContextParam && dataContext != null)
						return dataContext;
					break;

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
				{
					var unary = (UnaryExpression)expr;
					var operand = unary.Operand.EvaluateExpression(dataContext);
					if (operand == null)
						return null;
					break;
				}

				case ExpressionType.MemberAccess:
				{
					var member = (MemberExpression) expr;

					if (member.Member.IsFieldEx())
						return ((FieldInfo)member.Member).GetValue(member.Expression.EvaluateExpression(dataContext));

					if (member.Member is PropertyInfo propertyInfo)
					{
						var obj = member.Expression.EvaluateExpression(dataContext);
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
					var mc = (MethodCallExpression)expr;
					var arguments = mc.Arguments.Select(a => a.EvaluateExpression(dataContext)).ToArray();
					var instance  = mc.Object.EvaluateExpression(dataContext);

					if (instance == null && mc.Method.IsNullableGetValueOrDefault())
						return null;

					return mc.Method.Invoke(instance, arguments);
				}
			}

			expr      = dataContext == null ? expr : expr.Replace(ExpressionConstants.DataContextParam, Expression.Constant(dataContext, typeof(IDataContext)));
			var value = Expression.Lambda(expr).CompileExpression().DynamicInvoke();
			return value;
		}
	}
}
