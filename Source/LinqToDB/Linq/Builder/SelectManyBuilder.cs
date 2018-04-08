using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq.Builder
{
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

			if (!sequence.SelectQuery.GroupBy.IsEmpty         ||
				sequence.SelectQuery.Select.TakeValue != null ||
				sequence.SelectQuery.Select.SkipValue != null ||
				sequence.SelectQuery.Select .IsDistinct)
			{
				sequence = new SubQueryContext(sequence);
			}

			var context        = new SelectManyContext(buildInfo.Parent, collectionSelector, sequence);
			var expr           = collectionSelector.Body.Unwrap();

			var collectionInfo = new BuildInfo(context, expr, new SelectQuery());
			var collection     = builder.BuildSequence(collectionInfo);
			var leftJoin       = collection is DefaultIfEmptyBuilder.DefaultIfEmptyContext || collectionInfo.JoinType == JoinType.Left;
			var sql            = collection.SelectQuery;

			var sequenceTables = new HashSet<ISqlTableSource>(sequence.SelectQuery.From.Tables[0].GetTables());
			var newQuery       = null != QueryVisitor.Find(sql, e => e == collectionInfo.SelectQuery);
			var crossApply     = null != QueryVisitor.Find(sql, e =>
				e.ElementType == QueryElementType.TableSource && sequenceTables.Contains((ISqlTableSource)e)  ||
				e.ElementType == QueryElementType.SqlField    && sequenceTables.Contains(((SqlField)e).Table) ||
				e.ElementType == QueryElementType.Column      && sequenceTables.Contains(((SqlColumn)e).Parent));

			if (collection is JoinBuilder.GroupJoinSubQueryContext queryContext)
			{
				var groupJoin = queryContext.GroupJoin;

				groupJoin.SelectQuery.From.Tables[0].Joins[0].JoinType = JoinType.Inner;
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
							foundJoin.JoinType = leftJoin ? JoinType.OuterApply : JoinType.CrossApply;

							collection.SelectQuery.Where.ConcatSearchCondition(foundJoin.Condition);

							((ISqlExpressionWalkable)collection.SelectQuery.Where).Walk(false, e =>
							{
								if (e is SqlColumn column)
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
					var join = sql.OuterApply();
					sequence.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
					context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);

					return new SelectContext(buildInfo.Parent, resultSelector, sequence, context);
				}
			}

			void MoveSearchConditionsToJoin(SqlFromClause.Join join)
			{
				var tableSources = new HashSet<ISqlTableSource>();

				((ISqlExpressionWalkable)sql.Where.SearchCondition).Walk(false, e =>
				{
					if (e is ISqlTableSource ts && !tableSources.Contains(ts))
						tableSources.Add(ts);
					return e;
				});

				bool ContainsTable(ISqlTableSource tbl, IQueryElement qe)
				{
					return null != QueryVisitor.Find(qe, e =>
						e == tbl ||
						e.ElementType == QueryElementType.SqlField && tbl == ((SqlField) e).Table ||
						e.ElementType == QueryElementType.Column   && tbl == ((SqlColumn)e).Parent);
				}

				var conditions = sql.Where.SearchCondition.Conditions;

				if (conditions.Count > 0)
				{
					for (var i = conditions.Count - 1; i >= 0; i--)
					{
						var condition = conditions[i];

						if (!tableSources.Any(ts => ContainsTable(ts, condition)))
						{
							join.JoinedTable.Condition.Conditions.Insert(0, condition);
							conditions.RemoveAt(i);
						}
					}
				}
			}

			var joinType = collectionInfo.JoinType;

			if (collection is TableBuilder.TableContext table)
			{
//				if (collectionInfo.IsAssociationBuilt)
//				{
//					context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);
//					return new SelectContext(buildInfo.Parent, resultSelector, sequence, context);
//				}

				if (joinType == JoinType.Auto)
				{
					var isApplyJoin =
						//Common.Configuration.Linq.PrefereApply    ||
						collection.SelectQuery.Select.HasModifier ||
						table.SqlTable.TableArguments != null && table.SqlTable.TableArguments.Length > 0;

					joinType = isApplyJoin
						? (leftJoin ? JoinType.OuterApply : JoinType.CrossApply)
						: (leftJoin ? JoinType.Left : JoinType.Inner);
				}

				var join = CreateJoin(joinType, sql);
				join.JoinedTable.CanConvertApply = false;

				if (!(joinType == JoinType.CrossApply || joinType == JoinType.OuterApply))
				{
					MoveSearchConditionsToJoin(join);
				}

				// Association.
				//
				if (collection.Parent is TableBuilder.TableContext collectionParent &&
					collectionInfo.IsAssociationBuilt)
				{
					var ts = (SqlTableSource)QueryVisitor.Find(sequence.SelectQuery.From, e =>
					{
						if (e.ElementType == QueryElementType.TableSource)
						{
							var t = (SqlTableSource)e;
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

				context.Collection = new SubQueryContext(table, sequence.SelectQuery, false);
				return new SelectContext(buildInfo.Parent, resultSelector, sequence, context);
			}
			else
			{
				if (joinType == JoinType.Auto)
					joinType = leftJoin ? JoinType.OuterApply : JoinType.CrossApply;

				var join = CreateJoin(joinType, sql);

				if (!(joinType == JoinType.CrossApply || joinType == JoinType.OuterApply))
				{
					MoveSearchConditionsToJoin(join);
				}

				if (joinType == JoinType.CrossApply)
					join.JoinedTable.CanConvertApply = false;

				sequence.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);

				context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);
				return new SelectContext(buildInfo.Parent, resultSelector, sequence, context);
			}
		}

		static SqlFromClause.Join CreateJoin(JoinType joinType, SelectQuery sql)
		{
			return new SqlFromClause.Join(joinType, sql, null, false, null);
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
				get => _collection;
				set
				{
					_collection = value;
					_collection.Parent = this;
				}
			}

			public override Expression BuildExpression(Expression expression, int level, bool enforceServerSide)
			{
				if (expression == null)
					return Collection.BuildExpression(expression, level, enforceServerSide);

				var root = expression.GetRootObject(Builder.MappingSchema);

				if (root == Lambda.Parameters[0])
					return base.BuildExpression(expression, level, enforceServerSide);

				return Collection.BuildExpression(expression, level, enforceServerSide);
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

					var root = expression.GetRootObject(Builder.MappingSchema);

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

					var root = expression.GetRootObject(Builder.MappingSchema);

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

					var root = expression.GetRootObject(Builder.MappingSchema);

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

					var root = expression.GetRootObject(Builder.MappingSchema);

					if (root != Lambda.Parameters[0])
						return Collection.IsExpression(expression, level, requestFlag);
				}

				return base.IsExpression(expression, level, requestFlag);
			}
		}
	}
}
