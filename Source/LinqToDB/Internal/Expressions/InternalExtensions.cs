using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using LinqToDB.Async;
using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions.ExpressionVisitors;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Expressions
{
	static class InternalExtensions
	{
		#region IsConstant

		public static bool IsConstantable(this Type type, bool includingArrays)
		{
			if (type.IsEnum)
				return true;

			switch (type.GetTypeCodeEx())
			{
				case TypeCode.Int16   :
				case TypeCode.Int32   :
				case TypeCode.Int64   :
				case TypeCode.UInt16  :
				case TypeCode.UInt32  :
				case TypeCode.UInt64  :
				case TypeCode.SByte   :
				case TypeCode.Byte    :
				case TypeCode.Decimal :
				case TypeCode.Double  :
				case TypeCode.Single  :
				case TypeCode.Boolean :
				case TypeCode.String  :
				case TypeCode.Char    : return true;
			}

			if (type.IsNullable())
				return type.GetGenericArguments()[0].IsConstantable(includingArrays);

			if (includingArrays && type.IsArray)
				return type.GetElementType()!.IsConstantable(includingArrays);

			if (type == typeof(Sql.SqlID))
				return true;

			return false;
		}

		#endregion

		#region Path
		public static void Path<TContext>(this Expression expr, Expression path, TContext context, Action<TContext, Expression, Expression> func)
		{
			new PathVisitor<TContext>(context, path, func).Path(expr);
		}
		#endregion

		#region Helpers

		public static LambdaExpression UnwrapLambda(this Expression ex)
			=> (LambdaExpression)ex.Unwrap();

		[return: NotNullIfNotNull(nameof(ex))]
		public static Expression? Unwrap(this Expression? ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.Quote          :
				case ExpressionType.ConvertChecked :
				case ExpressionType.Convert        :
					return ((UnaryExpression)ex).Operand.Unwrap();
				case ExpressionType.Extension
					when ex is SqlAdjustTypeExpression adjustType:
				{
					return adjustType.Expression.Unwrap();
				}
			}

			return ex;
		}

		[return: NotNullIfNotNull(nameof(ex))]
		public static Expression? UnwrapConvert(this Expression? ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.ConvertChecked :
				case ExpressionType.Convert        :
				{
					var unaryExpression = (UnaryExpression)ex;
					if (unaryExpression.Method == null)
						return unaryExpression.Operand.UnwrapConvert();
					break;
				}
				case ExpressionType.Extension
					when ex is SqlAdjustTypeExpression adjustType:
				{
					return adjustType.Expression.Unwrap();
				}
			}

			return ex;
		}

		[return: NotNullIfNotNull(nameof(ex))]
		public static Expression? UnwrapConvertToObject(this Expression? ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.ConvertChecked:
				case ExpressionType.Convert:
				{
					var unaryExpression = (UnaryExpression)ex;
					if (unaryExpression.Type == typeof(object))
						return unaryExpression.Operand.UnwrapConvertToObject();
					break;
				}
			}

			return ex;
		}

		[return: NotNullIfNotNull(nameof(ex))]
		public static Expression? UnwrapConvertToNotObject(this Expression? ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.ConvertChecked:
				case ExpressionType.Convert:
				{
					var unaryExpression = (UnaryExpression)ex;
					if (unaryExpression.Operand.Type != typeof(object) && unaryExpression.Method == null)
						return unaryExpression.Operand.UnwrapConvertToNotObject();
					break;
				}
			}

			return ex;
		}

		public static Expression SkipMethodChain(this Expression expr, MappingSchema mappingSchema, out bool isQueryable)
		{
			return Sql.ExtensionAttribute.ExcludeExtensionChain(mappingSchema, expr, out isQueryable);
		}

		public static Dictionary<Expression,Expression> GetExpressionAccessors(this Expression expression, Expression path)
		{
			var accessors = new Dictionary<Expression,Expression>();

			expression.Path(path, accessors, static (accessors, e, p) =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.Call           :
					case ExpressionType.MemberAccess   :
					case ExpressionType.New            :
						if (!accessors.ContainsKey(e))
							accessors.Add(e, p);
						break;

					case ExpressionType.Constant       :
						if (!accessors.ContainsKey(e))
							accessors.Add(e, Expression.Property(p, ReflectionHelper.Constant.Value));
						break;

					case ExpressionType.ConvertChecked :
					case ExpressionType.Convert        :
						if (!accessors.ContainsKey(e))
						{
							var ue = (UnaryExpression)e;

							switch (ue.Operand.NodeType)
							{
								case ExpressionType.Call        :
								case ExpressionType.MemberAccess:
								case ExpressionType.New         :
								case ExpressionType.Constant    :

									accessors.Add(e, p);
									break;
							}
						}

						break;
				}
			});

			return accessors;
		}

