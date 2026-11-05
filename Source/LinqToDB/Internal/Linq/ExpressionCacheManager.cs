using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Expressions;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using static LinqToDB.Internal.Linq.QueryCacheCompareInfo;

namespace LinqToDB.Internal.Linq
{
	sealed class ExpressionCacheManager
	{
		readonly IUniqueIdGenerator<ExpressionCacheManager> _generator;

		public Expression MainExpression { get; }

		public Dictionary<Expression, DynamicExpressionInfo>?      DynamicAccessors         { get; private set; }
		public Dictionary<int, Dictionary<Expression, Expression>> AccessorsMapping         { get; } = new();
		public HashSet<Expression>                                 NonComparableExpressions { get; } = [];

		List<(object? value, Expression accessor)>?                     _byValueCompare;
		List<(SqlValue value, Expression accessor)>?                    _foldedValues;

		/// <summary>
		/// Whether a value folded into the SQL at build time is re-evaluated and compared on every lookup. Under
		/// the folding rule (a folded value is a function of the query expression) the compare never fails; it is
		/// kept as a net until that rule is proven over the whole suite on several providers.
		/// </summary>
		public static bool CompareFoldedValues = !string.Equals(Environment.GetEnvironmentVariable("LINQ2DB_COMPARE_FOLDED"), "0", StringComparison.Ordinal);
		Dictionary<int, (Expression param, ParameterCacheEntry entry)>? _parameterEntries;

		List<SqlParameter>? _nonQueryParameters;

		public Expression RootPath      { get; set; } = ExpressionBuilder.QueryExpressionContainerParam;
		public bool       HasParameters => _parameterEntries  != null;
		public bool       HasConstants  => _foldedValues != null;

		public ExpressionCacheManager(Expression mainExpression, IUniqueIdGenerator<ExpressionCacheManager> generator)
		{
			MainExpression = mainExpression;
			_generator     = generator;

			// ensure not zero
			_generator.GetNext();

			// initialize main expression accessors
			AccessorsMapping.Add(0, mainExpression.GetExpressionAccessors(Expression.PropertyOrField(RootPath, nameof(IQueryExpressions.MainExpression))));
		}

		/// <summary>
		/// Used for comparing query in cache to resolve whether generated expressions are equal.
		/// </summary>
		/// <param name="forExpression">Expression which is used as key to do not generate duplicate comparers.</param>
		/// <param name="dataContext">DataContext which is used to execute <paramref name="accessorFunc"/>.</param>
		/// <param name="accessorFunc">Function, which will used for retrieving current expression during cache comparison.</param>
		/// <returns>Result of execution of accessorFunc</returns>
		public Expression RegisterDynamicExpressionAccessor(Expression forExpression, IDataContext dataContext, MappingSchema mappingSchema,
			ExpressionAccessorFunc           accessorFunc)
		{
			var result = accessorFunc(dataContext, mappingSchema);

			DynamicAccessors ??= new(ExpressionEqualityComparer.Instance);

			if (!DynamicAccessors.ContainsKey(forExpression))
			{
				var info = new DynamicExpressionInfo(_generator.GetNext(), result, mappingSchema, accessorFunc);
				DynamicAccessors.Add(forExpression, info);

				var newRoot = Expression.Call(RootPath, nameof(IQueryExpressions.GetQueryExpression), Type.EmptyTypes, Expression.Constant(info.ExpressionId));
				AccessorsMapping.Add(info.ExpressionId, result.GetExpressionAccessors(newRoot));
			}

			return result;
		}

		public bool GetAccessorExpression(Expression expression, [NotNullWhen(true)] out Expression? accessor)
		{
			foreach (var mapping in AccessorsMapping.Values)
			{
				if (mapping.TryGetValue(expression, out accessor))
				{
					return true;
				}
			}

			accessor = null;
			return false;
		}

		public Expression ApplyAccessors(Expression expression)
		{
			return ApplyAccessors(expression, NonComparableExpressions);
		}

