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

				var join = table.SqlTable.TableArguments != null && table.SqlTable.TableArguments.Length > 0 ?
					(leftJoin ? SelectQuery.OuterApply(sql) : SelectQuery.CrossApply(sql)) :
					(leftJoin ? SelectQuery.LeftJoin  (sql) : SelectQuery.InnerJoin (sql));

				join.JoinedTable.Condition.Conditions.AddRange(sql.Where.SearchCondition.Conditions);
				join.JoinedTable.CanConvertApply = false;

				sql.Where.SearchCondition.Conditions.Clear();

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
				var join      = leftJoin ? SelectQuery.OuterApply(sql) : SelectQuery.CrossApply(sql);
				var subquery  = false;
				var tables    = sequence.SelectQuery.From.Tables;
				var baseTable = tables[0];

				// if new join has dependency to many From tables we have to convert them to INNER JOINS
				if (tables.Count > 1)
				{
					if (builder.DataContextInfo.SqlProviderFlags.IsCrossJoinSupported || builder.DataContextInfo.SqlProviderFlags.IsInnerJoinAsCrossSupported)
					{
						for (var i = tables.Count - 1; i > 0; i--)
						{
							baseTable.Joins.Add(new SelectQuery.JoinedTable(SelectQuery.JoinType.Inner, tables[i], false));
							tables.RemoveAt(i);
						}
						baseTable.Joins.Add(join.JoinedTable);
					}
					else
					{
						var outterQuery = new SelectQuery();

						outterQuery.Select.From.Tables.Add(new SelectQuery.TableSource(sequence.SelectQuery, null));
						outterQuery.Select.From.Tables[0].Joins.Add(join.JoinedTable);

						sequence.SelectQuery = outterQuery;
						subquery = true;

					}
				}
				else
					baseTable.Joins.Add(join.JoinedTable);

				context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);
				
				return new SelectContext(buildInfo.Parent, resultSelector, sequence,
					subquery? context.Collection:context);
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