#pragma warning disable RS0060 // API with optional parameter(s) should have the most parameters amongst its public overloads
		public static bool IsQueryable(this MethodCallExpression method, bool enumerable = true)
#pragma warning restore RS0060 // API with optional parameter(s) should have the most parameters amongst its public overloads
		{
			var type = method.Method.DeclaringType;

			return
				type == typeof(Queryable)                ||
				enumerable && type == typeof(Enumerable) ||
				type == typeof(LinqExtensions)           ||
				type == typeof(LinqInternalExtensions)   ||
				type == typeof(DataExtensions)           ||
				type == typeof(TableExtensions)          ||
				MemberCache.GetMemberInfo(method.Method).IsQueryable;
		}

		public static bool IsAsyncExtension(this MethodCallExpression method)
		{
			var type = method.Method.DeclaringType;

			return type == typeof(AsyncExtensions);
		}

		public static bool IsExtensionMethod(this MethodCallExpression methodCall, MappingSchema mapping)
		{
			return mapping.HasAttribute<Sql.ExtensionAttribute>(methodCall.Method.ReflectedType!, methodCall.Method);
		}

		public static bool IsQueryable(this MethodCallExpression method, string name)
		{
			return method.Method.Name == name && method.IsQueryable();
		}

		public static bool IsQueryable(this MethodCallExpression method, string[] names)
		{
			if (method.IsQueryable())
				foreach (var name in names)
					if (method.Method.Name == name)
						return true;

			return false;
		}

		public static bool IsAsyncExtension(this MethodCallExpression method, string[] names)
		{
			if (method.IsAsyncExtension())
				foreach (var name in names)
					if (method.Method.Name == name)
						return true;

			return false;
		}

		public static bool IsSameGenericMethod(this MethodCallExpression method, MethodInfo genericMethodInfo)
		{
			if (!method.Method.IsGenericMethod || method.Method.Name != genericMethodInfo.Name)
				return false;
			return method.Method.GetGenericMethodDefinitionCached() == genericMethodInfo;
		}

		public static bool IsSameGenericMethod(this MethodCallExpression method, MethodInfo[] genericMethodInfo)
		{
			if (!method.Method.IsGenericMethod)
				return false;

			var         mi = method.Method;
			MethodInfo? gd = null;

			foreach (var current in genericMethodInfo)
			{
				if (current.Name == mi.Name)
				{
					if (gd == null)
					{
						gd = mi.GetGenericMethodDefinitionCached();
					}

					if (gd.Equals(current))
						return true;
				}
			}

			return false;
		}

		public static bool IsAssociation(MemberInfo member, MappingSchema mappingSchema)
		{
			return mappingSchema.GetAttribute<AssociationAttribute>(member.DeclaringType!, member) != null;
		}

		public static bool IsAssociation(this MemberExpression memberExpression, MappingSchema mappingSchema)
		{
			return IsAssociation(memberExpression.Member, mappingSchema);
		}

		public static bool IsAssociation(this MethodCallExpression method, MappingSchema mappingSchema)
		{
			return IsAssociation(method.Method, mappingSchema);
		}

		private static readonly string[] CteMethodNames = { "AsCte", "GetCte" };

		public static bool IsCte(this MethodCallExpression method)
		{
			return method.IsQueryable(CteMethodNames);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullValue(this Expression expr)
		{
			return (expr is ConstantExpression { Value: null })
				|| (expr is DefaultExpression or DefaultValueExpression && expr.Type.IsNullableType());
		}

		public static Expression? GetArgumentByName(this MethodCallExpression methodCall, string parameterName)
		{
			var arguments = methodCall.Arguments;
			var parameters = methodCall.Method.GetParameters();

			for (var i = 0; i < parameters.Length; i++)
				if (parameters[i].Name == parameterName)
					return arguments[i];

			return default;
		}

		#endregion

		public static bool IsEvaluable(this Expression? expression, MappingSchema mappingSchema)
		{
			return expression?.NodeType switch
			{
				null                        => true,
				ExpressionType.Convert or ExpressionType.ConvertChecked => IsEvaluable(((UnaryExpression)expression).Operand, mappingSchema),
				ExpressionType.Default      => true,
				// don't return true for closure classes
				ExpressionType.Constant     => expression is ConstantExpression c && (c.Value == null || c.Value is string || c.Value.GetType().IsValueType),
				ExpressionType.MemberAccess => ((MemberExpression)expression).Member.GetExpressionAttribute(mappingSchema)?.ServerSideOnly != true && IsEvaluable(((MemberExpression)expression).Expression, mappingSchema),
				_                           => false,
			};
		}

		/// <summary>
		/// Optimizes expression context by evaluating constants and simplifying boolean operations.
		/// </summary>
		/// <param name="expression">Expression to optimize.</param>
		/// <returns>Optimized expression.</returns>
		[return: NotNullIfNotNull(nameof(expression))]
		public static Expression? OptimizeExpression(this Expression? expression, MappingSchema mappingSchema)
		{
			return TransformInfoVisitor<MappingSchema>.Create(mappingSchema, OptimizeExpressionTransformer).Transform(expression);
		}

		private static TransformInfo OptimizeExpressionTransformer(MappingSchema mappingSchema, Expression e)
		{
			var newExpr = e;
			if (e is BinaryExpression binary)
			{
				var left  = OptimizeExpression(binary.Left, mappingSchema)!;
				var right = OptimizeExpression(binary.Right, mappingSchema)!;

				if (left.Type != binary.Left.Type)
					left = Expression.Convert(left, binary.Left.Type);

				if (right.Type != binary.Right.Type)
					right = Expression.Convert(right, binary.Right.Type);

				newExpr = binary.Update(left, OptimizeExpression(binary.Conversion, mappingSchema) as LambdaExpression, right);
			}
			else if (e is UnaryExpression unaryExpression)
			{
				newExpr = unaryExpression.Update(OptimizeExpression(unaryExpression.Operand, mappingSchema));
				if (newExpr.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked && ((UnaryExpression)newExpr).Operand.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					// remove double convert
					newExpr = Expression.Convert(
						((UnaryExpression)((UnaryExpression)newExpr).Operand).Operand, newExpr.Type);
				}
			}

			if (IsEvaluable(newExpr, mappingSchema))
			{
				newExpr = newExpr.NodeType == ExpressionType.Constant
					? newExpr
					: Expression.Constant(newExpr.EvaluateExpression());
			}
			else
			{
				switch (newExpr)
				{
					case NewArrayExpression:
					{
						return new TransformInfo(newExpr, true);
					}
					case UnaryExpression unary when IsEvaluable(unary.Operand, mappingSchema):
					{
						newExpr = Expression.Constant(unary.EvaluateExpression());
						break;
					}
					case MemberExpression { Expression.NodeType: ExpressionType.Constant } me when IsEvaluable(me.Expression, mappingSchema):
					{
						newExpr = Expression.Constant(me.EvaluateExpression());
						break;
					}
					case BinaryExpression be when IsEvaluable(be.Left, mappingSchema) && IsEvaluable(be.Right, mappingSchema):
					{
						newExpr = Expression.Constant(be.EvaluateExpression());
						break;
					}
					case BinaryExpression { NodeType: ExpressionType.AndAlso } be:
					{
						if (IsEvaluable(be.Left, mappingSchema))
						{
							var leftBool = be.Left.EvaluateExpression() as bool?;
							if (leftBool == true)
								e = be.Right;
							else if (leftBool == false)
								newExpr = ExpressionInstances.False;
						}
						else if (IsEvaluable(be.Right, mappingSchema))
						{
							var rightBool = be.Right.EvaluateExpression() as bool?;
							if (rightBool == true)
								newExpr = be.Left;
							else if (rightBool == false)
								newExpr = ExpressionInstances.False;
						}

						break;
					}
					case BinaryExpression { NodeType: ExpressionType.OrElse } be:
					{
						if (IsEvaluable(be.Left, mappingSchema))
						{
							var leftBool = be.Left.EvaluateExpression() as bool?;
							if (leftBool == false)
								newExpr = be.Right;
							else if (leftBool == true)
								newExpr = ExpressionInstances.True;
						}
						else if (IsEvaluable(be.Right, mappingSchema))
						{
							var rightBool = be.Right.EvaluateExpression() as bool?;
							if (rightBool == false)
								newExpr = be.Left;
							else if (rightBool == true)
								newExpr = ExpressionInstances.True;
						}

						break;
					}
				}
			}

			if (newExpr.Type != e.Type)
				newExpr = Expression.Convert(newExpr, e.Type);

			return new TransformInfo(newExpr);
		}

		public static Expression ApplyLambdaToExpression(LambdaExpression convertLambda, Expression expression)
		{
			// Replace multiple parameters with single variable or single parameter with the reader expression.
			//
			if (expression.NodeType != ExpressionType.Parameter && convertLambda.Body.GetCount(convertLambda, static (convertLambda, e) => e == convertLambda.Parameters[0]) > 1)
			{
				var variable = Expression.Variable(expression.Type);
				var assign   = Expression.Assign(variable, expression);

				expression = Expression.Block(new[] { variable }, assign, convertLambda.GetBody(variable));
			}
			else
			{
				expression = convertLambda.GetBody(expression);
			}

			return expression;
		}
	}
}
