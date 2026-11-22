using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;

namespace LinqToDB.Internal.Expressions
{
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
			return expr switch
			{
				null => true,

				{ NodeType: ExpressionType.Default } => true,
				{ NodeType: ExpressionType.Constant } => true,

				// our own default-value node reduces to a constant, so it needs no compiled lambda
				DefaultValueExpression => true,

				MemberExpression { NodeType: ExpressionType.MemberAccess } member =>
					member.Member.MemberType is MemberTypes.Field or MemberTypes.Property
					&& IsSimpleEvaluatable(member.Expression),

				MethodCallExpression { NodeType: ExpressionType.Call } mc =>
					IsSimpleEvaluatable(mc.Object) && mc.Arguments.All(IsSimpleEvaluatable),

				// A quote evaluates to its operand, so it needs no compiled lambda - which also cannot handle
				// an operand referencing a free parameter, as a compiled-query tree does after CompileQuery
				// rewrites its parameters to ArrayIndex(ps, i).
				UnaryExpression { NodeType: ExpressionType.Quote } => true,

				_ => false,
			};
		}

		static object? EvaluateExpressionInternal(this Expression? expr)
		{
			if (expr == null)
				return null;

			switch (expr.NodeType)
			{
				case ExpressionType.Default:
					return ReflectionExtensions.GetDefaultValue(expr.Type);

				case ExpressionType.Constant:
					return ((ConstantExpression)expr).Value;

				case ExpressionType.Quote:
					return ((UnaryExpression)expr).Operand;

				// Reduce() is what produced this value before, via a compiled lambda - it honours a
				// mapping schema's custom default, so reducing here keeps the result identical.
				case ExpressionType.Extension when expr is DefaultValueExpression defaultValue:
					return defaultValue.Reduce().EvaluateExpressionInternal();

				case ExpressionType.MemberAccess:
				{
					var member = (MemberExpression) expr;

					if (member.Member is FieldInfo fieldInfo)
						return fieldInfo.GetValue(member.Expression.EvaluateExpressionInternal());

					if (member.Member is PropertyInfo propertyInfo)
					{
						var obj = member.Expression.EvaluateExpressionInternal();
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
					var arguments = mc.Arguments.Select(a => a.EvaluateExpressionInternal()).ToArray();
					var instance  = mc.Object.EvaluateExpressionInternal();

					if (instance == null && mc.Method.IsNullableGetValueOrDefault())
						return null;

					return mc.Method.InvokeExt(instance, arguments);
				}
			}

			throw new InvalidOperationException($"Expression '{expr}' cannot be evaluated");
		}

		public static object? EvaluateExpression(this Expression? expr)
		{
			if (expr == null)
				return null;

			if (IsSimpleEvaluatable(expr))
			{
				return expr.EvaluateExpressionInternal();
			}

			var value = Expression.Lambda(expr).CompileExpression().DynamicInvokeExt();
			return value;
		}

		/// <summary>
		/// Reads the value of a captured variable - a chain of field reads over a constant, or a static field -
		/// without running any getter or method. Reading a static field runs its type's static constructor if it has
		/// not run yet.
		/// </summary>
		/// <param name="expr">Expression to read.</param>
		/// <param name="value">The value read, when the method returns <see langword="true"/>.</param>
		/// <returns>
		/// <see langword="false"/> when <paramref name="expr"/> is anything other than such a chain, or a link of the
		/// chain is <see langword="null"/>.
		/// </returns>
		public static bool TryEvaluateCapturedValue(this Expression expr, out object? value)
		{
			switch (expr)
			{
				case MemberExpression { Member: FieldInfo field, Expression: null } when field.IsStatic:
					value = field.GetValue(null);
					return true;

				case MemberExpression { Member: FieldInfo field, Expression: { } instanceExpr }:
					if (TryReadLink(instanceExpr, out var instance) && instance != null)
					{
						value = field.GetValue(instance);
						return true;
					}

					break;
			}

			value = null;
			return false;

			static bool TryReadLink(Expression link, out object? linkValue)
			{
				if (link is ConstantExpression constant)
				{
					linkValue = constant.Value;
					return true;
				}

				return link.TryEvaluateCapturedValue(out linkValue);
			}
		}
	}
}
