using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
	using System.Linq;

	using LinqToDB.Expressions;
	using SqlQuery;

	class SelectManyBuilder : MethodCallBuilder
	{
		protected override bool CanBuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			return
				methodCall.IsQueryable("SelectMany") &&
				methodCall.Arguments.Count == 3      &&
				((LambdaExpression)methodCall.Arguments[1].Unwrap()).Parameters.Count == 1;
		}

		protected override IBuildContext BuildMethodCall(ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo)
		{
			var sequence           = builder.BuildSequence(new BuildInfo(buildInfo, methodCall.Arguments[0]));
			var collectionSelector = (LambdaExpression)methodCall.Arguments[1].Unwrap();
			var resultSelector     = (LambdaExpression)methodCall.Arguments[2].Unwrap();

			if (!sequence.SelectQuery.GroupBy.IsEmpty)
			{
				sequence = new SubQueryContext(sequence);
			}

			var context        = new SelectManyContext(buildInfo.Parent, collectionSelector, sequence);
			var expr           = collectionSelector.Body.Unwrap();

			var collectionInfo = new BuildInfo(context, expr, new SelectQuery());
			var collection     = builder.BuildSequence(collectionInfo);
			var leftJoin       = collection is DefaultIfEmptyBuilder.DefaultIfEmptyContext;
			var sql            = collection.SelectQuery;

			var sequenceTables = new HashSet<ISqlTableSource>(sequence.SelectQuery.From.Tables[0].GetTables());
			var newQuery       = null != new QueryVisitor().Find(sql, e => e == collectionInfo.SelectQuery);
			var crossApply     = null != new QueryVisitor().Find(sql, e =>
				e.ElementType == QueryElementType.TableSource && sequenceTables.Contains((ISqlTableSource)e)  ||
				e.ElementType == QueryElementType.SqlField    && sequenceTables.Contains(((SqlField)e).Table) ||
				e.ElementType == QueryElementType.Column      && sequenceTables.Contains(((SelectQuery.Column)e).Parent));

			if (collection is JoinBuilder.GroupJoinSubQueryContext)
			{
				var groupJoin = ((JoinBuilder.GroupJoinSubQueryContext)collection).GroupJoin;

				groupJoin.SelectQuery.From.Tables[0].Joins[0].JoinType = SelectQuery.JoinType.Inner;
				groupJoin.SelectQuery.From.Tables[0].Joins[0].IsWeak   = false;
			}

			if (!newQuery)
			{
				if (collection.SelectQuery.Select.HasModifier)
				{
					if (crossApply)
					{
						var foundJoin = context.SelectQuery.FindJoin(j => j.Table.Source == collection.SelectQuery);
						if (foundJoin != null)
						{
							foundJoin.JoinType = leftJoin ? SelectQuery.JoinType.OuterApply : SelectQuery.JoinType.CrossApply;

							collection.SelectQuery.Where.ConcatSearchCondition(foundJoin.Condition);

							((ISqlExpressionWalkable) collection.SelectQuery.Where).Walk(false, e =>
							{
								var column = e as SelectQuery.Column;
								if (column != null)
								{
									if (column.Parent == collection.SelectQuery)
										return column.UnderlyingColumn;
								}
								return e;
							});

							foundJoin.Condition.Conditions.Clear();
						}
					}
				}

				context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);
				return new SelectContext(buildInfo.Parent, resultSelector, sequence, context);
			}

			if (!crossApply)
			{
				if (!leftJoin)
				{
					context.Collection = new SubQueryContext(collection, sequence.SelectQuery, true);
					return new SelectContext(buildInfo.Parent, resultSelector, sequence, context);
				}
				else
				{
					var join = SelectQuery.OuterApply(sql);
					sequence.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
					context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);

					return new SelectContext(buildInfo.Parent, resultSelector, sequence, context);
				}
			}

			if (collection is TableBuilder.TableContext)
			{
//				if (collectionInfo.IsAssociationBuilt)
//				{
//					context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);
//					return new SelectContext(buildInfo.Parent, resultSelector, sequence, context);
//				}

				var table = (TableBuilder.TableContext)collection;

				var isApplyJoin = collection.SelectQuery.Select.HasModifier ||
				                  table.SqlTable.TableArguments != null && table.SqlTable.TableArguments.Length > 0;

				var join = isApplyJoin
					? (leftJoin ? SelectQuery.OuterApply(sql) : SelectQuery.CrossApply(sql))
					: (leftJoin ? SelectQuery.LeftJoin  (sql) : SelectQuery.InnerJoin(sql));

				join.JoinedTable.CanConvertApply = false;

				if (!isApplyJoin)
				{
					join.JoinedTable.Condition.Conditions.AddRange(sql.Where.SearchCondition.Conditions);
					sql.Where.SearchCondition.Conditions.Clear();
				}

				var collectionParent = collection.Parent as TableBuilder.TableContext;

				// Association.
				//
				if (collectionParent != null && collectionInfo.IsAssociationBuilt)
				{
					var ts = (SelectQuery.TableSource)new QueryVisitor().Find(sequence.SelectQuery.From, e =>
					{
						if (e.ElementType == QueryElementType.TableSource)
						{
							var t = (SelectQuery.TableSource)e;
							return t.Source == collectionParent.SqlTable;
						}

						return false;
					});

					ts.Joins.Add(join.JoinedTable);
				}
				else
				{
					//if (collectionInfo.IsAssociationBuilt)
					//{
					//	collectionInfo.AssosiationContext.ParentAssociationJoin.IsWeak = false;
					//}
					//else
					{
						sequence.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
					}
				}

				context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);
				return new SelectContext(buildInfo.Parent, resultSelector, sequence, context);
			}
			else
			{
				var join = leftJoin ? SelectQuery.OuterApply(sql) : SelectQuery.CrossApply(sql);
				sequence.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);

				context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);
				return new SelectContext(buildInfo.Parent, resultSelector, sequence, context);
			}
		}

		protected override SequenceConvertInfo Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression param)
		{
			return null;
		}

		public class SelectManyContext : SelectContext
		{
			public SelectManyContext(IBuildContext parent, LambdaExpression lambda, IBuildContext sequence)
				: base(parent, lambda, sequence)
			{
			}

			private IBuildContext _collection;
			public  IBuildContext  Collection
			{
				get { return _collection; }
				set
				{
					_collection = value;
					_collection.Parent = this;
				}
			}

			public override Expression BuildExpression(Expression expression, int level)
			{
				if (expression == null)
					return Collection.BuildExpression(expression, level);

				var root = expression.GetRootObject();

				if (root == Lambda.Parameters[0])
					return base.BuildExpression(expression, level);

				return Collection.BuildExpression(expression, level);
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				if (Collection == null)
					base.BuildQuery(query, queryParameter);

				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression expression, int level, ConvertFlags flags)
			{
				if (Collection != null)
				{
					if (expression == null)
						return Collection.ConvertToIndex(expression, level, flags);

					var root = expression.GetRootObject();

					if (root != Lambda.Parameters[0])
						return Collection.ConvertToIndex(expression, level, flags);
				}

				return base.ConvertToIndex(expression, level, flags);
			}

			public override SqlInfo[] ConvertToSql(Expression expression, int level, ConvertFlags flags)
			{
				if (Collection != null)
				{
					if (expression == null)
						return Collection.ConvertToSql(expression, level, flags);

					var root = expression.GetRootObject();

					if (root != Lambda.Parameters[0])
						return Collection.ConvertToSql(expression, level, flags);
				}

				return base.ConvertToSql(expression, level, flags);
			}

			public override IBuildContext GetContext(Expression expression, int level, BuildInfo buildInfo)
			{
				if (Collection != null)
				{
					if (expression == null)
						return Collection.GetContext(expression, level, buildInfo);

					var root = expression.GetRootObject();

					if (root != Lambda.Parameters[0])
						return Collection.GetContext(expression, level, buildInfo);
				}

				return base.GetContext(expression, level, buildInfo);
			}

			public override IsExpressionResult IsExpression(Expression expression, int level, RequestFor requestFlag)
			{
				if (Collection != null)
				{
					if (expression == null)
						return Collection.IsExpression(expression, level, requestFlag);

					var root = expression.GetRootObject();

					if (root != Lambda.Parameters[0])
						return Collection.IsExpression(expression, level, requestFlag);
				}

				return base.IsExpression(expression, level, requestFlag);
			}
		}
	}
}
