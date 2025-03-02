using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Conversion;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Infrastructure;
using LinqToDB.Internal.Linq.Builder.Visitors;
using LinqToDB.Internal.Linq.Translation;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.Metrics;

namespace LinqToDB.Internal.Linq.Builder
{
	internal sealed partial class ExpressionBuilder : IExpressionEvaluator
	{
		#region Sequence

		public bool TryFindBuilder(BuildInfo info, [NotNullWhen(true)] out ISequenceBuilder? builder)
		{
			builder = FindBuilderImpl(info, this);
			return builder != null;
		}

		// Declaring a partial FindBuilderImpl so that the source generator doesn't change semantics
		// and can use `RegisterImplementationSourceOutput` which is less taxing for VS / IDE.
		private static partial ISequenceBuilder? FindBuilderImpl(BuildInfo info, ExpressionBuilder builder);

		#endregion

		#region Pools

		public static readonly ObjectPool<SelectQuery>               QueryPool               = new(() => new SelectQuery(), sq => sq.Cleanup(), 100);
		public static readonly ObjectPool<ParentInfo>                ParentInfoPool           = new(() => new ParentInfo(), pi => pi.Cleanup(), 100);

		static readonly ObjectPool<PlaceholderCollectVisitor> _placeholderCollectorPool = new(() => new PlaceholderCollectVisitor(), sq => sq.Cleanup(), 100);

		#endregion

		#region Init

		readonly          Query                             _query;
		internal readonly IMemberTranslator                 _memberTranslator;
		readonly          ExpressionTreeOptimizationContext _optimizationContext;
		readonly          ParametersContext                 _parametersContext;

		ExpressionBuildVisitor _buildVisitor;

		public ExpressionTreeOptimizationContext   OptimizationContext => _optimizationContext;
		public ParametersContext                   ParametersContext   => _parametersContext;

		public SqlComment?                      Tag;
		public List<SqlQueryExtension>?         SqlQueryExtensions;
		public List<TableBuilder.TableContext>? TablesInScope;

		public bool ValidateSubqueries { get; }

		public readonly DataOptions DataOptions;

		public ExpressionBuilder(
			Query                             query,
			bool                              validateSubqueries,
			ExpressionTreeOptimizationContext optimizationContext,
			ParametersContext                 parametersContext,
			IDataContext                      dataContext,
			Expression                        expression,
			object?[]?                        parameterValues)
		{
			_query             = query;
			ValidateSubqueries = validateSubqueries;

			ParameterValues    = parameterValues;
			DataContext        = dataContext;
			DataOptions        = dataContext.Options;

			_memberTranslator = ((IInfrastructure<IServiceProvider>)dataContext).Instance.GetRequiredService<IMemberTranslator>();

			_buildVisitor = new ExpressionBuildVisitor(this);
			
			_globalModifier = TranslationModifier.Default.WithInlineParameters(dataContext.InlineParameters);

			if (DataOptions.DataContextOptions.MemberTranslators != null)
			{
				// register overriden translators first
				_memberTranslator = new CombinedMemberTranslator(DataOptions.DataContextOptions.MemberTranslators.Concat(new[] { _memberTranslator }));
			}

			_optimizationContext = optimizationContext;
			_parametersContext   = parametersContext;
			Expression           = expression;
		}

		#endregion

		#region Public Members

		public readonly IDataContext           DataContext;
		public readonly Expression             Expression;
		public readonly ParameterExpression[]? CompiledParameters;
		public readonly object?[]?             ParameterValues;

		public static readonly ParameterExpression QueryRunnerParam              = Expression.Parameter(typeof(IQueryRunner), "qr");
		public static readonly ParameterExpression DataReaderParam               = Expression.Parameter(typeof(DbDataReader), "rd");
		public static readonly ParameterExpression ParametersParam               = Expression.Parameter(typeof(object[]),     "ps");
		public static readonly ParameterExpression QueryExpressionContainerParam = Expression.Parameter(typeof(IQueryExpressions), "container");
		public static readonly ParameterExpression RowCounterParam               = Expression.Parameter(typeof(int),          "counter");

		public MappingSchema MappingSchema => DataContext.MappingSchema;

		#endregion

		#region Builder SQL

