using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Extensions;
	using Mapping;
	using SqlBuilder;
	using SqlProvider;
	using LinqToDB.Expressions;

	public partial class ExpressionBuilder
	{
		#region Sequence

		static readonly object _sync = new object();

		static List<ISequenceBuilder> _sequenceBuilders = new List<ISequenceBuilder>
		{
			new TableBuilder         (),
			new SelectBuilder        (),
			new SelectManyBuilder    (),
			new WhereBuilder         (),
			new OrderByBuilder       (),
			new GroupByBuilder       (),
			new JoinBuilder          (),
			new TakeSkipBuilder      (),
			new DefaultIfEmptyBuilder(),
			new DistinctBuilder      (),
			new FirstSingleBuilder   (),
			new AggregationBuilder   (),
			new ScalarSelectBuilder  (),
			new CountBuilder         (),
			new PassThroughBuilder   (),
			new TableAttributeBuilder(),
			new InsertBuilder        (),
			new InsertBuilder.Into   (),
			new InsertBuilder.Value  (),
			new InsertOrUpdateBuilder(),
			new UpdateBuilder        (),
			new UpdateBuilder.Set    (),
			new DeleteBuilder        (),
			new ContainsBuilder      (),
			new AllAnyBuilder        (),
			new ConcatUnionBuilder   (),
			new IntersectBuilder     (),
			new CastBuilder          (),
			new OfTypeBuilder        (),
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
		private  HashSet<Expression>               _subQueryExpressions;

		readonly public List<ParameterAccessor>    CurrentSqlParameters = new List<ParameterAccessor>();

#if FW4 || SILVERLIGHT

		readonly public List<ParameterExpression>  BlockVariables       = new List<ParameterExpression>();
		readonly public List<Expression>           BlockExpressions     = new List<Expression>();
		         public bool                       IsBlockDisable;

#else
		         public bool                       IsBlockDisable = true;
#endif

		readonly HashSet<Expression> _visitedExpressions;

		public ExpressionBuilder(
			Query                 query,
			IDataContextInfo      dataContext,
			Expression            expression,
			ParameterExpression[] compiledParameters)
		{
			_query               = query;
			_expressionAccessors = expression.GetExpressionAccessors(ExpressionParam);

			CompiledParameters = compiledParameters;
			DataContextInfo    = dataContext;
			OriginalExpression = expression;

			_visitedExpressions = new HashSet<Expression>();
			Expression         = ConvertExpressionTree(expression);
			_visitedExpressions = null;

			DataReaderLocal = Expression.Parameter(dataContext.DataContext.DataReaderType, "ldr");

			BlockVariables.  Add(DataReaderLocal);
			BlockExpressions.Add(Expression.Assign(DataReaderLocal, Expression.Convert(DataReaderParam, dataContext.DataContext.DataReaderType)));
		}

		#endregion

		#region Public Members

		public readonly IDataContextInfo      DataContextInfo;
		public readonly Expression            OriginalExpression;
		public readonly Expression            Expression;
		public readonly ParameterExpression[] CompiledParameters;
		public readonly List<IBuildContext>   Contexts = new List<IBuildContext>();

		private ISqlProvider _sqlProvider;
		public  ISqlProvider  SqlProvider
		{
			get { return _sqlProvider ?? (_sqlProvider = DataContextInfo.CreateSqlProvider()); }
		}

		public static readonly ParameterExpression ContextParam     = Expression.Parameter(typeof(QueryContext), "context");
		public static readonly ParameterExpression DataContextParam = Expression.Parameter(typeof(IDataContext), "dctx");
		public static readonly ParameterExpression DataReaderParam  = Expression.Parameter(typeof(IDataReader),  "rd");
		public        readonly ParameterExpression DataReaderLocal;
		public static readonly ParameterExpression ParametersParam  = Expression.Parameter(typeof(object[]),     "ps");
		public static readonly ParameterExpression ExpressionParam  = Expression.Parameter(typeof(Expression),   "expr");

		public MappingSchemaOld MappingSchema
		{
			get { return DataContextInfo.MappingSchema; }
		}

		#endregion

		#region Builder SQL

		internal Query<T> Build<T>()
		{
			var sequence = BuildSequence(new BuildInfo((IBuildContext)null, Expression, new SqlQuery()));
			
			if (_reorder)
				lock (_sync)
				{
					_reorder = false;
					_sequenceBuilders = _sequenceBuilders.OrderByDescending(_ => _.BuildCounter).ToList();
				}

			_query.Init(sequence, CurrentSqlParameters);

			var param = Expression.Parameter(typeof(Query<T>), "info");

			sequence.BuildQuery((Query<T>)_query, param);

			return (Query<T>)_query;
		}

		[JetBrains.Annotations.NotNull]
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

					return sequence;
				}

				n = builder.BuildCounter;
			}

			throw new LinqException("Sequence '{0}' cannot be converted to SQL.", buildInfo.Expression);
		}

		public SequenceConvertInfo ConvertSequence(BuildInfo buildInfo, ParameterExpression param)
		{
			buildInfo.Expression = buildInfo.Expression.Unwrap();

			foreach (var builder in _builders)
				if (builder.CanBuild(this, buildInfo))
					return builder.Convert(this, buildInfo, param);

			throw new LinqException("Sequence '{0}' cannot be converted to SQL.", buildInfo.Expression);
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

		public ParameterExpression SequenceParameter;

		Expression ConvertExpressionTree(Expression expression)
		{
			var expr = ConvertParameters(expression);

			expr = ExposeExpression  (expr);
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

			var sequence = ConvertSequence(new BuildInfo((IBuildContext)null, expr, new SqlQuery()), SequenceParameter);

			if (sequence != null)
			{
				if (sequence.Expression.Type != expr.Type)
				{
					if (isQueryable)
					{
						var p = sequence.ExpressionsToReplace.SingleOrDefault(s => s.Path.NodeType == ExpressionType.Parameter);

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

		Expression ExposeExpression(Expression expression)
		{
			return expression.Transform(expr =>
			{
				switch (expr.NodeType)
				{
					case ExpressionType.MemberAccess:
						{
							var me = (MemberExpression)expr;
							var l  = ConvertMethodExpression(me.Member);

							if (l != null)
							{
								var body = l.Body.Unwrap();
								var ex   = body.Transform(wpi => wpi.NodeType == ExpressionType.Parameter ? me.Expression : wpi);

								if (ex.Type != expr.Type)
									ex = new ChangeTypeExpression(ex, expr.Type);

								return ExposeExpression(ex);
							}

							break;
						}

					case ExpressionType.Constant :
						{
							var c = (ConstantExpression)expr;

							// Fix Mono behaviour.
							//
							//if (c.Value is IExpressionQuery)
							//	return ((IQueryable)c.Value).Expression;

							if (c.Value is IQueryable && !(c.Value is ITable))
							{
								var e = ((IQueryable)c.Value).Expression;

								if (!_visitedExpressions.Contains(e))
								{
									_visitedExpressions.Add(e);
									return ExposeExpression(e);
								}
							}

							break;
						}
				}

				return expr;
			});
		}

		#endregion

		#region OptimizeExpression

		private MethodInfo[] _enumerableMethods;
		public  MethodInfo[]  EnumerableMethods
		{
			get { return _enumerableMethods ?? (_enumerableMethods = typeof(Enumerable).GetMethods()); }
		}

		private MethodInfo[] _queryableMethods;
		public  MethodInfo[]  QueryableMethods
		{
			get { return _queryableMethods ?? (_queryableMethods = typeof(Queryable).GetMethods()); }
		}

		readonly Dictionary<Expression, Expression> _optimizedExpressions = new Dictionary<Expression, Expression>();

		Expression OptimizeExpression(Expression expression)
		{
			Expression expr;

			if (_optimizedExpressions.TryGetValue(expression, out expr))
				return expr;

			_optimizedExpressions[expression] = expr = expression.Transform((Func<Expression,Expression>)OptimizeExpressionImpl);

			return expr;
		}

		Expression OptimizeExpressionImpl(Expression expr)
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
								isList = me.Member.DeclaringType.GetInterfaces()
									.Any(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>));

							if (isList)
							{
								var mi = EnumerableMethods
									.First(m => m.Name == "Count" && m.GetParameters().Length == 1)
									.MakeGenericMethod(me.Expression.Type.GetItemType());

								return Expression.Call(null, mi, me.Expression);
							}
						}

						if (CompiledParameters == null && typeof(IQueryable).IsSameOrParentOf(expr.Type))
						{
							var ex = ConvertIQueriable(expr);

							if (!ReferenceEquals(ex, expr))
								return ConvertExpressionTree(ex);
						}

						return ConvertSubquery(expr);
					}

				case ExpressionType.Call :
					{
						var call = (MethodCallExpression)expr;

						if (call.IsQueryable())
						{
							switch (call.Method.Name)
							{
								case "Where"              : return ConvertWhere     (call);
								case "GroupBy"            : return ConvertGroupBy   (call);
								case "SelectMany"         : return ConvertSelectMany(call);
								case "Select"             : return ConvertSelect    (call);
								case "LongCount"          :
								case "Count"              :
								case "Single"             :
								case "SingleOrDefault"    :
								case "First"              :
								case "FirstOrDefault"     : return ConvertPredicate (call);
								case "Min"                :
								case "Max"                : return ConvertSelector  (call, true);
								case "Sum"                :
								case "Average"            : return ConvertSelector  (call, false);
								case "ElementAt"          :
								case "ElementAtOrDefault" : return ConvertElementAt (call);
							}
						}
						else
						{
							var l = ConvertMethodExpression(call.Method);

							if (l != null)
								return OptimizeExpression(ConvertMethod(call, l));

							if (CompiledParameters == null && typeof(IQueryable).IsSameOrParentOf(expr.Type))
							{
								var attr = GetTableFunctionAttribute(call.Method);

								if (attr == null)
								{
									var ex = ConvertIQueriable(expr);

									if (!ReferenceEquals(ex, expr))
										return ConvertExpressionTree(ex);
								}
							}
						}

						return ConvertSubquery(expr);
					}
			}

			return expr;
		}

		LambdaExpression ConvertMethodExpression(MemberInfo mi)
		{
			var attr = MappingSchema.NewSchema.GetAttribute<MethodExpressionAttribute>(mi, a => a.Configuration);

			if (attr != null)
			{
				Expression expr;

				if (mi is MethodInfo && ((MethodInfo)mi).IsGenericMethod)
				{
					var method = (MethodInfo)mi;
					var args   = method.GetGenericArguments();
					var names  = args.Select(t => t.Name).ToArray();
					var name   = string.Format(attr.MethodName, names);

					if (name != attr.MethodName)
						expr = Expression.Call(mi.DeclaringType, name, Array<Type>.Empty);
					else
						expr = Expression.Call(mi.DeclaringType, name, args);
				}
				else
				{
					expr = Expression.Call(mi.DeclaringType, attr.MethodName, Array<Type>.Empty);
				}

				var call = Expression.Lambda<Func<LambdaExpression>>(Expression.Convert(expr, typeof(LambdaExpression)));

				return call.Compile()();
			}

			return null;
		}

		Expression ConvertSubquery(Expression expr)
		{
			var ex = expr;

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
			var param = Expression.Parameter(call.Type, "p");
			var selector = expr.Transform(e => e == call ? param : e);
			var method = GetQueriableMethodInfo(call, (m, _) => m.Name == call.Method.Name && m.GetParameters().Length == 1);
			var select = call.Method.DeclaringType == typeof(Enumerable) ?
				EnumerableMethods
					.Where(m => m.Name == "Select" && m.GetParameters().Length == 2)
					.First(m => m.GetParameters()[1].ParameterType.GetGenericArguments().Length == 2) :
				QueryableMethods
					.Where(m => m.Name == "Select" && m.GetParameters().Length == 2)
					.First(m => m.GetParameters()[1].ParameterType.GetGenericArguments()[0].GetGenericArguments().Length == 2);

			call = (MethodCallExpression)OptimizeExpression(call);
			select = select.MakeGenericMethod(call.Type, expr.Type);
			method = method.MakeGenericMethod(expr.Type);

			return Expression.Call(null, method,
				Expression.Call(null, select,
					call.Arguments[0],
					Expression.Lambda(selector, param)));
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

						if (call.IsQueryable(AggregationBuilder.MethodNames))
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

			Expression expr = null;

			if (exprs.Count > 0)
			{
				expr = lparam;

				foreach (var ex in exprs)
				{
					var type   = typeof(ExpressionHoder<,>).MakeGenericType(expr.Type, ex.Type);
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
						ex = Expression.PropertyOrField(ex, "p");

					ex = Expression.PropertyOrField(ex, "ex");

					dic.Add(exprs[i], ex);

					if (_subQueryExpressions == null)
						_subQueryExpressions = new HashSet<Expression>();
					_subQueryExpressions.Add(ex);
				}

				var newBody = lbody.Transform(ex =>
				{
					Expression e;
					return dic.TryGetValue(ex, out e) ? e : ex;
				});

				var nparm = exprs.Aggregate<Expression,Expression>(parm, (c,t) => Expression.PropertyOrField(c, "p"));

				newBody = newBody.Transform(ex => ReferenceEquals(ex, lparam) ? nparm : ex);

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
					var parameter = Expression.Parameter(expr.Type, lparam.Name);

					methodInfo = GetMethodInfo(method, "Select");
					methodInfo = methodInfo.MakeGenericMethod(expr.Type, lparam.Type);
					method     = Expression.Call(methodInfo, method,
						Expression.Lambda(
							exprs.Aggregate((Expression)parameter, (current,_) => Expression.PropertyOrField(current, "p")),
							parameter));
				}
			}

			return method;
		}

		#endregion

		#region ConvertGroupBy

		public class GroupSubQuery<TKey,TElement>
		{
			public TKey     Key;
			public TElement Element;
		}

		interface IGroupByHelper
		{
			void Set(bool wrapInSubQuery, Expression sourceExpression, LambdaExpression keySelector, LambdaExpression elementSelector, LambdaExpression resultSelector);

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
			bool             _wrapInSubQuery;
			Expression       _sourceExpression;
			LambdaExpression _keySelector;
			LambdaExpression _elementSelector;
			LambdaExpression _resultSelector;

			public void Set(
				bool             wrapInSubQuery,
				Expression       sourceExpression,
				LambdaExpression keySelector,
				LambdaExpression elementSelector,
				LambdaExpression resultSelector)
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
					.GroupBy(_ => _.Key, elemParam => e)
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
					.GroupBy(_ => _.Key, elemParam => e)
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
					.GroupBy(_ => _.Key, elemParam => e)
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
					.GroupBy(_ => _.Key, elemParam => e)
					.Select (resParam => r)
					;

				var body    = func.Body.Unwrap();
				var keyArg  = GetLambda(body, 0, 0, 1).Parameters[0]; // .Select (selectParam
				var elemArg = GetLambda(body, 0, 2).   Parameters[0]; // .GroupBy(..., elemParam
				var resArg  = GetLambda(body, 1).      Parameters[0]; // .Select (resParam

				return Convert(func, keyArg, elemArg, resArg);
			}

			Expression Convert(
				LambdaExpression    func,
				ParameterExpression keyArg,
				ParameterExpression elemArg,
				ParameterExpression resArg)
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
						Expression obj = elemArg;

						if (_wrapInSubQuery)
							obj = Expression.PropertyOrField(elemArg, "Element");

						if (_elementSelector == null)
							return obj;

						return _elementSelector.Body.Transform(e => ReferenceEquals(e, _elementSelector.Parameters[0]) ? obj : e);
					}

					if (ReferenceEquals(ex, func.Parameters[3]))
						return _resultSelector.Body.Transform(e =>
						{
							if (ReferenceEquals(e, _resultSelector.Parameters[0]))
								return Expression.PropertyOrField(resArg, "Key");

							if (ReferenceEquals(e, _resultSelector.Parameters[1]))
								return resArg;

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
				(IGroupByHelper)Activator.CreateInstance(gtype);

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
						var attr = GetFunctionAttribute(ma.Member);

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
			Expression       _sourceExpression;
			LambdaExpression _colSelector;

			public void Set(Expression sourceExpression, LambdaExpression colSelector)
			{
				_sourceExpression = sourceExpression;
				_colSelector      = colSelector;
			}

			public Expression AddElementSelectorQ()
			{
				Expression<Func<IQueryable<TSource>,IEnumerable<TCollection>,IQueryable<TCollection>>> func = (source,col) => source
					.SelectMany(colParam => col, (s,c) => c)
					;

				var body   = func.Body.Unwrap();
				var colArg = GetLambda(body, 1).Parameters[0]; // .SelectMany(colParam

				return Convert(func, colArg);
			}

			public Expression AddElementSelectorE()
			{
				Expression<Func<IEnumerable<TSource>,IEnumerable<TCollection>,IEnumerable<TCollection>>> func = (source,col) => source
					.SelectMany(colParam => col, (s,c) => c)
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
				(ISelectManyHelper)Activator.CreateInstance(gtype);

			helper.Set(sourceExpression, colSelector);

			return method.Method.DeclaringType == typeof(Queryable) ?
				helper.AddElementSelectorQ() :
				helper.AddElementSelectorE();
		}

		#endregion

		#region ConvertPredicate

		Expression ConvertPredicate(MethodCallExpression method)
		{
			if (method.Arguments.Count != 2)
				return method;

			var cm = GetQueriableMethodInfo(method, (m,_) => m.Name == method.Method.Name && m.GetParameters().Length == 1);
			var wm = GetMethodInfo(method, "Where");

			var argType = method.Method.GetGenericArguments()[0];

			wm = wm.MakeGenericMethod(argType);
			cm = cm.MakeGenericMethod(argType);

			return Expression.Call(null, cm,
				Expression.Call(null, wm,
					OptimizeExpression(method.Arguments[0]),
					OptimizeExpression(method.Arguments[1])));
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
			var cm    = GetQueriableMethodInfo(method, (m,isDefault) =>
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

			return Expression.Call(null, cm,
				OptimizeExpression(Expression.Call(null, sm,
					method.Arguments[0],
					method.Arguments[1])));
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

			return Expression.Call(null,
				GetMethodInfo(method, "Select").MakeGenericMethod(types1[0], types2[1]),
				((MethodCallExpression)sequence).Arguments[0],
				Expression.Lambda(lambda.GetBody(sbody), slambda.Parameters[0]));
		}

		#endregion

		#region ConvertIQueriable

		Expression ConvertIQueriable(Expression expression)
		{
			if (expression.NodeType == ExpressionType.MemberAccess || expression.NodeType == ExpressionType.Call)
			{
				var p    = Expression.Parameter(typeof(Expression), "exp");
				var exas = expression.GetExpressionAccessors(p);
				var expr = ReplaceParameter(exas, expression, _ => {});

				if (expr.Find(e => e.NodeType == ExpressionType.Parameter && e != p) != null)
					return expression;

				var l    = Expression.Lambda<Func<Expression,IQueryable>>(Expression.Convert(expr, typeof(IQueryable)), new [] { p });
				var n    = _query.AddQueryableAccessors(expression, l);

				Expression accessor;

				_expressionAccessors.TryGetValue(expression, out accessor);

				var path =
					Expression.Call(
						Expression.Constant(_query),
						MemberHelper.MethodOf<Query>(a => a.GetIQueryable(0, null)),
						new[] { Expression.Constant(n), accessor ?? Expression.Constant(null, typeof(Expression)) });

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
				skipMethod = MemberHelper.MethodOf(() => LinqExtensions.Skip<object>(null, null));
				skipMethod = skipMethod.GetGenericMethodDefinition();
			}
			else
			{
				skipMethod = GetQueriableMethodInfo(method, (mi,_) => mi.Name == "Skip");
			}

			skipMethod = skipMethod.MakeGenericMethod(sourceType);

			var methodName  = method.Method.Name == "ElementAt" ? "First" : "FirstOrDefault";
			var firstMethod = GetQueriableMethodInfo(method, (mi,_) => mi.Name == methodName && mi.GetParameters().Length == 1);

			firstMethod = firstMethod.MakeGenericMethod(sourceType);

			return Expression.Call(null, firstMethod, Expression.Call(skipMethod, sequence, index));
		}

		#endregion

		#region Helpers

		MethodInfo GetQueriableMethodInfo(MethodCallExpression method, Func<MethodInfo,bool,bool> predicate)
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

		#endregion

		#endregion
	}
}
