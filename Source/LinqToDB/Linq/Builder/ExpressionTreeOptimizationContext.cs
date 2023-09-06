using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Data;
	using LinqToDB.Expressions;
	using Extensions;
	using Mapping;
	using SqlQuery;
	using Reflection;
	using LinqToDB.Common.Internal;

	public class ExpressionTreeOptimizationContext
	{
		HashSet<Expression>? _visitedExpressions;

		public IDataContext  DataContext   { get; }
		public MappingSchema MappingSchema { get; }

		public ExpressionTreeOptimizationContext(IDataContext dataContext)
		{
			DataContext = dataContext;
			MappingSchema = dataContext.MappingSchema;

			_expandExpressionTransformer =
				TransformVisitor<ExpressionTreeOptimizationContext>.Create(this,
					static (ctx, expr) => ctx.ExpandExpressionTransformer(expr));

			_optimizeExpressionTreeTransformer =
				TransformInfoVisitor<ExpressionTreeOptimizationContext>.Create(this,
					static (ctx, expr) => ctx.OptimizeExpressionTreeTransformer(expr, false));

			_optimizeExpressionTreeTransformerInProjection =
				TransformInfoVisitor<ExpressionTreeOptimizationContext>.Create(this,
					static (ctx, expr) => ctx.OptimizeExpressionTreeTransformer(expr, true));
		}

		private EqualsToVisitor.EqualsToInfo? _equalsToContextFalse;
		private EqualsToVisitor.EqualsToInfo? _equalsToContextTrue;
		internal EqualsToVisitor.EqualsToInfo GetSimpleEqualsToContext(bool compareConstantValues)
		{
			if (compareConstantValues)
			{
				if (_equalsToContextTrue == null)
					_equalsToContextTrue = EqualsToVisitor.PrepareEqualsInfo(DataContext, compareConstantValues: compareConstantValues);
				else
					_equalsToContextTrue.Reset();
				return _equalsToContextTrue;
			}
			else
			{
				if (_equalsToContextFalse == null)
					_equalsToContextFalse = EqualsToVisitor.PrepareEqualsInfo(DataContext, compareConstantValues: compareConstantValues);
				else
					_equalsToContextFalse.Reset();
				return _equalsToContextFalse;
			}
		}

		public void ClearVisitedCache()
		{
			_visitedExpressions?.Clear();
		}

		public static Expression AggregateExpression(Expression expression)
		{
			return _aggregateExpressionTransformer.Transform(expression);
		}

		private static readonly TransformVisitor<object?> _aggregateExpressionTransformer = TransformVisitor<object?>.Create(AggregateExpressionTransformer);
		private static Expression AggregateExpressionTransformer(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.Or:
				case ExpressionType.And:
				case ExpressionType.OrElse:
				case ExpressionType.AndAlso:
				{
					var stack  = new Stack<Expression>();
					var items  = new List<Expression>();
					var binary = (BinaryExpression) expr;

					stack.Push(binary.Right);
					stack.Push(binary.Left);
					while (stack.Count > 0)
					{
						var item = stack.Pop();
						if (item.NodeType == expr.NodeType)
						{
							binary = (BinaryExpression)item;
							stack.Push(binary.Right);
							stack.Push(binary.Left);
						}
						else
							items.Add(item);
					}

					if (items.Count > 3)
					{
						// having N items will lead to NxM recursive calls in expression visitors and
						// will result in stack overflow on relatively small numbers (~1000 items).
						// To fix it we will rebalance condition tree here which will result in
						// LOG2(N)*M recursive calls, or 10*M calls for 1000 items.
						//
						// E.g. we have condition A OR B OR C OR D OR E
						// as an expression tree it represented as tree with depth 5
						//   OR
						// A    OR
						//    B    OR
						//       C    OR
						//          D    E
						// for rebalanced tree it will have depth 4
						//                  OR
						//        OR
						//   OR        OR        OR
						// A    B    C    D    E    F
						// Not much on small numbers, but huge improvement on bigger numbers
						while (items.Count != 1)
						{
							items = CompactTree(items, expr.NodeType);
						}

						return items[0];
					}
					break;
				}
			}

			return expr;
		}

		static List<Expression> CompactTree(List<Expression> items, ExpressionType nodeType)
		{
			var result = new List<Expression>();

			// traverse list from left to right to preserve calculation order
			for (var i = 0; i < items.Count; i += 2)
			{
				if (i + 1 == items.Count)
				{
					// last non-paired item
					result.Add(items[i]);
				}
				else
				{
					result.Add(Expression.MakeBinary(nodeType, items[i], items[i + 1]));
				}
			}

			return result;
		}

		public Expression ExpandQueryableMethods(Expression expression)
		{
			var result = (_expandQueryableMethodsTransformer ??= TransformInfoVisitor<ExpressionTreeOptimizationContext>.Create(this, static (ctx, e) => ctx.ExpandQueryableMethodsTransformer(e)))
				.Transform(expression);

			return result;
		}

		private TransformInfoVisitor<ExpressionTreeOptimizationContext>? _expandQueryableMethodsTransformer;

		private TransformInfo ExpandQueryableMethodsTransformer(Expression expr)
		{
			if (expr.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expr;
				if (typeof(IQueryable<>).IsSameOrParentOf(mc.Type)
					&& !mc.IsQueryable(false)
					&& CanBeCompiled(mc, false))
				{
					var queryable = (IQueryable)mc.EvaluateExpression()!;

					if (!queryable.Expression.EqualsTo(mc, GetSimpleEqualsToContext(compareConstantValues: true)))
						return new TransformInfo(queryable.Expression, false, true);
				}
			}

			return new TransformInfo(expr);
		}

		public Expression ExpandExpression(Expression expression)
		{
			expression = AggregateExpression(expression);

			var result = _expandExpressionTransformer.Transform(expression);

			return result;
		}

		public Expression OptimizeExpressionTree(Expression expression, bool inProjection)
		{
			var transformer = inProjection
				? _optimizeExpressionTreeTransformerInProjection
				: _optimizeExpressionTreeTransformer;

			var result = expression;
			do
			{
				var prevExpression = result;
				result = transformer.Transform(prevExpression);
				if (prevExpression == result)
					break;
			} while (true);

			return result;
		}


		private TransformVisitor<ExpressionTreeOptimizationContext> _expandExpressionTransformer;

		private bool _expressionDependsOnParameters;

		public bool IsDependsOnParameters() => _expressionDependsOnParameters;

		public Expression ExpandExpressionTransformer(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)expr;

					List<Expression>? newArgs = null;
					for (var index = 0; index < mc.Arguments.Count; index++)
					{
						var arg = mc.Arguments[index];
						Expression? newArg = null;
						if (typeof(LambdaExpression).IsSameOrParentOf(arg.Type))
						{
							var argUnwrapped = arg.Unwrap();
							if (argUnwrapped.NodeType == ExpressionType.MemberAccess ||
									argUnwrapped.NodeType == ExpressionType.Call)
							{
								if (argUnwrapped.EvaluateExpression() is LambdaExpression lambda)
									newArg = ExpandExpression(lambda);
							}
						}

						if (newArg == null)
							newArgs?.Add(arg);
						else
						{
							newArgs ??= new List<Expression>(mc.Arguments.Take(index));
							newArgs.Add(newArg);
						}
					}

					if (newArgs != null)
					{
						mc = mc.Update(mc.Object, newArgs);
					}


					if (mc.Method.Name == "Compile" && typeof(LambdaExpression).IsSameOrParentOf(mc.Method.DeclaringType!))
					{
						if (mc.Object.EvaluateExpression() is LambdaExpression lambda)
						{
							return ExpandExpression(lambda);
						}
					}

					return mc;
				}

				case ExpressionType.Invoke:
				{
					var invocation = (InvocationExpression)expr;
					if (invocation.Expression.NodeType == ExpressionType.Call)
					{
						var mc = (MethodCallExpression)invocation.Expression;
						if (mc.Method.Name == "Compile" &&
								typeof(LambdaExpression).IsSameOrParentOf(mc.Method.DeclaringType!))
						{
							if (mc.Object.EvaluateExpression() is LambdaExpression lambda)
							{
								var newBody = lambda.Body;
								if (invocation.Arguments.Count > 0)
								{
									var map = new Dictionary<Expression, Expression>();
									for (int i = 0; i < invocation.Arguments.Count; i++)
										map.Add(lambda.Parameters[i], invocation.Arguments[i]);

									newBody = lambda.Body.Transform(map, static (map, se) =>
									{
										if (se.NodeType == ExpressionType.Parameter &&
												map.TryGetValue(se, out var newExpr))
											return newExpr;
										return se;
									});
								}

								return ExpandExpression(newBody);
							}
						}
					}
					break;
				}

				case ExpressionType.Conditional:
				{
					var conditional = (ConditionalExpression)expr;
					if (CanBeCompiled(conditional.Test, false))
					{
						var testValue = conditional.Test.EvaluateExpression();
						if (testValue is bool test)
						{
							_expressionDependsOnParameters = true;
							return test ? ExpandExpression(conditional.IfTrue) : ExpandExpression(conditional.IfFalse);
						}
					}
					break;
				}
			}

			return expr;
		}

		TransformInfoVisitor<ExpressionTreeOptimizationContext> _optimizeExpressionTreeTransformer;
		TransformInfoVisitor<ExpressionTreeOptimizationContext> _optimizeExpressionTreeTransformerInProjection;

		public TransformInfo OptimizeExpressionTreeTransformer(Expression expr, bool inProjection)
		{
			bool IsEqualConstants(Expression left, Expression right)
			{
				object? valueLeft;
				object? valueRight;

				if (left is ConstantExpression leftConst)
					valueLeft = leftConst.Value;
				else if (left is SqlPlaceholderExpression { Sql: SqlValue leftSqlValue })
					valueLeft = leftSqlValue.Value;
				else
					valueLeft = MappingSchema.GetDefaultValue(left.Type);

				if (right is ConstantExpression rightConst)
					valueRight = rightConst.Value;
				else if (right is SqlPlaceholderExpression { Sql: SqlValue rightSqlValue })
					valueRight = rightSqlValue.Value;
				else
					valueRight = MappingSchema.GetDefaultValue(right.Type);

				return Equals(valueLeft, valueRight);
			}

			bool IsComparable(Expression testExpr)
			{
				return testExpr.NodeType == ExpressionType.Constant || testExpr.NodeType == ExpressionType.Default ||
				       testExpr is DefaultValueExpression;
			}

			bool IsEqualValues(Expression left, Expression right)
			{
				if (IsComparable(left) && IsComparable(right))
				{
					return IsEqualConstants(left, right);
				}

				return false;
			}

			bool? IsNull(Expression testExpr)
			{
				if (testExpr.Type.IsValueType)
					return null;

				if (testExpr.NodeType == ExpressionType.Constant)
				{
					return ((ConstantExpression)testExpr).Value == null;
				}
				if (testExpr.NodeType == ExpressionType.Default)
				{
					return true;
				}
				if (testExpr.NodeType == ExpressionType.New || testExpr.NodeType == ExpressionType.MemberInit)
				{
					return false;
				}
				if (testExpr is DefaultValueExpression)
				{
					return true;
				}

				return null;
			}

			switch (expr.NodeType)
			{
				case ExpressionType.Conditional:
				{
					var conditional = (ConditionalExpression)expr;

					if (conditional.Test is ConstantExpression constExpr && constExpr.Value is bool b)
					{
						return new TransformInfo(b ? conditional.IfTrue : conditional.IfFalse);
					}

					break;
				}

				case ExpressionType.Not:
				{
					var notExpression = (UnaryExpression)expr;

					if (notExpression.Operand.NodeType == ExpressionType.Not)
						return new TransformInfo(((UnaryExpression)notExpression.Operand).Operand, false, true);

					if (notExpression.Operand.NodeType == ExpressionType.Extension &&
					    notExpression.Operand is SqlReaderIsNullExpression isnull)
					{
						return new TransformInfo(isnull.WithIsNot(!isnull.IsNot), false, true);
					}

					if (notExpression.Operand.NodeType == ExpressionType.Equal)
					{
						var equal = (BinaryExpression)notExpression.Operand;
						if (equal.Left.NodeType  != ExpressionType.Convert &&
						    equal.Right.NodeType != ExpressionType.Convert)
						{
							return new TransformInfo(Expression.NotEqual(equal.Left, equal.Right), false, true);
						}
					}

					if (notExpression.Operand.NodeType == ExpressionType.NotEqual)
					{
						var notEqual = (BinaryExpression)notExpression.Operand;
						if (notEqual.Left.NodeType  != ExpressionType.Convert &&
						    notEqual.Right.NodeType != ExpressionType.Convert)
						{
							return new TransformInfo(Expression.Equal(notEqual.Left, notEqual.Right), false, true);
						}
					}
					break;
				}

				case ExpressionType.Equal:
				{
					var binary = (BinaryExpression)expr;

					if (binary.Left is ConditionalExpression leftCond)
					{
						if (IsEqualValues(binary.Right, leftCond.IfTrue))
						{
							return new TransformInfo(leftCond.Test, false, true);
						}

						if (IsEqualValues(binary.Right, leftCond.IfFalse))
						{
							return new TransformInfo(Expression.Not(leftCond.Test), false, true);
						}
					}
					else if (binary.Right is ConditionalExpression rightCond)
					{
						if (IsEqualValues(binary.Left, rightCond.IfTrue))
						{
							return new TransformInfo(rightCond.Test, false, true);
						}

						if (IsEqualValues(binary.Left, rightCond.IfFalse))
						{
							return new TransformInfo(Expression.Not(rightCond.Test), false, true);
						}
					}

					if (inProjection)
					{
						var isNullLeft  = IsNull(binary.Left);
						var isNullRight = IsNull(binary.Right);

						if (isNullLeft != null && isNullRight != null)
						{
							return new TransformInfo(Expression.Constant(isNullLeft == isNullRight));
						}
					}

					break;
				}

				case ExpressionType.NotEqual:
				{
					var binary = (BinaryExpression)expr;

					if (binary.Left is ConditionalExpression leftCond)
					{
						if (IsEqualValues(binary.Right, leftCond.IfTrue))
						{
							return new TransformInfo(Expression.Not(leftCond.Test), false, true);
						}

						if (IsEqualValues(binary.Right, leftCond.IfFalse))
						{
							return new TransformInfo(leftCond.Test, false, true);
						}
					}
					else if (binary.Right is ConditionalExpression rightCond)
					{
						if (IsEqualValues(binary.Left, rightCond.IfTrue))
						{
							return new TransformInfo(Expression.Not(rightCond.Test), false, true);
						}

						if (IsEqualValues(binary.Left, rightCond.IfFalse))
						{
							return new TransformInfo(rightCond.Test, false, true);
						}
					}

					if (inProjection)
					{
						var isNullLeft  = IsNull(binary.Left);
						var isNullRight = IsNull(binary.Right);

						if (isNullLeft != null && isNullRight != null)
						{
							return new TransformInfo(Expression.Constant(isNullLeft != isNullRight));
						}
					}	

					break;
				}
			}

			return new TransformInfo(expr);
		}

		#region IsServerSideOnly

		class IsServerSideOnlyCheckVisitor : ExpressionVisitorBase
		{
			bool          _isServerSideOnly;
			MappingSchema _mappingSchema = default!;

			public bool IsServerSideOnly(Expression expression, MappingSchema mappingSchema)
			{
				Cleanup();

				_mappingSchema = mappingSchema;

				_ = Visit(expression);

				return _isServerSideOnly;
			}

			public override void Cleanup()
			{
				_isServerSideOnly = false;
				_mappingSchema    = default!;

				base.Cleanup();
			}

			public override Expression? Visit(Expression? node)
			{
				if (_isServerSideOnly)
					return node;

				return base.Visit(node);
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				var l = Expressions.ConvertMember(_mappingSchema, node.Expression?.Type, node.Member);

				if (l != null)
				{
					Visit(l.Body);
					return node;
				}

				var attr = node.Member.GetExpressionAttribute(_mappingSchema);
				if (attr != null && attr.ServerSideOnly)
				{
					_isServerSideOnly = true;
					return node;
				}

				return base.VisitMember(node);
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				var l = Expressions.ConvertMember(_mappingSchema, node.Object?.Type, node.Method);

				if (l != null)
				{
					Visit(l.Body);
					return node;
				}

				var attr = node.Method.GetExpressionAttribute(_mappingSchema);
				if (attr?.ServerSideOnly == true)
					_isServerSideOnly = true;

				return base.VisitMethodCall(node);
			}
		}

		static ObjectPool<IsServerSideOnlyCheckVisitor> _serverSideOnlyVisitorPool  = new(() => new IsServerSideOnlyCheckVisitor(), v => v.Cleanup(), 100);
		static ObjectPool<CanBeCompiledCheckVisitor> _canBeCompiledCheckVisitorPool = new(() => new CanBeCompiledCheckVisitor(), v => v.Cleanup(), 100);

		Dictionary<Expression, bool>? _isServerSideOnlyCache;

		private FindVisitor<ExpressionTreeOptimizationContext>? _isServerSideOnlyVisitor;

		public bool IsServerSideOnly(Expression expr, bool inProjection)
		{
			if (_isServerSideOnlyCache != null && _isServerSideOnlyCache.TryGetValue(expr, out var result))
				return result;

			if (expr.Type == typeof(Sql.SqlID))
			{
				result = true;
			}
			else
			{
				using var visitor = _serverSideOnlyVisitorPool.Allocate();
				result = visitor.Value.IsServerSideOnly(expr, MappingSchema);
			}

			(_isServerSideOnlyCache ??= new()).Add(expr, result);

			return result;
		}

		#endregion

		#region CanBeCompiled

		class CanBeCompiledCheckVisitor : ExpressionVisitorBase
		{
			bool _canBeCompiled;

			bool _inProjection;
			bool _inMethod;

			Stack<ReadOnlyCollection<ParameterExpression>>? _allowedParameters;

			ExpressionTreeOptimizationContext _optimizationContext = default!;

			public override void Cleanup()
			{
				_canBeCompiled       = true;
				_inMethod            = false;
				_optimizationContext = default!;
				_inProjection        = false;

				_allowedParameters?.Clear();

				base.Cleanup();
			}

			public override Expression? Visit(Expression? node)
			{
				if (!_canBeCompiled)
					return node;

				return base.Visit(node);
			}

			protected override Expression VisitLambda<T>(Expression<T> node)
			{
				if (!_inMethod)
				{
					_canBeCompiled = false;
					return node;
				}

				_allowedParameters ??= new();

				_allowedParameters.Push(node.Parameters);

				_ = base.VisitLambda(node);

				_allowedParameters.Pop();

				return node;
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				if (node == ExpressionBuilder.ParametersParam)
					return node;

				if (node == ExpressionConstants.DataContextParam)
				{
					if (_inMethod)
						_canBeCompiled = false;
				}
				else
				{
					_canBeCompiled = _allowedParameters != null && _allowedParameters.Any(ps => ps.Contains(node));
				}

				return node;
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				if (typeof(IDataContext).IsSameOrParentOf(node.Type))
				{
					_canBeCompiled = false;
					return node;
				}

				if (node.Expression != null && typeof(IDataContext).IsSameOrParentOf(node.Expression.Type) && typeof(IQueryable<>).IsSameOrParentOf(node.Type))
				{
					_canBeCompiled = false;
					return node;
				}

				_ = base.VisitMember(node);

				if (!_canBeCompiled)
				{
					return node;
				}

				if (_optimizationContext.IsServerSideOnly(node, false))
					_canBeCompiled = false;

				return node;
			}

			internal override Expression VisitContextRefExpression(ContextRefExpression node)
			{
				_canBeCompiled = false;
				return node;
			}

			internal override Expression VisitSqlErrorExpression(SqlErrorExpression node)
			{
				_canBeCompiled = false;
				return node;
			}

			public override Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
			{
				if (!_inProjection)
					_canBeCompiled = false;
				return node;
			}

			internal override Expression VisitSqlGenericParamAccessExpression(SqlGenericParamAccessExpression node)
			{
				_canBeCompiled = false;
				return node;
			}

			internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
			{
				_canBeCompiled = false;
				return node;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (typeof(IQueryable<>).IsSameOrParentOf(node.Type))
				{
					if (node.Arguments.Any(a => typeof(IDataContext).IsSameOrParentOf(a.Type)) || 
					    node.Object != null && typeof(IDataContext).IsSameOrParentOf(node.Object.Type))
					{
						_canBeCompiled = false;
						return node;
					}
				}

				if (!_canBeCompiled)
				{
					return node;
				}

				if (_optimizationContext.IsServerSideOnly(node, false))
					_canBeCompiled = false;

				var save = _inMethod;
				_inMethod = true;

				base.VisitMethodCall(node);

				_inMethod = save;

				return node;
			}

			internal override SqlGenericConstructorExpression.Assignment VisitSqlGenericAssignment(SqlGenericConstructorExpression.Assignment assignment)
			{
				_canBeCompiled = false;
				return assignment;
			}

			internal override SqlGenericConstructorExpression.Parameter VisitSqlGenericParameter(SqlGenericConstructorExpression.Parameter parameter)
			{
				_canBeCompiled = false;
				return parameter;
			}

			internal override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
			{
				_canBeCompiled = false;
				return node;
			}

			public bool CanBeCompiled(Expression  expression,
				ExpressionTreeOptimizationContext optimizationContext, bool inProjection)
			{
				Cleanup();

				_optimizationContext = optimizationContext;
				_inProjection        = inProjection;

				_ = Visit(expression);

				return _canBeCompiled;
			}
		}

		public bool CanBeCompiled(Expression expr, bool inProjection)
		{
			var visitor = _canBeCompiledCheckVisitorPool.Allocate();

			var result = visitor.Value.CanBeCompiled(expr, this, inProjection);
			
			return result;
		}

		#endregion

		#region CanBeConstant

		Expression? _lastExpr1;
		bool        _lastResult1;

		private FindVisitor<ExpressionTreeOptimizationContext>? _canBeConstantVisitor;
		public bool CanBeConstant(Expression expr)
		{
			if (_lastExpr1 == expr)
				return _lastResult1;

			var result = null == (_canBeConstantFindVisitor ??= FindVisitor<ExpressionTreeOptimizationContext>.Create(this, static (ctx, e) => ctx.CanBeConstantFind(e))).Find(expr);

			_lastExpr1 = expr;
			return _lastResult1 = result;
		}

		private FindVisitor<ExpressionTreeOptimizationContext>? _canBeConstantFindVisitor;
		private bool CanBeConstantFind(Expression ex)
		{
			if (ex is BinaryExpression || ex is UnaryExpression /*|| ex.NodeType == ExpressionType.Convert*/)
				return false;

			if (MappingSchema.GetConvertExpression(ex.Type, typeof(DataParameter), false, false) != null)
				return true;

			if (ex.Type == typeof(void))
				return true;

			switch (ex.NodeType)
			{
				case ExpressionType.Default : return false;
				case ExpressionType.Constant:
				{
					var c = (ConstantExpression)ex;

					if (c.Value == null || ex.Type.IsConstantable(false))
						return false;

					break;
				}

				case ExpressionType.MemberAccess:
				{
					var ma = (MemberExpression)ex;

					var l = Expressions.ConvertMember(MappingSchema, ma.Expression?.Type, ma.Member);

					if (l != null)
						return (_canBeConstantVisitor ??= FindVisitor<ExpressionTreeOptimizationContext>.Create(this, static (ctx, e) => ctx.CanBeConstant(e))).Find(l.Body.Unwrap()) == null;

					if (ma.Member.DeclaringType!.IsConstantable(false) || ma.Member.IsNullableValueMember())
						return false;

					break;
				}

				case ExpressionType.Call:
				{
					var mc = (MethodCallExpression)ex;

					if (mc.Method.DeclaringType!.IsConstantable(false) || mc.Method.DeclaringType == typeof(object))
						return false;

					var attr = mc.Method.GetExpressionAttribute(MappingSchema);

					if (attr != null && !attr.ServerSideOnly)
						return false;

					break;
				}
			}

			return true;
		}

		#endregion


		static Expression AliasCall(Expression expression, string alias)
		{
			return Expression.Call(Methods.LinqToDB.SqlExt.Alias.MakeGenericMethod(expression.Type), expression,
				Expression.Constant(alias));
		}

		private TransformInfoVisitor<ExpressionTreeOptimizationContext>? _exposeExpressionTransformer;

		private TransformInfo ExposeExpressionTransformer(Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.ArrayLength:
				{
					var ue = (UnaryExpression)expr;
					var ll = Expressions.ConvertMember(MappingSchema, ue.Operand?.Type, ue.Operand!.Type.GetProperty(nameof(Array.Length))!);
					if (ll != null)
					{
						var ex = ConvertMemberExpression(expr, ue.Operand!, ll);

						return new TransformInfo(ex, false, true);
					}

					break;
				}
				case ExpressionType.MemberAccess:
				{
					var me = (MemberExpression)expr;

					if (me.Member.IsNullableHasValueMember())
					{
						return new TransformInfo(Expression.NotEqual(me.Expression!, Expression.Constant(null, me.Expression!.Type)), false, true);
					}

					//if (me.Member.IsNullableValueMember())
					//{
					//	return new TransformInfo(Expression.Convert(me.Expression!, me.Type), false, true);
					//}

					/*
					if (CanBeCompiled(expr, true))
						break;
						*/

					var l  = ConvertMethodExpression(me.Expression?.Type ?? me.Member.ReflectedType!, me.Member, out var alias);

					if (l != null)
					{
						var ex = ConvertMemberExpression(expr, me.Expression!, l);

						return new TransformInfo(AliasCall(ex, alias!), false, true);
					}

					/*l = Expressions.ConvertMember(MappingSchema, me.Member.ReflectedType!, me.Member);

					if (l != null)
					{
						var ex = ConvertMemberExpression(expr, me.Expression!, l);

						return new TransformInfo(ex, false, true);
					}*/

					break;
				}

				case ExpressionType.Convert:
				{
					var ex = (UnaryExpression)expr;
					if (ex.Method != null)
					{
						var l = ConvertMethodExpression(ex.Method.DeclaringType!, ex.Method, out var alias);
						if (l != null)
						{
							var exposed = l.GetBody(ex.Operand);
							return new TransformInfo(exposed, false, true);
						}
					}

					break;
				}

				case ExpressionType.Constant:
				{
					var c = (ConstantExpression)expr;

					if (c.Value is IQueryable queryable && !(queryable is ITable))
					{
						var e = queryable.Expression;

						if (!(_visitedExpressions ??= new ()).Contains(e))
						{
							_visitedExpressions.Add(e);
							return new TransformInfo(e, false, true);
						}
					}

					break;
				}

				case ExpressionType.Invoke:
				{
					var invocation = (InvocationExpression)expr;
					if (invocation.Expression.NodeType == ExpressionType.Call)
					{
						var mc = (MethodCallExpression)invocation.Expression;
						if (mc.Method.Name == "Compile" &&
							typeof(LambdaExpression).IsSameOrParentOf(mc.Method.DeclaringType!))
						{
							if (mc.Object.EvaluateExpression() is LambdaExpression lambds)
							{
								var newBody = lambds.Body;
								if (invocation.Arguments.Count > 0)
								{
									var map = new Dictionary<Expression, Expression>();
									for (int i = 0; i < invocation.Arguments.Count; i++)
										map.Add(lambds.Parameters[i], invocation.Arguments[i]);

									newBody = lambds.Body.Transform(map, static (map, se) =>
									{
										if (se.NodeType == ExpressionType.Parameter &&
													map.TryGetValue(se, out var newExpr))
											return newExpr;
										return se;
									});
								}

								return new TransformInfo(newBody, false, true);
							}
						}
					}
					break;
				}

				case ExpressionType.Call:
				{
					var call = (MethodCallExpression)expr;

					var l = ConvertMethodExpression(call.Object?.Type ?? call.Method.ReflectedType!, call.Method, out var alias);

					if (l != null)
					{
						var converted = ConvertMethod(call, l);
						return new TransformInfo(converted, false, true);
					}

					break;
				}
			}

			return new TransformInfo(expr, false);
		}

		Dictionary<Expression, Expression>? _exposedCache;
		public Expression ExposeExpression(Expression expression)
		{
			if (expression is SqlGenericParamAccessExpression)
				return expression;

			if (_exposedCache != null && _exposedCache.TryGetValue(expression, out var result))
				return result;

			result = (_exposeExpressionTransformer ??=
				TransformInfoVisitor<ExpressionTreeOptimizationContext>.Create(this,
					static(ctx, e) => ctx.ExposeExpressionTransformer(e))).Transform(expression);

			(_exposedCache ??= new())[expression] = result;

			return result;
		}

		private static Expression ConvertMemberExpression(Expression expr, Expression root, LambdaExpression l)
		{
			var body  = l.Body.Unwrap();
			var parms = l.Parameters.ToDictionary(p => p);
			var ex    = body.Transform(
					(parms, root),
					static (context, wpi) =>
					{
						if (wpi.NodeType == ExpressionType.Parameter && context.parms.ContainsKey((ParameterExpression)wpi))
						{
							if (wpi.Type.IsSameOrParentOf(context.root.Type))
							{
								return context.root;
							}

							if (ExpressionConstants.DataContextParam.Type.IsSameOrParentOf(wpi.Type))
							{
								if (ExpressionConstants.DataContextParam.Type != wpi.Type)
									return Expression.Convert(ExpressionConstants.DataContextParam, wpi.Type);
								return ExpressionConstants.DataContextParam;
							}

							throw new LinqToDBException($"Can't convert {wpi} to expression.");
						}

						return wpi;
					});

			if (ex.Type != expr.Type)
			{
				//ex = new ChangeTypeExpression(ex, expr.Type);
				ex = Expression.Convert(ex, expr.Type);
			}

			return ex;
		}

		public LambdaExpression? ConvertMethodExpression(Type type, MemberInfo mi, out string? alias)
		{
			mi = type.GetMemberOverride(mi);

			var attr = MappingSchema.GetAttribute<ExpressionMethodAttribute>(type, mi);

			if (attr != null)
			{
				alias = attr.Alias ?? mi.Name;
				if (attr.Expression != null)
					return attr.Expression;

				if (!string.IsNullOrEmpty(attr.MethodName))
				{
					Expression expr;

					if (mi is MethodInfo method && method.IsGenericMethod)
					{
						var args  = method.GetGenericArguments();
						var names = args.Select(t => (object)t.Name).ToArray();
						var name  = string.Format(attr.MethodName, names);

						expr = Expression.Call(
							mi.DeclaringType!,
							name,
							name != attr.MethodName ? Array<Type>.Empty : args);
					}
					else
					{
						expr = Expression.Call(mi.DeclaringType!, attr.MethodName, Array<Type>.Empty);
					}

					var evaluated = (LambdaExpression?)expr.EvaluateExpression();
					return evaluated;
				}
			}

			alias = null;
			return null;
		}

		public Expression ConvertMethod(MethodCallExpression pi, LambdaExpression lambda)
		{
			var ef    = lambda.Body.Unwrap();
			var parms = new Dictionary<ParameterExpression,int>(lambda.Parameters.Count);
			var pn    = pi.Method.IsStatic ? 0 : -1;

			foreach (var p in lambda.Parameters)
				parms.Add(p, pn++);

			var pie = ef.Transform((pi, parms), static (context, wpi) =>
			{
				if (wpi.NodeType == ExpressionType.Parameter)
				{
					if (context.parms.TryGetValue((ParameterExpression)wpi, out var n))
					{
						if (n >= context.pi.Arguments.Count)
						{
							if (ExpressionConstants.DataContextParam.Type.IsSameOrParentOf(wpi.Type))
							{
								if (ExpressionConstants.DataContextParam.Type != wpi.Type)
									return Expression.Convert(ExpressionConstants.DataContextParam, wpi.Type);
								return ExpressionConstants.DataContextParam;
							}

							throw new LinqToDBException($"Can't convert {wpi} to expression.");
						}

						var result = n < 0 ? context.pi.Object! : context.pi.Arguments[n];

						if (result.Type != wpi.Type)
						{
							var noConvert = result.UnwrapConvert();
							if (noConvert.Type == wpi.Type)
							{
								result = noConvert;
							}
							else
							{
								if (noConvert.Type.IsValueType)
									result = Expression.Convert(noConvert, wpi.Type);
							}
						}

						return result;
					}
				}

				return wpi;
			});

			if (pi.Method.ReturnType != pie.Type)
			{
				pie = pie.UnwrapConvert();
				if (pi.Method.ReturnType != pie.Type)
				{
					// pie = new ChangeTypeExpression(pie, pi.Method.ReturnType);
					pie = Expression.Convert(pie, pi.Method.ReturnType);
				}
			}

			return pie;
		}

		#region PreferServerSide

		private FindVisitor<ExpressionTreeOptimizationContext>? _enforceServerSideVisitorTrue;
		private FindVisitor<ExpressionTreeOptimizationContext>? _enforceServerSideVisitorFalse;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private FindVisitor<ExpressionTreeOptimizationContext> GetVisitor(bool enforceServerSide)
		{
			if (enforceServerSide)
				return _enforceServerSideVisitorTrue ??= FindVisitor<ExpressionTreeOptimizationContext>.Create(this, static (ctx, e) => ctx.PreferServerSide(e, true));
			else
				return _enforceServerSideVisitorFalse ??= FindVisitor<ExpressionTreeOptimizationContext>.Create(this, static (ctx, e) => ctx.PreferServerSide(e, false));
		}

		public bool PreferServerSide(Expression expr, bool enforceServerSide)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var pi = (MemberExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Expression?.Type, pi.Member);

						if (l != null)
						{
							var info = l.Body.Unwrap();

							if (l.Parameters.Count == 1 && pi.Expression != null)
								info = info.Replace(l.Parameters[0], pi.Expression);

							return GetVisitor(enforceServerSide).Find(info) != null;
						}

						var attr = pi.Member.GetExpressionAttribute(MappingSchema);
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr, true);
					}

				case ExpressionType.Call:
					{
						var pi = (MethodCallExpression)expr;
						var l  = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

						if (l != null)
							return GetVisitor(enforceServerSide).Find(l.Body.Unwrap()) != null;

						var attr = pi.Method.GetExpressionAttribute(MappingSchema);
						return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeCompiled(expr, true);
					}
				default:
					{
						if (expr is BinaryExpression binary)
						{
							var l = Expressions.ConvertBinary(MappingSchema, binary);
							if (l != null)
							{
								var body = l.Body.Unwrap();
								var newExpr = body.Transform((l, binary), static (context, wpi) =>
								{
									if (wpi.NodeType == ExpressionType.Parameter)
									{
										if (context.l.Parameters[0] == wpi)
											return context.binary.Left;
										if (context.l.Parameters[1] == wpi)
											return context.binary.Right;
									}

									return wpi;
								});

								return PreferServerSide(newExpr, enforceServerSide);
							}
						}
						break;
					}
			}

			return false;
		}

		#endregion

	}
}
