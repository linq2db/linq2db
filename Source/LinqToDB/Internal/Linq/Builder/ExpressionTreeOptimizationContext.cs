using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Linq.Builder.Visitors;
using LinqToDB.Internal.Reflection;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.Linq.Builder
{
	public sealed class ExpressionTreeOptimizationContext
	{
		public IDataContext  DataContext   { get; }
		public MappingSchema MappingSchema { get; }

		public ExpressionTreeOptimizationContext(IDataContext dataContext)
		{
			DataContext = dataContext;
			MappingSchema = dataContext.MappingSchema;
		}

		#region IsServerSideOnly

		sealed class IsServerSideOnlyCheckVisitor : ExpressionVisitorBase
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
				if (node.Member.IsServerSideOnly(_mappingSchema))
				{
					_isServerSideOnly = true;
					return node;
				}

				return base.VisitMember(node);
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (node.Method.IsServerSideOnly(_mappingSchema))
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

				if (node.Method.IsGenericMethod && node.IsSameGenericMethod(Methods.LinqToDB.AggregateExecute))
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

				if (node.IsSameGenericMethod(Methods.LinqToDB.Select))
				{
					CanBeEvaluated = false;
					return node;
				}

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

				if (node.Method.IsGenericMethod && node.IsSameGenericMethod(Methods.LinqToDB.AggregateExecute))
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

		static readonly ObjectPool<IsImmutableVisitor> _isImmutableVisitorPool = new(() => new IsImmutableVisitor(), v => v.Cleanup(), 100);

		sealed class IsImmutableVisitor : ExpressionVisitorBase
		{
			MappingSchema _mappingSchema = default!;

			bool          IsImmutable { get; set; }

			public bool CanBeImmutable(Expression expression, MappingSchema mappingSchema)
			{
				_mappingSchema = mappingSchema;
				IsImmutable = true;
				Visit(expression);
				return IsImmutable;
			}

			public override void Cleanup()
			{
				_mappingSchema = default!;
				IsImmutable    = true;

				base.Cleanup();
			}

			[return : NotNullIfNotNull(nameof(node))]
			public override Expression? Visit(Expression? node)
			{
				if (!IsImmutable)
					return node;

				return base.Visit(node);
			}

			private static readonly HashSet<Type> _wholeImmutableTypes = new()
			{
				typeof(string),
				typeof(bool),
				typeof(sbyte),
				typeof(byte),
				typeof(short),
				typeof(ushort),
				typeof(int),
				typeof(uint),
				typeof(long),
				typeof(ulong),
				typeof(char),
			};

			static readonly HashSet<Type> _immutableInstanceMembers = new()
			{
				typeof(DateTime),
				typeof(DateTimeOffset),
				typeof(TimeSpan),
				typeof(Guid),
				typeof(Decimal),
				typeof(Uri),
				typeof(Version),
				typeof(System.Numerics.BigInteger),
#if NET5_0_OR_GREATER
					typeof(nint),
					typeof(nuint),
#endif
#if SUPPORTS_DATEONLY
					typeof(DateOnly),
					typeof(TimeOnly),
#endif
				typeof(Index),
				typeof(Range),
			};

			bool IsReadOnlyProperty(PropertyInfo property)
			{
				if (property.DeclaringType != null)
				{
					if (_wholeImmutableTypes.Contains(property.DeclaringType))
					{
						return true;
					}

					if (_immutableInstanceMembers.Contains(property.DeclaringType) && property.GetMethod != null && !property.GetMethod.IsStatic)
					{
						return true;
					}
				}

				var setMethod = property.SetMethod;
				if (setMethod != null)
				{
					// Check if the SetMethod is init-only
					return setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
				}

#if SUPPORTS_READONLY
				// Check if the property belongs to a readonly struct and is not modifying state
				if (property.DeclaringType?.IsValueType == true &&
				    property.DeclaringType.IsDefined(typeof(IsReadOnlyAttribute), false))
				{
					return true;
				}
#endif
				return false;
			}

			bool IsReadOnlyMethod(MethodInfo method)
			{
				// Check if the method is marked with [IsReadOnly] (for .NET 5+)
#if SUPPORTS_READONLY
				if (method.GetAttributes<IsReadOnlyAttribute>().Length > 0)
				{
					return true;
				}

				// Check if the method belongs to a readonly struct and is not modifying state
				if (method.DeclaringType?.IsValueType == true &&
				    method.DeclaringType.IsDefined(typeof(IsReadOnlyAttribute), false))
				{
					// Instance methods in readonly structs are implicitly read-only
					if (!method.IsStatic && method.Name != ".ctor")
					{
						return true;
					}
				}
#endif
				if (method.DeclaringType != null)
				{
					if (_wholeImmutableTypes.Contains(method.DeclaringType))
					{
						return true;
					}

					if (method.IsStatic)
					{
						if (method.DeclaringType == typeof(Math))
						{
							return true;
						}

						if (method.DeclaringType != null)
						{
							if (method.GetExpressionAttribute(_mappingSchema) is { PreferServerSide: false, IsPure: true }
								&& !method.IsServerSideOnly(_mappingSchema))
							{
								return true;
							}
						}
					}
					else
					{
						if (method.DeclaringType != null && _immutableInstanceMembers.Contains(method.DeclaringType))
						{
							return true;
						}

						if (method.DeclaringType == typeof(object) && method.Name == nameof(ToString))
						{
							return true;
						}
					}
				}

				return false;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (IsReadOnlyMethod(node.Method))
					return base.VisitMethodCall(node);

				// we cannot predict the result of method call
				IsImmutable = false;
				return node;
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				if (node.Expression is null)
				{
					if (node.Member is FieldInfo { IsStatic: true, IsInitOnly: true })
					{
						return node;
					}

					if (node.Member is PropertyInfo property && IsReadOnlyProperty(property))
					{
						return node;
					}

					IsImmutable = false;
				}
				else
				{
					if (node.Expression.UnwrapConvert() is not ConstantExpression)
						return base.VisitMember(node);

					if (node.Member is PropertyInfo property && IsReadOnlyProperty(property))
					{
						Visit(node.Expression);
						return node;
					}

					IsImmutable = false;
				}

				return node;
			}

			public override Expression VisitSqlQueryRootExpression(SqlQueryRootExpression node)
			{
				IsImmutable = false;
				return node;
			}

			public override Expression VisitSqlPlaceholderExpression(SqlPlaceholderExpression node)
			{
				IsImmutable = false;
				return node;
			}

			protected override Expression VisitParameter(ParameterExpression node)
			{
				if (node == ExpressionBuilder.ParametersParam || node == ExpressionBuilder.QueryExpressionContainerParam)
				{
					IsImmutable = false;
					return node;
				}

				return node;
			}

		}

		public bool IsImmutable(Expression expr)
		{
			if (_lastExpr1 == expr)
				return _lastResult1;

			using var visitor = _isImmutableVisitorPool.Allocate();

			var result = visitor.Value.CanBeImmutable(expr, MappingSchema);

			_lastExpr1 = expr;
			return _lastResult1 = result;
		}

		#endregion

		#region PreferServerSide

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool PreferServerSide(bool enforceServerSide, Expression expr)
		{
			return expr.Find((context: this, enforceServerSide), static (ctx, e) => ctx.context.PreferServerSide(e, ctx.enforceServerSide)) != null;
		}

		public bool PreferServerSide(Expression expr, bool enforceServerSide)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
				{
					var pi = (MemberExpression)expr;
					var l  = LinqToDB.Linq.Expressions.ConvertMember(MappingSchema, pi.Expression?.Type, pi.Member);

					if (l != null)
					{
						var info = l.Body.Unwrap();

						if (l.Parameters.Count == 1 && pi.Expression != null)
							info = info.Replace(l.Parameters[0], pi.Expression);

						return PreferServerSide(enforceServerSide, info);
					}

					var attr = pi.Member.GetExpressionAttribute(MappingSchema);
					return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeEvaluatedOnClient(expr);
				}

				case ExpressionType.Call:
				{
					var pi = (MethodCallExpression)expr;
					var l  = LinqToDB.Linq.Expressions.ConvertMember(MappingSchema, pi.Object?.Type, pi.Method);

					if (l != null)
						return PreferServerSide(enforceServerSide, l.Body.Unwrap());

					var attr = pi.Method.GetExpressionAttribute(MappingSchema);
					return attr != null && (attr.PreferServerSide || enforceServerSide) && !CanBeEvaluatedOnClient(expr);
				}

				default:
				{
					if (expr is UnaryExpression unary)
					{
						var l = LinqToDB.Linq.Expressions.ConvertUnary(MappingSchema, unary);
						if (l != null)
						{
							var body = l.Body.Unwrap();
							var newExpr = body.Transform((l, unary), static (context, wpi) =>
							{
								if (wpi.NodeType == ExpressionType.Parameter)
								{
									if (context.l.Parameters[0] == wpi)
										return context.unary.Operand;
								}

								return wpi;
							});

							return PreferServerSide(newExpr, enforceServerSide);
						}
					}

					if (expr is BinaryExpression binary)
					{
						var l = LinqToDB.Linq.Expressions.ConvertBinary(MappingSchema, binary);
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
