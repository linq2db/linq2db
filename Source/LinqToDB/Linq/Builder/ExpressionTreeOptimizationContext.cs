using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using LinqToDB.Expressions.ExpressionVisitors;
	using Common.Internal;
	using Mapping;
	using SqlQuery;

	public class ExpressionTreeOptimizationContext
	{
		public IDataContext  DataContext   { get; }
		public MappingSchema MappingSchema { get; }

		public ExpressionTreeOptimizationContext(IDataContext dataContext)
		{
			DataContext = dataContext;
			MappingSchema = dataContext.MappingSchema;

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
					_equalsToContextTrue = EqualsToVisitor.PrepareEqualsInfo(DataContext, compareConstantValues : compareConstantValues);
				else
					_equalsToContextTrue.Reset();
				return _equalsToContextTrue;
			}
			else
			{
				if (_equalsToContextFalse == null)
					_equalsToContextFalse = EqualsToVisitor.PrepareEqualsInfo(DataContext, compareConstantValues : compareConstantValues);
				else
					_equalsToContextFalse.Reset();
				return _equalsToContextFalse;
			}
		}

		public Expression AggregateExpression(Expression expression)
		{
			return _aggregateExpressionTransformer.Transform(expression);
		}

		private static readonly TransformVisitor<object?> _aggregateExpressionTransformer = TransformVisitor<object?>.Create(AggregateExpressionTransformer);

		internal static Expression AggregateExpressionTransformer(Expression expr)
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

		TransformInfoVisitor<ExpressionTreeOptimizationContext> _optimizeExpressionTreeTransformer;
		TransformInfoVisitor<ExpressionTreeOptimizationContext> _optimizeExpressionTreeTransformerInProjection;

		public TransformInfo OptimizeExpressionTreeTransformer(Expression expr, bool inProjection)
		{
			bool IsEqualConstants(Expression left, Expression right)
			{
				var valueLeft = left switch
				{
					ConstantExpression leftConst                         => leftConst.Value,
					SqlPlaceholderExpression { Sql: SqlValue leftConst } => leftConst.Value,
					_ => MappingSchema.GetDefaultValue(left.Type),
				};

				var valueRight = right switch
				{
					ConstantExpression rightConst                         => rightConst.Value,
					SqlPlaceholderExpression { Sql: SqlValue rightConst } => rightConst.Value,
					_ => MappingSchema.GetDefaultValue(right.Type),
				};

				return Equals(valueLeft, valueRight);
			}

			bool IsComparable(Expression testExpr)
			{
				return testExpr.NodeType is ExpressionType.Constant or ExpressionType.Default 
					|| testExpr is DefaultValueExpression;
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
				return testExpr switch
				{
					{ Type.IsValueType: true } => null,

					ConstantExpression { NodeType: ExpressionType.Constant, Value: var v } => v == null,

					{ NodeType: ExpressionType.Default } => true,

					{ NodeType: ExpressionType.New or ExpressionType.MemberInit }
						or SqlGenericConstructorExpression => false,

					DefaultValueExpression => true,

					_ => null,
				};
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
						if (equal.Left.NodeType is not ExpressionType.Convert and not ExpressionType.ConvertChecked &&
						    equal.Right.NodeType is not ExpressionType.Convert and not ExpressionType.ConvertChecked)
						{
							return new TransformInfo(Expression.NotEqual(equal.Left, equal.Right), false, true);
						}
					}

					if (notExpression.Operand.NodeType == ExpressionType.NotEqual)
					{
						var notEqual = (BinaryExpression)notExpression.Operand;
						if (notEqual.Left.NodeType is not ExpressionType.Convert and not ExpressionType.ConvertChecked &&
						    notEqual.Right.NodeType is not ExpressionType.Convert and not ExpressionType.ConvertChecked)
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
			bool                                                 _isServerSideOnly;
			MappingSchema                                        _mappingSchema  = default!;
			List<(Expression expression, bool isServerSideOnly)> _serverSideOnlyTree = new();

			public IReadOnlyList<(Expression expression, bool isServerSideOnly)> ServerSideOnlyTree => _serverSideOnlyTree;

			public bool IsServerSideOnly(Expression expression, MappingSchema mappingSchema)
			{
				Cleanup();

				_mappingSchema = mappingSchema;

				_ = Visit(expression);

				return _isServerSideOnly;
			}

			public override void Cleanup()
			{
				_serverSideOnlyTree.Clear();
				_isServerSideOnly = false;
				_mappingSchema    = default!;

				base.Cleanup();
			}

			public override Expression? Visit(Expression? node)
			{
				var current = _isServerSideOnly;

				var newNode = base.Visit(node);

				if (newNode != null)
				{
					//if (current != _isServerSideOnly)
					//{ }

					_serverSideOnlyTree.Add((newNode, _isServerSideOnly));
				}

				return newNode;
			}

			protected override Expression VisitMember(MemberExpression node)
			{
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
				var attr = node.Method.GetExpressionAttribute(_mappingSchema);
				if (attr?.ServerSideOnly == true)
				{
					_isServerSideOnly = true;
					return node;
				}

				var tableFunction = node.Method.GetTableFunctionAttribute(_mappingSchema);
				if (tableFunction != null)
				{
					_isServerSideOnly = true;
					return node;
				}

				return base.VisitMethodCall(node);
			}
		}

		static ObjectPool<IsServerSideOnlyCheckVisitor> _serverSideOnlyVisitorPool  = new(() => new IsServerSideOnlyCheckVisitor(), v => v.Cleanup(), 100);
		static ObjectPool<CanBeCompiledCheckVisitor> _canBeCompiledCheckVisitorPool = new(() => new CanBeCompiledCheckVisitor(), v => v.Cleanup(), 100);

		Dictionary<Expression, bool>? _isServerSideOnlyCache;

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

			MappingSchema _mappingSchema = default!;

			Stack<ReadOnlyCollection<ParameterExpression>>? _allowedParameters;

			ExpressionTreeOptimizationContext _optimizationContext = default!;

			bool CanBeCompiledFlag
			{
				get => _canBeCompiled;
				set
				{
					_canBeCompiled = value;
				}
			}

			public bool CanBeCompiled(Expression expression, MappingSchema mappingSchema, ExpressionTreeOptimizationContext optimizationContext, bool inProjection)
			{
				Cleanup();

				_optimizationContext = optimizationContext;
				_inProjection        = inProjection;
				_mappingSchema       = mappingSchema;

				_ = Visit(expression);

				return _canBeCompiled;
			}

			public override void Cleanup()
			{
				_canBeCompiled       = true;
				_inMethod            = false;
				_optimizationContext = default!;
				_inProjection        = false;
				_mappingSchema       = default!;

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
					CanBeCompiledFlag = false;
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
						CanBeCompiledFlag = false;
				}
				else
				{
					if (node != ExpressionBuilder.QueryExpressionContainerParam && (_allowedParameters == null || !_allowedParameters.Any(ps => ps.Contains(node))))
					{
						CanBeCompiledFlag = false;
					}
				}

				return node;
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				var save = _inMethod;
				_inMethod = true;

				_ = base.VisitMember(node);

				_inMethod = save;

				if (!CanBeCompiledFlag)
				{
					return node;
				}

				if (_optimizationContext.IsServerSideOnly(node, false))
					CanBeCompiledFlag = false;

				return node;
			}

			internal override Expression VisitContextRefExpression(ContextRefExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}

			internal override Expression VisitSqlErrorExpression(SqlErrorExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}

			public override Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
			{
				if (!_inProjection)
					CanBeCompiledFlag = false;
				return node;
			}

			internal override Expression VisitSqlGenericParamAccessExpression(SqlGenericParamAccessExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}

			internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (typeof(IQueryable<>).IsSameOrParentOf(node.Type))
				{
					if (node.Arguments.Any(a => typeof(IDataContext).IsSameOrParentOf(a.Type)) ||
					    node.Object != null && typeof(IDataContext).IsSameOrParentOf(node.Object.Type))
					{
						CanBeCompiledFlag = false;
						return node;
					}
				}

				if (!CanBeCompiledFlag)
				{
					return node;
				}

				if (node.Method.DeclaringType == typeof(DataExtensions))
				{
					CanBeCompiledFlag = false;
					return node;
				}

				if (_optimizationContext.IsServerSideOnly(node, false))
					CanBeCompiledFlag = false;

				var save = _inMethod;
				_inMethod = true;

				base.VisitMethodCall(node);

				_inMethod = save;

				return node;
			}

			internal override SqlGenericConstructorExpression.Assignment VisitSqlGenericAssignment(SqlGenericConstructorExpression.Assignment assignment)
			{
				CanBeCompiledFlag = false;
				return assignment;
			}

			internal override SqlGenericConstructorExpression.Parameter VisitSqlGenericParameter(SqlGenericConstructorExpression.Parameter parameter)
			{
				CanBeCompiledFlag = false;
				return parameter;
			}

			public override Expression VisitSqlGenericConstructorExpression(SqlGenericConstructorExpression node)
			{
				CanBeCompiledFlag = false;
				return node;
			}

			public override Expression VisitSqlQueryRootExpression(SqlQueryRootExpression node)
			{
				if (((IConfigurationID)node.MappingSchema).ConfigurationID ==
				    ((IConfigurationID)_mappingSchema).ConfigurationID)
				{
					if (_inMethod)
					{
						return node;
					}
				}

				CanBeCompiledFlag = false;
				return node;
			}
		}

		public bool CanBeCompiled(Expression expr, bool inProjection)
		{
			var visitor = _canBeCompiledCheckVisitorPool.Allocate();

			var result = visitor.Value.CanBeCompiled(expr, MappingSchema, this, inProjection);

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
			if (ex is BinaryExpression)
				return false;

			if (ex.Type == typeof(void))
				return true;

			switch (ex.NodeType)
			{
				case ExpressionType.Default : return false;
				case ExpressionType.Constant:
				{
					var c = (ConstantExpression)ex;

					if (c.Value == null || c.Value.GetType().IsConstantable(false))
						return false;

					return true;
				}

				case ExpressionType.ConvertChecked:
				case ExpressionType.Convert:
				{
					var unary = (UnaryExpression)ex;
					if (unary.Operand.Type.IsNullableType() && !unary.Type.IsNullableType())
						return true;

					return false;
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

				case ExpressionType.Extension:
				{
					if (ex is DefaultValueExpression)
					{
						if (ex.Type.IsConstantable(false))
							return false;
					}

					break;
				}
			}

			return true;
		}

		#endregion

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

					if (mi is MethodInfo { IsGenericMethod: true } method)
					{
						var args  = method.GetGenericArguments();
						var names = args.Select(t => (object)t.Name).ToArray();
						var name  = string.Format(CultureInfo.InvariantCulture, attr.MethodName, names);

						expr = Expression.Call(
							mi.DeclaringType!,
							name,
							name != attr.MethodName ? [] : args);
					}
					else
					{
						expr = Expression.Call(mi.DeclaringType!, attr.MethodName, []);
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