		public Expression ApplyAccessors(Expression expression, HashSet<Expression> modified)
		{
			var transformed = expression.Transform(
				(modified, paramContext : this),
				static (context, expr) =>
				{
					// TODO: !!! Code should be synched with ReplaceParameter !!!
					if (expr.NodeType == ExpressionType.ArrayIndex && ((BinaryExpression)expr).Left == ExpressionBuilder.ParametersParam)
					{
						return new TransformInfo(expr, true);
					}

					if (expr is ConstantExpression { Value: not null } && context.paramContext.GetAccessorExpression(expr, out var accessor))
					{
						 context.modified.Add(expr);

						if (accessor.Type != expr.Type)
							accessor = Expression.Convert(accessor, expr.Type);

						return new TransformInfo(accessor);
					}

					return new TransformInfo(expr);
				});

			return transformed;
		}

		/// <summary>
		/// Suggests a display name for a parameter built from <paramref name="expression"/>, taken from the
		/// member the value is read from rather than from the column it is compared against - a parameter
		/// carries a value, so it reads better named after that value's source. When the expression is not
		/// itself a member access, the walk follows the value's own spine and returns the first member access
		/// it reaches: through unary operators, into the array or container of an element read, and into the
		/// target of a parameterless <c>GetValueOrDefault</c>.
		/// Only that spine is walked - indices and call arguments are not - so <c>dict[key]</c> is named after
		/// <c>dict</c>. The index itself must not reach the name: it is substituted out of the query-cache
		/// key, so a cached query would carry the index of whichever call site built it first.
		/// A method call that <i>computes</i> a new value would give a name that describes the wrong thing -
		/// the parameter behind <c>today.AddDays(-7)</c> is not <c>today</c> - so those keep returning
		/// <see langword="null"/> and are named the way they were before source-based naming existed.
		/// </summary>
		public static string? SuggestParameterDisplayName(Expression? expression)
		{
			return expression switch
			{
				MemberExpression member when member.Member.IsNullableValueMember() =>
					SuggestParameterDisplayName(member.Expression) ?? member.Member.Name,

				MemberExpression { Member.Name: var name } => name,

				UnaryExpression { Operand: var operand } =>
					SuggestParameterDisplayName(operand),

				// values[0] over an array - name after the array, not the target column
				BinaryExpression { NodeType: ExpressionType.ArrayIndex, Left: var array } =>
					SuggestParameterDisplayName(array),

				// list[0], dict[key], value.GetValueOrDefault() - the call returns what the target holds
				MethodCallExpression { Object: { } target } call when IsValuePreservingCall(call) =>
					SuggestParameterDisplayName(target),

				_ => null,
			};

			static bool IsValuePreservingCall(MethodCallExpression call)
			{
				var method = call.Method;

				// An indexer read hands back an element the container already holds. Matching the shape - a
				// special-name getter taking at least one argument - rather than the name get_Item keeps
				// [IndexerName]-renamed indexers in, string's Chars above all, and plain getters out. An explicit
				// interface implementation's getter carries the interface name in front: "...IList<T>.get_Item".
				if (method.IsSpecialName
					&& (method.Name.StartsWith("get_", StringComparison.Ordinal) || method.Name.Contains(".get_", StringComparison.Ordinal))
					&& call.Arguments.Count > 0)
					return true;

				// Nullable<T>.GetValueOrDefault() returns the target's own value - but the overload taking a
				// default can return that argument instead, so it must not lend the target's name.
				return string.Equals(method.Name, nameof(Nullable<>.GetValueOrDefault), StringComparison.Ordinal)
					&& call.Arguments.Count == 0
					&& method.DeclaringType is { IsGenericType: true } declaringType
					&& declaringType.GetGenericTypeDefinition() == typeof(Nullable<>);
			}
		}

		public void RegisterParameterEntry(Expression paramExpr, ParameterCacheEntry entry)
		{
			_parameterEntries ??= new();

			if (_parameterEntries.ContainsKey(entry.ParameterId))
				return;

			_parameterEntries.Add(entry.ParameterId, (paramExpr, entry));
		}

