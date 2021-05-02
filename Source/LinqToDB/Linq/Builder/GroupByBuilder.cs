using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
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

			var sequence        = builder.BuildSequence(new BuildInfo(buildInfo, sequenceExpr));
			var keySequence     = sequence;

			var groupingType    = methodCall.Type.GetGenericArguments()[0];
			var keySelector     = (LambdaExpression)methodCall.Arguments[1].Unwrap()!;
			var elementSelector = (LambdaExpression)methodCall.Arguments[2].Unwrap()!;

			if (wrapSequence)
			{ 
				sequence = new SubQueryContext(sequence);
			}

			sequence     = new SubQueryContext(sequence);
			var key      = new KeyContext(buildInfo.Parent, keySelector, sequence);
			if (groupingKind != GroupingType.GroupBySets)
			{
				var groupSql = builder.ConvertExpressions(key, keySelector.Body.Unwrap(), ConvertFlags.Key, null);

				var allowed = groupSql.Where(s => !QueryHelper.IsConstantFast(s.Sql));

				foreach (var sql in allowed)
					sequence.SelectQuery.GroupBy.Expr(sql.Sql);
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
					sequence.SelectQuery.GroupBy.Items.Add(
						new SqlGroupingSet(groupSql.Select(s => keySequence.SelectQuery.Select.AddColumn(s.Sql))));
				}
			}

			sequence.SelectQuery.GroupBy.GroupingType = groupingKind;

			var element = new SelectContext (buildInfo.Parent, elementSelector, sequence/*, key*/);
			var groupBy = new GroupByContext(buildInfo.Parent, sequenceExpr, groupingType, sequence, key, element, builder.IsGroupingGuardDisabled);

			Debug.WriteLine("BuildMethodCall GroupBy:\n" + groupBy.SelectQuery);

			return groupBy;
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		#endregion

		#region KeyContext

		internal class KeyContext : SelectContext
		{
			public KeyContext(IBuildContext? parent, LambdaExpression lambda, params IBuildContext[] sequences)
				: base(parent, lambda, sequences)
			{
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				return base.BuildExpression(expression, level, true);
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				base.BuildQuery(query, queryParameter);
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				return base.ConvertToSql(expression, level, flags);
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				return base.ConvertToIndex(expression, level, flags);
			}

			public override int ConvertToParentIndex(int index, IBuildContext context)
			{
				return base.ConvertToParentIndex(index, context);
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				return base.IsExpression(expression, level, requestFlag);
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				return base.GetContext(expression, level, buildInfo);
			}
		}

		#endregion

		#region GroupByContext

		internal class GroupByContext : SequenceContextBase
		{
			public GroupByContext(
				IBuildContext? parent,
				Expression     sequenceExpr,
				Type           groupingType,
				IBuildContext  sequence,
				KeyContext     key,
				SelectContext  element,
				bool           isGroupingGuardDisabled)
				: base(parent, sequence, null)
			{
				_sequenceExpr = sequenceExpr;
				_key          = key;
				Element       = element;
				_groupingType = groupingType;

				_isGroupingGuardDisabled = isGroupingGuardDisabled;

				key.Parent = this;
			}

			readonly Expression    _sequenceExpr;
			readonly KeyContext    _key;
			readonly Type          _groupingType;
			readonly bool          _isGroupingGuardDisabled;

			public SelectContext   Element { get; }

			internal class Grouping<TKey,TElement> : IGrouping<TKey,TElement>
			{
				public Grouping(
					TKey                    key,
					IQueryRunner            queryRunner,
					List<ParameterAccessor> parameters,
					Func<IDataContext,TKey,object?[]?,IQueryable<TElement>> itemReader)
				{
					Key = key;

					_queryRunner = queryRunner;
					_parameters   = parameters;
					_itemReader   = itemReader;

					if (Configuration.Linq.PreloadGroups)
					{
						_items = GetItems();
					}
				}

				private  IList<TElement>?                                        _items;
				readonly IQueryRunner                                            _queryRunner;
				readonly List<ParameterAccessor>                                 _parameters;
				readonly Func<IDataContext,TKey,object?[]?,IQueryable<TElement>> _itemReader;

				public TKey Key { get; private set; }

				List<TElement> GetItems()
				{
					using (var db = _queryRunner.DataContext.Clone(true))
					{
						var ps = new object?[_parameters.Count];

						for (var i = 0; i < ps.Length; i++)
							ps[i] = _parameters[i].OriginalAccessor(_queryRunner.Expression, _queryRunner.DataContext, _queryRunner.Parameters);

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
						if (context.Element.Lambda.Parameters.Count == 1 &&
							context.Element.Body == context.Element.Lambda.Parameters[0])
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

					var parameters = context.Builder.CurrentSqlParameters
						.Select((p,i) => new { p, i })
						.ToDictionary(_ => _.p.Expression, _ => _.i);
					var paramArray = Expression.Parameter(typeof(object[]), "ps");

					var groupExpression = context._sequenceExpr.Transform(
						new { parameters, paramArray },
						static (context, e) =>
						{
							if (context.parameters.TryGetValue(e, out var idx))
							{
								return
									Expression.Convert(
										Expression.ArrayIndex(context.paramArray, Expression.Constant(idx)),
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
							Expression.Constant(context.Builder.CurrentSqlParameters),
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
				if (expression == null)
					return BuildGrouping();

				if (level != 0)
				{
					var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

					if (levelExpression.NodeType == ExpressionType.MemberAccess)
					{
						var ma = (MemberExpression)levelExpression;

						if (ma.Member.Name == "Key" && ma.Member.DeclaringType == _groupingType)
						{
							var isBlockDisable = Builder.IsBlockDisable;

							Builder.IsBlockDisable = true;

							var r = ReferenceEquals(levelExpression, expression) ?
								_key.BuildExpression(null,       0, enforceServerSide) :
								_key.BuildExpression(expression, level + 1, enforceServerSide);

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
									var p   = Element.Parent;
									var ctx = new ExpressionContext(Parent, Element, l);
									var sql = Builder.ConvertToSql(ctx, l.Body, true);

									Builder.ReplaceParent(ctx, p);

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
					var expr = attribute.GetExpression(Builder.DataContext, SelectQuery, call, (e, descriptor) =>
					{
						var ex = e.Unwrap();

						if (ex is LambdaExpression l)
						{
							var p = Element.Parent;
							var ctx = new ExpressionContext(Parent, Element, l);

							var res = Builder.ConvertToSql(ctx, l.Body, true, descriptor);

							Builder.ReplaceParent(ctx, p);
							return res;
						}

						if (rootArgument == e && typeof(IGrouping<,>).IsSameOrParentOf(ex.Type))
						{
							return Element.ConvertToSql(null, 0, ConvertFlags.Field)
								.Select(_ => _.Sql)
								.FirstOrDefault();
						}

						if (typeof(IGrouping<,>).IsSameOrParentOf(Builder.GetRootObject(ex).Type))
						{
							return ConvertToSql(ex, 0, ConvertFlags.Field)
								.Select(_ => _.Sql)
								.FirstOrDefault();
						}

						return Builder.ConvertToExtensionSql(Element, ex, descriptor);
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

							args[i - 1] = Builder.ConvertToSql(ctx, l.Body, true);

							Builder.ReplaceParent(ctx, p);
						}
						else
						{
							args[i - 1] = Builder.ConvertToSql(Element, ex, true);
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
				if (expression == null)
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

				if (level == 0 && expression.NodeType == ExpressionType.MemberAccess)
					level += 1;
				if (level > 0)
				{
					switch (expression.NodeType)
					{
						case ExpressionType.Call         :
							{
								var e = (MethodCallExpression)expression;

								if (e.IsQueryable() || e.IsAggregate(Builder.MappingSchema))
								{
									return new[] { new SqlInfo(ConvertEnumerable(e)) };
								}

								break;
							}

						case ExpressionType.MemberAccess :
							{
								var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

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

											return _key.ConvertToSql(expression, level + 1, flags);
										}
									}

									return Sequence.ConvertToSql(expression, level, flags);
								}

								break;
							}
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
				if (level != 0)
				{
					var levelExpression = expression!.GetLevelExpression(Builder.MappingSchema, level);

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
					else if (levelExpression.NodeType == ExpressionType.Call)
						if (requestFlag == RequestFor.Expression)
							return IsExpressionResult.True;
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
						SelectQuery.GroupBy.Items.Add(expr);
				}

				return base.ConvertToParentIndex(index, this);
			}

			static Expression MakeSubQueryExpression(MappingSchema mappingSchema, Expression sequence,
				ParameterExpression param, Expression expr1, Expression expr2)
			{
				var filterLambda = Expression.Lambda(ExpressionBuilder.Equal(mappingSchema, expr1, expr2), param);
				return TypeHelper.MakeMethodCall(Methods.Enumerable.Where, sequence, filterLambda);
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				if (expression == null && buildInfo != null)
				{
					if (buildInfo.Parent is SelectManyBuilder.SelectManyContext sm)
					{
						var expr = MakeSubQueryExpression(
							Builder.MappingSchema,
							Sequence.Expression!,
							_key.Lambda.Parameters[0],
							ExpressionHelper.PropertyOrField(sm.Lambda.Parameters[0], "Key"),
							_key.Lambda.Body);

						return Builder.BuildSequence(new BuildInfo(buildInfo, expr));
					}

					//if (buildInfo.Parent == this)
					{
						var expr = MakeSubQueryExpression(
							Builder.MappingSchema,
							_sequenceExpr,
							_key.Lambda.Parameters[0],
							ExpressionHelper.PropertyOrField(buildInfo.Expression, "Key"),
							_key.Lambda.Body);

						var ctx = Builder.BuildSequence(new BuildInfo(buildInfo, expr));

						ctx.SelectQuery.Properties.Add(Tuple.Create("from_group_by", SelectQuery));

						return ctx;
					}

					//return this;
				}

				if (level != 0)
				{
					var levelExpression = expression!.GetLevelExpression(Builder.MappingSchema, level);

					if (levelExpression.NodeType == ExpressionType.MemberAccess)
					{
						var ma = (MemberExpression)levelExpression;

						if (ma.Member.Name == "Key" && ma.Member.DeclaringType == _groupingType)
						{
							return ReferenceEquals(levelExpression, expression) ?
								_key.GetContext(null,       0,         buildInfo!) :
								_key.GetContext(expression, level + 1, buildInfo!);
						}
					}
				}

				if (buildInfo != null && level == 0 && expression?.NodeType == ExpressionType.Parameter)
				{
					var expr = MakeSubQueryExpression(
						Builder.MappingSchema,
						_sequenceExpr,
						_key.Lambda.Parameters[0],
						ExpressionHelper.PropertyOrField(buildInfo.Expression, "Key"),
						_key.Lambda.Body);

					expr = TypeHelper.MakeMethodCall(Methods.Enumerable.Select, expr, Element.Lambda);

					var ctx = Builder.BuildSequence(new BuildInfo(buildInfo, expr));

					ctx.SelectQuery.Properties.Add(Tuple.Create("from_group_by", SelectQuery));

					return ctx;
				}

				throw new NotImplementedException();
			}
		}

		#endregion
	}
}
