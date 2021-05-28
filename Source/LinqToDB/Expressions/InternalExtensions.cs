using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Expressions
{
	using LinqToDB.Extensions;
	using Reflection;
	using Linq;
	using Linq.Builder;
	using Mapping;
	using LinqToDB.Common;

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
			=> (LambdaExpression)((UnaryExpression)ex).Operand.Unwrap();

		[return: NotNullIfNotNull("ex")]
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
			}

			return ex;
		}

		[return: NotNullIfNotNull("ex")]
		public static Expression? UnwrapConvert(this Expression? ex)
		{
			if (ex == null)
				return null;

			switch (ex.NodeType)
			{
				case ExpressionType.ConvertChecked :
				case ExpressionType.Convert        :
				{
					if (((UnaryExpression)ex).Method == null)
						return ((UnaryExpression)ex).Operand.UnwrapConvert();
					break;
				}
			}

			return ex;
		}

		[return: NotNullIfNotNull("ex")]
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

		[return: NotNullIfNotNull("ex")]
		public static Expression? UnwrapWithAs(this Expression? ex)
		{
			return ex?.NodeType switch
			{
				null                  => null,
				ExpressionType.TypeAs => ((UnaryExpression)ex).Operand.Unwrap(),
				_                     => ex.Unwrap(),
			};
		}

		private static readonly MethodInfo[] SkipPathThroughMethods = new [] { Methods.Enumerable.AsQueryable, Methods.LinqToDB.SqlExt.ToNotNull };

		public static Expression SkipPathThrough(this Expression expr)
		{
			while (expr is MethodCallExpression mce && mce.IsSameGenericMethod(SkipPathThroughMethods))
				expr = mce.Arguments[0];
			return expr;
		}

		public static Expression SkipMethodChain(this Expression expr, MappingSchema mappingSchema)
		{
			return Sql.ExtensionAttribute.ExcludeExtensionChain(mappingSchema, expr);
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
								case ExpressionType.Call           :
								case ExpressionType.MemberAccess   :
								case ExpressionType.New            :
								case ExpressionType.Constant       :

									accessors.Add(e, p);
									break;
							}
						}

						break;
				}
			});

			return accessors;
		}

		[return: NotNullIfNotNull("expr")]
		public static Expression? GetRootObject(Expression? expr, MappingSchema mapping)
		{
			if (expr == null)
				return null;

			expr = expr.SkipMethodChain(mapping);
			expr = expr.SkipPathThrough();

			switch (expr.NodeType)
			{
				case ExpressionType.Call         :
					{
						var e = (MethodCallExpression)expr;

						if (e.Object != null)
							return GetRootObject(e.Object, mapping);

						if (e.Arguments?.Count > 0 &&
						    (e.IsQueryable()
						     || e.IsAggregate(mapping)
						     || e.IsAssociation(mapping)
						     || e.Method.IsSqlPropertyMethodEx()
						     || e.IsSameGenericMethod(TunnelMethods)))
							return GetRootObject(e.Arguments[0], mapping);

						break;
					}

				case ExpressionType.MemberAccess :
					{
						var e = (MemberExpression)expr;

						if (e.Expression != null)
							return GetRootObject(e.Expression.UnwrapWithAs(), mapping);

						break;
					}
			}

			return expr;
		}

		public static List<Expression> GetMembers(this Expression? expr)
		{
			if (expr == null)
				return new List<Expression>();

			List<Expression> list;

			switch (expr.NodeType)
			{
				case ExpressionType.Call         :
					{
						var e = (MethodCallExpression)expr;

						if (e.Object != null)
							list = GetMembers(e.Object);
						else if (e.Arguments?.Count > 0 && e.IsQueryable())
							list = GetMembers(e.Arguments[0]);
						else
							list = new List<Expression>();

						break;
					}

				case ExpressionType.MemberAccess :
					{
						var e = (MemberExpression)expr;

						list = e.Expression != null ? GetMembers(e.Expression.Unwrap()) : new List<Expression>();

						break;
					}

				default                          :
					list = new List<Expression>();
					break;
			}

			list.Add(expr);

			return list;
		}

		public static bool IsQueryable(this MethodCallExpression method, bool enumerable = true)
		{
			var type = method.Method.DeclaringType;

			return
				type == typeof(Queryable) ||
				enumerable && type == typeof(Enumerable) ||
				type == typeof(LinqExtensions) ||
				type == typeof(DataExtensions) ||
				type == typeof(TableExtensions);
		}

		public static bool IsAsyncExtension(this MethodCallExpression method, bool enumerable = true)
		{
			var type = method.Method.DeclaringType;

			return type == typeof(AsyncExtensions);
		}

		public static bool IsAggregate(this MethodCallExpression methodCall, MappingSchema mapping)
		{
			if (methodCall.IsQueryable(AggregationBuilder.MethodNames) || methodCall.IsQueryable(CountBuilder.MethodNames))
				return true;

			if (methodCall.Arguments.Count > 0)
			{
				var function = AggregationBuilder.GetAggregateDefinition(methodCall, mapping);
				return function != null;
			}

			return false;
		}

		public static bool IsExtensionMethod(this MethodCallExpression methodCall, MappingSchema mapping)
		{
			var functions = mapping.GetAttributes<Sql.ExtensionAttribute>(methodCall.Method.ReflectedType!,
				methodCall.Method,
				static f => f.Configuration);
			return functions.Any();
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

		public static bool IsAssociation(this MethodCallExpression method, MappingSchema mappingSchema)
		{
			return mappingSchema.GetAttribute<AssociationAttribute>(method.Method.DeclaringType!, method.Method) != null;
		}

		private static readonly string[] CteMethodNames = { "AsCte", "GetCte" };

		public static bool IsCte(this MethodCallExpression method, MappingSchema mappingSchema)
		{
			return method.IsQueryable(CteMethodNames);
		}

		static Expression FindLevel(Expression expression, MappingSchema mapping, int level, ref int current)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Call :
					{
						var call = (MethodCallExpression)expression;
						var expr = ExtractMethodCallTunnelExpression(call, mapping);

						if (expr != null)
						{
							var ex = FindLevel(expr, mapping, level, ref current);

							if (level == current)
								return ex;

							current++;
						}

						break;
					}

				case ExpressionType.MemberAccess:
					{
						var e = ((MemberExpression)expression);

						if (e.Expression != null)
						{
							var expr = FindLevel(e.Expression.UnwrapWithAs(), mapping, level, ref current);

							if (level == current)
								return expr;

							current++;
						}

						break;
					}
			}

			return expression;
		}

		/// <summary>
		/// Returns part of expression based on its level.
		/// </summary>
		/// <param name="expression">Base expression that needs decomposition.</param>
		/// <param name="mapping">Maping schema.</param>
		/// <param name="level">Level that should be to be extracted.</param>
		/// <returns>Exstracted expression.</returns>
		/// <example>
		/// This sample shows what method returns for expression [c.ParentId].
		/// <code>
		/// expression.GetLevelExpression(mapping, 0) == [c]
		/// expression.GetLevelExpression(mapping, 1) == [c.ParentId]
		/// </code>
		/// </example>
		public static Expression GetLevelExpression(this Expression expression, MappingSchema mapping, int level)
		{
			var current = 0;
			var expr    = FindLevel(expression, mapping, level, ref current);

			if (expr == null || current != level)
				throw new InvalidOperationException();

			return expr;
		}

		private static readonly MethodInfo[] TunnelMethods = new [] { Methods.LinqToDB.SqlExt.ToNotNull, Methods.LinqToDB.SqlExt.Alias };

		static Expression? ExtractMethodCallTunnelExpression(MethodCallExpression call, MappingSchema mapping)
		{
			var expr = call.Object;

			if (expr == null && call.Arguments.Count > 0 &&
			    (call.IsQueryable()
			     || call.IsAggregate(mapping)
			     || call.IsExtensionMethod(mapping)
			     || call.IsAssociation(mapping)
				 || call.Method.IsSqlPropertyMethodEx()
				 || call.IsSameGenericMethod(TunnelMethods)
			     )
			    )
			{
				expr = call.Arguments[0];
			}

			return expr;
		}

		public static int GetLevel(this Expression expression, MappingSchema mapping)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.Call :
					{
						var call = (MethodCallExpression)expression;
						var expr = ExtractMethodCallTunnelExpression(call, mapping);
						if (expr != null)
						{
							return GetLevel(expr.UnwrapWithAs(), mapping) + 1;
						}

						break;
					}

				case ExpressionType.MemberAccess:
					{
						var e = ((MemberExpression)expression);

						if (e.Expression != null)
							return GetLevel(e.Expression.UnwrapWithAs(), mapping) + 1;

						break;
					}
			}

			return 0;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T? EvaluateExpression<T>(this Expression? expr)
			where T : class
		{
			return expr.EvaluateExpression() as T;
		}

		public static object? EvaluateExpression(this Expression? expr)
		{
			if (expr == null)
				return null;

			switch (expr.NodeType)
			{
				case ExpressionType.Constant:
					return ((ConstantExpression)expr).Value;

				case ExpressionType.Convert:
				case ExpressionType.ConvertChecked:
					{
						var unary = (UnaryExpression)expr;
						var operand = unary.Operand.EvaluateExpression();
						if (operand == null)
							return null;
						break;
					}

				case ExpressionType.MemberAccess:
					{
						var member = (MemberExpression) expr;

						if (member.Member.IsFieldEx())
							return ((FieldInfo)member.Member).GetValue(member.Expression.EvaluateExpression());

						if (member.Member.IsPropertyEx())
						{
							var obj = member.Expression.EvaluateExpression();
							if (obj == null && ((PropertyInfo)member.Member).IsNullableValueMember())
								return null;
							return ((PropertyInfo)member.Member).GetValue(obj, null);
						}
						
						break;
					}
				case ExpressionType.Call:
					{
						var mc = (MethodCallExpression)expr;
						var arguments = mc.Arguments.Select(EvaluateExpression).ToArray();
						var instance  = mc.Object.EvaluateExpression();

						if (instance == null && mc.Method.IsNullableGetValueOrDefault())
							return null;
						
						return mc.Method.Invoke(instance, arguments);
					}
			}

			var value = Expression.Lambda(expr).CompileExpression().DynamicInvoke();
			return value;
		}

		public static Expression? GetArgumentByName(this MethodCallExpression methodCall, string parameterName)
		{
			var arguments = methodCall.Arguments;
			var parameters = methodCall.Method.GetParameters();
			for (int i = 0; i < parameters.Length; i++)
				if (parameters[i].Name == parameterName)
					return arguments[i];
			return default;
		}

		#endregion


		public static bool IsEvaluable(Expression? expression)
		{
			return expression?.NodeType switch
			{
				null                        => true,
				ExpressionType.Convert      => IsEvaluable(((UnaryExpression)expression).Operand),
				ExpressionType.Constant     => true,
				ExpressionType.MemberAccess => IsEvaluable(((MemberExpression)expression).Expression),
				_                           => false,
			};
		}

		/// <summary>
		/// Optimizes expression context by evaluating constants and simplifying boolean operations.
		/// </summary>
		/// <param name="expression">Expression to optimize.</param>
		/// <returns>Optimized expression.</returns>
		public static Expression? OptimizeExpression(this Expression? expression)
		{
			return _optimizeExpressionVisitor.Transform(expression);
		}

		private static readonly TransformInfoVisitor<object?> _optimizeExpressionVisitor = TransformInfoVisitor<object?>.Create(OptimizeExpressionTransformer);

		private static TransformInfo OptimizeExpressionTransformer(Expression e)
		{
			var newExpr = e;
			if (e is BinaryExpression binary)
			{
				var left  = OptimizeExpression(binary.Left)!;
				var right = OptimizeExpression(binary.Right)!;

				if (left.Type != binary.Left.Type)
					left = Expression.Convert(left, binary.Left.Type);

				if (right.Type != binary.Right.Type)
					right = Expression.Convert(right, binary.Right.Type);

				newExpr = binary.Update(left, OptimizeExpression(binary.Conversion) as LambdaExpression, right);
			}
			else if (e is UnaryExpression unaryExpression)
			{
				newExpr = unaryExpression.Update(OptimizeExpression(unaryExpression.Operand));
				if (newExpr.NodeType == ExpressionType.Convert && ((UnaryExpression)newExpr).Operand.NodeType == ExpressionType.Convert)
				{
					// remove double convert
					newExpr = Expression.Convert(
						((UnaryExpression)((UnaryExpression)newExpr).Operand).Operand, newExpr.Type);
				}
			}

			if (IsEvaluable(newExpr))
			{
				newExpr = newExpr.NodeType == ExpressionType.Constant
					? newExpr
					: Expression.Constant(EvaluateExpression(newExpr));
			}
			else
			{
				switch (newExpr)
				{
					case NewArrayExpression:
					{
						return new TransformInfo(newExpr, true);
					}
					case UnaryExpression unary when IsEvaluable(unary.Operand):
					{
						newExpr = Expression.Constant(EvaluateExpression(unary));
						break;
					}
					case MemberExpression me when me.Expression?.NodeType == ExpressionType.Constant:
					{
						newExpr = Expression.Constant(EvaluateExpression(me));
						break;
					}
					case BinaryExpression be when IsEvaluable(be.Left) && IsEvaluable(be.Right):
					{
						newExpr = Expression.Constant(EvaluateExpression(be));
						break;
					}
					case BinaryExpression be when be.NodeType == ExpressionType.AndAlso:
					{
						if (IsEvaluable(be.Left))
						{
							var leftBool = EvaluateExpression(be.Left) as bool?;
							if (leftBool == true)
								e = be.Right;
							else if (leftBool == false)
								newExpr = ExpressionHelper.FalseConstant;
						}
						else if (IsEvaluable(be.Right))
						{
							var rightBool = EvaluateExpression(be.Right) as bool?;
							if (rightBool == true)
								newExpr = be.Left;
							else if (rightBool == false)
								newExpr = ExpressionHelper.FalseConstant;
						}

						break;
					}
					case BinaryExpression be when be.NodeType == ExpressionType.OrElse:
					{
						if (IsEvaluable(be.Left))
						{
							var leftBool = EvaluateExpression(be.Left) as bool?;
							if (leftBool == false)
								newExpr = be.Right;
							else if (leftBool == true)
								newExpr = ExpressionHelper.TrueConstant;
						}
						else if (IsEvaluable(be.Right))
						{
							var rightBool = EvaluateExpression(be.Right) as bool?;
							if (rightBool == false)
								newExpr = be.Left;
							else if (rightBool == true)
								newExpr = ExpressionHelper.TrueConstant;
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