		public bool TryFindParameterEntry(Expression paramExpr, [NotNullWhen(true)] out ParameterCacheEntry? entry)
		{
			if (_parameterEntries == null)
			{
				entry = null;
				return false;
			}

			foreach (var pair in _parameterEntries.Values)
			{
				if (ReferenceEquals(pair.param, paramExpr))
				{
					entry = pair.entry;
					return true;
				}
			}

			entry = null;
			return false;
		}

		/// <summary>
		/// Registers parameter entry in cache. Two occurrences share one parameter only when they read the same C#
		/// variable - a captured local or field of the same closure object, a static field, a compiled-query
		/// parameter - through the same access path: fields, auto-properties, Nullable.Value/HasValue, array length,
		/// an element at a literal index and conversions. Values are never compared. Any method call, including a
		/// property getter with a body, gets its own parameter per occurrence: two calls the user wrote are two reads.
		/// </summary>
		/// <param name="paramExpr">Expression the parameter value is read from.</param>
		/// <param name="paramEntry">Cache entry being registered.</param>
		/// <param name="finalParameterId">Id of the parameter to use - an existing one when a duplicate was
		/// found, otherwise <paramref name="paramEntry"/>'s own.</param>
		public void RegisterParameterEntry(Expression paramExpr, ParameterCacheEntry paramEntry, out int finalParameterId)
		{
			_parameterEntries ??= new();

			var pure = IsPureChain(paramExpr);

			foreach (var (param, entry) in _parameterEntries.Values)
			{
				if ((pure || ReferenceEquals(param, paramExpr))
					&& ExpressionEqualityComparer.Instance.Equals(param, paramExpr)
					&& entry.DbDataType.EqualsDbOnly(paramEntry.DbDataType)
					&& ExpressionEqualityComparer.Instance.Equals(entry.ClientValueGetter, paramEntry.ClientValueGetter)
					&& ExpressionEqualityComparer.Instance.Equals(entry.ClientToProviderConverter, paramEntry.ClientToProviderConverter)
					&& ExpressionEqualityComparer.Instance.Equals(entry.ItemAccessor, paramEntry.ItemAccessor)
					&& ExpressionEqualityComparer.Instance.Equals(entry.DbDataTypeAccessor, paramEntry.DbDataTypeAccessor))
				{
					finalParameterId = entry.ParameterId;
					return;
				}
			}

			_parameterEntries.Add(paramEntry.ParameterId, (paramExpr, paramEntry));
			finalParameterId = paramEntry.ParameterId;
		}

		/// <summary>
		/// Whether <paramref name="expression"/> names a C# variable through an access path without a call: a
		/// constant (closure object), a compiled-query parameter or a static field, through fields, auto-properties,
		/// <c>Nullable.Value</c>/<c>HasValue</c>, array length, an element at a literal index, and conversions. A
		/// property getter with a body or any method call (<c>GetValueOrDefault()</c> included) is a call.
		/// </summary>
		static bool IsPureChain(Expression expression)
		{
			while (true)
			{
				switch (expression)
				{
					case ConstantExpression:
					case ParameterExpression:
						return true;

					case UnaryExpression { NodeType: ExpressionType.Convert or ExpressionType.ConvertChecked or ExpressionType.ArrayLength } unary:
						expression = unary.Operand;
						continue;

					case BinaryExpression { NodeType: ExpressionType.ArrayIndex, Right: ConstantExpression } index:
						expression = index.Left;
						continue;

					case MemberExpression member:
						if (member.Member is PropertyInfo property)
						{
							var nullable = property.DeclaringType is { IsGenericType: true } owner && owner.GetGenericTypeDefinition() == typeof(Nullable<>);

							if (!nullable && property.GetMethod?.IsDefined(typeof(CompilerGeneratedAttribute), false) != true)
								return false;
						}

						if (member.Expression == null)
							return member.Member is FieldInfo;

						expression = member.Expression;
						continue;

					default:
						return false;
				}
			}
		}

