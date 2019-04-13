﻿using System;
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
					throw new NotSupportedException($"Explicit construction of entity type '{body.Type}' in group by is not allowed.");
			}

			return (methodCall.Arguments[methodCall.Arguments.Count - 1].Unwrap().NodeType == ExpressionType.Lambda);
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequenceExpr    = methodCall.Arguments[0];
			var sequence        = builder.BuildSequence(new BuildInfo(buildInfo, sequenceExpr));
			var groupingType    = methodCall.Type.GetGenericArgumentsEx()[0];
			var keySelector     = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var elementSelector = (LambdaExpression)methodCall.Arguments[2].Unwrap();

			if (methodCall.Arguments[0].NodeType == ExpressionType.Call)
			{
				var call = (MethodCallExpression)methodCall.Arguments[0];

				if (call.Method.Name == "Select")
				{
					var type = ((LambdaExpression)call.Arguments[1].Unwrap()).Body.Type;

					if (type.IsGenericTypeEx() && type.GetGenericTypeDefinition() == typeof(ExpressionBuilder.GroupSubQuery<,>))
					{
						sequence = new SubQueryContext(sequence);
					}
				}
			}

			var key      = new KeyContext(buildInfo.Parent, keySelector, sequence);
			var groupSql = builder.ConvertExpressions(key, keySelector.Body.Unwrap(), ConvertFlags.Key);

			if (sequence.SelectQuery.Select.IsDistinct       ||
			    sequence.SelectQuery.GroupBy.Items.Count > 0 ||
			    groupSql.Any(_ => !(_.Sql is SqlField || _.Sql is SqlColumn)))
			{
				sequence = new SubQueryContext(sequence);
				key      = new KeyContext(buildInfo.Parent, keySelector, sequence);
				groupSql = builder.ConvertExpressions(key, keySelector.Body.Unwrap(), ConvertFlags.Key);
			}

			foreach (var sql in groupSql)
				sequence.SelectQuery.GroupBy.Expr(sql.Sql);

