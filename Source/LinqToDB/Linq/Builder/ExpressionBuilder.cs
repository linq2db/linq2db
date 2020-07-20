using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
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
	using SqlQuery;
	using LinqToDB.Expressions;

	partial class ExpressionBuilder
	{
		#region Sequence

		static readonly object _sync = new object();

		static List<ISequenceBuilder> _sequenceBuilders = new List<ISequenceBuilder>
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
			new CountBuilder               (),
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
			new ArrayBuilder               (),
			new AsSubQueryBuilder          (),
			new DisableGroupingGuardBuilder(),
			new InlineParametersBuilder    (),
			new HasUniqueKeyBuilder        (),
		};

		public static void AddBuilder(ISequenceBuilder builder)
		{
			_sequenceBuilders.Add(builder);
		}

		#endregion

		#region Init

		readonly Query                             _query;
		readonly List<ISequenceBuilder>            _builders = _sequenceBuilders;
		private  bool                              _reorder;
		readonly Dictionary<Expression,Expression> _expressionAccessors;
		private  HashSet<Expression>?              _subQueryExpressions;
		readonly ExpressionTreeOptimizationContext  _optimizationContext;

		public readonly List<ParameterAccessor>    CurrentSqlParameters = new List<ParameterAccessor>();

		public readonly List<ParameterExpression>  BlockVariables       = new List<ParameterExpression>();
		public readonly List<Expression>           BlockExpressions     = new List<Expression>();
		public          bool                       IsBlockDisable;
		public          int                        VarIndex;

		public ExpressionBuilder(
			Query                  query,
			IDataContext           dataContext,
			Expression             expression,
			ParameterExpression[]? compiledParameters)
		{
			_query               = query;

			_expressionAccessors = expression.GetExpressionAccessors(ExpressionParam);

			CollectQueryDepended(expression);

			CompiledParameters   = compiledParameters;
			DataContext          = dataContext;
			OriginalExpression   = expression;

			_optimizationContext = new ExpressionTreeOptimizationContext(dataContext);
			Expression           = ConvertExpressionTree(expression);
			_optimizationContext.ClearVisitedCache();
			
			DataReaderLocal      = BuildVariable(DataReaderParam, "ldr");
		}

		#endregion

		#region Public Members

		public readonly IDataContext           DataContext;
		public readonly Expression             OriginalExpression;
		public readonly Expression             Expression;
		public readonly ParameterExpression[]? CompiledParameters;
		public readonly List<IBuildContext>    Contexts = new List<IBuildContext>();

		public static readonly ParameterExpression QueryRunnerParam = Expression.Parameter(typeof(IQueryRunner), "qr");
		public static readonly ParameterExpression DataContextParam = Expression.Parameter(typeof(IDataContext), "dctx");
		public static readonly ParameterExpression DataReaderParam  = Expression.Parameter(typeof(IDataReader),  "rd");
		public        readonly ParameterExpression DataReaderLocal;
		public static readonly ParameterExpression ParametersParam  = Expression.Parameter(typeof(object[]),     "ps");
		public static readonly ParameterExpression ExpressionParam  = Expression.Parameter(typeof(Expression),   "expr");

		public MappingSchema MappingSchema => DataContext.MappingSchema;

		#endregion

		#region Builder SQL

		internal Query<T> Build<T>()
		{
			var sequence = BuildSequence(new BuildInfo((IBuildContext?)null, Expression, new SelectQuery()));

			if (_reorder)
				lock (_sync)
				{
					_reorder = false;
					_sequenceBuilders = _sequenceBuilders.OrderByDescending(_ => _.BuildCounter).ToList();
				}

			_query.Init(sequence, CurrentSqlParameters);

			var param = Expression.Parameter(typeof(Query<T>), "info");

			sequence.BuildQuery((Query<T>)_query, param);

			_query.SetPreambles(_preambles);

			return (Query<T>)_query;
		}

		public IBuildContext BuildSequence(BuildInfo buildInfo)
		{
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

					return sequence!;
				}

				n = builder.BuildCounter;
			}

			throw new LinqException("Sequence '{0}' cannot be converted to SQL.", buildInfo.Expression);
		}

		public ISequenceBuilder GetBuilder(BuildInfo buildInfo)
		{
			buildInfo.Expression = buildInfo.Expression.Unwrap();

			foreach (var builder in _builders)
				if (builder.CanBuild(this, buildInfo))
					return builder;

			throw new LinqException("Sequence '{0}' cannot be converted to SQL.", buildInfo.Expression);
		}

		public SequenceConvertInfo? ConvertSequence(BuildInfo buildInfo, ParameterExpression? param, bool throwExceptionIfCantConvert)
		{
			buildInfo.Expression = buildInfo.Expression.Unwrap();

			foreach (var builder in _builders)
				if (builder.CanBuild(this, buildInfo))
					return builder.Convert(this, buildInfo, param);

			if (throwExceptionIfCantConvert)
				throw new LinqException("Sequence '{0}' cannot be converted to SQL.", buildInfo.Expression);

			return null;
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
						var p = sequence.ExpressionsToReplace.Single(s => s.Path.NodeType == ExpressionType.Parameter);

						return Expression.Call(
							((MethodCallExpression)expr).Method.DeclaringType,
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

		#endregion

		#region ConvertParameters

		Expression ConvertParameters(Expression expression)
		{
			return expression.Transform(expr =>
			{
				switch (expr.NodeType)
				{
					case ExpressionType.Parameter:
						if (CompiledParameters != null)
						{
							var idx = Array.IndexOf(CompiledParameters, (ParameterExpression)expr);

							if (idx > 0)
								return
									Expression.Convert(
										Expression.ArrayIndex(
											ParametersParam,
											Expression.Constant(Array.IndexOf(CompiledParameters, (ParameterExpression)expr))),
										expr.Type);
						}

						break;
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

		private MethodInfo[]? _enumerableMethods;
		public  MethodInfo[]   EnumerableMethods => _enumerableMethods ??= typeof(Enumerable).GetMethods();

		private MethodInfo[]? _queryableMethods;
		public  MethodInfo[]   QueryableMethods  => _queryableMethods  ??= typeof(Queryable). GetMethods();

		readonly Dictionary<Expression, Expression> _optimizedExpressions = new Dictionary<Expression, Expression>();

		static void CollectLambdaParameters(Expression expression, HashSet<ParameterExpression> foundParameters)
		{
			expression.Visit(e =>
			{
				if (e.NodeType == ExpressionType.Lambda)
				{
					foundParameters.AddRange(((LambdaExpression)e).Parameters);
				}
			});
		}

		Expression OptimizeExpression(Expression expression)
		{
			if (_optimizedExpressions.TryGetValue(expression, out var expr))
				return expr;

			expr = ExposeExpression(expression);
			var currentParameters = new HashSet<ParameterExpression>();
			CollectLambdaParameters(expression, currentParameters);
			expr = expr.Transform(e => OptimizeExpressionImpl(e, currentParameters));

			_optimizedExpressions[expression] = expr;

			_optimizationContext.RelocateAlias(expression, expr);

			return expr;
		}

		TransformInfo OptimizeExpressionImpl(Expression expr, HashSet<ParameterExpression> currentParameters)
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
									.Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ICollection<>));

							if (isList)
							{
								var mi = EnumerableMethods
									.First(m => m.Name == "Count" && m.GetParameters().Length == 1)
									.MakeGenericMethod(me.Expression.Type.GetItemType());

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
								case "Where"                : return new TransformInfo(ConvertWhere         (call));
								case "GroupBy"              : return new TransformInfo(ConvertGroupBy       (call));
								case "SelectMany"           : return new TransformInfo(ConvertSelectMany    (call));
								case "Select"               : return new TransformInfo(ConvertSelect        (call));
								case "LongCount"            :
								case "Count"                :
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
								case "Min"                  :
								case "Max"                  : return new TransformInfo(ConvertSelector      (call, true));
								case "Sum"                  :
								case "Average"              : return new TransformInfo(ConvertSelector      (call, false));
								case "MinAsync"             :
								case "MaxAsync"             : return new TransformInfo(ConvertSelectorAsync (call, true));
								case "SumAsync"             :
								case "AverageAsync"         : return new TransformInfo(ConvertSelectorAsync (call, false));
								case "ElementAt"            :
								case "ElementAtOrDefault"   : return new TransformInfo(ConvertElementAt     (call));
								case "LoadWith"             : return new TransformInfo(expr, true);
								case "With"                 : return new TransformInfo(expr);
							}
						}

						var l = ConvertMethodExpression(call.Object?.Type ?? call.Method.ReflectedType!, call.Method, out var alias);

						if (l != null)
						{
							var optimized = OptimizeExpression(ConvertMethod(call, l));
							_optimizationContext.RegisterAlias(optimized, alias!);
							return new TransformInfo(optimized);
						}

						if (CompiledParameters == null && typeof(IQueryable).IsSameOrParentOf(expr.Type))
						{
							var attr = GetTableFunctionAttribute(call.Method);

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

		LambdaExpression? ConvertMethodExpression(Type type, MemberInfo mi, out string? alias)
		{
			return _optimizationContext.ConvertMethodExpression(type, mi, out alias);
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
			var selector = expr.Transform(e => e == call ? param : e);
			var method   = GetQueryableMethodInfo(call, (m, _) => m.Name == call.Method.Name && m.GetParameters().Length == 1);
			var select   = call.Method.DeclaringType == typeof(Enumerable) ?
				EnumerableMethods
					.Where(m => m.Name == "Select" && m.GetParameters().Length == 2)
					.First(m => m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2) :
				QueryableMethods
					.Where(m => m.Name == "Select" && m.GetParameters().Length == 2)
					.First(m => m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2);

			call   = (MethodCallExpression)OptimizeExpression(call);
			select = select.MakeGenericMethod(call.Type, expr.Type);
			method = method.MakeGenericMethod(expr.Type);

			var converted = Expression.Call(null, method,
				Expression.Call(null, select, call.Arguments[0], Expression.Lambda(selector, param)));

			return converted;
		}

		#endregion

		#region ConvertWhere

		Expression ConvertWhere(MethodCallExpression method)
		{
			var sequence  = OptimizeExpression(method.Arguments[0]);
			var predicate = OptimizeExpression(method.Arguments[1]);
			var lambda    = (LambdaExpression)predicate.Unwrap();
			var lparam    = lambda.Parameters[0];
			var lbody     = lambda.Body;

			if (lambda.Parameters.Count > 1)
				return method;

			var exprs     = new List<Expression>();

			lbody.Visit(ex =>
			{
				if (ex.NodeType == ExpressionType.Call)
				{
					var call = (MethodCallExpression)ex;

					if (call.Arguments.Count > 0)
					{
						var arg = call.Arguments[0];

						if (call.IsAggregate(MappingSchema))
						{
							while (arg.NodeType == ExpressionType.Call && ((MethodCallExpression)arg).Method.Name == "Select")
								arg = ((MethodCallExpression)arg).Arguments[0];

							if (arg.NodeType == ExpressionType.Call)
								exprs.Add(ex);
						}
						else if (call.IsQueryable(CountBuilder.MethodNames))
						{
							//while (arg.NodeType == ExpressionType.Call && ((MethodCallExpression) arg).Method.Name == "Select")
							//	arg = ((MethodCallExpression) arg).Arguments[0];

							if (arg.NodeType == ExpressionType.Call)
								exprs.Add(ex);
						}
					}
				}
			});

			Expression? expr = null;

			if (exprs.Count > 0)
			{
				expr = lparam;

				foreach (var ex in exprs)
				{
					var type   = typeof(ExpressionHolder<,>).MakeGenericType(expr.Type, ex.Type);
					var fields = type.GetFields();

					expr = Expression.MemberInit(
						Expression.New(type),
						Expression.Bind(fields[0], expr),
						Expression.Bind(fields[1], ex));
				}

				var dic  = new Dictionary<Expression, Expression>();
				var parm = Expression.Parameter(expr.Type, lparam.Name);

				for (var i = 0; i < exprs.Count; i++)
				{
					Expression ex = parm;

					for (var j = i; j < exprs.Count - 1; j++)
						ex = ExpressionHelper.PropertyOrField(ex, "p");

					ex = ExpressionHelper.PropertyOrField(ex, "ex");

					dic.Add(exprs[i], ex);

					if (_subQueryExpressions == null)
						_subQueryExpressions = new HashSet<Expression>();
					_subQueryExpressions.Add(ex);
				}

				var newBody = lbody.Transform(ex => dic.TryGetValue(ex, out var e) ? e : ex);

				var nparm = exprs.Aggregate<Expression,Expression>(parm, (c,t) => ExpressionHelper.PropertyOrField(c, "p"));

				newBody   = newBody.Transform(ex => ReferenceEquals(ex, lparam) ? nparm : ex);
				predicate = Expression.Lambda(newBody, parm);

				var methodInfo = GetMethodInfo(method, "Select");

				methodInfo = methodInfo.MakeGenericMethod(lparam.Type, expr.Type);
				sequence   = Expression.Call(methodInfo, sequence, Expression.Lambda(expr, lparam));
			}

			if (!ReferenceEquals(sequence, method.Arguments[0]) || !ReferenceEquals(predicate, method.Arguments[1]))
			{
				var methodInfo  = method.Method.GetGenericMethodDefinition();
				var genericType = sequence.Type.GetGenericArguments()[0];
				var newMethod   = methodInfo.MakeGenericMethod(genericType);

				method = Expression.Call(newMethod, sequence, predicate);

				if (exprs.Count > 0)
				{
					var parameter = Expression.Parameter(expr!.Type, lparam.Name);

					methodInfo = GetMethodInfo(method, "Select");
					methodInfo = methodInfo.MakeGenericMethod(expr.Type, lparam.Type);
					method     = Expression.Call(methodInfo, method,
						Expression.Lambda(
							exprs.Aggregate((Expression)parameter, (current,_) => ExpressionHelper.PropertyOrField(current, "p")),
							parameter));
				}
			}

			return method;
		}

		#endregion

		#region ConvertGroupBy

		public class GroupSubQuery<TKey,TElement>
		{
			public TKey     Key     = default!;
			public TElement Element = default!;
		}

		interface IGroupByHelper
		{
			void Set(bool wrapInSubQuery, Expression sourceExpression, LambdaExpression keySelector, LambdaExpression? elementSelector, LambdaExpression? resultSelector);

			Expression AddElementSelectorQ  ();
			Expression AddElementSelectorE  ();
			Expression AddResultQ           ();
			Expression AddResultE           ();
			Expression WrapInSubQueryQ      ();
			Expression WrapInSubQueryE      ();
			Expression WrapInSubQueryResultQ();
			Expression WrapInSubQueryResultE();
		}

		class GroupByHelper<TSource,TKey,TElement,TResult> : IGroupByHelper
		{
			bool              _wrapInSubQuery;
			Expression        _sourceExpression = null!;
			LambdaExpression  _keySelector      = null!;
			LambdaExpression? _elementSelector;
			LambdaExpression? _resultSelector;

			public void Set(
				bool              wrapInSubQuery,
				Expression        sourceExpression,
				LambdaExpression  keySelector,
				LambdaExpression? elementSelector,
				LambdaExpression? resultSelector)
			{
				_wrapInSubQuery   = wrapInSubQuery;
				_sourceExpression = sourceExpression;
				_keySelector      = keySelector;
				_elementSelector  = elementSelector;
				_resultSelector   = resultSelector;
			}

			public Expression AddElementSelectorQ()
			{
				Expression<Func<IQueryable<TSource>,TKey,TElement,TResult,IQueryable<IGrouping<TKey,TSource>>>> func = (source,key,e,r) => source
					.GroupBy(keyParam => key, _ => _)
					;

				var body   = func.Body.Unwrap();
				var keyArg = GetLambda(body, 1).Parameters[0]; // .GroupBy(keyParam

				return Convert(func, keyArg, null, null);
			}

			public Expression AddElementSelectorE()
			{
				Expression<Func<IEnumerable<TSource>,TKey,TElement,TResult,IEnumerable<IGrouping<TKey,TSource>>>> func = (source,key,e,r) => source
					.GroupBy(keyParam => key, _ => _)
					;

				var body   = func.Body.Unwrap();
				var keyArg = GetLambda(body, 1).Parameters[0]; // .GroupBy(keyParam

				return Convert(func, keyArg, null, null);
			}

			public Expression AddResultQ()
			{
				Expression<Func<IQueryable<TSource>,TKey,TElement,TResult,IQueryable<TResult>>> func = (source,key,e,r) => source
					.GroupBy(keyParam => key, elemParam => e)
					.Select (resParam => r)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 1).Parameters[0]; // .GroupBy(keyParam
				var elemArg = GetLambda(body, 0, 2).Parameters[0]; // .GroupBy(..., elemParam
				var resArg  = GetLambda(body, 1).   Parameters[0]; // .Select (resParam

				return Convert(func, keyArg, elemArg, resArg);
			}

			public Expression AddResultE()
			{
				Expression<Func<IEnumerable<TSource>,TKey,TElement,TResult,IEnumerable<TResult>>> func = (source,key,e,r) => source
					.GroupBy(keyParam => key, elemParam => e)
					.Select (resParam => r)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 1).Parameters[0]; // .GroupBy(keyParam
				var elemArg = GetLambda(body, 0, 2).Parameters[0]; // .GroupBy(..., elemParam
				var resArg  = GetLambda(body, 1).   Parameters[0]; // .Select (resParam

				return Convert(func, keyArg, elemArg, resArg);
			}

			public Expression WrapInSubQueryQ()
			{
				Expression<Func<IQueryable<TSource>,TKey,TElement,TResult,IQueryable<IGrouping<TKey,TElement>>>> func = (source,key,e,r) => source
					.Select(selectParam => new GroupSubQuery<TKey,TSource>
					{
						Key     = key,
						Element = selectParam
					})
					.GroupBy(underscore => underscore.Key, elemParam => e)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 1).Parameters[0]; // .Select (selectParam
				var elemArg = GetLambda(body, 2).   Parameters[0]; // .GroupBy(..., elemParam

				return Convert(func, keyArg, elemArg, null);
			}

			public Expression WrapInSubQueryE()
			{
				Expression<Func<IEnumerable<TSource>,TKey,TElement,TResult,IEnumerable<IGrouping<TKey,TElement>>>> func = (source,key,e,r) => source
					.Select(selectParam => new GroupSubQuery<TKey,TSource>
					{
						Key     = key,
						Element = selectParam
					})
					.GroupBy(underscore => underscore.Key, elemParam => e)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 1).Parameters[0]; // .Select (selectParam
				var elemArg = GetLambda(body, 2).   Parameters[0]; // .GroupBy(..., elemParam

				return Convert(func, keyArg, elemArg, null);
			}

			public Expression WrapInSubQueryResultQ()
			{
				Expression<Func<IQueryable<TSource>,TKey,TElement,TResult,IQueryable<TResult>>> func = (source,key,e,r) => source
					.Select(selectParam => new GroupSubQuery<TKey,TSource>
					{
						Key     = key,
						Element = selectParam
					})
					.GroupBy(underscore => underscore.Key, elemParam => e)
					.Select (resParam => r)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 0, 1).Parameters[0]; // .Select (selectParam
				var elemArg = GetLambda(body, 0, 2).   Parameters[0]; // .GroupBy(..., elemParam
				var resArg  = GetLambda(body, 1).      Parameters[0]; // .Select (resParam

				return Convert(func, keyArg, elemArg, resArg);
			}

			public Expression WrapInSubQueryResultE()
			{
				Expression<Func<IEnumerable<TSource>,TKey,TElement,TResult,IEnumerable<TResult>>> func = (source,key,e,r) => source
					.Select(selectParam => new GroupSubQuery<TKey,TSource>
					{
						Key     = key,
						Element = selectParam
					})
					.GroupBy(underscore => underscore.Key, elemParam => e)
					.Select (resParam => r)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 0, 1).Parameters[0]; // .Select (selectParam
				var elemArg = GetLambda(body, 0, 2).   Parameters[0]; // .GroupBy(..., elemParam
				var resArg  = GetLambda(body, 1).      Parameters[0]; // .Select (resParam

				return Convert(func, keyArg, elemArg, resArg);
			}

			Expression Convert(
				LambdaExpression     func,
				ParameterExpression  keyArg,
				ParameterExpression? elemArg,
				ParameterExpression? resArg)
			{
				var body = func.Body.Unwrap();
				var expr = body.Transform(ex =>
				{
					if (ReferenceEquals(ex, func.Parameters[0]))
						return _sourceExpression;

					if (ReferenceEquals(ex, func.Parameters[1]))
						return _keySelector.Body.Transform(e => ReferenceEquals(e, _keySelector.Parameters[0]) ? keyArg : e);

					if (ReferenceEquals(ex, func.Parameters[2]))
					{
						Expression obj = elemArg!;

						if (_wrapInSubQuery)
							obj = ExpressionHelper.PropertyOrField(elemArg!, "Element");

						if (_elementSelector == null)
							return obj;

						return _elementSelector.Body.Transform(e => ReferenceEquals(e, _elementSelector!.Parameters[0]) ? obj : e);
					}

					if (ReferenceEquals(ex, func.Parameters[3]))
						return _resultSelector!.Body.Transform(e =>
						{
							if (ReferenceEquals(e, _resultSelector.Parameters[0]))
								return ExpressionHelper.PropertyOrField(resArg!, "Key");

							if (ReferenceEquals(e, _resultSelector.Parameters[1]))
								return resArg!;

							return e;
						});

					return ex;
				});

				return expr;
			}
		}

		static LambdaExpression GetLambda(Expression expression, params int[] n)
		{
			foreach (var i in n)
				expression = ((MethodCallExpression)expression).Arguments[i].Unwrap();
			return (LambdaExpression)expression;
		}

		Expression ConvertGroupBy(MethodCallExpression method)
		{
			if (method.Arguments[method.Arguments.Count - 1].Unwrap().NodeType != ExpressionType.Lambda)
				return method;

			var types = method.Method.GetGenericMethodDefinition().GetGenericArguments()
				.Zip(method.Method.GetGenericArguments(), (n, t) => new { n = n.Name, t })
				.ToDictionary(_ => _.n, _ => _.t);

			var sourceExpression = OptimizeExpression(method.Arguments[0].Unwrap());
			var keySelector      = (LambdaExpression)OptimizeExpression(method.Arguments[1].Unwrap());
			var elementSelector  = types.ContainsKey("TElement") ? (LambdaExpression)OptimizeExpression(method.Arguments[2].Unwrap()) : null;
			var resultSelector   = types.ContainsKey("TResult")  ?
				(LambdaExpression)OptimizeExpression(method.Arguments[types.ContainsKey("TElement") ? 3 : 2].Unwrap()) : null;

			var needSubQuery = null != ConvertExpression(keySelector.Body.Unwrap()).Find(IsExpression);

			if (!needSubQuery && resultSelector == null && elementSelector != null)
				return method;

			var gtype  = typeof(GroupByHelper<,,,>).MakeGenericType(
				types["TSource"],
				types["TKey"],
				types.ContainsKey("TElement") ? types["TElement"] : types["TSource"],
				types.ContainsKey("TResult")  ? types["TResult"]  : types["TSource"]);

			var helper =
				//Expression.Lambda<Func<IGroupByHelper>>(
				//	Expression.Convert(Expression.New(gtype), typeof(IGroupByHelper)))
				//.Compile()();
				(IGroupByHelper)Activator.CreateInstance(gtype)!;

			helper.Set(needSubQuery, sourceExpression, keySelector, elementSelector, resultSelector);

			if (method.Method.DeclaringType == typeof(Queryable))
			{
				if (!needSubQuery)
					return resultSelector == null ? helper.AddElementSelectorQ() : helper.AddResultQ();

				return resultSelector == null ? helper.WrapInSubQueryQ() : helper.WrapInSubQueryResultQ();
			}
			else
			{
				if (!needSubQuery)
					return resultSelector == null ? helper.AddElementSelectorE() : helper.AddResultE();

				return resultSelector == null ? helper.WrapInSubQueryE() : helper.WrapInSubQueryResultE();
			}
		}

		bool IsExpression(Expression ex)
		{
			switch (ex.NodeType)
			{
				case ExpressionType.Convert        :
				case ExpressionType.ConvertChecked :
				case ExpressionType.MemberInit     :
				case ExpressionType.New            :
				case ExpressionType.NewArrayBounds :
				case ExpressionType.NewArrayInit   :
				case ExpressionType.Parameter      : return false;
				case ExpressionType.MemberAccess   :
					{
						var ma   = (MemberExpression)ex;
						var attr = GetExpressionAttribute(ma.Member);

						if (attr != null)
							return true;

						return false;
					}
			}

			return true;
		}

		#endregion

		#region ConvertSelectMany

		interface ISelectManyHelper
		{
			void Set(Expression sourceExpression, LambdaExpression colSelector);

			Expression AddElementSelectorQ();
			Expression AddElementSelectorE();
		}

		class SelectManyHelper<TSource,TCollection> : ISelectManyHelper
		{
			Expression       _sourceExpression = null!;
			LambdaExpression _colSelector = null!;

			public void Set(Expression sourceExpression, LambdaExpression colSelector)
			{
				_sourceExpression = sourceExpression;
				_colSelector      = colSelector;
			}

			public Expression AddElementSelectorQ()
			{
				Expression<Func<IQueryable<TSource>,IEnumerable<TCollection>,IQueryable<TCollection>>> func = (source,col) => source
					.SelectMany(cp => col, (s,c) => c)
					;

				var body   = func.Body.Unwrap();
				var colArg = GetLambda(body, 1).Parameters[0]; // .SelectMany(colParam

				return Convert(func, colArg);
			}

			public Expression AddElementSelectorE()
			{
				Expression<Func<IEnumerable<TSource>,IEnumerable<TCollection>,IEnumerable<TCollection>>> func = (source,col) => source
					.SelectMany(cp => col, (s,c) => c)
					;

				var body   = func.Body.Unwrap();
				var colArg = GetLambda(body, 1).Parameters[0]; // .SelectMany(colParam

				return Convert(func, colArg);
			}

			Expression Convert(LambdaExpression func, ParameterExpression colArg)
			{
				var body = func.Body.Unwrap();
				var expr = body.Transform(ex =>
				{
					if (ex == func.Parameters[0])
						return _sourceExpression;

					if (ex == func.Parameters[1])
						return _colSelector.Body.Transform(e => e == _colSelector.Parameters[0] ? colArg : e);

					return ex;
				});

				return expr;
			}
		}

		Expression ConvertSelectMany(MethodCallExpression method)
		{
			if (method.Arguments.Count != 2 || ((LambdaExpression)method.Arguments[1].Unwrap()).Parameters.Count != 1)
				return method;

			var types = method.Method.GetGenericMethodDefinition().GetGenericArguments()
				.Zip(method.Method.GetGenericArguments(), (n, t) => new { n = n.Name, t })
				.ToDictionary(_ => _.n, _ => _.t);

			var sourceExpression = OptimizeExpression(method.Arguments[0].Unwrap());
			var colSelector      = (LambdaExpression)OptimizeExpression(method.Arguments[1].Unwrap());

			var gtype  = typeof(SelectManyHelper<,>).MakeGenericType(types["TSource"], types["TResult"]);
			var helper =
				//Expression.Lambda<Func<ISelectManyHelper>>(
				//	Expression.Convert(Expression.New(gtype), typeof(ISelectManyHelper)))
				//.Compile()();
				(ISelectManyHelper)Activator.CreateInstance(gtype)!;

			helper.Set(sourceExpression, colSelector);

			var converted = method.Method.DeclaringType == typeof(Queryable) ?
				helper.AddElementSelectorQ() :
				helper.AddElementSelectorE();

			return converted;
		}

		#endregion

		#region ConvertPredicate

		Expression ConvertPredicate(MethodCallExpression method)
		{
			if (method.Arguments.Count != 2)
				return method;

			var cm = GetQueryableMethodInfo(method, (m,_) => m.Name == method.Method.Name && m.GetParameters().Length == 1);
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

			var cm = typeof(AsyncExtensions).GetMethods().First(m => m.Name == method.Method.Name && m.GetParameters().Length == 2);
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

		#region ConvertSelector

		Expression ConvertSelector(MethodCallExpression method, bool isGeneric)
		{
			if (method.Arguments.Count != 2)
				return method;

			isGeneric = isGeneric && method.Method.DeclaringType == typeof(Queryable);

			var types = GetMethodGenericTypes(method);
			var sm    = GetMethodInfo(method, "Select");
			var cm    = GetQueryableMethodInfo(method, (m,isDefault) =>
			{
				if (m.Name == method.Method.Name)
				{
					var ps = m.GetParameters();

					if (ps.Length == 1)
					{
						if (isGeneric)
							return true;

						var ts = ps[0].ParameterType.GetGenericArguments();
						return ts[0] == types[1] || isDefault && ts[0].IsGenericParameter;
					}
				}

				return false;
			});

			var argType = types[0];

			sm = sm.MakeGenericMethod(argType, types[1]);

			if (cm.IsGenericMethodDefinition)
				cm = cm.MakeGenericMethod(types[1]);

			var converted = Expression.Call(null, cm,
				OptimizeExpression(Expression.Call(null, sm,
					method.Arguments[0],
					method.Arguments[1])));

			return converted;
		}

		Expression ConvertSelectorAsync(MethodCallExpression method, bool isGeneric)
		{
			if (method.Arguments.Count != 3)
				return method;

			isGeneric = isGeneric && method.Method.DeclaringType == typeof(AsyncExtensions);

			var types = GetMethodGenericTypes(method);
			var sm    = GetMethodInfo(method, "Select");
			var cm    = typeof(AsyncExtensions).GetMethods().First(m =>
			{
				if (m.Name == method.Method.Name)
				{
					var ps = m.GetParameters();

					if (ps.Length == 2)
					{
						if (isGeneric)
							return true;

						var ts = ps[0].ParameterType.GetGenericArguments();
						return ts[0] == types[1];// || isDefault && ts[0].IsGenericParameter;
					}
				}

				return false;
			});

			var argType = types[0];

			sm = sm.MakeGenericMethod(argType, types[1]);

			if (cm.IsGenericMethodDefinition)
				cm = cm.MakeGenericMethod(types[1]);

			var converted = Expression.Call(null, cm,
				OptimizeExpression(Expression.Call(null, sm,
					method.Arguments[0],
					method.Arguments[1])),
				OptimizeExpression(method.Arguments[2]));

			return converted;
		}

		#endregion

		#region ConvertSelect

		Expression ConvertSelect(MethodCallExpression method)
		{
			var sequence = OptimizeExpression(method.Arguments[0]);
			var lambda   = (LambdaExpression)method.Arguments[1].Unwrap();

			if (lambda.Parameters.Count > 1 ||
				sequence.NodeType != ExpressionType.Call ||
				((MethodCallExpression)sequence).Method.Name != method.Method.Name)
			{
				return method;
			}

			var slambda = (LambdaExpression)((MethodCallExpression)sequence).Arguments[1].Unwrap();
			var sbody   = slambda.Body.Unwrap();

			if (slambda.Parameters.Count > 1 || sbody.NodeType != ExpressionType.MemberAccess)
				return method;

			lambda = (LambdaExpression)OptimizeExpression(lambda);

			var types1 = GetMethodGenericTypes((MethodCallExpression)sequence);
			var types2 = GetMethodGenericTypes(method);

			var converted =  Expression.Call(null,
				GetMethodInfo(method, "Select").MakeGenericMethod(types1[0], types2[1]),
				((MethodCallExpression)sequence).Arguments[0],
				Expression.Lambda(lambda.GetBody(sbody), slambda.Parameters[0]));

			return converted;
		}

		#endregion

		#region ConvertIQueryable

		Expression ConvertIQueryable(Expression expression, HashSet<ParameterExpression> currentParameters)
		{
			bool HasParamatersDefined(Expression testedExpression, IEnumerable<ParameterExpression> allowed)
			{
				var current = new HashSet<ParameterExpression>(allowed);
				var result  = null == testedExpression.Find(e =>
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
				var p    = Expression.Parameter(typeof(Expression), "exp");
				var exas = expression.GetExpressionAccessors(p);
				var expr = ReplaceParameter(exas, expression, _ => {}).ValueExpression;

				var allowedParameters = new HashSet<ParameterExpression>(currentParameters) { p };

				var parameters = new[] { p };
				if (!HasParamatersDefined(expr, parameters))
				{
					// trying to evaluate Querybale method.
					if (expression.NodeType == ExpressionType.Call && HasParamatersDefined(expr, parameters.Concat(allowedParameters)))
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
							if (CanBeCompiled(callExpression))
							{
								if (!(callExpression.EvaluateExpression() is IQueryable appliedQuery))
									throw new LinqToDBException($"Method call '{expression}' returned null value.");
								var newExpression = appliedQuery.Expression.Transform(e =>
									e == fakeQuery.Expression ? firstArgument : e);
								return newExpression;
							}
						}
					}
					return expression;
				}

				var l    = Expression.Lambda<Func<Expression,IQueryable>>(Expression.Convert(expr, typeof(IQueryable)), parameters);
				var n    = _query.AddQueryableAccessors(expression, l);

				_expressionAccessors.TryGetValue(expression, out var accessor);

				var path =
					Expression.Call(
						Expression.Constant(_query),
						MemberHelper.MethodOf<Query>(a => a.GetIQueryable(0, null!)),
						Expression.Constant(n), accessor ?? Expression.Constant(null, typeof(Expression)));

				var qex = _query.GetIQueryable(n, expression);

				if (expression.NodeType == ExpressionType.Call && qex.NodeType == ExpressionType.Call)
				{
					var m1 = (MethodCallExpression)expression;
					var m2 = (MethodCallExpression)qex;

					if (m1.Method == m2.Method)
						return expression;
				}

				foreach (var a in qex.GetExpressionAccessors(path))
					if (!_expressionAccessors.ContainsKey(a.Key))
						_expressionAccessors.Add(a.Key, a.Value);

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
				skipMethod = GetQueryableMethodInfo(method, (mi,_) => mi.Name == "Skip");
			}

			skipMethod = skipMethod.MakeGenericMethod(sourceType);

			var methodName  = method.Method.Name == "ElementAt" ? "First" : "FirstOrDefault";
			var firstMethod = GetQueryableMethodInfo(method, (mi,_) => mi.Name == methodName && mi.GetParameters().Length == 1);

			firstMethod = firstMethod.MakeGenericMethod(sourceType);

			var skipCall = Expression.Call(skipMethod, sequence, index);
			if (_expressionAccessors.TryGetValue(method, out var accessor))
			{
				_expressionAccessors.Add(skipCall, accessor);
			}

			var converted = Expression.Call(null, firstMethod, skipCall);

			return converted;
		}

		#endregion

		#region SqQueryDepended support

		void CollectQueryDepended(Expression expr)
		{
			expr.Visit(e =>
			{
				if (e.NodeType == ExpressionType.Call)
				{
					var call = (MethodCallExpression)e;
					var parameters = call.Method.GetParameters();
					for (int i = 0; i < parameters.Length; i++)
					{
						var attr = parameters[i].GetCustomAttributes(typeof(SqlQueryDependentAttribute), false).Cast<SqlQueryDependentAttribute>()
							.FirstOrDefault();
						if (attr != null)
							_query.AddQueryDependedObject(call.Arguments[i], attr);
					}
				}
			});
		}

		public Expression AddQueryableMemberAccessors(AccessorMember memberInfo, IDataContext dataContext,
			Func<MemberInfo, IDataContext, Expression> qe)
		{
			return _query.AddQueryableMemberAccessors(memberInfo.MemberInfo, dataContext, qe);
		}


		#endregion

		#region Helpers

		MethodInfo GetQueryableMethodInfo(MethodCallExpression method, [InstantHandle] Func<MethodInfo,bool,bool> predicate)
		{
			return method.Method.DeclaringType == typeof(Enumerable) ?
				EnumerableMethods.FirstOrDefault(m => predicate(m, false)) ?? EnumerableMethods.First(m => predicate(m, true)):
				QueryableMethods. FirstOrDefault(m => predicate(m, false)) ?? QueryableMethods. First(m => predicate(m, true));
		}

		MethodInfo GetMethodInfo(MethodCallExpression method, string name)
		{
			return method.Method.DeclaringType == typeof(Enumerable) ?
				EnumerableMethods
					.Where(m => m.Name == name && m.GetParameters().Length == 2)
					.First(m => m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2) :
				QueryableMethods
					.Where(m => m.Name == name && m.GetParameters().Length == 2)
					.First(m => m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2);
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
		internal static BinaryExpression Equal(MappingSchema mappingSchema, Expression left, Expression right)
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

					if (leftIsPrimitive == true && rightIsPrimitive == false && rightConvert.Item2 != null)
						right = rightConvert.Item2.GetBody(right);
					else if (leftIsPrimitive == false && rightIsPrimitive == true && leftConvert.Item2 != null)
						left = leftConvert.Item2.GetBody(left);
					else if (rightConvert.Item2 != null)
						right = rightConvert.Item2.GetBody(right);
					else if (leftConvert.Item2 != null)
						left = leftConvert.Item2.GetBody(left);
				}
			}

			return Expression.Equal(left, right);
		}


		Dictionary<Expression, Expression?> _rootExpressions = new Dictionary<Expression, Expression?>();

		[return: NotNullIfNotNull("expr")]
		internal Expression? GetRootObject(Expression? expr)
		{
			if (expr == null)
				return null;

			if (_rootExpressions.TryGetValue(expr, out var root))
				return root;

			root = InternalExtensions.GetRootObject(expr, MappingSchema);
			_rootExpressions.Add(expr, root);
			return root;
		}

		#endregion
	}
}