		public static Expression CorrectAccessorExpression(Expression accessorExpression, IDataContext dataContext)
		{
			// see #820
			accessorExpression = accessorExpression.Transform(dataContext, static (context, e) =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.MemberAccess:
					{
						var ma = (MemberExpression) e;

						if (ma.Member.IsNullableValueMember())
						{
							return Expression.Condition(
								Expression.Equal(ma.Expression!, Expression.Constant(null, ma.Expression!.Type)),
								Expression.Default(e.Type),
								e);
						}

						return e;
					}
					case ExpressionType.Convert:
					case ExpressionType.ConvertChecked:
					{
						var ce = (UnaryExpression) e;
						if (ce.Operand.Type.IsNullableType && !ce.Type.IsNullableType)
						{
							return Expression.Condition(
								Expression.Equal(ce.Operand, Expression.Constant(null, ce.Operand.Type)),
								Expression.Default(e.Type),
								e);
						}

						return e;
					}

					case ExpressionType.Extension:
					{
						if (e is SqlQueryRootExpression root)
						{
							var newExpr = (Expression)ExpressionConstants.DataContextParam;
							if (newExpr.Type != e.Type)
								newExpr = Expression.Convert(newExpr, e.Type);
							return newExpr;
						}

						return e;
					}
					default:
						return e;
				}
			})!;

