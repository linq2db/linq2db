using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using JetBrains.Annotations;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Extensions;
	using Mapping;
	using Reflection;
	using SqlQuery;
	using LinqToDB.Expressions;
	using LinqToDB.Common.Internal;

	internal sealed partial class ExpressionBuilder
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
			new ChangeTypeExpressionBuilder(),
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
			ParameterExpression[]?            compiledParameters)
		{
			_query               = query;

			CollectQueryDepended(expression);

			CompiledParameters = compiledParameters;
			DataContext        = dataContext;
			DataOptions        = dataContext.Options;
			OriginalExpression = expression;

			_optimizationContext = optimizationContext;
			_parametersContext   = parametersContext;
			Expression           = ConvertExpressionTree(expression);
			_optimizationContext.ClearVisitedCache();
		}

		#endregion

		#region Public Members

		public readonly IDataContext           DataContext;
		public readonly Expression             OriginalExpression;
		public readonly Expression             Expression;
		public readonly ParameterExpression[]? CompiledParameters;

		public static readonly ParameterExpression QueryRunnerParam = Expression.Parameter(typeof(IQueryRunner), "qr");
		public static readonly ParameterExpression DataContextParam = Expression.Parameter(typeof(IDataContext), "dctx");
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

			_query.SetPreambles(preambles);

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

			expr = FinalizeProjection(query, sequence, expr, queryParameter, ref preambles, previousKeys);

			sequence.SetRunQuery(query, expr);
		}

		/// <summary>
		/// Contains information from which expression sequence were built. Used for Eager Loading.
		/// </summary>
		Dictionary<IBuildContext, Expression> _sequenceExpressions = new();

		public Expression GetSequenceExpression(IBuildContext sequence)
		{
			if (_sequenceExpressions.TryGetValue(sequence, out var expr))
				return expr;

			if (sequence is SubQueryContext sc)
				return GetSequenceExpression(sc.SubQuery);

			if (sequence is ScopeContext scoped)
				return GetSequenceExpression(scoped.Context);

			throw new InvalidOperationException("Sequence has no registered expression");
		}

		public IBuildContext? TryBuildSequence(BuildInfo buildInfo)
		{
			var originalExpression = buildInfo.Expression;

			buildInfo.Expression = buildInfo.Expression.Unwrap();

			var n = _builders[0].BuildCounter;

			foreach (var builder in _builders)
			{
				if (builder.CanBuild(this, buildInfo))
				{
					var sequence = builder.BuildSequence(this, buildInfo);

					lock (builder)
						builder.BuildCounter++;

					_reorder = _reorder || n < builder.BuildCounter;

					if (sequence != null && !buildInfo.IsTest)
					{
						_sequenceExpressions[sequence] = originalExpression;
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

		public SequenceConvertInfo? ConvertSequence(BuildInfo buildInfo, ParameterExpression? param, bool throwExceptionIfCantConvert)
		{
			buildInfo.Expression = buildInfo.Expression.Unwrap();

			foreach (var builder in _builders)
				if (builder.CanBuild(this, buildInfo))
					return builder.Convert(this, buildInfo, param);

			if (throwExceptionIfCantConvert)
				throw new LinqException("Sequence '{0}' cannot be converted to SQL.", SqlErrorExpression.PrepareExpression(buildInfo.Expression));

			return null;
		}

		public Expression ExpandSequenceExpression(BuildInfo buildInfo)
		{
			buildInfo.Expression = buildInfo.Expression.Unwrap();
			buildInfo.Expression = MakeExpression(buildInfo.Parent, buildInfo.Expression, ProjectFlags.Expand);

			foreach (var builder in _builders)
				if (builder.CanBuild(this, buildInfo))
					return builder.Expand(this, buildInfo);

			return buildInfo.Expression;
		}

		public bool IsSequence(BuildInfo buildInfo)
		{
			buildInfo.Expression = buildInfo.Expression.Unwrap();

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

			expr = ConvertParameters (expr);
			expr = OptimizeExpression(expr);

			var paramType   = expr.Type;
			var isQueryable = false;

			if (expression.NodeType == ExpressionType.Call)
			{
				var call = (MethodCallExpression)expression;

				if (call.IsQueryable() && call.Object == null && call.Arguments.Count > 0 && call.Type.IsGenericType)
				{
					var type = call.Type.GetGenericTypeDefinition();

					if (type == typeof(IQueryable<>) || type == typeof(IEnumerable<>))
					{
						var arg = call.Type.GetGenericArguments();

						if (arg.Length == 1)
						{
							paramType   = arg[0];
							isQueryable = true;
						}
					}
				}
			}

			SequenceParameter = Expression.Parameter(paramType, "cp");

			var sequence = ConvertSequence(new BuildInfo((IBuildContext?)null, expr, new SelectQuery()), SequenceParameter, false);

			if (sequence != null)
			{
				if (sequence.Expression.Type != expr.Type)
				{
					if (isQueryable)
					{
						var p = sequence.ExpressionsToReplace!.Single(static s => s.Path.NodeType == ExpressionType.Parameter);

						return Expression.Call(
							((MethodCallExpression)expr).Method.DeclaringType!,
							"Select",
							new[] { p.Path.Type, paramType },
							sequence.Expression,
							Expression.Lambda(p.Expr, (ParameterExpression)p.Path));
					}

					throw new InvalidOperationException();
				}

				return sequence.Expression;
			}

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

		public Expression ExposeExpression(Expression expression)
		{
			var result = _optimizationContext.ExposeExpression(expression);
			return result;
		}

		#endregion

		#region OptimizeExpression

		public static readonly MethodInfo[] EnumerableMethods      = typeof(Enumerable     ).GetMethods();
		public static readonly MethodInfo[] QueryableMethods       = typeof(Queryable      ).GetMethods();
		public static readonly MethodInfo[] AsyncExtensionsMethods = typeof(AsyncExtensions).GetMethods();

		Dictionary<Expression, Expression>? _optimizedExpressions;

		static void CollectLambdaParameters(Expression expression, HashSet<ParameterExpression> foundParameters)
		{
			expression.Visit(foundParameters, static (foundParameters, e) =>
			{
				if (e.NodeType == ExpressionType.Lambda)
					foundParameters.AddRange(((LambdaExpression)e).Parameters);
			});
		}

		Expression OptimizeExpression(Expression expression)
		{
			if (_optimizedExpressions != null && _optimizedExpressions.TryGetValue(expression, out var expr))
				return expr;

			expr = ExposeExpression(expression);
			var currentParameters = new HashSet<ParameterExpression>();
			CollectLambdaParameters(expression, currentParameters);
			expr = expr.Transform((builder: this, currentParameters), static (ctx, e) => ctx.builder.OptimizeExpressionImpl(ctx.currentParameters, e));

			(_optimizedExpressions ??= new())[expression] = expr;

			return expr;
		}

		TransformInfo OptimizeExpressionImpl(HashSet<ParameterExpression> currentParameters, Expression expr)
		{
			switch (expr.NodeType)
			{
				case ExpressionType.MemberAccess:
					{
						var me = (MemberExpression)expr;

						// Replace Count with Count()
						//
						if (me.Member.Name == "Count")
						{
							var isList = typeof(ICollection).IsAssignableFrom(me.Member.DeclaringType);

							if (!isList)
								isList =
									me.Member.DeclaringType!.IsGenericType &&
									me.Member.DeclaringType.GetGenericTypeDefinition() == typeof(ICollection<>);

							if (!isList)
								isList = me.Member.DeclaringType!.GetInterfaces()
									.Any(static t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));

							if (isList)
							{
								var mi = EnumerableMethods
									.First(static m => m.Name == "Count" && m.GetParameters().Length == 1)
									.MakeGenericMethod(me.Expression!.Type.GetItemType()!);

								return new TransformInfo(Expression.Call(null, mi, me.Expression));
							}
						}

						if (CompiledParameters == null && typeof(IQueryable).IsSameOrParentOf(expr.Type))
						{
							var ex = ConvertIQueryable(expr, currentParameters);

							if (!ReferenceEquals(ex, expr))
								return new TransformInfo(ConvertExpressionTree(ex));
						}

						return new TransformInfo(ConvertSubquery(expr));
					}

				case ExpressionType.Call :
					{
						var call = (MethodCallExpression)expr;

						if (call.IsQueryable() || call.IsAsyncExtension())
						{
							switch (call.Method.Name)
							{
								case "Single"               :
								case "SingleOrDefault"      :
								case "First"                :
								case "FirstOrDefault"       : return new TransformInfo(ConvertPredicate     (call));
								case "LongCountAsync"       :
								case "CountAsync"           :
								case "SingleAsync"          :
								case "SingleOrDefaultAsync" :
								case "FirstAsync"           :
								case "FirstOrDefaultAsync"  : return new TransformInfo(ConvertPredicateAsync(call));
								case "ElementAt"            :
								case "ElementAtOrDefault"   : return new TransformInfo(ConvertElementAt     (call));
								case "LoadWithAsTable"      : return new TransformInfo(expr, true);
								case "With"                 : return new TransformInfo(expr);
								case "LoadWith":
								case "ThenLoad":
								{
									var mc   = (MethodCallExpression)expr;
									var args = new Expression[mc.Arguments.Count];

									// skipping second argument
									for (int i = 0; i < mc.Arguments.Count; i++)
									{
										args[i] = i == 1 ? mc.Arguments[i] : OptimizeExpression(mc.Arguments[i]);
									}

									mc = mc.Update(mc.Object, args);
									return new TransformInfo(mc, true);
								}
							}
						}

						if (CompiledParameters == null && typeof(IQueryable).IsSameOrParentOf(expr.Type))
						{
							var attr = call.Method.GetTableFunctionAttribute(MappingSchema);

							if (attr == null && !call.IsQueryable())
							{
								var ex = ConvertIQueryable(expr, currentParameters);

								if (!ReferenceEquals(ex, expr))
									return new TransformInfo(ConvertExpressionTree(ex));
							}
						}

						return new TransformInfo(ConvertSubquery(expr));
					}
			}

			return new TransformInfo(expr);
		}

		Expression ConvertSubquery(Expression expr)
		{
			Expression? ex = expr;

			while (ex != null)
			{
				switch (ex.NodeType)
				{
					case ExpressionType.MemberAccess : ex = ((MemberExpression)ex).Expression; break;
					case ExpressionType.Call         :
						{
							var call = (MethodCallExpression)ex;

							if (call.Object == null)
							{
								if (call.IsQueryable())
									switch (call.Method.Name)
									{
										case "Single"          :
										case "SingleOrDefault" :
										case "First"           :
										case "FirstOrDefault"  :
											return ConvertSingleOrFirst(expr, call);
									}

								return expr;
							}

							ex = call.Object;

							break;
						}
					default: return expr;
				}
			}

			return expr;
		}

		Expression ConvertSingleOrFirst(Expression expr, MethodCallExpression call)
		{
			var param    = Expression.Parameter(call.Type, "p");
			var selector = expr.Replace(call, param);
			var method   = GetQueryableMethodInfo(call, call, static (call, m, _) => m.Name == call.Method.Name && m.GetParameters().Length == 1);
			var select   = call.Method.DeclaringType == typeof(Enumerable) ?
				EnumerableMethods
					.Where(static m => m.Name == "Select" && m.GetParameters().Length == 2)
					.First(static m => m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2) :
				QueryableMethods
					.Where(static m => m.Name == "Select" && m.GetParameters().Length == 2)
					.First(static m => m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2);

			call   = (MethodCallExpression)OptimizeExpression(call);
			select = select.MakeGenericMethod(call.Type, expr.Type);
			method = method.MakeGenericMethod(expr.Type);

			var converted = Expression.Call(null, method,
				Expression.Call(null, select, call.Arguments[0], Expression.Lambda(selector, param)));

			return converted;
		}

		#endregion

		#region ConvertPredicate

		Expression ConvertPredicate(MethodCallExpression method)
		{
			if (method.Arguments.Count != 2)
				return method;

			var cm = GetQueryableMethodInfo(method, method, static (method, m,_) => m.Name == method.Method.Name && m.GetParameters().Length == 1);
			var wm = GetMethodInfo(method, "Where");

			var argType = method.Method.GetGenericArguments()[0];

			wm = wm.MakeGenericMethod(argType);
			cm = cm.MakeGenericMethod(argType);

			var converted = Expression.Call(null, cm,
				Expression.Call(null, wm,
					OptimizeExpression(method.Arguments[0]),
					OptimizeExpression(method.Arguments[1])));

			return converted;
		}

		Expression ConvertPredicateAsync(MethodCallExpression method)
		{
			if (method.Arguments.Count != 3)
				return method;

			MethodInfo? cm = null;
			foreach (var m in AsyncExtensionsMethods)
			{
				if (m.Name == method.Method.Name && m.GetParameters().Length == 2)
				{
					cm = m;
					break;
				}
			}

			if (cm == null)
				throw new InvalidOperationException("Sequence contains no elements");

			var wm = GetMethodInfo(method, "Where");

			var argType = method.Method.GetGenericArguments()[0];

			wm = wm.MakeGenericMethod(argType);
			cm = cm.MakeGenericMethod(argType);

			var converted = Expression.Call(null, cm,
				Expression.Call(null, wm,
					OptimizeExpression(method.Arguments[0]),
					OptimizeExpression(method.Arguments[1])),
				OptimizeExpression(method.Arguments[2]));

			return converted;
		}

		#endregion

		#region ConvertIQueryable

		Expression ConvertIQueryable(Expression expression, HashSet<ParameterExpression> currentParameters)
		{
			static bool HasParametersDefined(Expression testedExpression, IEnumerable<ParameterExpression> allowed)
			{
				var current = new HashSet<ParameterExpression>(allowed);
				var result  = null == testedExpression.Find(current, static (current, e) =>
				{
					if (e is LambdaExpression lambda)
					{
						// allow parameters, declared inside expr
						foreach (var param in lambda.Parameters)
							current.Add(param);
					}
					else if (e is ParameterExpression pe)
						return !current.Contains(pe);

					return false;
				});

				return result;
			}

			if (expression.NodeType == ExpressionType.MemberAccess || expression.NodeType == ExpressionType.Call)
			{
				if (expression.NodeType == ExpressionType.Call)
				{
					var mc = (MethodCallExpression)expression;
					if (mc.Method.DeclaringType != null && MappingSchema.HasAttribute<Sql.QueryExtensionAttribute>(mc.Method.DeclaringType, mc.Method))
						return mc;
				}

				var p    = Expression.Parameter(typeof(Expression), "exp");
				var exas = expression.GetExpressionAccessors(p);
				var expr = _parametersContext.ReplaceParameter(exas, expression, forceConstant: false, null).ValueExpression;

				var allowedParameters = new HashSet<ParameterExpression>(currentParameters) { p };

				var parameters = new[] { p };
				if (!HasParametersDefined(expr, parameters))
				{
					// trying to evaluate Queryable method.
					if (expression.NodeType == ExpressionType.Call && HasParametersDefined(expr, parameters.Concat(allowedParameters)))
					{
						var callExpression = (MethodCallExpression)expression;
						var firstArgument  = callExpression.Arguments[0];
						if (typeof(IQueryable<>).IsSameOrParentOf(firstArgument.Type))
						{
							var elementType =
								EagerLoading.GetEnumerableElementType(firstArgument.Type, MappingSchema);

							var fakeQuery = ExpressionQueryImpl.CreateQuery(elementType, DataContext, null);

							callExpression = callExpression.Update(callExpression.Object,
								new[] { fakeQuery.Expression }.Concat(callExpression.Arguments.Skip(1)));
							if (CanBeCompiled(callExpression, false))
							{
								if (!(callExpression.EvaluateExpression() is IQueryable appliedQuery))
									throw new LinqToDBException($"Method call '{expression}' returned null value.");
								var newExpression = appliedQuery.Expression.Replace(fakeQuery.Expression, firstArgument);
								return newExpression;
							}
						}
					}
					return expression;
				}

				var l    = Expression.Lambda<Func<Expression,IQueryable>>(Expression.Convert(expr, typeof(IQueryable)), parameters);
				var n    = _query.AddQueryableAccessors(expression, l);

				_parametersContext._expressionAccessors.TryGetValue(expression, out var accessor);
				if (accessor == null)
					throw new LinqToDBException($"IQueryable value accessor for '{expression}' not found.");

				var path =
					Expression.Call(
						Expression.Constant(_query),
						Methods.Query.GetIQueryable,
						ExpressionInstances.Int32(n), accessor, Expression.Constant(true));

				var qex = _query.GetIQueryable(n, expression, force: false);

				if (expression.NodeType == ExpressionType.Call && qex.NodeType == ExpressionType.Call)
				{
					var m1 = (MethodCallExpression)expression;
					var m2 = (MethodCallExpression)qex;

					if (m1.Method == m2.Method)
						return expression;
				}

				foreach (var a in qex.GetExpressionAccessors(path))
					if (!_parametersContext._expressionAccessors.ContainsKey(a.Key))
						_parametersContext._expressionAccessors.Add(a.Key, a.Value);

				return qex;
			}

			throw new InvalidOperationException();
		}

		#endregion

		#region ConvertElementAt

		Expression ConvertElementAt(MethodCallExpression method)
		{
			var sequence   = OptimizeExpression(method.Arguments[0]);
			var index      = OptimizeExpression(method.Arguments[1]).Unwrap();
			var sourceType = method.Method.GetGenericArguments()[0];

			MethodInfo skipMethod;

			if (index.NodeType == ExpressionType.Lambda)
			{
				skipMethod = MemberHelper.MethodOf(() => LinqExtensions.Skip<object>(null!, null!));
				skipMethod = skipMethod.GetGenericMethodDefinition();
			}
			else
			{
				skipMethod = GetQueryableMethodInfo((object?)null, method, static (_,mi,_) => mi.Name == "Skip");
			}

			skipMethod = skipMethod.MakeGenericMethod(sourceType);

			var methodName  = method.Method.Name == "ElementAt" ? "First" : "FirstOrDefault";
			var firstMethod = GetQueryableMethodInfo(methodName, method, static (methodName, mi,_) => mi.Name == methodName && mi.GetParameters().Length == 1);

			firstMethod = firstMethod.MakeGenericMethod(sourceType);

			var skipCall = Expression.Call(skipMethod, sequence, method.Arguments[1]);

			var converted = Expression.Call(null, firstMethod, skipCall);

			return converted;
		}

		#endregion

		#region SqQueryDepended support

		void CollectQueryDepended(Expression expr)
		{
			expr.Visit(_query, static (query, e) =>
			{
				if (e.NodeType == ExpressionType.Call)
				{
					var call = (MethodCallExpression)e;
					var parameters = call.Method.GetParameters();
					for (int i = 0; i < parameters.Length; i++)
					{
						var attr = parameters[i].GetAttribute<SqlQueryDependentAttribute>();
						if (attr != null)
							query.AddQueryDependedObject(call.Arguments[i], attr);
					}
				}
			});
		}

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

		MethodInfo GetQueryableMethodInfo<TContext>(TContext context, MethodCallExpression method, [InstantHandle] Func<TContext,MethodInfo, bool,bool> predicate)
		{
			if (method.Method.DeclaringType == typeof(Enumerable))
			{
				foreach (var m in EnumerableMethods)
					if (predicate(context, m, false))
						return m;
				foreach (var m in EnumerableMethods)
					if (predicate(context, m, true))
						return m;
			}
			else
			{
				foreach (var m in QueryableMethods)
					if (predicate(context, m, false))
						return m;
				foreach (var m in QueryableMethods)
					if (predicate(context, m, true))
						return m;
			}

			throw new InvalidOperationException("Sequence contains no elements");
		}

		MethodInfo GetMethodInfo(MethodCallExpression method, string name)
		{
			if (method.Method.DeclaringType == typeof(Enumerable))
			{
				foreach (var m in EnumerableMethods)
					if (m.Name == name && m.GetParameters().Length == 2
						&& m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2)
						return m;
			}
			else
			{
				foreach (var m in QueryableMethods)
					if (m.Name == name && m.GetParameters().Length == 2
						&& m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2)
						return m;
			}

			throw new InvalidOperationException("Sequence contains no elements");
		}

		static Type[] GetMethodGenericTypes(MethodCallExpression method)
		{
			return method.Method.DeclaringType == typeof(Enumerable) ?
				method.Method.GetParameters()[1].ParameterType.GetGenericArguments() :
				method.Method.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments();
		}

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
			if (left.Type != right.Type)
			{
				if (right.Type.CanConvertTo(left.Type))
					right = Expression.Convert(right, left.Type);
				else if (left.Type.CanConvertTo(right.Type))
					left = Expression.Convert(left, right.Type);
				else
				{
					var rightConvert = ConvertBuilder.GetConverter(mappingSchema, right.Type, left. Type);
					var leftConvert  = ConvertBuilder.GetConverter(mappingSchema, left. Type, right.Type);

					var leftIsPrimitive  = left. Type.IsPrimitive;
					var rightIsPrimitive = right.Type.IsPrimitive;

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

			return Expression.Equal(left, right);
		}

		#endregion
	}
}
