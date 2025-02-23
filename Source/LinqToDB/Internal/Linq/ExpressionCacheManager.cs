using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Extensions;
using LinqToDB.Infrastructure;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq.Builder;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using static LinqToDB.Internal.Linq.QueryCacheCompareInfo;

namespace LinqToDB.Internal.Linq
{
	sealed class ParameterCacheEntry
	{
		public ParameterCacheEntry(
			int         parameterId,
			string      parameterName,
			DbDataType  dbDataType,
			Expression  clientValueGetter,
			Expression? clientToProviderConverter,
			Expression? itemAccessor,
			Expression? dbDataTypeAccessor)
		{
			ParameterId               = parameterId;
			DbDataTypeAccessor        = dbDataTypeAccessor;
			ParameterName             = parameterName;
			DbDataType                = dbDataType;
			ClientToProviderConverter = clientToProviderConverter;
			ClientValueGetter         = clientValueGetter;
			ItemAccessor              = itemAccessor;
		}

		public readonly int        ParameterId;
		public readonly string     ParameterName;
		public readonly DbDataType DbDataType;

		/// <summary>
		/// Body of Expression&lt;Func&lt;IQueryExpressions, IDataContext?, object?[]?, object?&gt;&gt;
		/// </summary>
		public readonly Expression ClientValueGetter;
		/// <summary>
		/// Body of Expression&lt;Func&lt;object?, object?&gt;&gt;
		/// </summary>
		public readonly Expression? ClientToProviderConverter;
		/// <summary>
		/// Body of Expression&lt;Func&lt;object?, object?&gt;&gt;
		/// </summary>
		public readonly Expression? ItemAccessor;
		/// <summary>
		/// Body of Expression&lt;Func&lt;object?, DbDataType&gt;&gt;
		/// </summary>
		public readonly Expression? DbDataTypeAccessor;

		public bool    IsEvaluated    { get; private set; }
		public object? EvaluatedValue { get; private set; }

		public void SetEvaluatedValue(object? value)
		{
			EvaluatedValue = value;
			IsEvaluated = true;
		}
	}

	class ExpressionCacheManager
	{
		readonly IUniqueIdGenerator<ExpressionCacheManager> _generator;

		public Expression MainExpression { get; }

		public Dictionary<Expression, DynamicExpressionInfo>?      DynamicAccessors         { get; private set; }
		public Dictionary<int, Dictionary<Expression, Expression>> AccessorsMapping         { get; } = new();
		public HashSet<Expression>                                 NonComparableExpressions { get; } = [];

		List<(object? value, Expression accessor)>?                     _byValueCompare;
		List<(SqlValue value, Expression accessor)>?                    _bySqlValueCompare;
		Dictionary<int, (Expression main, Expression other)>?           _duplicateParametersCheck;
		Dictionary<int, (Expression param, ParameterCacheEntry entry)>? _parameterEntries;

		List<SqlParameter>? _nonQueryParameters;

		public Expression RootPath      { get; set; } = ExpressionBuilder.QueryExpressionContainerParam;
		public bool       HasParameters => _parameterEntries  != null;
		public bool       HasConstants  => _bySqlValueCompare != null;

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