//			new QueryVisitor().Visit(sequence.SelectQuery.From, e =>
//			{
//				if (e.ElementType == QueryElementType.JoinedTable)
//				{
//					var jt = (SelectQuery.JoinedTable)e;
//					if (jt.JoinType == SelectQuery.JoinType.Inner)
//						jt.IsWeak = false;
//				}
//			});

			var element = new SelectContext (buildInfo.Parent, elementSelector, sequence/*, key*/);
			var groupBy = new GroupByContext(buildInfo.Parent, sequenceExpr, groupingType, sequence, key, element);

			Debug.WriteLine("BuildMethodCall GroupBy:\n" + groupBy.SelectQuery);

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

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				return base.BuildExpression(expression, level, true);
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				base.BuildQuery(query, queryParameter);
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				return base.ConvertToSql(expression, level, flags);
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				return base.ConvertToIndex(expression, level, flags);
			}

			public override int ConvertToParentIndex(int index, IBuildContext context)
			{
				return base.ConvertToParentIndex(index, context);
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				return base.IsExpression(expression, level, requestFlag);
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				return base.GetContext(expression, level, buildInfo);
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
					IQueryRunner            queryRunner,
					List<ParameterAccessor> parameters,
					Func<IDataContext,TKey,object[],IQueryable<TElement>> itemReader)
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

				private  IList<TElement>                                       _items;
				readonly IQueryRunner                                          _queryRunner;
				readonly List<ParameterAccessor>                               _parameters;
				readonly Func<IDataContext,TKey,object[],IQueryable<TElement>> _itemReader;

				public TKey Key { get; private set; }

				List<TElement> GetItems()
				{
					using (var db = _queryRunner.DataContext.Clone(true))
					{
						var ps = new object[_parameters.Count];

						for (var i = 0; i < ps.Length; i++)
							ps[i] = _parameters[i].Accessor(_queryRunner.Expression, _queryRunner.Parameters);

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
					if (Configuration.Linq.GuardGrouping)
					{
						if (context._element.Lambda.Parameters.Count == 1 &&
							context._element.Body == context._element.Lambda.Parameters[0])
						{
							var ex = new LinqToDBException(
								"You should explicitly specify selected fields for server-side GroupBy() call or add AsEnumerable() call before GroupBy() to perform client-side grouping.\n" +
								"Set Configuration.Linq.GuardGrouping = false to disable this check."
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
						MemberHelper.MethodOf(() => Queryable.Where(null, (Expression<Func<TSource,bool>>)null)),
						groupExpression,
						Expression.Lambda<Func<TSource,bool>>(
							ExpressionBuilder.Equal(context.Builder.MappingSchema, context._key.Lambda.Body, keyParam),
							new[] { context._key.Lambda.Parameters[0] }));

					expr = Expression.Call(
						null,
						MemberHelper.MethodOf(() => Queryable.Select(null, (Expression<Func<TSource,TElement>>)null)),
						expr,
						context._element.Lambda);

// ReSharper restore AssignNullToNotNullAttribute

					var lambda = Expression.Lambda<Func<IDataContext,TKey,object[],IQueryable<TElement>>>(
						Expression.Convert(expr, typeof(IQueryable<TElement>)),
						Expression.Parameter(typeof(IDataContext), "ctx"),
						keyParam,
						paramArray);

					var itemReader      = CompiledQuery.Compile(lambda);
					var keyExpr         = context._key.BuildExpression(null, 0, false);
					var dataReaderLocal = context.Builder.DataReaderLocal;

					if (!Configuration.AvoidSpecificDataProviderAPI && keyExpr.Find(e => e == dataReaderLocal) != null)
					{
						keyExpr = Expression.Block(
							new[] { context.Builder.DataReaderLocal },
							new[]
							{
								Expression.Assign(dataReaderLocal, Expression.Convert(ExpressionBuilder.DataReaderParam, context.Builder.DataContext.DataReaderType)),
								keyExpr
							});
					}

					var keyReader  = Expression.Lambda<Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],TKey>>(
						keyExpr,
						new []
						{
							ExpressionBuilder.QueryRunnerParam,
							ExpressionBuilder.DataContextParam,
							ExpressionBuilder.DataReaderParam,
							ExpressionBuilder.ExpressionParam,
							ExpressionBuilder.ParametersParam
						});

					return Expression.Call(
						null,
						MemberHelper.MethodOf(() => GetGrouping(null, null, null, null, null, null, null, null)),
						new Expression[]
						{
							ExpressionBuilder.QueryRunnerParam,
							ExpressionBuilder.DataContextParam,
							ExpressionBuilder.DataReaderParam,
							Expression.Constant(context.Builder.CurrentSqlParameters),
							ExpressionBuilder.ExpressionParam,
							ExpressionBuilder.ParametersParam,
							Expression.Constant(keyReader.Compile()),
							Expression.Constant(itemReader)
						});
				}

				static IGrouping<TKey,TElement> GetGrouping(
					IQueryRunner             runner,
					IDataContext             dataContext,
					IDataReader              dataReader,
					List<ParameterAccessor>  parameterAccessor,
					Expression               expr,
					object[]                 ps,
					Func<IQueryRunner,IDataContext,IDataReader,Expression,object[],TKey> keyReader,
					Func<IDataContext,TKey,object[],IQueryable<TElement>>                itemReader)
				{
					var key = keyReader(runner, dataContext, dataReader, expr, ps);
					return new Grouping<TKey,TElement>(key, runner, parameterAccessor, itemReader);
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

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
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
								var largs = l.Type.GetGenericArgumentsEx();

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
					Builder.MappingSchema.GetAttribute<Sql.ExpressionAttribute>(call.Method.DeclaringType, call.Method,
						c => c.Configuration);

				if (attribute != null)
				{
					var expr = attribute.GetExpression(Builder.DataContext, SelectQuery, call, e =>
					{
						var ex = e.Unwrap();

						if (ex is LambdaExpression)
						{
							var l = (LambdaExpression) ex;
							var p = _element.Parent;
							var ctx = new ExpressionContext(Parent, _element, l);

							var res = Builder.ConvertToSql(ctx, l.Body, true);

							Builder.ReplaceParent(ctx, p);
							return res;
						}
						else
						{
							return Builder.ConvertToSql(_element, ex, true);
						}
					});

					if (expr != null)
						return expr;
				}

				if (call.Arguments.Count > 1)
				{
					for (var i = 1; i < call.Arguments.Count; i++)
					{
						var ex = call.Arguments[i].Unwrap();

						if (ex is LambdaExpression)
						{
							var l   = (LambdaExpression) ex;
							var p   = _element.Parent;
							var ctx = new ExpressionContext(Parent, _element, l);

							args[i - 1] = Builder.ConvertToSql(ctx, l.Body, true);

							Builder.ReplaceParent(ctx, p);
						}
						else
						{
							args[i - 1] = Builder.ConvertToSql(_element, ex, true);
						}
					}
				}
				else
				{
					args = _element.ConvertToSql(null, 0, ConvertFlags.Field).Select(_ => _.Sql).ToArray();
				}

				if (attribute != null)
					return attribute.GetExpression(call.Method, args);

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

								if (e.IsQueryable() || e.IsAggregate(Builder.MappingSchema))
								{
									return new[] { new SqlInfo { Sql = ConvertEnumerable(e) } };
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
											_keyProperty = _groupingType.GetPropertyEx("Key");

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

			readonly Dictionary<Tuple<Expression,int,ConvertFlags>,SqlInfo[]> _expressionIndex =
				new Dictionary<Tuple<Expression,int,ConvertFlags>,SqlInfo[]>();

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				var key = Tuple.Create(expression, level, flags);

				SqlInfo[] info;

				if (!_expressionIndex.TryGetValue(key, out info))
				{
					info = ConvertToSql(expression, level, flags);

					foreach (var item in info)
					{
						item.Query = SelectQuery;
						item.Index = SelectQuery.Select.Add(item.Sql);
					}
				}

				return info;
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				if (level != 0)
				{
					var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

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

				if (!SelectQuery.GroupBy.Items.Any(_ => ReferenceEquals(_, expr) || (expr is SqlColumn && ReferenceEquals(_, ((SqlColumn)expr).Expression))))
					SelectQuery.GroupBy.Items.Add(expr);

				return base.ConvertToParentIndex(index, this);
			}

			interface IContextHelper
			{
				Expression GetContext(MappingSchema mappingSchema, Expression sequence, ParameterExpression param, Expression expr1, Expression expr2);
			}

			class ContextHelper<T> : IContextHelper
			{
				public Expression GetContext(MappingSchema mappingSchema, Expression sequence, ParameterExpression param, Expression expr1, Expression expr2)
				{
// ReSharper disable AssignNullToNotNullAttribute
					//ReflectionHelper.Expressor<object>.MethodExpressor(_ => Queryable.Where(null, (Expression<Func<T,bool>>)null)),
					var mi   = MemberHelper.MethodOf(() => Enumerable.Where(null, (Func<T,bool>)null));
// ReSharper restore AssignNullToNotNullAttribute
					var arg2 = Expression.Lambda<Func<T,bool>>(ExpressionBuilder.Equal(mappingSchema, expr1, expr2), new[] { param });

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
							Builder.MappingSchema,
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
							Builder.MappingSchema,
							_sequenceExpr,
							_key.Lambda.Parameters[0],
							Expression.PropertyOrField(buildInfo.Expression, "Key"),
							_key.Lambda.Body);

						var ctx = Builder.BuildSequence(new BuildInfo(buildInfo, expr));

						ctx.SelectQuery.Properties.Add(Tuple.Create("from_group_by", SelectQuery));

						return ctx;
					}

					//return this;
				}

				if (level != 0)
				{
					var levelExpression = expression.GetLevelExpression(Builder.MappingSchema, level);

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