			return accessorExpression;
		}

		[return: NotNullIfNotNull(nameof(expression))]
		Expression? PrepareExpressionAccessors(IDataContext dc, Expression? expression, HashSet<Expression> modified)
		{
			if (expression == null)
				return null;

			var transformed = ApplyAccessors(expression, modified);
			transformed = CorrectAccessorExpression(transformed, dc);
			return transformed;
		}

		sealed class PlaceholderContext
		{
			public PlaceholderContext(HashSet<Expression> nonComparable, HashSet<Expression>? parameterized, HashSet<Expression>? folded, HashSet<Expression>? dependentArguments, MappingSchema mappingSchema)
			{
				NonComparable      = nonComparable;
				Parameterized      = parameterized;
				Folded             = folded;
				DependentArguments = dependentArguments;
				MappingSchema      = mappingSchema;
			}

			public HashSet<Expression>  NonComparable      { get; }
			public HashSet<Expression>? Parameterized      { get; }
			public HashSet<Expression>? Folded             { get; }
			public HashSet<Expression>? DependentArguments { get; }
			public MappingSchema        MappingSchema      { get; }

			public Dictionary<Expression, ConstantPlaceholderExpression> Replacements { get; } = new();
			public Dictionary<object, int>                               Ordinals     { get; } = new(ReferenceEqualityComparer.Instance);
			public Dictionary<object, int>                               Occurrences  { get; } = new(ReferenceEqualityComparer.Instance);

			/// <summary>
			/// Counts how often each object constant occurs in <paramref name="expression"/>: only an object that
			/// occurs more than once needs an ordinal, because only then does the key say "the same object again",
			/// which a lookup must check. A query whose closures each occur once pays nothing at lookup.
			/// </summary>
			public void CountObjects(Expression expression)
			{
				expression.Visit(this, static (ctx, e) =>
				{
					if (e is ConstantExpression { Value: { } value } && value is not string && !value.GetType().IsValueType)
						ctx.Occurrences[value] = ctx.Occurrences.TryGetValue(value, out var n) ? n + 1 : 1;
				});
			}

			public ConstantPlaceholderExpression Placeholder(Expression e)
			{
				// Two occurrences of one object get one ordinal; a value-type or string constant that became a
				// parameter (Take(2)), or an object met only once, has no identity worth recording.
				var ordinal = -1;

				if (e is ConstantExpression { Value: { } value } && value is not string && !value.GetType().IsValueType && Occurrences.TryGetValue(value, out var occurrences) && occurrences > 1)
				{
					if (!Ordinals.TryGetValue(value, out ordinal))
					{
						ordinal = Ordinals.Count;
						Ordinals.Add(value, ordinal);
					}
				}

				var placeholder = new ConstantPlaceholderExpression(e.Type, ordinal);
				Replacements[e] = placeholder;
				return placeholder;
			}
		}

		/// <summary>
		/// All non-value type expressions and parameterized expressions will be transformed to ConstantPlaceholderExpression. It prevents from caching big reference classes in cache.
		/// </summary>
		Expression ReplaceParameterizedAndClosures(Expression expression, PlaceholderContext context)
		{
			var result = expression.Transform(context, static (ctx, e) =>
			{
				if (ctx.Replacements.TryGetValue(e, out var constantPlaceholder))
				{
					return new TransformInfo(constantPlaceholder, true);
				}

				if (ctx.NonComparable.Contains(e) && e is not ConstantExpression { Value: null })
				{
					// A scalar literal an accessor merely read (an extension argument, a duplicate check) is still
					// written in the query: it stays in the key by value. Only a literal the builder turned into a
					// parameter is taken out, so that one query serves every value at that position. A constant
					// a [SqlQueryDependent] argument reads stays too: its content shaped the SQL and is compared
					// by the attribute.
					if (e is ConstantExpression literal
						&& (literal.Value is not Array && !ExpressionCacheHelpers.ShouldRemoveConstantFromCache(literal, ctx.MappingSchema) && (ctx.Parameterized?.Contains(e) != true || ctx.Folded?.Contains(e) == true)
							|| ctx.DependentArguments?.Contains(e) == true))
					{
						return new TransformInfo(e);
					}

					return new TransformInfo(ctx.Placeholder(e), true);
				}

				if (e.NodeType == ExpressionType.Constant)
				{
					var c = (ConstantExpression)e;

					if (ExpressionCacheHelpers.ShouldRemoveConstantFromCache(c, ctx.MappingSchema))
					{
						return new TransformInfo(ctx.Placeholder(e));
					}
				}

				return new TransformInfo(e);
			});

			return result;
		}

		static void CollectDependentConstantArguments(Expression expression, HashSet<Expression> result)
		{
			expression.Visit(result, static (set, e) =>
			{
				if (e is MethodCallExpression call && SqlQueryDependentAttributeHelper.GetQueryDependentAttributes(call.Method) is { } attributes)
				{
					for (var i = 0; i < call.Arguments.Count; i++)
					{
						if (attributes[i] != null && call.Arguments[i] is ConstantExpression)
							set.Add(call.Arguments[i]);
					}
				}
			});
		}

		/// <summary>
		/// Builds <see cref="QueryCacheCompareInfo"/> for current cache state.
		/// </summary>
		/// <param name="dc"></param>
		/// <param name="parameterExpressions"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public (QueryCacheCompareInfo compareInfo, List<ParameterAccessor>? parameterAccessors, IQueryExpressions expressions) BuildQueryCacheCompareInfo(IExpressionEvaluator evaluator, IDataContext dc,
			IQueryExpressions parameterExpressions, List<SqlParameter>? parameters, List<SqlValue>? sqlValues)
		{
			List<SqlParameter>? knownParameters = null;
			if (parameters != null)
			{
				if (_nonQueryParameters == null)
					knownParameters = parameters;
				else
					knownParameters = parameters.Concat(_nonQueryParameters).ToList();
			}
			else
			{
				knownParameters = _nonQueryParameters;
			}

			List<ParameterAccessor>? parameterAccessors = null;
			var nonComparable = NonComparableExpressions;
			if (_parameterEntries != null && knownParameters != null)
			{
				var usedEntries = _parameterEntries.Where(e => knownParameters.Exists(p => p.AccessorId == e.Key)).Select(e => e.Value).ToList();
				if (usedEntries.Count > 0)
				{
					nonComparable = [.. NonComparableExpressions];

					parameterAccessors = new List<ParameterAccessor>(usedEntries.Count);
					foreach (var (paramExpr, entry) in usedEntries)
					{
						var sqlParameter = knownParameters.First(p => p.AccessorId == entry.ParameterId);

						var clientValueGetterExpr = PrepareExpressionAccessors(dc, entry.ClientValueGetter, nonComparable);
						var clientToProviderConverterExpr = PrepareExpressionAccessors(dc, entry.ClientToProviderConverter, nonComparable);
						var itemAccessorExpr = PrepareExpressionAccessors(dc, entry.ItemAccessor, nonComparable);
						var dbDataTypeAccessorExpr = PrepareExpressionAccessors(dc, entry.DbDataTypeAccessor, nonComparable);

						var clientValueGetterLambda = Expression.Lambda<Func<IQueryExpressions, IDataContext?, object?[]?, object?>>(clientValueGetterExpr.EnsureType<object>(),
							ExpressionBuilder.QueryExpressionContainerParam, ExpressionConstants.DataContextParam, ExpressionBuilder.ParametersParam);

						var clientValueGetter = clientValueGetterLambda.CompileExpression();

						Func<object?,object?>?    clientToProviderConverter = null;
						Func<object?,object?>?    itemAccessor              = null;
						Func<object?,DbDataType>? dbDataTypeAccessor        = null;

						if (clientToProviderConverterExpr != null)
						{
							var clientToProviderConverterLambda = Expression.Lambda<Func<object?, object?>>(clientToProviderConverterExpr.EnsureType<object>(), ParametersContext.ItemParameter);
							clientToProviderConverter = clientToProviderConverterLambda.CompileExpression();
						}

						if (itemAccessorExpr != null)
						{
							var itemAccessorLambda = Expression.Lambda<Func<object?, object?>>(itemAccessorExpr.EnsureType<object>(), ParametersContext.ItemParameter);
							itemAccessor = itemAccessorLambda.CompileExpression();
						}

						if (dbDataTypeAccessorExpr != null)
						{
							var dbDataTypeAccessorLambda = Expression.Lambda<Func<object?, DbDataType>>(dbDataTypeAccessorExpr.EnsureType<DbDataType>(), ParametersContext.ItemParameter);
							dbDataTypeAccessor = dbDataTypeAccessorLambda.CompileExpression();
						}

						var accessor = new ParameterAccessor(
							entry.ParameterId,
							clientValueGetter,
							clientToProviderConverter,
							itemAccessor,
							dbDataTypeAccessor,
							sqlParameter)
#if DEBUG
							{
								AccessorExpr = clientValueGetterLambda,
							}
#endif
							;

						parameterAccessors.Add(accessor);
					}
				}
			}

			List<DynamicExpressionInfo>? dynamicAccessors   = null;

			// Literals the builder turned into parameters (Take(2), Set(f, 1), Sql.Parameter(2)): the key keeps a
			// placeholder at those positions.
			HashSet<Expression>? parameterizedLiterals = null;

			if (_parameterEntries != null)
			{
				foreach (var (paramExpr, _) in _parameterEntries.Values)
				{
					paramExpr.Visit(parameterizedLiterals ??= [], static (set, e) =>
					{
						if (e is ConstantExpression)
							set.Add(e);
					});
				}
			}

			var dependentArguments = new HashSet<Expression>();

			CollectDependentConstantArguments(MainExpression, dependentArguments);

			if (DynamicAccessors != null)
			{
				foreach (var da in DynamicAccessors.Values)
					CollectDependentConstantArguments(da.Used, dependentArguments);
			}

			// The literals of a folded expression that survived into the SQL stay in the key by value even when a
			// parameter accessor reaches one of them elsewhere in the tree (one constant node under Where and
			// under Take); a fold the builder discarded (Take's argument before it became a parameter) does not count.
			HashSet<Expression>? foldedLiterals = null;

			if (_foldedValues != null && sqlValues is { Count: > 0 })
			{
				foreach (var (value, accessor) in _foldedValues)
				{
					if (sqlValues.Contains(value))
					{
						accessor.Visit(foldedLiterals ??= [], static (set, e) =>
						{
							if (e is ConstantExpression)
								set.Add(e);
						});
					}
				}
			}

			var placeholderContext = new PlaceholderContext(nonComparable, parameterizedLiterals, foldedLiterals, dependentArguments, dc.MappingSchema);

			placeholderContext.CountObjects(MainExpression);

			if (DynamicAccessors != null)
			{
				foreach (var da in DynamicAccessors.Values)
					placeholderContext.CountObjects(da.Used);
			}

			if (DynamicAccessors != null)
			{
				var runtimeExpressions = new RuntimeExpressionsContainer(parameterExpressions.MainExpression);
				dynamicAccessors = new List<DynamicExpressionInfo>(DynamicAccessors.Count);

				foreach (var da in DynamicAccessors.Values)
				{
					runtimeExpressions.AddExpression(da.ExpressionId, da.Used);

					var replaced = ReplaceParameterizedAndClosures(da.Used, placeholderContext);

					dynamicAccessors.Add(da with { Used = replaced });
				}

				parameterExpressions = runtimeExpressions;
			}

			List<(ValueAccessorFunc main, ValueAccessorFunc other)>? comparisionFunctions = null;

			if (_byValueCompare != null)
			{
				foreach (var (value, accessor) in _byValueCompare)
				{
					var mainValueExpr  = Expression.Constant(value).EnsureType<object>();
					var otherValueExpr = PrepareExpressionAccessors(dc, accessor, nonComparable).EnsureType<object>();

					var mainFunc = Expression.Lambda<ValueAccessorFunc>(mainValueExpr, ExpressionBuilder.QueryExpressionContainerParam, ExpressionConstants.DataContextParam,
							ExpressionBuilder.ParametersParam)
						.CompileExpression();

					var otherFunc = Expression.Lambda<ValueAccessorFunc>(otherValueExpr, ExpressionBuilder.QueryExpressionContainerParam, ExpressionConstants.DataContextParam,
							ExpressionBuilder.ParametersParam)
						.CompileExpression();

					comparisionFunctions ??= [];
					comparisionFunctions.Add((mainFunc, otherFunc));
				}
			}

			if (CompareFoldedValues && _foldedValues != null && sqlValues is { Count: > 0 })
			{
				foreach (var (value, accessor) in _foldedValues)
				{
					if (!sqlValues.Contains(value))
						continue;

					var mainValueExpr = Expression.Constant(evaluator.Evaluate(accessor)).EnsureType<object>();

					var otherValueExpr = PrepareExpressionAccessors(dc, accessor, nonComparable).EnsureType<object>();

					var mainFunc = Expression.Lambda<ValueAccessorFunc>(mainValueExpr, ExpressionBuilder.QueryExpressionContainerParam, ExpressionConstants.DataContextParam,
							ExpressionBuilder.ParametersParam)
						.CompileExpression();

					var otherFunc = Expression.Lambda<ValueAccessorFunc>(otherValueExpr, ExpressionBuilder.QueryExpressionContainerParam, ExpressionConstants.DataContextParam,
							ExpressionBuilder.ParametersParam)
						.CompileExpression();

					comparisionFunctions ??= [];
					comparisionFunctions.Add((mainFunc, otherFunc));
				}
			}

			var mainExpression = ReplaceParameterizedAndClosures(MainExpression, placeholderContext);
			var compareInfo    = new QueryCacheCompareInfo(mainExpression, dynamicAccessors, comparisionFunctions, placeholderContext.Ordinals.Count > 0);

			return (compareInfo, parameterAccessors, parameterExpressions);
		}

		/// <summary>
		/// Registers parameter which is used in non-query operation and not present in result SQL. Like handling Skip in Providers which do not support it.
		/// </summary>
		/// <param name="parameter"></param>
		public void RegisterNonQueryParameter(SqlParameter parameter)
		{
			_nonQueryParameters ??= new();
			_nonQueryParameters.Add(parameter);
		}

		/// <summary>
		/// Registers expression as value. It will be replaced with constant placeholder during cache comparison.
		/// </summary>
		/// <param name="expression"></param>
		public void MarkAsValue(Expression expression, object? currentValue)
		{
			// The value is compared with a fresh read on every lookup; an array is snapshotted, or a mutation in
			// place would compare the array with itself.
			if (currentValue is Array array)
				currentValue = array.Clone();

			_byValueCompare ??= new();
			_byValueCompare.Add((currentValue, expression));
		}

		public void RegisterSqlValue(Expression constantExpr, SqlValue value)
		{
			_foldedValues ??= new();
			_foldedValues.Add((value, constantExpr));
		}

	}
}
