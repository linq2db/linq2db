using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Common;
	using Extensions;
	using LinqToDB.Expressions;
	using Mapping;
	using SqlQuery;
	using Reflection;

	class GroupByBuilder : MethodCallBuilder
	{
		private static readonly MethodInfo[] GroupingSetMethods = new [] { Methods.LinqToDB.GroupBy.Rollup, Methods.LinqToDB.GroupBy.Cube, Methods.LinqToDB.GroupBy.GroupingSets };

		#region Builder Methods

		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			if (!methodCall.IsQueryable("GroupBy"))
				return false;

			var body = ((LambdaExpression)methodCall.Arguments[1].Unwrap()).Body.Unwrap();

			if (body.NodeType == ExpressionType	.MemberInit)
			{
				var mi = (MemberInitExpression)body;
				bool throwExpr;

				if (mi.NewExpression.Arguments.Count > 0 || mi.Bindings.Count == 0)
					throwExpr = true;
				else
					throwExpr = mi.Bindings.Any(b => b.BindingType != MemberBindingType.Assignment);

				if (throwExpr)
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in group by is not allowed.");
			}

			return (methodCall.Arguments[methodCall.Arguments.Count - 1].Unwrap().NodeType == ExpressionType.Lambda);
		}

		static IEnumerable<Expression> EnumGroupingSets(Expression expression)
		{
			switch (expression.NodeType)
			{
				case ExpressionType.New:
					{
						var newExpression = (NewExpression)expression;

						foreach (var arg in newExpression.Arguments)
						{
							yield return arg;
						}
						break;
					}
			}
		}


		/*

		-- describing subqueries when building GroupByContext

		 SELECT GroupByContext.*
		 FROM 
		 (
		    SELECT
				GroupByContext.SubQuery.Field1,
				GroupByContext.SubQuery.Field2,
				Count(*),
				SUM(GroupByContext.SubQuery.Field3)
		    FROM 
			(
				SELECT dataSubquery.*
				FROM (
				   SELECT dataSequence.*
				   FROM dataSequence
				   -- all associations are attached here
				) dataSubquery
		    ) GroupByContext.SubQuery	-- groupingSubquery
		    GROUP BY
					GroupByContext.SubQuery.Field1,
					GroupByContext.SubQuery.Field2
		 ) GroupByContext

		 OUTER APPLY (  -- applying complex aggregates
			SELECT Count(*) FROM dataSubquery
			WHERE dataSubquery.Field > 10 AND 
				-- filter by grouping key
				dataSubquery.Field1 == GroupByContext.Field1 AND dataSubquery.Field2 == GroupByContext.Field2
		 )

		 */
		
		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequenceExpr    = methodCall.Arguments[0];
			var wrapSequence    = false;
			LambdaExpression?   groupingKey = null;
			var groupingKind    = GroupingType.Default;
			if (sequenceExpr.NodeType == ExpressionType.Call)
			{
				var call = (MethodCallExpression)methodCall.Arguments[0];

				if (call.IsQueryable("Select"))
				{
					var selectParam = (LambdaExpression)call.Arguments[1].Unwrap();
					var type = selectParam.Body.Type;

					if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ExpressionBuilder.GroupSubQuery<,>))
					{
						wrapSequence = true;

						var selectParamBody = selectParam.Body.Unwrap();
						MethodCallExpression? groupingMethod = null;
						if (selectParamBody is MemberInitExpression mi)
						{
							var assignment = mi.Bindings.OfType<MemberAssignment>().FirstOrDefault(m => m.Member.Name == "Key");
							if (assignment?.Expression.NodeType == ExpressionType.Call)
							{
								var mc = (MethodCallExpression)assignment.Expression;
								if (mc.IsSameGenericMethod(GroupingSetMethods))
								{
									groupingMethod = mc;
									groupingKey    = (LambdaExpression)mc.Arguments[0].Unwrap();
									if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.Rollup))
										groupingKind = GroupingType.Rollup;
									else if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.Cube))
										groupingKind = GroupingType.Cube;
									else if (mc.IsSameGenericMethod(Methods.LinqToDB.GroupBy.GroupingSets))
										groupingKind = GroupingType.GroupBySets;
									else throw new InvalidOperationException();
								}
							}
						}

						if (groupingMethod != null && groupingKey != null)
						{
							sequenceExpr = sequenceExpr.Replace(groupingMethod, groupingKey.Body.Unwrap());
						}

					}
				}
			}

			var dataSequence = builder.BuildSequence(new BuildInfo(buildInfo, sequenceExpr));

			var dataSubquery     = new SubQueryContext(dataSequence);
			var groupingSubquery = new SubQueryContext(dataSubquery);

			var keySequence     = dataSequence;

			var groupingType    = methodCall.Type.GetGenericArguments()[0];
			var keySelector     = (LambdaExpression)methodCall.Arguments[1].Unwrap()!;
			LambdaExpression elementSelector;
			if (methodCall.Arguments.Count >= 3)
			{
				elementSelector = (LambdaExpression)methodCall.Arguments[2].Unwrap()!;
			}
			else
			{
				var param = Expression.Parameter(groupingType.GetGenericArguments()[1], "selector");
				elementSelector = Expression.Lambda(param, param);
			}

			var key                 = new KeyContext(groupingSubquery, keySelector, buildInfo.IsSubQuery, keySequence);
			var keyRef              = new ContextRefExpression(keySelector.Parameters[0].Type, key);
			var currentPlaceholders = new List<SqlPlaceholderExpression>();
			if (groupingKind != GroupingType.GroupBySets)
			{
				AppendGrouping(groupingSubquery, currentPlaceholders, builder, dataSequence, key.Body);
			}
			else
			{
				var goupingSetBody = groupingKey!.Body;
				var groupingSets = EnumGroupingSets(goupingSetBody).ToArray();
				if (groupingSets.Length == 0)
					throw new LinqException($"Invalid grouping sets expression '{goupingSetBody}'.");

				foreach (var groupingSet in groupingSets)
				{
					var groupSql = builder.ConvertExpressions(keySequence, groupingSet, ConvertFlags.Key, null);
					groupingSubquery.SelectQuery.GroupBy.Items.Add(
						new SqlGroupingSet(groupSql.Select(s => keySequence.SelectQuery.Select.AddColumn(s.Sql))));
				}
			}

			groupingSubquery.SelectQuery.GroupBy.GroupingType = groupingKind;

			var element = new ElementContext(buildInfo.Parent, elementSelector, buildInfo.IsSubQuery, dataSubquery);
			var groupBy = new GroupByContext(groupingSubquery, sequenceExpr, groupingType, key, keyRef, currentPlaceholders, element, builder.IsGroupingGuardDisabled);

			// Will be used for eager loading generation
			element.GroupByContext = groupBy;
			// Will be used for completing GroupBy part
			key.GroupByContext = groupBy;

			Debug.WriteLine("BuildMethodCall GroupBy:\n" + groupBy.SelectQuery);

			return groupBy;
		}

		/// <summary>
		/// Appends GroupBy items to <paramref name="sequence"/> SelectQuery.
		/// </summary>
		/// <param name="sequence">Context which contains groping query.</param>
		/// <param name="currentPlaceholders"></param>
		/// <param name="builder"></param>
		/// <param name="onSequence">Context from which level we want to get groping SQL.</param>
		/// <param name="path">Actual expression which should be translated to grouping keys.</param>
		static void AppendGrouping(IBuildContext sequence, List<SqlPlaceholderExpression> currentPlaceholders, ExpressionBuilder builder, IBuildContext onSequence, Expression path)
		{
			if (builder.TryConvertToSqlExpr(onSequence, path, ProjectFlags.SQL) is SqlPlaceholderExpression groupKey)
			{
				groupKey = (SqlPlaceholderExpression)builder.UpdateNesting(sequence, groupKey);
				AppendGroupBy(builder, currentPlaceholders, sequence.SelectQuery, groupKey);
			}
			else
			{
				// only keys
				var groupSqlExpr = builder.BuildSqlExpression(new Dictionary<Expression, Expression>(), onSequence, path, ProjectFlags.SQL | ProjectFlags.Keys);
				groupSqlExpr = builder.UpdateNesting(sequence, groupSqlExpr);

				AppendGroupBy(builder, currentPlaceholders, sequence.SelectQuery, groupSqlExpr);
			}
		}

		static void AppendGroupBy(ExpressionBuilder builder, List<SqlPlaceholderExpression> currentPlaceholders, SelectQuery query, Expression result)
		{
			var placeholders = builder.CollectDistinctPlaceholders(result);
			var allowed      = placeholders.Where(p => !QueryHelper.IsConstantFast(p.Sql));

			foreach (var p in allowed)
			{
				if (currentPlaceholders.Find(cp => ExpressionEqualityComparer.Instance.Equals(cp.Path, p.Path)) == null)
				{
					currentPlaceholders.Add(p);
					query.GroupBy.Expr(p.Sql);
				}
			}
		}


		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		#endregion

		#region Element Context

		internal class ElementContext : SelectContext
		{
			public ElementContext(IBuildContext? parent, LambdaExpression lambda, bool isSubQuery, params IBuildContext[] sequences) : base(parent, lambda, isSubQuery, sequences)
			{
			}

			public GroupByContext GroupByContext { get; set; } = null!;

			public override Expression GetEagerLoadExpression(Expression path)
			{
				var subquery = GroupByContext.MakeSubQueryExpression(new ContextRefExpression(path.Type, GroupByContext));
				return subquery;
			}

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.HasFlag(ProjectFlags.Root) && SequenceHelper.IsSameContext(path, this))
					return path;

				var newExpr = base.MakeExpression(path, flags);

				return newExpr;
			}
		}

		#endregion

		#region KeyContext

		internal class KeyContext : SelectContext
		{
			public KeyContext(IBuildContext? parent, LambdaExpression lambda, bool isSubQuery, params IBuildContext[] sequences)
				: base(parent, lambda, isSubQuery, sequences)
			{
			}

			public GroupByContext GroupByContext { get; set; } = null!;

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (flags.HasFlag(ProjectFlags.Root) && SequenceHelper.IsSameContext(path, this))
					return path;

				var newFlags = flags;
				if (newFlags.HasFlag(ProjectFlags.Expression))
					newFlags = (newFlags & ~ProjectFlags.Expression) | ProjectFlags.SQL;

				var result = base.MakeExpression(path, newFlags);

				result = Builder.MakeExpression(result, newFlags);

				// appending missing keys
				AppendGroupBy(Builder, GroupByContext.CurrentPlaceholders, GroupByContext.SubQuery.SelectQuery, result);

				return result;
			}
		}

		#endregion

		#region GroupByContext

		internal class GroupByContext : SubQueryContext
		{
			public GroupByContext(
				IBuildContext  sequence,
				Expression     sequenceExpr,
				Type           groupingType,
				KeyContext     key,
				ContextRefExpression keyRef,
				List<SqlPlaceholderExpression> currentPlaceholders,
				SelectContext  element,
				bool           isGroupingGuardDisabled)
				: base(sequence)
			{
				_sequenceExpr        = sequenceExpr;
				_key                 = key;
				_keyRef              = keyRef;
				CurrentPlaceholders = currentPlaceholders;
				Element              = element;
				_groupingType        = groupingType;

				_isGroupingGuardDisabled = isGroupingGuardDisabled;

				key.Parent = this;
			}

			readonly Expression                     _sequenceExpr;
			readonly KeyContext                     _key;
			readonly ContextRefExpression           _keyRef;
			public   List<SqlPlaceholderExpression> CurrentPlaceholders { get; }
			readonly Type                           _groupingType;
			readonly bool                           _isGroupingGuardDisabled;

			public SelectContext   Element { get; }

			public override Expression MakeExpression(Expression path, ProjectFlags flags)
			{
				if (SequenceHelper.IsSameContext(path, this) &&
				    (flags.HasFlag(ProjectFlags.Root) || flags.HasFlag(ProjectFlags.Test)))
				{
					return path;
				}

				if (SequenceHelper.IsSameContext(path, this) && flags.HasFlag(ProjectFlags.Keys))
				{
					var result = Builder.MakeExpression(_keyRef, flags);
					return result;
				}

				if (path is MemberExpression me && me.Expression is ContextRefExpression && me.Member.Name == "Key")
				{
					var keyPath = new ContextRefExpression(me.Type, _key);

					return keyPath;
				}

				var newPath = SequenceHelper.CorrectExpression(path, this, Element);

				return newPath;
			}

			internal class Grouping<TKey,TElement> : IGrouping<TKey,TElement>
			{
				public Grouping(
					TKey                    key,
					IQueryRunner            queryRunner,
					List<ParameterAccessor> parameters,
					Func<IDataContext,TKey,object?[]?,IQueryable<TElement>> itemReader)
				{
					Key = key;

					_queryExpression = queryRunner.Expression;
					_queryParameters = queryRunner.Parameters;
					_dataContext     = queryRunner.DataContext;
					_parameters      = parameters;
					_itemReader      = itemReader;

					if (Configuration.Linq.PreloadGroups)
					{
						_items = GetItems();
					}
				}

				private  IList<TElement>?                                        _items;
				readonly IDataContext                                            _dataContext;
				readonly Expression                                              _queryExpression;
				readonly object?[]?                                              _queryParameters;
				readonly List<ParameterAccessor>                                 _parameters;
				readonly Func<IDataContext,TKey,object?[]?,IQueryable<TElement>> _itemReader;

				public TKey Key { get; private set; }

				List<TElement> GetItems()
				{
					using (var db = _dataContext.Clone(true))
					{
						var ps = new object?[_parameters.Count];

						for (var i = 0; i < ps.Length; i++)
							ps[i] = _parameters[i].OriginalAccessor(_queryExpression, db, _queryParameters);

						return _itemReader(db, Key, ps).ToList();
					}
				}

				public IEnumerator<TElement> GetEnumerator()
				{
					if (_items == null)
						_items = GetItems();

					return _items.GetEnumerator();
				}

				IEnumerator IEnumerable.GetEnumerator()
				{
					return GetEnumerator();
				}
			}

			interface IGroupByHelper
			{
				Expression GetGrouping(GroupByContext context);
			}

			class GroupByHelper<TKey,TElement,TSource> : IGroupByHelper
			{
				public Expression GetGrouping(GroupByContext context)
				{
					if (Configuration.Linq.GuardGrouping && !context._isGroupingGuardDisabled)
					{
						if (context.Element.Body is ContextRefExpression)
						{
							var ex = new LinqToDBException(
								"You should explicitly specify selected fields for server-side GroupBy() call or add AsEnumerable() call before GroupBy() to perform client-side grouping.\n" +
								"Set Configuration.Linq.GuardGrouping = false to disable this check.\n" +
								"Additionally this guard exception can be disabled by extension GroupBy(...).DisableGuard().\n" +
								"NOTE! By disabling this guard you accept additional Database Connection(s) to the server for processing such requests."
							)
							{
								HelpLink = "https://github.com/linq2db/linq2db/issues/365"
							};

							throw ex;
						}
					}

					var parameters = context.Builder.ParametersContext.CurrentSqlParameters
						.Select((p, i) => (p, i))
						.ToDictionary(_ => _.p.Expression, _ => _.i);
					var paramArray = Expression.Parameter(typeof(object[]), "ps");

					var groupExpression = context._sequenceExpr.Transform(
						(parameters, paramArray),
						static (context, e) =>
						{
							if (context.parameters.TryGetValue(e, out var idx))
							{
								return
									Expression.Convert(
										Expression.ArrayIndex(context.paramArray, ExpressionInstances.Int32(idx)),
										e.Type);
							}

							return e;
						});

					var keyParam = Expression.Parameter(typeof(TKey), "key");

// ReSharper disable AssignNullToNotNullAttribute

					var expr = Expression.Call(
						null,
						MemberHelper.MethodOf(() => Queryable.Where(null, (Expression<Func<TSource,bool>>)null!)),
						groupExpression,
						Expression.Lambda<Func<TSource,bool>>(
							ExpressionBuilder.Equal(context.Builder.MappingSchema, context._key.Lambda.Body, keyParam),
							new[] { context._key.Lambda.Parameters[0] }));

					expr = Expression.Call(
						null,
						MemberHelper.MethodOf(() => Queryable.Select(null, (Expression<Func<TSource,TElement>>)null!)),
						expr,
						context.Element.Lambda);

// ReSharper restore AssignNullToNotNullAttribute

					var lambda = Expression.Lambda<Func<IDataContext,TKey,object?[]?,IQueryable<TElement>>>(
						Expression.Convert(expr, typeof(IQueryable<TElement>)),
						Expression.Parameter(typeof(IDataContext), "ctx"),
						keyParam,
						paramArray);

					var itemReader      = CompiledQuery.Compile(lambda);
					var keyExpr         = context._key.BuildExpression(null, 0, false);

					return Expression.Call(
						null,
						MemberHelper.MethodOf(() => GetGrouping(null!, null!, default!, null!)),
						new Expression[]
						{
							ExpressionBuilder.QueryRunnerParam,
							Expression.Constant(context.Builder.ParametersContext.CurrentSqlParameters),
							keyExpr,
							Expression.Constant(itemReader)
						});
				}

				static IGrouping<TKey,TElement> GetGrouping(
					IQueryRunner                                            runner,
					List<ParameterAccessor>                                 parameterAccessor,
					TKey                                                    key,
					Func<IDataContext,TKey,object?[]?,IQueryable<TElement>> itemReader)
				{
					return new Grouping<TKey,TElement>(key, runner, parameterAccessor, itemReader);
				}
			}

			Expression BuildGrouping()
			{
				var gtype = typeof(GroupByHelper<,,>).MakeGenericType(
					_key.Lambda.Body.Type,
					Element.Lambda.Body.Type,
					_key.Lambda.Parameters[0].Type);

				var isBlockDisable = Builder.IsBlockDisable;

				Builder.IsBlockDisable = true;

				var helper = (IGroupByHelper)Activator.CreateInstance(gtype)!;
				var expr   = helper.GetGrouping(this);

				Builder.IsBlockDisable = isBlockDisable;

				return expr;
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				if (SequenceHelper.IsSameContext(expression, this))
				{
					return BuildGrouping();
				}

				if (expression!.NodeType == ExpressionType.MemberAccess)
				{
					var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, 1);

					if (levelExpression.NodeType == ExpressionType.MemberAccess)
					{
						var ma = (MemberExpression)levelExpression;

						if (ma.Member.Name == "Key" && ma.Member.DeclaringType == _groupingType)
						{
							var keyRef = new ContextRefExpression(levelExpression.Type, _key);

							var keyExpression = expression.Replace(levelExpression, keyRef);

							var isBlockDisable = Builder.IsBlockDisable;

							Builder.IsBlockDisable = true;

							var r = _key.BuildExpression(keyExpression, 0, enforceServerSide);

							Builder.IsBlockDisable = isBlockDisable;

							return r;
						}
					}
				}

				throw new NotImplementedException();
			}

			ISqlExpression ConvertEnumerable(MethodCallExpression call)
			{
				if (call.IsAggregate(Builder.MappingSchema))
				{
					if (call.Arguments[0].NodeType == ExpressionType.Call)
					{
						var arg = (MethodCallExpression)call.Arguments[0];

						if (arg.Method.Name == "Select")
						{
							var arg0 = arg.Arguments[0].SkipPathThrough();

							if (arg0.NodeType != ExpressionType.Call)
							{
								var l     = (LambdaExpression)arg.Arguments[1].Unwrap();
								var largs = l.Type.GetGenericArguments();

								if (largs.Length == 2)
								{
									var body = SequenceHelper.PrepareBody(l, Element);
									var sql  = Builder.ConvertToSql(this, body, unwrap: true);


									return new SqlFunction(call.Type, call.Method.Name, true, sql);
								}
							}
						}
					}
				}

				var rootArgument = call.Arguments[0].SkipMethodChain(Builder.MappingSchema);

				if (rootArgument.NodeType == ExpressionType.Call)
				{
					var ctx = Builder.GetSubQuery(this, call);

					if (Builder.DataContext.SqlProviderFlags.IsSubQueryColumnSupported)
						return ctx.SelectQuery;

					var join = ctx.SelectQuery.CrossApply();

					SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);

					return ctx.SelectQuery.Select.Columns[0];
				}

				var args = new ISqlExpression[call.Arguments.Count - 1];

				if (CountBuilder.MethodNames.Contains(call.Method.Name))
				{
					if (args.Length > 0)
						throw new InvalidOperationException();

					return SqlFunction.CreateCount(call.Type, SelectQuery);
				}

				var attribute =
					Builder.MappingSchema.GetAttribute<Sql.ExpressionAttribute>(call.Method.DeclaringType!, call.Method,
						c => c.Configuration);

				if (attribute != null)
				{
					var expr = attribute.GetExpression((context: this, rootArgument), Builder.DataContext, SelectQuery, call, static (context, e, descriptor) =>
					{
						var ex = e.Unwrap();

						if (ex is LambdaExpression l)
						{
							var p = context.context.Element.Parent;
							var ctx = new ExpressionContext(context.context.Parent, context.context.Element, l);

							var res = context.context.Builder.ConvertToSql(ctx, l.Body, unwrap: true, columnDescriptor: descriptor);

							context.context.Builder.ReplaceParent(ctx, p);
							return res;
						}

						if (context.rootArgument == e && typeof(IGrouping<,>).IsSameOrParentOf(ex.Type))
						{
							return context.context.Element.ConvertToSql(null, 0, ConvertFlags.Field)
								.Select(_ => _.Sql)
								.FirstOrDefault();
						}

						if (typeof(IGrouping<,>).IsSameOrParentOf(context.context.Builder.GetRootObject(ex).Type))
						{
							return context.context.ConvertToSql(ex, 0, ConvertFlags.Field)
								.Select(_ => _.Sql)
								.FirstOrDefault();
						}

						return context.context.Builder.ConvertToExtensionSql(context.context.Element, ex, descriptor);
					});

					if (expr != null)
						return expr;
				}

				if (call.Arguments.Count > 1)
				{
					for (var i = 1; i < call.Arguments.Count; i++)
					{
						var ex = call.Arguments[i].Unwrap();

						if (ex is LambdaExpression l)
						{
							var p   = Element.Parent;
							var ctx = new ExpressionContext(Parent, Element, l);

							args[i - 1] = Builder.ConvertToSql(ctx, l.Body, unwrap: true);

							Builder.ReplaceParent(ctx, p);
						}
						else
						{
							args[i - 1] = Builder.ConvertToSql(Element, ex, unwrap: true);
						}
					}
				}
				else
				{
					args = Element.ConvertToSql(null, 0, ConvertFlags.Field).Select(_ => _.Sql).ToArray();
				}

				return new SqlFunction(call.Type, call.Method.Name, true, args);
			}

			PropertyInfo? _keyProperty;

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				if (SequenceHelper.IsSameContext(expression, this))
				{
					if (flags == ConvertFlags.Field && !_key.IsScalar)
						return Element.ConvertToSql(null, 0, flags);
					var keys = _key.ConvertToSql(null, 0, flags);
					for (var i = 0; i < keys.Length; i++)
					{
						var key = keys[i];
						keys[i] = key.AppendMember(_keyProperty!);
					}

					return keys;
				}

				switch (expression.NodeType)
				{
					case ExpressionType.Call:
					{
						var e = (MethodCallExpression)expression;

						if (e.IsQueryable() || e.IsAggregate(Builder.MappingSchema))
						{
							return new[] {new SqlInfo(ConvertEnumerable(e))};
						}

						break;
					}

					case ExpressionType.MemberAccess:
					{
						var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, 1);

						if (levelExpression.NodeType == ExpressionType.MemberAccess)
						{
							var e = (MemberExpression)levelExpression;

							if (e.Member.Name == "Key")
							{
								if (_keyProperty == null)
									_keyProperty = _groupingType.GetProperty("Key");

								if (e.Member == _keyProperty)
								{
									if (ReferenceEquals(levelExpression, expression))
										return _key.ConvertToSql(null, 0, flags);

									var keyRef   = new ContextRefExpression(levelExpression.Type, _key);
									var replaced = expression.Replace(levelExpression, keyRef);

									return _key.ConvertToSql(replaced, 0, flags);
								}
							}

							expression = SequenceHelper.CorrectExpression(expression, this, _key);

							return _key.ConvertToSql(expression, level, flags);
						}

						break;
					}
				}

				throw new LinqException("Expression '{0}' cannot be converted to SQL.", expression);
			}

			readonly Dictionary<Tuple<Expression?,int,ConvertFlags>,SqlInfo[]> _expressionIndex = new ();

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				var key = Tuple.Create(expression, level, flags);

				if (!_expressionIndex.TryGetValue(key, out var info))
				{
					info = ConvertToSql(expression, level, flags);

					for (var i = 0; i < info.Length; i++)
					{
						var item = info[i];
						info[i] = item
							.WithQuery(SelectQuery)
							.WithIndex(SelectQuery.Select.Add(item.Sql));
					}
				}

				return info;
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				/*if (expression != null)
				{
					//var levelExpression = expression!.GetLevelExpression(Builder.MappingSchema, level);
					var levelExpression = expression;

					if (levelExpression.NodeType == ExpressionType.MemberAccess)
					{
						var ma = (MemberExpression)levelExpression;

						if (ma.Member.Name == "Key" && ma.Member.DeclaringType == _groupingType)
						{
							return ReferenceEquals(levelExpression, expression) ?
								_key.IsExpression(null,       0,         requestFlag) :
								_key.IsExpression(expression, level + 1, requestFlag);
						}
					}
					else if (levelExpression is ContextRefExpression contextRef && contextRef.BuildContext == this)
					{
						return _key.IsExpression(null, 0, requestFlag);
					}
					else if (levelExpression.NodeType == ExpressionType.Call)
						if (requestFlag == RequestFor.Expression)
							return IsExpressionResult.True;
				}*/

				if (expression != null)
				{
					if (expression!.NodeType == ExpressionType.MemberAccess)
					{
						var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, 1);

						if (levelExpression.NodeType == ExpressionType.MemberAccess)
						{
							var ma = (MemberExpression)levelExpression;

							if (ma.Member.Name == "Key" && ma.Member.DeclaringType == _groupingType)
							{
								var keyRef = new ContextRefExpression(levelExpression.Type, _key);

								var keyExpression = expression.Replace(levelExpression, keyRef);

								var r = _key.IsExpression(keyExpression, 0, requestFlag);

								return r;
							}
						}
					}
				}

				return IsExpressionResult.False;
			}

			public override int ConvertToParentIndex(int index, IBuildContext context)
			{
				var expr = SelectQuery.Select.Columns[index].Expression;

				if (((Builder.DataContext.SqlProviderFlags.IsGroupByColumnRequred && expr is SqlColumn)
					|| !QueryHelper.IsConstant(expr))
					&& !SelectQuery.GroupBy.EnumItems().Any(_ => ReferenceEquals(_, expr) || (expr is SqlColumn column && ReferenceEquals(_, column.Expression))))
				{
					if (SelectQuery.GroupBy.GroupingType == GroupingType.GroupBySets)
						SelectQuery.GroupBy.Items.Add(new SqlGroupingSet(new[] { expr }));
					else
					{
						SelectQuery.GroupBy.Items.Add(expr);
					}
				}

				return base.ConvertToParentIndex(index, this);
			}

			static Expression MakeSubQueryExpression(MappingSchema mappingSchema, Expression sequence,
				ParameterExpression param, Expression expr1, Expression expr2)
			{
				var filterLambda = Expression.Lambda(ExpressionBuilder.Equal(mappingSchema, expr1, expr2), param);
				return TypeHelper.MakeMethodCall(Methods.Enumerable.Where, sequence, filterLambda);
			}

			public Expression MakeSubQueryExpression(Expression buildExpression)
			{
				var expr = MakeSubQueryExpression(
					Builder.MappingSchema,
					_sequenceExpr,
					_key.Lambda.Parameters[0],
					ExpressionHelper.PropertyOrField(buildExpression, "Key"),
					_key.Lambda.Body);

				expr = TypeHelper.MakeMethodCall(Methods.Enumerable.Select, expr, Element.Lambda);

				return expr;
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				if (buildInfo.AggregationTest)
					return new AggregationRoot(Element);

				if (!buildInfo.IsSubQuery)
					return this;

				if (buildInfo.IsAggregation)
				{
					return this;
				}

				if (!SequenceHelper.IsSameContext(expression, this))
					return null;

				var expr = MakeSubQueryExpression(buildInfo.Expression);

				var ctx = Builder.BuildSequence(new BuildInfo(buildInfo, expr) { IsAggregation = false });

				return ctx;
			}
		}

		#endregion

		public class AggregationRoot : PassThroughContext
		{
			public AggregationRoot(IBuildContext context) : base(context)
			{
				SelectQuery = new SelectQuery();
			}

			public override SelectQuery SelectQuery { get; set; }
		}
	}
}
