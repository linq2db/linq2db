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
		#region Path
		public static void Path<TContext>(this Expression expr, Expression path, TContext context, Action<TContext, Expression, Expression> func)
		{
			new PathVisitor<TContext>(context, path, func).Path(expr);
		}
		#endregion

		#region Helpers
		extension(Expression? ex)
		{
			[return: NotNullIfNotNull(nameof(ex))]
			public LambdaExpression? UnwrapLambda()
				=> (LambdaExpression?)ex.Unwrap();

			[return: NotNullIfNotNull(nameof(ex))]
			public Expression? Unwrap()
			{
				if (ex == null)
					return null;

				switch (ex.NodeType)
				{
					case ExpressionType.Quote:
					case ExpressionType.ConvertChecked:
					case ExpressionType.Convert:
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
			public Expression? UnwrapUnary()
			{
				if (ex == null)
					return null;

				while (ex.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.TypeAs)
					ex = ((UnaryExpression)ex).Operand;

				return ex;
			}

			[return: NotNullIfNotNull(nameof(ex))]
			public Expression? UnwrapConvert()
			{
				if (ex == null)
					return null;

				switch (ex.NodeType)
				{
					case ExpressionType.ConvertChecked:
					case ExpressionType.Convert:
					{
						var unaryExpression = (UnaryExpression)ex;
						if (unaryExpression.Method == null)
							return unaryExpression.Operand.UnwrapConvert();
						break;
					}
					case ExpressionType.Extension
						when ex is SqlAdjustTypeExpression adjustType:
					{
						return adjustType.Expression.UnwrapConvert();
					}
				}

				return ex;
			}

			[return: NotNullIfNotNull(nameof(ex))]
			public Expression? UnwrapAdjustType()
			{
				if (ex == null)
					return null;

				switch (ex.NodeType)
				{
					case ExpressionType.Extension
						when ex is SqlAdjustTypeExpression adjustType:
					{
						return adjustType.Expression.UnwrapAdjustType();
					}
				}

				return ex;
			}

			[return: NotNullIfNotNull(nameof(ex))]
			public Expression? UnwrapConvertToSelf()
			{
				if (ex == null)
					return null;

				switch (ex.NodeType)
				{
					case ExpressionType.ConvertChecked:
					case ExpressionType.Convert:
					{
						var unaryExpression = (UnaryExpression)ex;
						if (unaryExpression.Method == null
							&& unaryExpression.Type.IsSameOrParentOf(unaryExpression.Operand.Type))
						{
							return unaryExpression.Operand.UnwrapConvertToSelf();
						}

						break;
					}
				}

				return ex;
			}

			[return: NotNullIfNotNull(nameof(ex))]
			public Expression? UnwrapConvertToObject()
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
			public Expression? UnwrapConvertToNotObject()
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
		}

		extension (Expression expression)
		{
			public Expression SkipMethodChain(MappingSchema mappingSchema, out bool isQueryable)
			{
				return Sql.ExtensionAttribute.ExcludeExtensionChain(mappingSchema, expression, out isQueryable);
			}

			public Dictionary<Expression, Expression> GetExpressionAccessors(Expression path)
			{
				var accessors = new Dictionary<Expression,Expression>();

				expression.Path(path, accessors, static (accessors, e, p) =>
				{
					switch (e.NodeType)
					{
						case ExpressionType.Call:
						case ExpressionType.MemberAccess:
						case ExpressionType.New:
							accessors.TryAdd(e, p);
							break;

						case ExpressionType.Constant:
							accessors.TryAdd(e, Expression.Property(p, ReflectionHelper.Constant.Value));
							break;

						case ExpressionType.ConvertChecked:
						case ExpressionType.Convert:
							var ue = (UnaryExpression)e;

							switch (ue.Operand.NodeType)
							{
								case ExpressionType.Call:
								case ExpressionType.MemberAccess:
								case ExpressionType.New:
								case ExpressionType.Constant:
									accessors.TryAdd(e, p);
									break;
							}

							break;
					}
				});

				return accessors;
			}

			public bool IsNullValue =>
				expression is
					ConstantExpression { Value: null }
					or ((DefaultExpression or DefaultValueExpression) and { Type.IsNullableOrReferenceType: true });
		}

		extension(MethodCallExpression method)
		{
			public bool IsQueryable
			{
				get
				{
					var type = method.Method.DeclaringType;

					return
						type == typeof(Queryable) ||
						type == typeof(Enumerable) ||
						type == typeof(LinqExtensions) ||
						type == typeof(LinqInternalExtensions) ||
						type == typeof(DataExtensions) ||
						type == typeof(TableExtensions) ||
						MemberCache.GetMemberInfo(method.Method).IsQueryable;
				}
			}

			public bool IsOrderByMethodName =>
				method.Method.Name is
					nameof(Queryable.OrderBy)
					or nameof(Queryable.OrderByDescending)
					or nameof(Queryable.ThenBy)
					or nameof(Queryable.ThenByDescending);

			public bool IsAllowedAggregationMethodName =>
				method is { IsOrderByMethodName: true } or
				{
					Method.Name:
						nameof(Queryable.Select)
						or nameof(Queryable.Where)
						or nameof(Queryable.Distinct),
				};

			public bool IsAsyncExtension =>
				method.Method.DeclaringType == typeof(AsyncExtensions);

			public bool IsExtensionMethod(MappingSchema mapping)
			{
				return mapping.HasAttribute<Sql.ExtensionAttribute>(method.Method.ReflectedType!, method.Method);
			}

			public bool IsSameGenericMethod(MethodInfo genericMethodInfo)
			{
				if (!string.Equals(method.Method.Name, genericMethodInfo.Name, StringComparison.Ordinal))
					return false;

				if (!method.Method.IsGenericMethod)
					return method.Method == genericMethodInfo;

				return method.Method.GetGenericMethodDefinitionCached() == genericMethodInfo;
			}

			public bool IsSameGenericMethod(MethodInfo genericMethodInfo1, MethodInfo genericMethodInfo2)
			{
				return method.IsSameGenericMethod(genericMethodInfo1) || method.IsSameGenericMethod(genericMethodInfo2);
			}

			public bool IsSameGenericMethod(MethodInfo[] genericMethodInfo)
			{
				if (!method.Method.IsGenericMethod)
					return false;

				var         mi = method.Method;
				MethodInfo? gd = null;

				foreach (var current in genericMethodInfo)
				{
					if (string.Equals(current.Name, mi.Name, StringComparison.Ordinal))
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

			public bool IsAssociation(MappingSchema mappingSchema)
			{
				return IsAssociation(method.Method, mappingSchema);
			}

			public Expression? GetArgumentByName(string parameterName)
			{
				var arguments = method.Arguments;
				var parameters = method.Method.GetParameters();

				for (var i = 0; i < parameters.Length; i++)
					if (string.Equals(parameters[i].Name, parameterName, StringComparison.Ordinal))
						return arguments[i];

				return default;
			}
		}

		public static bool IsAssociation(MemberInfo member, MappingSchema mappingSchema)
		{
			return mappingSchema.GetAttribute<AssociationAttribute>(member.DeclaringType!, member) != null;
		}

		public static bool IsAssociation(this MemberExpression memberExpression, MappingSchema mappingSchema)
		{
			return IsAssociation(memberExpression.Member, mappingSchema);
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

		#endregion

		/// <summary>
		/// Optimizes expression context by evaluating constants and simplifying boolean operations.
		/// Uses visitor pattern to prevent stack overflow in deeply nested expressions.
		/// </summary>
		/// <param name="expression">Expression to optimize.</param>
		/// <param name="canBeEvaluatedOnClient">Function to determine if an expression can be evaluated on the client side.</param>
		/// <returns>Optimized expression.</returns>
		[return: NotNullIfNotNull(nameof(expression))]
		public static Expression? OptimizeExpression(this Expression? expression, Func<Expression, bool> canBeEvaluatedOnClient)
		{
			if (expression == null)
				return null;

			using var visitor = ExpressionOptimizerVisitor.Pool.Allocate();
			visitor.Value.Initialize(canBeEvaluatedOnClient);

			var optimized = visitor.Value.Visit(expression);
			return optimized;
		}

		sealed class ExpressionOptimizerVisitor : ExpressionVisitorBase
		{
			public static readonly ObjectPool<ExpressionOptimizerVisitor> Pool = new(() => new ExpressionOptimizerVisitor(), v => v.Cleanup(), 100);

			const int              MaxDepth               = 100;

			Func<Expression, bool> _canBeEvaluatedOnClient = default!;
			int                    _depth;
			bool                   _allowEvaluation;

			// Track expressions being optimized to detect circular references
			private HashSet<Expression> _visitedExpressions = default!;

			public void Initialize(Func<Expression, bool> canBeEvaluatedOnClient)
			{
				Cleanup();
				_canBeEvaluatedOnClient = canBeEvaluatedOnClient;
				_depth = 0;
				_visitedExpressions = new HashSet<Expression>(Utils.ObjectReferenceEqualityComparer<Expression>.Default);
			}

			public override void Cleanup()
			{
				base.Cleanup();
				_canBeEvaluatedOnClient = default!;
				_depth = 0;
				_visitedExpressions = default!;
				_allowEvaluation = false;
			}

			bool IsEvaluable(Expression expr)
			{
				if (!_allowEvaluation)
					return false;

				return _canBeEvaluatedOnClient(expr);
			}

			[return: NotNullIfNotNull(nameof(node))]
			public override Expression? Visit(Expression? node)
			{
				if (node == null)
					return null;

				// Depth check to prevent stack overflow
				if (_depth >= MaxDepth)
					return node;

				// Circular reference detection
				if (!_visitedExpressions.Add(node))
					return node;

				_depth++;

				var newNode = base.Visit(node);

				_depth--;
				_visitedExpressions.Remove(node);

				return newNode;
			}

			protected override Expression VisitLambda<T>(Expression<T> node)
			{
				var saveAllowEvaluation = _allowEvaluation;

				_allowEvaluation = true;
				var newNode = base.VisitLambda(node);
				_allowEvaluation = saveAllowEvaluation;
				return newNode;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				var saveAllowEvaluation = _allowEvaluation;

				_allowEvaluation = false;
				var newNode = base.VisitMethodCall(node);
				_allowEvaluation = saveAllowEvaluation;

				return newNode;
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
				var left  = Visit(node.Left);
				var right = Visit(node.Right);

				// Ensure type compatibility
				if (left.Type != node.Left.Type)
					left = Expression.Convert(left, node.Left.Type);

				if (right.Type != node.Right.Type)
					right = Expression.Convert(right, node.Right.Type);

				var conversion = Visit(node.Conversion) as LambdaExpression;
				var newExpr = node.Update(left, conversion, right);

				// Try to evaluate if both sides are evaluable
				if (IsEvaluable(newExpr))
				{
					return Expression.Constant(newExpr.EvaluateExpression());
				}

				// Optimize boolean operations
				return newExpr.NodeType switch
				{
					ExpressionType.AndAlso => OptimizeAndAlso((BinaryExpression)newExpr),
					ExpressionType.OrElse => OptimizeOrElse((BinaryExpression)newExpr),
					_ => newExpr,
				};
			}

			protected override Expression VisitUnary(UnaryExpression node)
			{
				var operand = Visit(node.Operand);
				var newExpr = node.Update(operand);

				// Remove double convert: Convert(Convert(x, T1), T2) => Convert(x, T2)
				if (newExpr.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked
					&& operand.NodeType is ExpressionType.Convert or ExpressionType.ConvertChecked)
				{
					var innerOperand = ((UnaryExpression)operand).Operand;
					return Visit(Expression.Convert(innerOperand, newExpr.Type));
				}

				// Try to evaluate if operand is evaluable
				if (IsEvaluable(newExpr))
				{
					return Expression.Constant(newExpr.EvaluateExpression());
				}

				return newExpr;
			}

			protected override Expression VisitConditional(ConditionalExpression node)
			{
				var test    = Visit(node.Test);
				var ifTrue  = Visit(node.IfTrue);
				var ifFalse = Visit(node.IfFalse);

				if (IsEvaluable(test))
				{
					var testValue = test.EvaluateExpression() as bool?;
					if (testValue == true)
						return ifTrue;
					if (testValue == false)
						return ifFalse;
				}

				return node.Update(test, ifTrue, ifFalse);
			}

			private Expression OptimizeAndAlso(BinaryExpression node)
			{
				if (IsEvaluable(node.Left))
				{
					var leftBool = node.Left.EvaluateExpression() as bool?;
					if (leftBool == true)
						return node.Right;
					if (leftBool == false)
						return ExpressionInstances.False;
				}

				if (IsEvaluable(node.Right))
				{
					var rightBool = node.Right.EvaluateExpression() as bool?;
					if (rightBool == true)
						return node.Left;
					if (rightBool == false)
						return ExpressionInstances.False;
				}

				return node;
			}

			private Expression OptimizeOrElse(BinaryExpression node)
			{
				if (IsEvaluable(node.Left))
				{
					var leftBool = node.Left.EvaluateExpression() as bool?;
					if (leftBool == false)
						return node.Right;
					if (leftBool == true)
						return ExpressionInstances.True;
				}

				if (IsEvaluable(node.Right))
				{
					var rightBool = node.Right.EvaluateExpression() as bool?;
					if (rightBool == false)
						return node.Left;
					if (rightBool == true)
						return ExpressionInstances.True;
				}

				return node;
			}
		}
	}
}