		public Query<T> Build<T>(ref IQueryExpressions expressions)
		{
			using var m = ActivityService.Start(ActivityID.Build);

			var sequence = BuildSequence(new BuildInfo((IBuildContext?)null, Expression, new SelectQuery()));

			using var mq = ActivityService.Start(ActivityID.BuildQuery);

			_query.Init(sequence);

			var param = Expression.Parameter(typeof(Query<T>), "info");

			List<Preamble>? preambles = null;
			BuildQuery((Query<T>)_query, sequence, param, ref preambles, []);

			if (_query.ErrorExpression == null)
			{
				foreach (var q in _query.Queries)
				{
					if (Tag?.Lines.Count > 0)
					{
						(q.Statement.Tag ??= new()).Lines.AddRange(Tag.Lines);
					}

					if (SqlQueryExtensions != null)
					{
						(q.Statement.SqlQueryExtensions ??= new()).AddRange(SqlQueryExtensions);
					}
				}

				_query.SetPreambles(preambles);

				expressions = FinalizeQueryCacheInformation((Query<T>)_query, preambles);
			}

			return (Query<T>)_query;
		}

		bool BuildQuery<T>(
			Query<T>            query,
			IBuildContext       sequence,
			ParameterExpression queryParameter,
			ref List<Preamble>? preambles,
			Expression[]        previousKeys)
		{
			var expr = _buildVisitor.BuildExpression(sequence, new ContextRefExpression(typeof(T), sequence), buildPurpose: BuildPurpose.Expression);

			var finalized = FinalizeProjection(query, sequence, expr, queryParameter, ref preambles, previousKeys);

			var error = SequenceHelper.FindError(finalized);
			if (error != null)
			{
				query.ErrorExpression = error;
				return false;
			}

			using (ActivityService.Start(ActivityID.FinalizeQuery))
			{
				foreach (var queryInfo in query.Queries)
				{
					queryInfo.Statement = query.SqlOptimizer.Finalize(query.MappingSchema, queryInfo.Statement, query.DataOptions);

					if (queryInfo.Statement.SelectQuery != null)
					{
						if (!SqlProviderHelper.IsValidQuery(queryInfo.Statement.SelectQuery, parentQuery: null, fakeJoin: null, columnSubqueryLevel: null, DataContext.SqlProviderFlags, out var errorMessage))
						{
							query.ErrorExpression = new SqlErrorExpression(Expression, errorMessage, Expression.Type);
							return false;
						}
					}
				}

				// Applying accessors to all found constants in mapping expression
				finalized = ParametersContext.ApplyAccessors(finalized);

				query.IsFinalized = true;
				sequence.SetRunQuery(query, finalized);
				FinalizeQueryCacheInformation(query, preambles);
			}

			return true;
		}

		IQueryExpressions FinalizeQueryCacheInformation<T>(Query<T> query, List<Preamble>? preambles)
		{
			List<SqlParameter>? builtParameters = null;
			List<SqlValue>?     builtValues     = null;

			if (ParametersContext.CacheManager.HasParameters || ParametersContext.CacheManager.HasConstants)
			{
				var usedParameters = new HashSet<SqlParameter>();
				var usedValues     = new HashSet<SqlValue>();

				foreach (var queryInfo in query.Queries)
				{
					QueryHelper.CollectParametersAndValues(queryInfo.Statement, usedParameters, usedValues);
				}

				if (preambles?.Count > 0)
				{
					foreach (var preamble in preambles)
					{
						preamble.GetUsedParametersAndValues(usedParameters, usedValues);
					}
				}

				builtParameters = usedParameters.ToList();
				builtValues     = usedValues.ToList();
			}

			var (compareInfo, parameterAccessors, expressions) = ParametersContext.CacheManager.BuildQueryCacheCompareInfo(this, DataContext, ParametersContext.ParametersExpression, builtParameters, builtValues);
			
			query.ParameterAccessors = parameterAccessors;
			query.CompareInfo        = compareInfo;
			query.BuiltParameters    = builtParameters;

			return expressions;
		}

		Stack<Expression>? _recursiveBuildItems;

		internal void PushRecursive(Expression expression)
		{
			_recursiveBuildItems ??= new();
			_recursiveBuildItems.Push(expression);
		}

		internal void PopRecursive(Expression expression)
		{
			if (_recursiveBuildItems == null || _recursiveBuildItems.Count == 0 || !ReferenceEquals(expression, _recursiveBuildItems.Pop()))
				throw new InvalidOperationException("Wrong Push/Pop for Recursive Build");
		}

