using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqToDB.Linq.Builder
{
	using Extensions;
	using SqlBuilder;

	class GroupByBuilder : MethodCallBuilder
	{
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
					throw new NotSupportedException(string.Format("Explicit construction of entity type '{0}' in group by is not allowed.", body.Type));
			}

			return (methodCall.Arguments[methodCall.Arguments.Count - 1].Unwrap().NodeType == ExpressionType.Lambda);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequenceExpr    = methodCall.Arguments[0];
			var sequence        = builder.BuildSequence(new BuildInfo(buildInfo, sequenceExpr));
			var groupingType    = methodCall.Type.GetGenericArguments()[0];
			var keySelector     = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var elementSelector = (LambdaExpression)methodCall.Arguments[2].Unwrap();

			if (methodCall.Arguments[0].NodeType == ExpressionType.Call)
			{
				var call = (MethodCallExpression)methodCall.Arguments[0];

				if (call.Method.Name == "Select")
				{
					var type = ((LambdaExpression)call.Arguments[1].Unwrap()).Body.Type;

					if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ExpressionBuilder.GroupSubQuery<,>))
					{
						sequence = new SubQueryContext(sequence);
					}
				}
			}

			var key      = new KeyContext(buildInfo.Parent, keySelector, sequence);
			var groupSql = builder.ConvertExpressions(key, keySelector.Body.Unwrap(), ConvertFlags.Key);

			if (sequence.SqlQuery.GroupBy.Items.Count > 0 || groupSql.Any(_ => !(_.Sql is SqlField || _.Sql is SqlQuery.Column)))
			{
				sequence = new SubQueryContext(sequence);
				key      = new KeyContext(buildInfo.Parent, keySelector, sequence);
				groupSql = builder.ConvertExpressions(key, keySelector.Body.Unwrap(), ConvertFlags.Key);
			}

			foreach (var sql in groupSql)
				sequence.SqlQuery.GroupBy.Expr(sql.Sql);

			new QueryVisitor().Visit(sequence.SqlQuery.From, e =>
			{
				if (e.ElementType == QueryElementType.JoinedTable)
				{
					var jt = (SqlQuery.JoinedTable)e;
					if (jt.JoinType == SqlQuery.JoinType.Inner)
						jt.IsWeak = false;
				}
			});

			var element = new SelectContext (buildInfo.Parent, elementSelector, sequence/*, key*/);
			var groupBy = new GroupByContext(buildInfo.Parent, sequenceExpr, groupingType, sequence, key, element);

			return groupBy;
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		#endregion

		#region KeyContext

		internal class KeyContext : SelectContext
		{
			public KeyContext(IBuildContext parent, LambdaExpression lambda, params IBuildContext[] sequences)
				: base(parent, lambda, sequences)
			{
			}
		}

		#endregion

		#region GroupByContext

		internal class GroupByContext : SequenceContextBase
		{
			public GroupByContext(
				IBuildContext parent,
				Expression   sequenceExpr,
				Type          groupingType,
				IBuildContext sequence,
				KeyContext    key,
				SelectContext element)
				: base(parent, sequence, null)
			{
				_sequenceExpr = sequenceExpr;
				_key          = key;
				_element      = element;
				_groupingType = groupingType;

				key.Parent = this;
			}

			readonly Expression    _sequenceExpr;
			readonly KeyContext    _key;
			readonly SelectContext _element;
			readonly Type          _groupingType;

			internal class Grouping<TKey,TElement> : IGrouping<TKey,TElement>
			{
				public Grouping(
					TKey                    key,
					QueryContext            queryContext,
					List<ParameterAccessor> parameters,
					Func<IDataContext,TKey,object[],IQueryable<TElement>> itemReader)
				{
					Key = key;

					_queryContext = queryContext;
					_parameters   = parameters;
					_itemReader   = itemReader;

					if (Common.Configuration.Linq.PreloadGroups)
					{
						_items = GetItems();
					}
				}

				private  IList<TElement>                                       _items;
				readonly QueryContext                                          _queryContext;
				readonly List<ParameterAccessor>                               _parameters;
				readonly Func<IDataContext,TKey,object[],IQueryable<TElement>> _itemReader;

				public TKey Key { get; private set; }

				List<TElement> GetItems()
				{
					var db = _queryContext.GetDataContext();

					try
					{
						var ps = new object[_parameters.Count];

						for (var i = 0; i < ps.Length; i++)
							ps[i] = _parameters[i].Accessor(_queryContext.Expression, _queryContext.CompiledParameters);

						return _itemReader(db.DataContextInfo.DataContext, Key, ps).ToList();
					}
					finally
					{
						_queryContext.ReleaseDataContext(db);
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
					var parameters = context.Builder.CurrentSqlParameters
						.Select((p,i) => new { p, i })
						.ToDictionary(_ => _.p.Expression, _ => _.i);
					var paramArray = Expression.Parameter(typeof(object[]), "ps");

					var groupExpression = context._sequenceExpr.Transform(e =>
					{
						int idx;

						if (parameters.TryGetValue(e, out idx))
						{
							return
								Expression.Convert(
									Expression.ArrayIndex(paramArray, Expression.Constant(idx)),
									e.Type);
						}

						return e;
					});

					var keyParam = Expression.Parameter(typeof(TKey), "key");

// ReSharper disable AssignNullToNotNullAttribute

					var expr = Expression.Call(
						null,
						ReflectionHelper.Expressor<object>.MethodExpressor(_ => Queryable.Where(null, (Expression<Func<TSource,bool>>)null)),
						groupExpression,
						Expression.Lambda<Func<TSource,bool>>(
							Expression.Equal(context._key.Lambda.Body, keyParam),
							new[] { context._key.Lambda.Parameters[0] }));

					expr = Expression.Call(
						null,
						ReflectionHelper.Expressor<object>.MethodExpressor(_ => Queryable.Select(null, (Expression<Func<TSource,TElement>>)null)),
						expr,
						context._element.Lambda);

// ReSharper restore AssignNullToNotNullAttribute

					var lambda = Expression.Lambda<Func<IDataContext,TKey,object[],IQueryable<TElement>>>(
						Expression.Convert(expr, typeof(IQueryable<TElement>)),
						Expression.Parameter(typeof(IDataContext), "ctx"),
						keyParam,
						paramArray);

					var itemReader = CompiledQuery.Compile(lambda);
					var keyExpr    = context._key.BuildExpression(null, 0);
					var keyReader  = Expression.Lambda<Func<QueryContext,IDataContext,IDataReader,Expression,object[],TKey>>(
						keyExpr,
						new []
						{
							ExpressionBuilder.ContextParam,
							ExpressionBuilder.DataContextParam,
							ExpressionBuilder.DataReaderParam,
							ExpressionBuilder.ExpressionParam,
							ExpressionBuilder.ParametersParam,
						});

					return Expression.Call(
						null,
						ReflectionHelper.Expressor<object>.MethodExpressor(_ => GetGrouping(null, null, null, null, null, null, null, null)),
						new Expression[]
						{
							ExpressionBuilder.ContextParam,
							ExpressionBuilder.DataContextParam,
							ExpressionBuilder.DataReaderParam,
							Expression.Constant(context.Builder.CurrentSqlParameters),
							ExpressionBuilder.ExpressionParam,
							ExpressionBuilder.ParametersParam,
							Expression.Constant(keyReader.Compile()),
							Expression.Constant(itemReader),
						});
				}

				static IGrouping<TKey,TElement> GetGrouping(
					QueryContext             context,
					IDataContext             dataContext,
					IDataReader              dataReader,
					List<ParameterAccessor>  parameterAccessor,
					Expression               expr,
					object[]                 ps,
					Func<QueryContext,IDataContext,IDataReader,Expression,object[],TKey> keyReader,
					Func<IDataContext,TKey,object[],IQueryable<TElement>>                itemReader)
				{
					var key = keyReader(context, dataContext, dataReader, expr, ps);
					return new Grouping<TKey,TElement>(key, context, parameterAccessor, itemReader);
				}
			}

			Expression BuildGrouping()
			{
				var gtype = typeof(GroupByHelper<,,>).MakeGenericType(
					_key.Lambda.Body.Type,
					_element.Lambda.Body.Type,
					_key.Lambda.Parameters[0].Type);

				var isBlockDisable = Builder.IsBlockDisable;

				Builder.IsBlockDisable = true;

				var helper = (IGroupByHelper)Activator.CreateInstance(gtype);
				var expr   = helper.GetGrouping(this);

				Builder.IsBlockDisable = isBlockDisable;

				return expr;
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				if (expression == null)
					return BuildGrouping();

				if (level != 0)
				{
					var levelExpression = expression.GetLevelExpression(level);

					if (levelExpression.NodeType == ExpressionType.MemberAccess)
					{
						var ma = (MemberExpression)levelExpression;

						if (ma.Member.Name == "Key" && ma.Member.DeclaringType == _groupingType)
						{
							return ReferenceEquals(levelExpression, expression) ?
								_key.BuildExpression(null,       0) :
								_key.BuildExpression(expression, level + 1);
						}
					}
				}

				throw new NotImplementedException();
			}

			ISqlExpression ConvertEnumerable(MethodCallExpression call)
			{
				if (AggregationBuilder.MethodNames.Contains(call.Method.Name))
				{
					if (call.Arguments[0].NodeType == ExpressionType.Call)
					{
						var arg = (MethodCallExpression)call.Arguments[0];

						if (arg.Method.Name == "Select")
						{
							if (arg.Arguments[0].NodeType != ExpressionType.Call)
							{
								var l     = (LambdaExpression)arg.Arguments[1].Unwrap();
								var largs = l.Type.GetGenericArguments();

								if (largs.Length == 2)
								{
									var p   = _element.Parent;
									var ctx = new ExpressionContext(Parent, _element, l);
									var sql = Builder.ConvertToSql(ctx, l.Body, true);

									Builder.ReplaceParent(ctx, p);

									return new SqlFunction(call.Type, call.Method.Name, sql);
								}
							}
						}
					}
				}

				if (call.Arguments[0].NodeType == ExpressionType.Call)
				{
					var ctx = Builder.GetSubQuery(this, call);

					if (Builder.SqlProvider.IsSubQueryColumnSupported)
						return ctx.SqlQuery;

					var join = ctx.SqlQuery.CrossApply();

					SqlQuery.From.Tables[0].Joins.Add(join.JoinedTable);

					return ctx.SqlQuery.Select.Columns[0];
				}

				var args = new ISqlExpression[call.Arguments.Count - 1];

				if (CountBuilder.MethodNames.Contains(call.Method.Name))
				{
					if (args.Length > 0)
						throw new InvalidOperationException();

					return SqlFunction.CreateCount(call.Type, SqlQuery);
				}

				if (call.Arguments.Count > 1)
				{
					for (var i = 1; i < call.Arguments.Count; i++)
					{
						var ex = call.Arguments[i].Unwrap();

						if (ex is LambdaExpression)
						{
							var l   = (LambdaExpression)ex;
							var p   = _element.Parent;
							var ctx = new ExpressionContext(Parent, _element, l);

							args[i - 1] = Builder.ConvertToSql(ctx, l.Body, true);

							Builder.ReplaceParent(ctx, p);
						}
						else
						{
							throw new NotImplementedException();
						}
					}
				}
				else
				{
					args = _element.ConvertToSql(null, 0, ConvertFlags.Field).Select(_ => _.Sql).ToArray();
				}

				return new SqlFunction(call.Type, call.Method.Name, args);
			}

			PropertyInfo _keyProperty;

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				if (expression == null)
					return _key.ConvertToSql(null, 0, flags);

				if (level > 0)
				{
					switch (expression.NodeType)
					{
						case ExpressionType.Call         :
							{
								var e = (MethodCallExpression)expression;

								if (e.Method.DeclaringType == typeof(Enumerable))
								{
									return new[] { new SqlInfo { Sql = ConvertEnumerable(e) } };
								}

								break;
							}

						case ExpressionType.MemberAccess :
							{
								var levelExpression = expression.GetLevelExpression(level);

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

				throw new NotImplementedException();
			}

			readonly Dictionary<Tuple<Expression,int,ConvertFlags>,SqlInfo[]> _expressionIndex = new Dictionary<Tuple<Expression,int,ConvertFlags>,SqlInfo[]>();

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				var key = Tuple.Create(expression, level, flags);

				SqlInfo[] info;

				if (!_expressionIndex.TryGetValue(key, out info))
				{
					info = ConvertToSql(expression, level, flags);

					foreach (var item in info)
					{
						item.Query = SqlQuery;
						item.Index = SqlQuery.Select.Add(item.Sql);
					}
				}

				return info;
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				if (level != 0)
				{
					var levelExpression = expression.GetLevelExpression(level);

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
				}

				return IsExpressionResult.False;
			}

			public override int ConvertToParentIndex(int index, IBuildContext _context)
			{
				var expr = SqlQuery.Select.Columns[index].Expression;

				if (!SqlQuery.GroupBy.Items.Exists(_ => ReferenceEquals(_, expr) || (expr is SqlQuery.Column && ReferenceEquals(_, ((SqlQuery.Column)expr).Expression))))
					SqlQuery.GroupBy.Items.Add(expr);

				return base.ConvertToParentIndex(index, this);
			}

			interface IContextHelper
			{
				Expression GetContext(Expression sequence, ParameterExpression param, Expression expr1, Expression expr2);
			}

			class ContextHelper<T> : IContextHelper
			{
				public Expression GetContext(Expression sequence, ParameterExpression param, Expression expr1, Expression expr2)
				{
// ReSharper disable AssignNullToNotNullAttribute
					//ReflectionHelper.Expressor<object>.MethodExpressor(_ => Queryable.Where(null, (Expression<Func<T,bool>>)null)),
					var mi   = ReflectionHelper.Expressor<object>.MethodExpressor(_ => Enumerable.Where(null, (Func<T,bool>)null));
// ReSharper restore AssignNullToNotNullAttribute
					var arg2 = Expression.Lambda<Func<T,bool>>(Expression.Equal(expr1, expr2), new[] { param });

					return Expression.Call(null, mi, sequence, arg2);
				}
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				if (expression == null && buildInfo != null)
				{
					if (buildInfo.Parent is SelectManyBuilder.SelectManyContext)
					{
						var sm     = (SelectManyBuilder.SelectManyContext)buildInfo.Parent;
						var ctype  = typeof(ContextHelper<>).MakeGenericType(_key.Lambda.Parameters[0].Type);
						var helper = (IContextHelper)Activator.CreateInstance(ctype);
						var expr   = helper.GetContext(
							Sequence.Expression,
							_key.Lambda.Parameters[0],
							Expression.PropertyOrField(sm.Lambda.Parameters[0], "Key"),
							_key.Lambda.Body);

						return Builder.BuildSequence(new BuildInfo(buildInfo, expr));
					}

					//if (buildInfo.Parent == this)
					{
						var ctype  = typeof(ContextHelper<>).MakeGenericType(_key.Lambda.Parameters[0].Type);
						var helper = (IContextHelper)Activator.CreateInstance(ctype);
						var expr   = helper.GetContext(
							_sequenceExpr,
							_key.Lambda.Parameters[0],
							Expression.PropertyOrField(buildInfo.Expression, "Key"),
							_key.Lambda.Body);

						var ctx = Builder.BuildSequence(new BuildInfo(buildInfo, expr));

						ctx.SqlQuery.Properties.Add(Tuple.Create("from_group_by", this.SqlQuery));

						return ctx;
					}

					//return this;
				}

				if (level != 0)
				{
					var levelExpression = expression.GetLevelExpression(level);

					if (levelExpression.NodeType == ExpressionType.MemberAccess)
					{
						var ma = (MemberExpression)levelExpression;

						if (ma.Member.Name == "Key" && ma.Member.DeclaringType == _groupingType)
						{
							return ReferenceEquals(levelExpression, expression) ?
								_key.GetContext(null,       0,         buildInfo) :
								_key.GetContext(expression, level + 1, buildInfo);
						}
					}
				}

				throw new NotImplementedException();
			}
		}

		#endregion
	}
}
