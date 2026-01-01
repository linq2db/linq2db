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
			if (
				type.IsEnum
				|| type.TypeCode
					is TypeCode.Int16
					or TypeCode.Int32
					or TypeCode.Int64
					or TypeCode.UInt16
					or TypeCode.UInt32
					or TypeCode.UInt64
					or TypeCode.SByte
					or TypeCode.Byte
					or TypeCode.Decimal
					or TypeCode.Double
					or TypeCode.Single
					or TypeCode.Boolean
					or TypeCode.String
					or TypeCode.Char
			)
			{
				return true;
			}

			if (type.IsNullableType)
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
				case ExpressionType.ConvertChecked:
				case ExpressionType.Convert       :
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
		public static Expression? UnwrapAdjustType(this Expression? ex)
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
		public static Expression? UnwrapConvertToSelf(this Expression? ex)
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
			if (method.Method.Name != genericMethodInfo.Name)
				return false;

			if (!method.Method.IsGenericMethod)
				return method.Method == genericMethodInfo;

			return method.Method.GetGenericMethodDefinitionCached() == genericMethodInfo;
		}

		public static bool IsSameGenericMethod(this MethodCallExpression method, MethodInfo genericMethodInfo1, MethodInfo genericMethodInfo2)
		{
			return method.IsSameGenericMethod(genericMethodInfo1) || method.IsSameGenericMethod(genericMethodInfo2);
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
				|| (expr is DefaultExpression or DefaultValueExpression && expr.Type.IsNullableOrReferenceType());
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
				_depth                  = 0;
				_visitedExpressions     = default!;
				_allowEvaluation        = false;
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
				switch (newExpr.NodeType)
				{
					case ExpressionType.AndAlso:
						return OptimizeAndAlso((BinaryExpression)newExpr);
					
					case ExpressionType.OrElse:
						return OptimizeOrElse((BinaryExpression)newExpr);
				}

				return newExpr;
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