		internal bool IsUnderRecursiveBuild(Expression expression)
		{
			if (_recursiveBuildItems == null || _recursiveBuildItems.Count == 0)
				return false;

			if (!_recursiveBuildItems.Contains(expression, ExpressionEqualityComparer.Instance))
				return false;

			return true;
		}

		/// <summary>
		/// Used internally to avoid RecursiveCTE build failing
		/// </summary>
		internal bool IsRecursiveBuild => _recursiveBuildItems?.Count > 0;

		/// <summary>
		/// Contains information from which expression sequence were built. Used for Eager Loading.
		/// </summary>
		Dictionary<IBuildContext, Expression> _sequenceExpressions = new();

		public Expression? GetSequenceExpression(IBuildContext sequence)
		{
			if (_sequenceExpressions.TryGetValue(sequence, out var expr))
				return expr;

			return sequence switch
			{
				SubQueryContext sc => GetSequenceExpression(sc.SubQuery),
				ScopeContext sc => GetSequenceExpression(sc.Context),
				_ => null,
			};
		}

		public void RegisterSequenceExpression(IBuildContext sequence, Expression expression)
		{
			if (!_sequenceExpressions.ContainsKey(sequence))
			{
				_sequenceExpressions[sequence] = expression;
			}
		}

		Expression UnwrapSequenceExpression(Expression expression)
		{
			var result = expression.Unwrap();

			if (result is SqlDefaultIfEmptyExpression defaultIfEmpty)
				result = UnwrapSequenceExpression(defaultIfEmpty.InnerExpression);

			if (result.NodeType is ExpressionType.Conditional)
			{
				var newResult = RemoveNullPropagation(result, true);

				if (!ReferenceEquals(newResult, result))
					return UnwrapSequenceExpression(newResult);
			}

			return result;
		}

		Expression ExpandToRoot(Expression expression, BuildInfo buildInfo)
		{
			expression = UnwrapSequenceExpression(expression);
			Expression result;
			do
			{
				result = buildInfo.IsAggregation ? BuildAggregationRootExpression(expression) : BuildSubqueryExpression(expression);
				result = UnwrapSequenceExpression(result);

				if (ExpressionEqualityComparer.Instance.Equals(expression, result))
					break;

				expression = result;

			} while (true);

			return result;
		}

		public BuildSequenceResult TryBuildSequence(BuildInfo buildInfo)
		{
			using var m = ActivityService.Start(ActivityID.BuildSequence);

			var originalExpression = buildInfo.Expression;

			var expanded = ExpandToRoot(buildInfo.Expression, buildInfo);

			if (!ReferenceEquals(expanded, originalExpression))
				buildInfo = new BuildInfo(buildInfo, expanded);

			if (!TryFindBuilder(buildInfo, out var builder))
				return BuildSequenceResult.NotSupported();

			using var mb = ActivityService.Start(ActivityID.BuildSequenceBuild);

			#if DEBUG

			Debug.WriteLine($"Building {builder.GetType().Name}");

			#endif

			var result = builder.BuildSequence(this, buildInfo);

			if (result.BuildContext != null)
			{
				RegisterSequenceExpression(result.BuildContext, originalExpression);
			}

			if (!result.IsSequence)
				return BuildSequenceResult.Error(originalExpression);

			return result;
		}

		public IBuildContext BuildSequence(BuildInfo buildInfo)
		{
			var buildResult = TryBuildSequence(buildInfo);
			if (buildResult.BuildContext == null)
			{
				var errorExpr = buildResult.ErrorExpression ?? buildInfo.Expression;

				if (errorExpr is SqlErrorExpression error)
					throw error.CreateException();

				throw SqlErrorExpression.CreateException(errorExpr, buildResult.AdditionalDetails);
			}

			return buildResult.BuildContext;
		}

		public bool IsSequence(IBuildContext? parent, Expression expression)
		{
			using var query = QueryPool.Allocate();
			return IsSequence(new BuildInfo(parent, expression, query.Value));
		}

		public bool IsSequence(BuildInfo buildInfo)
		{
			var originalExpression = buildInfo.Expression;

			buildInfo.Expression = ExpandToRoot(originalExpression, buildInfo);

			if (!TryFindBuilder(buildInfo, out var builder))
				return false;
			
			return builder.IsSequence(this, buildInfo);
		}

