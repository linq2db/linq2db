using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
			bool          _isServerSideOnly;
			MappingSchema _mappingSchema  = default!;

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

		static readonly ObjectPool<IsServerSideOnlyCheckVisitor> _serverSideOnlyVisitorPool  = new(() => new IsServerSideOnlyCheckVisitor(), v => v.Cleanup(), 100);
		static readonly ObjectPool<CanBeEvaluatedOnClientCheckVisitor> _canBeEvaluatedOnClientCheckVisitorPool = new(() => new CanBeEvaluatedOnClientCheckVisitor(), v => v.Cleanup(), 100);

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
						(node.Object != null && typeof(IDataContext).IsSameOrParentOf(node.Object.Type)))
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
					&& ((IConfigurationID)node.MappingSchema).ConfigurationID == ((IConfigurationID)_mappingSchema).ConfigurationID)
				{
					return node;
				}

				CanBeEvaluated = false;
				return node;
			}

			internal override Expression VisitSqlPathExpression(SqlPathExpression node)
			{
				CanBeEvaluated = false;
				return node;
			}
		}

		/// <summary>
		/// Check if <paramref name="expr"/> could be evaluated on client side.
		/// </summary>
		public bool CanBeEvaluatedOnClient(Expression expr)
		{
			using var visitor = _canBeEvaluatedOnClientCheckVisitorPool.Allocate();

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

			// Members of the BCL whose result depends on the ambient culture unless the call names a culture or a
			// comparison explicitly. Their value is not a function of the expression alone.
			static readonly HashSet<string> _cultureSensitiveMethods = new(StringComparer.Ordinal)
			{
				nameof(object.ToString),
				nameof(string.Format),
				nameof(int.Parse),
				nameof(int.TryParse),
				nameof(string.ToUpper),
				nameof(string.ToLower),
				nameof(string.Compare),
				nameof(string.CompareTo),
				nameof(string.StartsWith),
				nameof(string.EndsWith),
				nameof(string.IndexOf),
				nameof(string.LastIndexOf),
			};

			// Types whose default text form varies with the culture: a decimal separator, a date pattern.
			static readonly HashSet<Type> _cultureFormattedTypes = new()
			{
				typeof(float),
				typeof(double),
				typeof(decimal),
				typeof(DateTime),
				typeof(DateTimeOffset),
#if SUPPORTS_DATEONLY
				typeof(DateOnly),
				typeof(TimeOnly),
#endif
			};

			static bool DependsOnAmbientCulture(MethodCallExpression node)
			{
				var method = node.Method;

				if (!_cultureSensitiveMethods.Contains(method.Name))
					return false;

				var parameters = method.GetParameters();

				foreach (var parameter in parameters)
				{
					var type = parameter.ParameterType;
					if (type == typeof(IFormatProvider) || type == typeof(CultureInfo) || type == typeof(StringComparison))
						return false;
				}

				if (string.Equals(method.Name, nameof(object.ToString), StringComparison.Ordinal)
					|| string.Equals(method.Name, nameof(int.Parse), StringComparison.Ordinal)
					|| string.Equals(method.Name, nameof(int.TryParse), StringComparison.Ordinal))
				{
					// The value's own type decides, not the static type the call is made through (object, Nullable<T>).
					var valueType = (node.Object?.UnwrapConvert().Type ?? method.DeclaringType!).UnwrapNullableType();

					if (_cultureFormattedTypes.Contains(valueType))
						return true;

					// An integer, bool, char, Guid or string formats the same everywhere unless a format is given.
					return parameters.Length > 0 && valueType != typeof(string) && valueType != typeof(bool) && valueType != typeof(char) && valueType != typeof(Guid);
				}

				return true;
			}

			static bool IsNullable(Type? type)
			{
				return type is { IsGenericType: true } && type.GetGenericTypeDefinition() == typeof(Nullable<>);
			}

			// A member that cannot change after the object is constructed: a readonly field, an init-only or
			// get-only auto-property, or an instance member of an immutable BCL type. Reading it through an
			// immutable receiver yields a value fixed by the expression.
			bool IsReadOnlyMember(MemberInfo member)
			{
				if (member is FieldInfo field)
					return field.IsInitOnly;

				return member is PropertyInfo property && IsReadOnlyProperty(property);
			}

			bool IsReadOnlyProperty(PropertyInfo property)
			{
				var getMethod = property.GetMethod;

				if (getMethod == null || getMethod.IsStatic)
				{
					// A static property runs code: DateTime.Now, DateTime.Today, CultureInfo.CurrentCulture.
					return false;
				}

				if (property.DeclaringType != null)
				{
					if (_wholeImmutableTypes.Contains(property.DeclaringType))
					{
						return true;
					}

					if (_immutableInstanceMembers.Contains(property.DeclaringType) || IsNullable(property.DeclaringType))
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

				// A get-only auto-property reads a readonly backing field; a get-only property with a body runs code.
				if (getMethod.IsDefined(typeof(CompilerGeneratedAttribute), false))
				{
					return true;
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
					if (!method.IsStatic && !string.Equals(method.Name, ".ctor", StringComparison.Ordinal))
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

						if (method.DeclaringType == typeof(object) && string.Equals(method.Name, nameof(ToString), StringComparison.Ordinal))
						{
							return true;
						}
					}
				}

				return false;
			}

			protected override Expression VisitMethodCall(MethodCallExpression node)
			{
				if (IsReadOnlyMethod(node.Method) && !DependsOnAmbientCulture(node))
					return base.VisitMethodCall(node);

				// we cannot predict the result of method call
				IsImmutable = false;
				return node;
			}

			protected override Expression VisitBinary(BinaryExpression node)
			{
				// An element read from an array that already exists is read from a mutable object, however the array
				// itself is reached; only an array written in the query (new[] { 1, 2 }) is fixed.
				if (node.NodeType == ExpressionType.ArrayIndex && node.Left.NodeType != ExpressionType.NewArrayInit)
				{
					IsImmutable = false;
					return node;
				}

				return base.VisitBinary(node);
			}

			protected override Expression VisitIndex(IndexExpression node)
			{
				if (node.Object?.NodeType != ExpressionType.NewArrayInit)
				{
					IsImmutable = false;
					return node;
				}

				return base.VisitIndex(node);
			}

			protected override Expression VisitConstant(ConstantExpression node)
			{
				// A literal written in the query is null or a scalar. An object, a collection or a delegate held
				// in a constant is a run-time input: the cache key keeps only its type, so a value read through it
				// must stay a parameter.
				if (node.Value is not null && (node.Value is IEnumerable and not string || !_mappingSchema.IsScalarType(node.Value.GetType())))
				{
					IsImmutable = false;
				}

				return node;
			}

			protected override Expression VisitMember(MemberExpression node)
			{
				if (node.Expression is null)
				{
					// A static readonly field or a constant is fixed once its type is initialised: a function of
					// the expression. A static property runs code (DateTime.Now, CultureInfo.CurrentCulture) and a
					// mutable static field changes under the query: both are run-time inputs.
					if (node.Member is FieldInfo { IsStatic: true } field && (field.IsInitOnly || field.IsLiteral))
					{
						return node;
					}

					IsImmutable = false;
				}
				else
				{
					// A member of an object written in the query (new { Category = "Some" }.Category) is one of its
					// constructor arguments; otherwise the member must be unchangeable after construction and the
					// receiver fixed itself.
					if (node.Expression is NewExpression { Members: not null } created && created.Members.Contains(node.Member)
						|| IsReadOnlyMember(node.Member))
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