		public void RegisterDuplicateCheck(int parameterId, Expression mainParam, Expression otherParam)
		{
			_duplicateParametersCheck ??= new();
			if (_duplicateParametersCheck.ContainsKey(parameterId))
				return;

			_duplicateParametersCheck.Add(parameterId, (mainParam, otherParam));
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
		/// Registers parameter entry in cache. Searches for duplicates and registers them.
		/// </summary>
		/// <param name="paramExpr"></param>
		/// <param name="entry"></param>
		public void RegisterParameterEntry(Expression paramExpr, ParameterCacheEntry entry, Func<Expression, object?>? evaluator, out int finalParameterId)
		{
			void EnsureEvaluated(ParameterCacheEntry localEntry, Expression expr)
			{
				if (localEntry.IsEvaluated)
					return;

				localEntry.SetEvaluatedValue(evaluator(expr));
			}

			_parameterEntries ??= new();

			foreach (var pair in _parameterEntries.Values)
			{
				if (ExpressionEqualityComparer.Instance.Equals(pair.param, paramExpr)
					&& pair.entry.DbDataType.Equals(entry.DbDataType)
					&& ExpressionEqualityComparer.Instance.Equals(pair.entry.ClientValueGetter, entry.ClientValueGetter)
				    && ExpressionEqualityComparer.Instance.Equals(pair.entry.ClientToProviderConverter, entry.ClientToProviderConverter)
				    && ExpressionEqualityComparer.Instance.Equals(pair.entry.ItemAccessor, entry.ItemAccessor)
				    && ExpressionEqualityComparer.Instance.Equals(pair.entry.DbDataTypeAccessor, entry.DbDataTypeAccessor))
				{
					// found duplicate, we have to register value comparison

					finalParameterId = pair.entry.ParameterId;

					// register for duplicates only non the same parameter expressions
					if (!ReferenceEquals(pair.param, paramExpr))
					{
						RegisterDuplicateCheck(pair.entry.ParameterId, pair.entry.ClientValueGetter, entry.ClientValueGetter);
					}

					return;
				}
			}

			// find duplicates by name and value
			if (evaluator != null)
			{
				var testedName = SuggestParameterName(paramExpr);
				if (testedName != null)
				{
					foreach (var pair in _parameterEntries.Values)
					{
						if (paramExpr.Type.UnwrapNullableType() != pair.param.Type.UnwrapNullableType())
							continue;

						var iteratedName = SuggestParameterName(pair.param);

						if (
							iteratedName == testedName
							&& !ExpressionEqualityComparer.Instance.Equals(pair.param, paramExpr)
							&& pair.entry.DbDataType.EqualsDbOnly(entry.DbDataType)
							&& ExpressionEqualityComparer.Instance.Equals(pair.entry.ClientToProviderConverter, entry.ClientToProviderConverter)
							&& ExpressionEqualityComparer.Instance.Equals(pair.entry.ItemAccessor, entry.ItemAccessor)
							&& ExpressionEqualityComparer.Instance.Equals(pair.entry.DbDataTypeAccessor, entry.DbDataTypeAccessor))
						{
							EnsureEvaluated(pair.entry, pair.param);
							EnsureEvaluated(entry, paramExpr);

							if (Equals(pair.entry.EvaluatedValue, entry.EvaluatedValue))
							{
								// found duplicate, we have to register value comparison

								finalParameterId = pair.entry.ParameterId;

								RegisterDuplicateCheck(pair.entry.ParameterId, pair.entry.ClientValueGetter, entry.ClientValueGetter);

								return;
							}
						}
					}
				}
			
			}

			_parameterEntries.Add(entry.ParameterId, (paramExpr, entry));
			finalParameterId = entry.ParameterId;
		}

		public static string? SuggestParameterName(Expression? expression)
		{
			if (expression is MemberExpression member)
			{
				var result = member.Member.Name;
				if (member.Member.IsNullableValueMember())
					result = SuggestParameterName(member.Expression) ?? result;
				return result;
			}

			if (expression is UnaryExpression unary)
				return SuggestParameterName(unary.Operand);

			return null;
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
						if (ce.Operand.Type.IsNullable() && !ce.Type.IsNullable())
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

		/// <summary>
		/// All non-value type expressions and parameterized expressions will be transformed to ConstantPlaceholderExpression. It prevents from caching big reference classes in cache.
		/// </summary>
		/// <param name="expression"></param>
		/// <param name="nonComparable"></param>
		/// <param name="newParameterized"></param>
		/// <returns></returns>
		Expression ReplaceParameterizedAndClosures(Expression expression, HashSet<Expression> nonComparable, Dictionary<Expression, ConstantPlaceholderExpression>? newParameterized, MappingSchema mappingSchema)
		{
			var result = expression.Transform((nonComparable, newParameterized, mappingSchema), static (ctx, e) =>
			{
				if (ctx.newParameterized != null && ctx.newParameterized.TryGetValue(e, out var constantPlaceholder))
				{
					return new TransformInfo(constantPlaceholder, true);
				}

				if (ctx.nonComparable.Contains(e) && e is not ConstantExpression { Value: null })
				{
					var newValue = new ConstantPlaceholderExpression(e.Type);
					ctx.newParameterized![e] = newValue;
					return new TransformInfo(newValue, true);
				}

				if (e.NodeType == ExpressionType.Constant)
				{
					var c = (ConstantExpression)e;

					if (ExpressionCacheHelpers.ShouldRemoveConstantFromCache(c, ctx.mappingSchema))
					{
						var replacement = new ConstantPlaceholderExpression(e.Type);
						ctx.newParameterized![e] = replacement;
						return new TransformInfo(replacement);
					}
				}

				return new TransformInfo(e);
			});

			return result;
		}

		/// <summary>
		/// Builds <see cref="QueryCacheCompareInfo"/> for current cache state.
		/// </summary>
		/// <param name="dc"></param>
		/// <param name="parameterExpressions"></param>
		/// <param name="parameters"></param>
		/// <param name="sqlValues"></param>
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
				var usedEntries = _parameterEntries.Where(e => knownParameters.Any(p => p.AccessorId == e.Key)).Select(e => e.Value).ToList();
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
								AccessorExpr = clientValueGetterLambda
							}
#endif
							;

						parameterAccessors.Add(accessor);
					}
				}
			}

			List<DynamicExpressionInfo>? dynamicAccessors   = null;

			var replacements = new Dictionary<Expression, ConstantPlaceholderExpression>();

			if (DynamicAccessors != null)
			{
				var runtimeExpressions = new RuntimeExpressionsContainer(parameterExpressions.MainExpression);
				dynamicAccessors = new List<DynamicExpressionInfo>(DynamicAccessors.Count);

				foreach (var da in DynamicAccessors.Values)
				{
					runtimeExpressions.AddExpression(da.ExpressionId, da.Used);

					var replaced = ReplaceParameterizedAndClosures(da.Used, nonComparable, replacements, dc.MappingSchema);

					dynamicAccessors.Add(da with { Used = replaced });
				}

				parameterExpressions = runtimeExpressions;
			}

			List<(ValueAccessorFunc main, ValueAccessorFunc other)>? comparisionFunctions = null;

			if (_duplicateParametersCheck != null)
			{
				foreach (var (main, other) in _duplicateParametersCheck.Values)
				{
					var mainValueExpr  = PrepareExpressionAccessors(dc, main, nonComparable).EnsureType<object>();
					var otherValueExpr = PrepareExpressionAccessors(dc, other, nonComparable).EnsureType<object>();

					// IQueryExpressions queryExpressions, IDataContext  dataContext, object?[]? compiledParameters
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

			if (_bySqlValueCompare != null && sqlValues is { Count: > 0})
			{
				foreach (var (value, accessor) in _bySqlValueCompare)
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

			var mainExpression = ReplaceParameterizedAndClosures(MainExpression, nonComparable, replacements, dc.MappingSchema);
			var compareInfo    = new QueryCacheCompareInfo(mainExpression, dynamicAccessors, comparisionFunctions);

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
			_byValueCompare ??= new();
			_byValueCompare.Add((currentValue, expression));
		}

		public void RegisterSqlValue(Expression constantExpr, SqlValue value)
		{
			_bySqlValueCompare = _bySqlValueCompare ?? new();
			_bySqlValueCompare.Add((value, constantExpr));
		}

	}
}