		#endregion

		#region ConvertExpression

		public Expression ConvertExpressionTree(Expression expression)
		{
			var expr = ExposeExpression(expression, DataContext, _optimizationContext, ParameterValues, optimizeConditions:false, compactBinary:true);

			return expr;
		}

		#endregion

		#region ExposeExpression

		static ObjectPool<ExposeExpressionVisitor> _exposeVisitorPool = new(() => new ExposeExpressionVisitor(), v => v.Cleanup(), 100);

		public static Expression ExposeExpression(Expression expression, IDataContext dataContext, ExpressionTreeOptimizationContext optimizationContext, object?[]? parameterValues, bool optimizeConditions, bool compactBinary)
		{
			using var visitor = _exposeVisitorPool.Allocate();

			var result = visitor.Value.ExposeExpression(dataContext, optimizationContext, parameterValues, expression, false, optimizeConditions, compactBinary, isSingleConvert: false);

			return result;
		}

		#endregion

		#region OptimizeExpression

		public static readonly MethodInfo[] EnumerableMethods      = typeof(Enumerable     ).GetMethods();

		#endregion

		#region ConvertElementAt

		#endregion

		#region Set Context Helpers

		Dictionary<int, int>? _generatedSetIds;

		public int GenerateSetId(int sourceId)
		{
			_generatedSetIds ??= new ();

			if (_generatedSetIds.TryGetValue(sourceId, out var setId))
				return setId;

			setId = _generatedSetIds.Count;
			_generatedSetIds.Add(sourceId, setId);
			return setId;
		}

		#endregion

		#region Helpers

#if DEBUG
		int _contextCounter;

		public int GenerateContextId()
		{
			var nextId = ++_contextCounter;
			return nextId;
		}
#endif

		/// <summary>
		/// Gets Expression.Equal if <paramref name="left"/> and <paramref name="right"/> expression types are not same
		/// <paramref name="right"/> would be converted to <paramref name="left"/>
		/// </summary>
		/// <param name="mappingSchema"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static BinaryExpression Equal(MappingSchema mappingSchema, Expression left, Expression right)
		{
			var leftType  = left.Type;
			leftType = leftType.ToNullableUnderlying();
			var rightType = right.Type;
			rightType = rightType.ToNullableUnderlying();

			if (leftType != rightType)
			{
				if (rightType.CanConvertTo(leftType))
					right = Expression.Convert(right, leftType);
				else if (leftType.CanConvertTo(rightType))
					left = Expression.Convert(left, rightType);
				else
				{
					var rightConvert = ConvertBuilder.GetConverter(mappingSchema, rightType, leftType);
					var leftConvert  = ConvertBuilder.GetConverter(mappingSchema, leftType, rightType);

					var leftIsPrimitive  = leftType.IsPrimitive;
					var rightIsPrimitive = rightType.IsPrimitive;

					if (leftIsPrimitive && !rightIsPrimitive && rightConvert.Item2 != null)
						right = rightConvert.Item2.GetBody(right);
					else if (!leftIsPrimitive && rightIsPrimitive && leftConvert.Item2 != null)
						left = leftConvert.Item2.GetBody(left);
					else if (rightConvert.Item2 != null)
						right = rightConvert.Item2.GetBody(right);
					else if (leftConvert.Item2 != null)
						left = leftConvert.Item2.GetBody(left);
				}
			}

			if (left.Type != right.Type)
			{
				if (left.Type.IsNullable())
					right = Expression.Convert(right, left.Type);
				else
					left = Expression.Convert(left, right.Type);
			}

			return Expression.Equal(left, right);
		}

		public object? EvaluateExpression(Expression? expression)
		{
			return EvaluationHelper.EvaluateExpression(expression, DataContext, ParameterValues);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T? EvaluateExpression<T>(Expression? expr)
			where T : class
		{
			return EvaluateExpression(expr) as T;
		}

		#endregion

		#region IExpressionEvaluator

		public bool CanBeEvaluated(Expression expression)
		{
			return CanBeEvaluatedOnClient(expression);
		}

		public object? Evaluate(Expression expression)
		{
			return EvaluateExpression(expression);
		}

		#endregion
	}
}
