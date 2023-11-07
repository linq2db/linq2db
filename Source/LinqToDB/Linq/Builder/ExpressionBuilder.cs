using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Extensions;
	using Mapping;
	using SqlQuery;
	using Visitors;
	using LinqToDB.Expressions;
	using LinqToDB.Common.Internal;

	internal sealed partial class ExpressionBuilder : IExpressionEvaluator
	{
		#region Sequence

		static readonly object _sync = new ();

		static IReadOnlyList<ISequenceBuilder> _sequenceBuilders = new ISequenceBuilder[]
		{
			new TableBuilder               (),
			new IgnoreFiltersBuilder       (),
			new ContextRefBuilder          (),
			new SelectBuilder              (),
			new SelectManyBuilder          (),
			new WhereBuilder               (),
			new OrderByBuilder             (),
			new RemoveOrderByBuilder       (),
			new GroupByBuilder             (),
			new JoinBuilder                (),
			new GroupJoinBuilder           (),
			new AllJoinsBuilder            (),
			new AllJoinsLinqBuilder        (),
			new TakeSkipBuilder            (),
			new ElementAtBuilder           (),
			new DefaultIfEmptyBuilder      (),
			new DistinctBuilder            (),
			new FirstSingleBuilder         (),
			new AggregationBuilder         (),
			new MethodChainBuilder         (),
			new ScalarSelectBuilder        (),
			new SelectQueryBuilder         (),
			new PassThroughBuilder         (),
			new TableAttributeBuilder      (),
			new InsertBuilder              (),
			new InsertBuilder.Into         (),
			new InsertBuilder.Value        (),
			new InsertOrUpdateBuilder      (),
			new UpdateBuilder              (),
			new UpdateBuilder.Set          (),
			new DeleteBuilder              (),
			new ContainsBuilder            (),
			new AllAnyBuilder              (),
			new SetOperationBuilder        (),
			new CastBuilder                (),
			new OfTypeBuilder              (),
			new AsUpdatableBuilder         (),
			new AsValueInsertableBuilder   (),
			new LoadWithBuilder            (),
			new DropBuilder                (),
			new TruncateBuilder            (),
			new WithTableExpressionBuilder (),
			new MergeBuilder                             (),
			new MergeBuilder.InsertWhenNotMatched        (),
			new MergeBuilder.UpdateWhenMatched           (),
			new MergeBuilder.UpdateWhenMatchedThenDelete (),
			new MergeBuilder.UpdateWhenNotMatchedBySource(),
			new MergeBuilder.DeleteWhenMatched           (),
			new MergeBuilder.DeleteWhenNotMatchedBySource(),
			new MergeBuilder.On                          (),
			new MergeBuilder.Merge                       (),
			new MergeBuilder.MergeInto                   (),
			new MergeBuilder.Using                       (),
			new MergeBuilder.UsingTarget                 (),
			new ContextParser              (),
			new AsSubQueryBuilder          (),
			new DisableGroupingGuardBuilder(),
			new InlineParametersBuilder    (),
			new HasUniqueKeyBuilder        (),
			new MultiInsertBuilder         (),
			new TagQueryBuilder            (),
			new EnumerableBuilder          (),
			new QueryExtensionBuilder      (),
			new QueryNameBuilder           (),
		};

		#endregion

		#region Pools

		public static readonly ObjectPool<SelectQuery> QueryPool = new(() => new SelectQuery(), sq => sq.Cleanup(), 100);
		public static readonly ObjectPool<ParentInfo> ParentInfoPool = new(() => new ParentInfo(), pi => pi.Cleanup(), 100);

		#endregion

		#region Init

		readonly Query                             _query;
		readonly IReadOnlyList<ISequenceBuilder>   _builders = _sequenceBuilders;
		bool                                       _reorder;
		readonly ExpressionTreeOptimizationContext _optimizationContext;
		readonly ParametersContext                 _parametersContext;

		public ExpressionTreeOptimizationContext   OptimizationContext => _optimizationContext;
		public ParametersContext                   ParametersContext   => _parametersContext;

		public SqlComment?                      Tag;
		public List<SqlQueryExtension>?         SqlQueryExtensions;
		public List<TableBuilder.TableContext>? TablesInScope;

		public readonly DataOptions DataOptions;

		public ExpressionBuilder(
			Query                             query,
			ExpressionTreeOptimizationContext optimizationContext,
			ParametersContext                 parametersContext,
			IDataContext                      dataContext,
			Expression                        expression,
			ParameterExpression[]?            compiledParameters,
			object?[]?                        parameterValues)
		{
			_query               = query;

			CompiledParameters = compiledParameters;
			ParameterValues    = parameterValues;
			DataContext        = dataContext;
			DataOptions        = dataContext.Options;
			OriginalExpression = expression;

			_optimizationContext = optimizationContext;
			_parametersContext   = parametersContext;
			Expression           = expression;
			_optimizationContext.ClearVisitedCache();

			PreferExistsForScalar = DataOptions.LinqOptions.PreferExistsForScalar;
		}

		#endregion

		#region Public Members

		public readonly IDataContext           DataContext;
		public readonly Expression             OriginalExpression;
		public readonly Expression             Expression;
		public readonly ParameterExpression[]? CompiledParameters;
		public readonly object?[]?             ParameterValues;

		public static readonly ParameterExpression QueryRunnerParam = Expression.Parameter(typeof(IQueryRunner), "qr");
		public static readonly ParameterExpression DataReaderParam  = Expression.Parameter(typeof(DbDataReader), "rd");
		public static readonly ParameterExpression ParametersParam  = Expression.Parameter(typeof(object[]),     "ps");
		public static readonly ParameterExpression ExpressionParam  = Expression.Parameter(typeof(Expression),   "expr");
		public static readonly ParameterExpression RowCounterParam  = Expression.Parameter(typeof(int),          "counter");

		public MappingSchema MappingSchema => DataContext.MappingSchema;

		#endregion

		#region Builder SQL

		internal bool DisableDefaultIfEmpty;

		public Query<T> Build<T>()
		{
			var sequence = BuildSequence(new BuildInfo((IBuildContext?)null, Expression, new SelectQuery()));

			if (_reorder)
				lock (_sync)
				{
					_reorder = false;
					_sequenceBuilders = _sequenceBuilders.OrderByDescending(static _ => _.BuildCounter).ToArray();
				}

			_query.Init(sequence, _parametersContext.CurrentSqlParameters);

			var param = Expression.Parameter(typeof(Query<T>), "info");

			List<Preamble>? preambles = null;
			BuildQuery((Query<T>)_query, sequence, param, ref preambles, Array<Expression>.Empty);

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
			_query.SetParametrized(_parametersContext.GetParametrized());

			return (Query<T>)_query;
		}

		void BuildQuery<T>(
			Query<T>            query,
			IBuildContext       sequence,
			ParameterExpression queryParameter,
			ref List<Preamble>? preambles,
			Expression[]        previousKeys)
		{
			var expr = MakeExpression(sequence, new ContextRefExpression(typeof(T), sequence), ProjectFlags.Expression);

			var finalized = FinalizeProjection(query, sequence, expr, queryParameter, ref preambles, previousKeys);

			sequence.SetRunQuery(query, finalized);
		}

		/// <summary>
		/// Contains information from which expression sequence were built. Used for Eager Loading.
		/// </summary>
		Dictionary<IBuildContext, Expression> _sequenceExpressions = new();

		public Expression? GetSequenceExpression(IBuildContext sequence)
		{
			if (_sequenceExpressions.TryGetValue(sequence, out var expr))
				return expr;

			if (sequence is SubQueryContext sc)
				return GetSequenceExpression(sc.SubQuery);

			if (sequence is ScopeContext scoped)
				return GetSequenceExpression(scoped.Context);

			return null;
		}

		public void AddAsParametrized(Expression expression)
		{
			_parametersContext.AddAsParametrized(expression);
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
			return result;
		}

		Expression ExpandToRoot(Expression expression, BuildInfo buildInfo)
		{
			var flags = buildInfo.IsAggregation ? ProjectFlags.AggregationRoot : ProjectFlags.Root;

			flags = buildInfo.GetFlags(flags) | ProjectFlags.Subquery;

			expression = UnwrapSequenceExpression(expression);
			Expression result;
			do
			{
				result = MakeExpression(buildInfo.Parent, expression, flags);
				result = UnwrapSequenceExpression(result);

				if (ExpressionEqualityComparer.Instance.Equals(expression, result))
					break;

				expression = result;

			} while (true);

			return result;
		}

		public IBuildContext? TryBuildSequence(BuildInfo buildInfo)
		{
			var originalExpression = buildInfo.Expression;

			var expanded = ExpandToRoot(buildInfo.Expression, buildInfo);

			if (!ReferenceEquals(expanded, originalExpression))
				buildInfo = new BuildInfo(buildInfo, expanded);

			var n = _builders[0].BuildCounter;

			foreach (var builder in _builders)
			{
				if (builder.CanBuild(this, buildInfo))
				{
					var sequence = builder.BuildSequence(this, buildInfo);

					lock (builder)
						builder.BuildCounter++;

					_reorder = _reorder || n < builder.BuildCounter;

					if (sequence != null)
					{
#if DEBUG
						if (!buildInfo.IsTest)
							QueryHelper.DebugCheckNesting(sequence.GetResultStatement(), buildInfo.IsSubQuery);
#endif
						RegisterSequenceExpression(sequence, originalExpression);
					}

					return sequence;
				}

				n = builder.BuildCounter;
			}

			return null;
		}

		public IBuildContext BuildSequence(BuildInfo buildInfo)
		{
			var sequence = TryBuildSequence(buildInfo);
			if (sequence == null)
				throw new LinqException("Sequence '{0}' cannot be converted to SQL.", SqlErrorExpression.PrepareExpression(buildInfo.Expression));
			return sequence;
		}

		public ISequenceBuilder? GetBuilder(BuildInfo buildInfo, bool throwIfNotFound = true)
		{
			buildInfo.Expression = buildInfo.Expression.Unwrap();

			foreach (var builder in _builders)
				if (builder.CanBuild(this, buildInfo))
					return builder;

			if (throwIfNotFound)
				throw new LinqException("Sequence '{0}' cannot be converted to SQL.", SqlErrorExpression.PrepareExpression(buildInfo.Expression));
			return null;
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

			foreach (var builder in _builders)
				if (builder.CanBuild(this, buildInfo))
					return builder.IsSequence(this, buildInfo);

			return false;
		}

		#endregion

		#region ConvertExpression

		public ParameterExpression? SequenceParameter;

		public Expression ConvertExpressionTree(Expression expression)
		{
			var expr = expression;

			expr = ExposeExpression(expression, DataContext, _optimizationContext, optimizeConditions:false, compactBinary:true);

			return expr;
		}

		public static Expression CorrectDataConnectionReference(Expression queryExpression, Expression dataContextExpression)
		{
			var result = queryExpression.Transform(dataContextExpression, static(dc, e) =>
			{
				if (e.NodeType != ExpressionType.Parameter && e.NodeType != ExpressionType.Convert &&
				    e.NodeType != ExpressionType.ConvertChecked
				    && dc.Type.IsSameOrParentOf(e.Type))
				{
					var newExpr = dc;
					if (newExpr.Type != e.Type)
						newExpr = Expression.Convert(newExpr, e.Type);
					return newExpr;
				}

				return e;
			});

			return result;
		}

		#endregion

		#region ConvertParameters

		Expression ConvertParameters(Expression expression)
		{
			if (CompiledParameters == null) return expression;

			return expression.Transform(CompiledParameters, static(compiledParameters, expr) =>
			{
				if (expr.NodeType == ExpressionType.Parameter)
				{
					var idx = Array.IndexOf(compiledParameters, (ParameterExpression)expr);
					if (idx >= 0)
						return Expression.Convert(
							Expression.ArrayIndex(ParametersParam, ExpressionInstances.Int32(idx)),
							expr.Type);
				}

				return expr;
			});
		}

		#endregion

		#region ExposeExpression

		static ObjectPool<ExposeExpressionVisitor> _exposeVisitorPool = new(() => new ExposeExpressionVisitor(), v => v.Cleanup(), 100);

		public static Expression ExposeExpression(Expression expression, IDataContext dataContext, ExpressionTreeOptimizationContext optimizationContext, bool optimizeConditions, bool compactBinary)
		{
			using var visitor = _exposeVisitorPool.Allocate();

			var result = visitor.Value.ExposeExpression(dataContext, optimizationContext, expression, false, optimizeConditions, compactBinary);

			return result;
		}

		public Expression ExposeSingleExpression(Expression expression, bool inProjection)
		{
			return _optimizationContext.ExposeExpressionTransformer(expression).Expression;
		}

		#endregion

		#region OptimizeExpression

		public static readonly MethodInfo[] EnumerableMethods      = typeof(Enumerable     ).GetMethods();
		public static readonly MethodInfo[] QueryableMethods       = typeof(Queryable      ).GetMethods();

		#endregion

		#region ConvertElementAt

		#endregion

		#region SqQueryDepended support

		public Expression AddQueryableMemberAccessors<TContext>(TContext context, AccessorMember memberInfo, IDataContext dataContext,
			Func<TContext, MemberInfo, IDataContext, Expression> qe)
		{
			return _query.AddQueryableMemberAccessors(context, memberInfo.MemberInfo, dataContext, qe);
		}

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
			if (expression == null)
				return null;

			// Shortcut for constants
			if (expression.NodeType == ExpressionType.Constant)
				return ((ConstantExpression)expression).Value;

			var expr = expression.Transform(e =>
			{
				if (e is SqlQueryRootExpression root)
				{
					if (((IConfigurationID)root.MappingSchema).ConfigurationID ==
					    ((IConfigurationID)DataContext.MappingSchema).ConfigurationID)
					{
						return Expression.Constant(DataContext, e.Type);
					}
				}
				else if (e.NodeType == ExpressionType.ArrayIndex)
				{
					if (ParameterValues != null)
					{
						var arrayIndexExpr = (BinaryExpression)e;

						var index = EvaluateExpression(arrayIndexExpr.Right) as int?;
						if (index != null)
						{
							return Expression.Constant(ParameterValues[index.Value]);
						}
					}
				}
				else if (e.NodeType == ExpressionType.Parameter)
				{
					if (e == ExpressionConstants.DataContextParam)
					{
						return Expression.Constant(DataContext, e.Type);
					}
				}

				return e;
			});

			return expr.EvaluateExpression();
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
			return CanBeCompiled(expression, false);
		}

		public object? Evaluate(Expression expression)
		{
			return EvaluateExpression(expression);
		}

		#endregion
	}
}
