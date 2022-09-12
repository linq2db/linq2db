using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;

namespace LinqToDB.Linq.Builder
{
	using Async;
	using Common;
	using Common.Internal;
	using Extensions;
	using Reflection;
	using SqlQuery;

	partial class ExpressionBuilder
	{

		static void CollectDependencies(Expression expression, HashSet<Expression> dependencies)
		{
			var toIgnore     = new HashSet<Expression>();
			expression.Visit((dependencies, toIgnore), static (ctx, e) =>
			{
				if (ctx.toIgnore.Contains(e))
					return;

				if (e.NodeType == ExpressionType.MemberAccess)
				{
					var current = e;
					do
					{
						if (current is not MemberExpression me)
							break;

						current = me.Expression;
						if (current is ContextRefExpression)
						{
							ctx.dependencies.Add(e);
							// add others in path to ignore
							var subCurrent = (MemberExpression)e;
							do
							{
								ctx.toIgnore.Add(subCurrent);

								if (subCurrent.Expression is not MemberExpression sm)
									break;

								subCurrent = sm;
							} while (true);

							break;
						}
					} while (true);
				}
				else if (e is BinaryExpression binary)
				{
					if (binary.Left is ContextRefExpression)
						ctx.dependencies.Add(binary.Left);
					if (binary.Right is ContextRefExpression)
						ctx.dependencies.Add(binary.Right);
				}
			});
		}

		static Expression GenerateKeyExpression(Expression[] members, int startIndex)
		{
			var count = members.Length - startIndex;
			if (count == 0)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			Expression[] arguments;

			if (count > MutableTuple.MaxMemberCount)
			{
				count     = MutableTuple.MaxMemberCount;
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count - 1);
				arguments[count - 1] = GenerateKeyExpression(members, startIndex + count);
			}
			else
			{
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count);
			}

			var type         = MutableTuple.MTypes[count - 1];
			var concreteType = type.MakeGenericType(arguments.Select(a => a.Type).ToArray());
			var constructor = concreteType.GetConstructor(Type.EmptyTypes) ??
			                  throw new LinqToDBException($"Can not retrieve default constructor for '{type.Name}'");

