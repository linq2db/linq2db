using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using LinqToDB.Expressions;
	using LinqToDB.Expressions.ExpressionVisitors;
	using Common.Internal;
	using Mapping;
	using Visitors;

	public class ExpressionTreeOptimizationContext
	{
		public IDataContext  DataContext   { get; }
		public MappingSchema MappingSchema { get; }

		public ExpressionTreeOptimizationContext(IDataContext dataContext)
		{
			DataContext = dataContext;
			MappingSchema = dataContext.MappingSchema;
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

		#region IsServerSideOnly

		class IsServerSideOnlyCheckVisitor : ExpressionVisitorBase
		{
			bool                                                 _isServerSideOnly;
			MappingSchema                                        _mappingSchema  = default!;

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
				var current = _isServerSideOnly;

				var newNode = base.Visit(node);

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
		static ObjectPool<CanBeEvaluatedOnClientCheckVisitor> _canBeEvaluatedOnClientCheckVisitorPool = new(() => new CanBeEvaluatedOnClientCheckVisitor(), v => v.Cleanup(), 100);

		Dictionary<Expression, bool>? _isServerSideOnlyCache;

		public bool IsServerSideOnly(Expression expr)
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

		#region CanBeEvaluatedOnClient

		sealed class CanBeEvaluatedOnClientCheckVisitor : CanBeEvaluatedOnClientCheckVisitorBase
		{
			MappingSchema _mappingSchema = default!;

			/// <summary>
			/// Check if <paramref name="expression"/> could be evaluated on client side.
			/// </summary>
			public bool CanBeEvaluatedOnClient(Expression expression, MappingSchema mappingSchema, ExpressionTreeOptimizationContext optimizationContext)
			{
				Cleanup();

				OptimizationContext = optimizationContext;
				_mappingSchema       = mappingSchema;

				_ = Visit(expression);

				return CanBeEvaluated;
			}

			public override void Cleanup()
			{
				_mappingSchema = default!;

				base.Cleanup();
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				if (node == ExpressionBuilder.ParametersParam || node == ExpressionBuilder.QueryExpressionContainerParam)
					return node;

				if (node == ExpressionConstants.DataContextParam)
				{
					if (InMethod)
						CanBeEvaluated = false;

					return node;
				}

				return base.VisitParameter(node);
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				var save = InMethod;
				InMethod = true;

				_ = base.VisitMember(node);

				InMethod = save;

				if (!CanBeEvaluated)
					return node;

				if (OptimizationContext.IsServerSideOnly(node))
					CanBeEvaluated = false;

				return node;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (!CanBeEvaluated)
					return node;

				if (typeof(IQueryable<>).IsSameOrParentOf(node.Type))
				{
					if (node.Arguments.Any(static a => typeof(IDataContext).IsSameOrParentOf(a.Type)) ||
						node.Object != null && typeof(IDataContext).IsSameOrParentOf(node.Object.Type))
					{
						CanBeEvaluated = false;
						return node;
					}
				}

				if (node.Method.DeclaringType == typeof(DataExtensions))
				{
					CanBeEvaluated = false;
					return node;
				}

				return base.VisitMethodCall(node);
			}

			public override Expression VisitSqlQueryRootExpression(SqlQueryRootExpression node)
			{
				if (InMethod
					&& ((IConfigurationID)node.MappingSchema).ConfigurationID ==
					((IConfigurationID)_mappingSchema).ConfigurationID)
				{
					return node;
				}

				CanBeEvaluated = false;
				return node;
			}
		}

		/// <summary>
		/// Check if <paramref name="expr"/> could be evaluated on client side.
		/// </summary>
		public bool CanBeEvaluatedOnClient(Expression expr)
		{
			var visitor = _canBeEvaluatedOnClientCheckVisitorPool.Allocate();

			var result = visitor.Value.CanBeEvaluatedOnClient(expr, MappingSchema, this);

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
					return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeEvaluatedOnClient(expr);
				}

				case ExpressionType.Call:
				{
					var pi = (MethodCallExpression)expr;
					var l  = Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

					if (l != null)
						return GetVisitor(enforceServerSide).Find(l.Body.Unwrap()) != null;

					var attr = pi.Method.GetExpressionAttribute(MappingSchema);
					return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeEvaluatedOnClient(expr);
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