			var newExpression = Expression.New(constructor);
			var initExpression = Expression.MemberInit(newExpression,
				arguments.Select((a, i) => Expression.Bind(concreteType.GetProperty("Item" + (i + 1))!, a)));
			return initExpression;
		}

		struct KeyDetailEnvelope<TKey, TDetail>
			where TKey: notnull
		{
			public TKey    Key;
			public TDetail Detail;
		}

		public static Type GetEnumerableElementType(Type type)
		{
			var genericType = typeof(IEnumerable<>).GetGenericType(type);
			if (genericType == null)
				throw new InvalidOperationException($"Type '{type.Name}' do not implement IEnumerable");

			return genericType.GetGenericArguments()[0];
		}

		Expression ExpandContexts(IBuildContext currentContext, Expression expression)
		{
			var result = expression.Transform((builder: this, currentContext), static (ctx, e) =>
			{
				if (e.NodeType == ExpressionType.Extension || e.NodeType == ExpressionType.MemberAccess ||
				    e.NodeType == ExpressionType.Call)
				{
					if (e.NodeType == ExpressionType.MemberAccess)
					{
						if (null != e.Find(e, (_, e) => e.NodeType == ExpressionType.Parameter))
						{
							return new TransformInfo(e);
						}
					}

					var newExpr = ctx.builder.MakeExpression(ctx.currentContext, e, ProjectFlags.Expand);

					return new TransformInfo(newExpr, false);
				}

				return new TransformInfo(e);
			});

			return result;
		}

		Expression ProcessEagerLoadingExpression(
			IBuildContext          buildContext,  
			SqlEagerLoadExpression eagerLoad,
			ParameterExpression    queryParameter, 
			List<Preamble>         preambles,
			Expression[]           previousKeys)
		{
			var cloningContext       = new CloningContext();
			var clonedParentContext  = cloningContext.CloneContext(buildContext);
			
			var dependencies = new HashSet<Expression>(ExpressionEqualityComparer.Instance);

			var sequenceExpression = eagerLoad.SequenceExpression;

			sequenceExpression = ExpandContexts(buildContext, sequenceExpression);

			var correctedSequence    = cloningContext.CloneExpression(sequenceExpression);

			var clonedMainContextRef = cloningContext.CloneExpression(eagerLoad.ContextRef);
			CollectDependencies(sequenceExpression, dependencies);

			dependencies.AddRange(previousKeys);

			var mainKeys   = new Expression[dependencies.Count];
			var detailKeys = new Expression[dependencies.Count];

			int i = 0;
			foreach (var dependency in dependencies)
			{
				mainKeys[i]   = dependency;
				detailKeys[i] = cloningContext.CloneExpression(dependency);
				++i;
			}

			cloningContext.UpdateContextParents();

			Expression resultExpression;

			var mainType   = GetEnumerableElementType(clonedMainContextRef.Type);
			var detailType = GetEnumerableElementType(eagerLoad.Type);

			if (dependencies.Count == 0)
			{
				var detailSequence = BuildSequence(new BuildInfo((IBuildContext?)null, correctedSequence, new SelectQuery()));

				var parameters = new object[] { detailSequence, correctedSequence, queryParameter, preambles };

				resultExpression = (Expression)_buildPreambleQueryDetachedMethodInfo
					.MakeGenericMethod(detailType)
					.Invoke(this, parameters);
			}
			else
			{
				var mainKeyExpression   = GenerateKeyExpression(mainKeys, 0);
				var detailKeyExpression = GenerateKeyExpression(detailKeys, 0);

				var keyDetailType   = typeof(KeyDetailEnvelope<,>).MakeGenericType(mainKeyExpression.Type, detailType);
				var mainParameter   = Expression.Parameter(mainType, "m");
				var detailParameter = Expression.Parameter(detailType, "d");

				var keyDetailExpression = Expression.MemberInit(Expression.New(keyDetailType),
					Expression.Bind(keyDetailType.GetField(nameof(KeyDetailEnvelope<int, int>.Key)), detailKeyExpression),
					Expression.Bind(keyDetailType.GetField(nameof(KeyDetailEnvelope<int, int>.Detail)), detailParameter));

				var clonedParentContextRef = new ContextRefExpression(clonedMainContextRef.Type, clonedParentContext);

				Expression sourceQuery = clonedParentContextRef;

				if (!typeof(IQueryable<>).IsSameOrParentOf(sourceQuery.Type))
				{
					sourceQuery = Expression.Call(Methods.Enumerable.AsQueryable.MakeGenericMethod(mainType), sourceQuery);
				}

				sourceQuery = Expression.Call(Methods.LinqToDB.SelectDistinct.MakeGenericMethod(mainType), sourceQuery);

				var selector = Expression.Lambda(keyDetailExpression, mainParameter, detailParameter);

				var detailSelectorBody = correctedSequence;

				var detailSelector = (LambdaExpression)_buildSelectManyDetailSelectorInfo
					.MakeGenericMethod(mainType, detailType).Invoke(null, new object[] { detailSelectorBody, mainParameter });

				var selectManyCall =
					Expression.Call(
						Methods.Queryable.SelectManyProjection.MakeGenericMethod(mainType, detailType, keyDetailType),
						sourceQuery, Expression.Quote(detailSelector), Expression.Quote(selector));

				var saveExpressionCache = _expressionCache;

				_expressionCache = saveExpressionCache.ToDictionary(p =>
						new SqlCacheKey(
							cloningContext.CorrectExpression(p.Key.Expression),
							cloningContext.CorrectContext(p.Key.Context), p.Key.ColumnDescriptor,
							cloningContext.CorrectElement(p.Key.SelectQuery), p.Key.Flags),
					p => cloningContext.CorrectExpression(p.Value), SqlCacheKey.SqlCacheKeyComparer);

				var saveColumnsCache = _columnCache;

				_columnCache = _columnCache.ToDictionary(p =>
						new ColumnCacheKey(
							cloningContext.CorrectExpression(p.Key.Expression),
							p.Key.ResultType,
							cloningContext.CorrectElement(p.Key.SelectQuery),
							cloningContext.CorrectElement(p.Key.ParentQuery)),
					p => cloningContext.CorrectExpression(p.Value), ColumnCacheKey.ColumnCacheKeyComparer);

				var saveSqlCache = _cachedSql;

				_cachedSql = _cachedSql.ToDictionary(p =>
						new SqlCacheKey(
							cloningContext.CorrectExpression(p.Key.Expression),
							cloningContext.CorrectContext(p.Key.Context), p.Key.ColumnDescriptor,
							cloningContext.CorrectElement(p.Key.SelectQuery), p.Key.Flags),
					p => cloningContext.CorrectExpression(p.Value), SqlCacheKey.SqlCacheKeyComparer);

				var detailSequence = BuildSequence(new BuildInfo((IBuildContext?)null, selectManyCall,
					clonedParentContextRef.BuildContext.SelectQuery));

				var parameters = new object[] { detailSequence, mainKeyExpression, selectManyCall, queryParameter, preambles, detailKeys };

				resultExpression = (Expression)_buildPreambleQueryAttachedMethodInfo
					.MakeGenericMethod(mainKeyExpression.Type, detailType)
					.Invoke(this, parameters);

				_expressionCache = saveExpressionCache;
				_columnCache     = saveColumnsCache;
				_cachedSql       = saveSqlCache;
			}

			resultExpression = AdjustType(resultExpression, eagerLoad.Type, MappingSchema);

			return resultExpression;
		}

		static MethodInfo _buildSelectManyDetailSelectorInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(BuildSelectManyDetailSelector), BindingFlags.Static | BindingFlags.NonPublic) ?? throw new InvalidOperationException();

		static LambdaExpression BuildSelectManyDetailSelector<TMain, TDetail>(Expression body, ParameterExpression mainParam)
		{
			return Expression.Lambda<Func<TMain, IEnumerable<TDetail>>>(body, mainParam);
		}

		static MethodInfo _buildPreambleQueryAttachedMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(BuildPreambleQueryAttached), BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException();

		Expression BuildPreambleQueryAttached<TKey, T>(
			IBuildContext       sequence, 
			Expression          keyExpression,
			Expression          queryExpression, 
			ParameterExpression queryParameter,
			List<Preamble>      preambles,
			Expression[]        previousKeys) 
			where TKey : notnull
		{
			var query = new Query<KeyDetailEnvelope<TKey, T>>(DataContext, queryExpression);

			query.Init(sequence, _parametersContext.CurrentSqlParameters);

			BuildQuery(query, sequence, queryParameter, ref preambles!, previousKeys);

			var idx      = preambles.Count;
			var preamble = new Preamble<TKey, T>(query);
			preambles.Add(preamble);

			var getListMethod = MemberHelper.MethodOf((PreambleResult<TKey, T> c) => c.GetList(default!));

			var resultExpression =
				Expression.Call(
					Expression.Convert(Expression.ArrayIndex(PreambleParam, ExpressionInstances.Int32(idx)),
						typeof(PreambleResult<TKey, T>)), getListMethod, keyExpression);


			return resultExpression;
		}

		static MethodInfo _buildPreambleQueryDetachedMethodInfo =
			typeof(ExpressionBuilder).GetMethod(nameof(BuildPreambleQueryDetached), BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new InvalidOperationException();

		Expression BuildPreambleQueryDetached<T>(
			IBuildContext       sequence, 
			Expression          queryExpression, 
			ParameterExpression queryParameter,
			List<Preamble>      preambles) 
		{
			var query = new Query<T>(DataContext, queryExpression);

			query.Init(sequence, _parametersContext.CurrentSqlParameters);

			BuildQuery(query, sequence, queryParameter, ref preambles!, Array<Expression>.Empty);

			var idx      = preambles.Count;
			var preamble = new DatachedPreamble<T>(query);
			preambles.Add(preamble);

			var resultExpression = Expression.Convert(Expression.ArrayIndex(PreambleParam, ExpressionInstances.Int32(idx)), typeof(List<T>));

			return resultExpression;
		}

		Expression CompleteEagerLoadingExpressions(
			Expression          expression,     
			IBuildContext       buildContext,
			ParameterExpression queryParameter,
			ref List<Preamble>? preambles,
			Expression[]        previousKeys)
		{
			Dictionary<Expression, Expression>? eagerLoadingCache = null;

			var preamblesLocal = preambles;

			var updatedEagerLoading = expression.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Extension && e is SqlEagerLoadExpression eagerLoad)
				{
					eagerLoadingCache ??= new Dictionary<Expression, Expression>(ExpressionEqualityComparer.Instance);
					if (!eagerLoadingCache.TryGetValue(eagerLoad.SequenceExpression, out var preambleExpression))
					{
						preamblesLocal     ??= new List<Preamble>();

						preambleExpression = ProcessEagerLoadingExpression(buildContext, eagerLoad, queryParameter, preamblesLocal, previousKeys);
						eagerLoadingCache.Add(eagerLoad.SequenceExpression, preambleExpression);
					}

					return preambleExpression;
				}

				return e;
			});

			preambles = preamblesLocal;

			return updatedEagerLoading;
		}

		class DatachedPreamble<T> : Preamble
		{
			readonly Query<T> _query;

			public DatachedPreamble(Query<T> query)
			{
				_query = query;
			}

			public override object Execute(IDataContext dataContext, Expression expression, object?[]? parameters, object?[]? preambles)
			{
				return _query.GetResultEnumerable(dataContext, expression, preambles, preambles).ToList();
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, Expression expression, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken)
			{
				return await _query.GetResultEnumerable(dataContext, expression, preambles, preambles)
					.ToListAsync(cancellationToken)
					.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}

		class Preamble<TKey, T> : Preamble
			where TKey : notnull
		{
			readonly Query<KeyDetailEnvelope<TKey, T>> _query;

			public Preamble(Query<KeyDetailEnvelope<TKey, T>> query)
			{
				_query = query;
			}

			public override object Execute(IDataContext dataContext, Expression expression, object?[]? parameters, object?[]? preambles)
			{
				var result = new PreambleResult<TKey, T>();
				foreach (var e in _query.GetResultEnumerable(dataContext, expression, preambles, preambles))
				{
					result.Add(e.Key, e.Detail);
				}

				return result;
			}

			public override async Task<object> ExecuteAsync(IDataContext dataContext, Expression expression, object?[]? parameters, object[]? preambles,
				CancellationToken                                  cancellationToken)
			{
				var result = new PreambleResult<TKey, T>();

				var enumerator = _query.GetResultEnumerable(dataContext, expression, preambles, preambles)
					.GetAsyncEnumerator(cancellationToken);

				while (await enumerator.MoveNextAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext))
				{
					var e = enumerator.Current;
					result.Add(e.Key, e.Detail);
				}

				return result;
			}
		}

		class PreambleResult<TKey, T>
			where TKey : notnull
		{
			Dictionary<TKey, List<T>>? _items;
			TKey                       _prevKey = default!;
			List<T>?                   _prevList;

			public void Add(TKey key, T item)
			{
				List<T>? list;

				if (_prevList != null && _prevKey!.Equals(key))
				{
					list = _prevList;
				}
				else
				{
					if (_items == null)
					{
						_items = new Dictionary<TKey, List<T>>();
						list   = new List<T>();
						_items.Add(key, list);
					}
					else if (!_items.TryGetValue(key, out list))
					{
						list = new List<T>();
						_items.Add(key, list);
					}

					_prevKey  = key;
					_prevList = list;
				}

				list.Add(item);
			}

			public List<T> GetList(TKey key)
			{
				if (_items == null || !_items.TryGetValue(key, out var list))
					return new List<T>();
				return list;
			}
		}

	}
}
