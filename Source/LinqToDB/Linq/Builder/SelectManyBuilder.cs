using System;
using System.Collections.Generic;
using System.Diagnostics;
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

			var expr = collectionSelector.Body.Unwrap();

			DefaultIfEmptyBuilder.DefaultIfEmptyContext? defaultIfEmpty = null;
			if (expr is MethodCallExpression mc && AllJoinsBuilder.IsMatchingMethod(mc, true))
			{
				defaultIfEmpty = new DefaultIfEmptyBuilder.DefaultIfEmptyContext(buildInfo.Parent, sequence, null);
				sequence       = new SubQueryContext(defaultIfEmpty);

				defaultIfEmpty.Disabled = true;
			}
			else if (sequence.SelectQuery.HasSetOperators || !sequence.SelectQuery.IsSimple || sequence.GetType() == typeof(SelectContext))
				// TODO: we should create subquery unconditionally and let optimizer remove it later if it is not needed,
				// but right now it breaks at least association builder so it is not a small change
				sequence = new SubQueryContext(sequence);

			var context        = new SelectManyContext(buildInfo.Parent, collectionSelector, sequence);
			context.SetAlias(collectionSelector.Parameters[0].Name);

			expr = SequenceHelper.PrepareBody(collectionSelector, sequence).Unwrap();

			var collectionInfo = new BuildInfo(context, expr, new SelectQuery());
			var collection     = builder.BuildSequence(collectionInfo);
			if (resultSelector.Parameters.Count > 1)
				collection.SetAlias(resultSelector.Parameters[1].Name);

			if (defaultIfEmpty != null && (collectionInfo.JoinType == JoinType.Right || collectionInfo.JoinType == JoinType.Full))
				defaultIfEmpty.Disabled = false;

			var leftJoin       = collection is DefaultIfEmptyBuilder.DefaultIfEmptyContext || collectionInfo.JoinType == JoinType.Left;
			var sql            = collection.SelectQuery;

			var newQuery       = QueryHelper.ContainsElement(sql, collectionInfo.SelectQuery);
			var sequenceTables = new HashSet<ISqlTableSource>(QueryHelper.EnumerateAccessibleSources(sequence.SelectQuery));
			var crossApply     = QueryHelper.IsDependsOn(sql, sequenceTables);

			if (collection is JoinBuilder.GroupJoinSubQueryContext queryContext)
			{
				var groupJoin = queryContext.GroupJoin!;

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

							((ISqlExpressionWalkable) collection.SelectQuery.Where).Walk(new WalkOptions(), e =>
							{
								if (e is SqlColumn column)
								{
									if (column.Parent == collection.SelectQuery)
										return column.Expression!;
								}
								return e;
							});

							foundJoin.Condition.Conditions.Clear();
						}
					}
				}

				context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);
				return new SelectContext(buildInfo.Parent, resultSelector, sequence, context.Collection);
			}

			if (!crossApply)
			{
				if (!leftJoin)
				{
					context.Collection = new SubQueryContext(collection, sequence.SelectQuery, true);
					return new SelectContext(buildInfo.Parent, resultSelector, sequence, context.Collection);
				}
				else
				{
					var join = sql.OuterApply();
					sequence.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
					context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);

					return new SelectContext(buildInfo.Parent, resultSelector, sequence, context.Collection);
				}
			}

			var joinType = collectionInfo.JoinType;

			if (collection is TableBuilder.TableContext table)
			{
//				if (collectionInfo.IsAssociationBuilt)
//				{
//					context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);
//					return new SelectContext(buildInfo.Parent, resultSelector, sequence, context.Collection);
//				}

				if (joinType == JoinType.Auto)
				{
					var isApplyJoin =
						//Common.Configuration.Linq.PrefereApply    ||
						collection.SelectQuery.Select.HasModifier ||
						table.SqlTable.TableArguments != null && table.SqlTable.TableArguments.Length > 0 ||
						table.SqlTable is SqlRawSqlTable rawTable && rawTable.Parameters.Length > 0;

					joinType = isApplyJoin
						? (leftJoin ? JoinType.OuterApply : JoinType.CrossApply)
						: (leftJoin ? JoinType.Left : JoinType.Inner);
				}

				var join = CreateJoin(joinType, sql);
				join.JoinedTable.CanConvertApply = false;

				if (!(joinType == JoinType.CrossApply || joinType == JoinType.OuterApply))
				{
					QueryHelper.MoveSearchConditionsToJoin(sql, join.JoinedTable, null);
				}

				// Association.
				//
				if (collection.Parent is TableBuilder.TableContext collectionParent &&
					collectionInfo.IsAssociationBuilt)
				{
					var ts = (SqlTableSource)new QueryVisitor().Find(sequence.SelectQuery.From, e =>
					{
						if (e.ElementType == QueryElementType.TableSource)
						{
							var t = (SqlTableSource)e;
							return t.Source == collectionParent.SqlTable;
						}

						return false;
					})!;

					ts.Joins.Add(join.JoinedTable);
				}
				else
				{
					sequence.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
				}

				context.Collection = new SubQueryContext(table, sequence.SelectQuery, false);
				return new SelectContext(buildInfo.Parent, resultSelector, sequence, context.Collection);
			}
			else
			{
				if (joinType == JoinType.Auto)
					joinType = leftJoin ? JoinType.OuterApply : JoinType.CrossApply;

				var join = CreateJoin(joinType, sql);

				if (!(joinType == JoinType.CrossApply || joinType == JoinType.OuterApply))
				{
					QueryHelper.MoveSearchConditionsToJoin(sql, join.JoinedTable, null);
				}

				sequence.SelectQuery.From.Tables[0].Joins.Add(join.JoinedTable);
				
				context.Collection = new SubQueryContext(collection, sequence.SelectQuery, false);
				return new SelectContext(buildInfo.Parent, resultSelector, sequence, context.Collection);
			}
		}

		static SqlFromClause.Join CreateJoin(JoinType joinType, SelectQuery sql)
		{
			return new SqlFromClause.Join(joinType, sql, null, false, null);
		}

		protected override SequenceConvertInfo? Convert(
			ExpressionBuilder builder, MethodCallExpression methodCall, BuildInfo buildInfo, ParameterExpression? param)
		{
			return null;
		}

		public class SelectManyContext : SelectContext
		{
			public SelectManyContext(IBuildContext? parent, LambdaExpression lambda, IBuildContext sequence)
				: base(parent, lambda, sequence)
			{
			}

			private IBuildContext? _collection;
			public  IBuildContext?  Collection
			{
				get => _collection;
				set
				{
					_collection        = value!;
					_collection.Parent = this;
				}
			}

			public override Expression BuildExpression(Expression? expression, int level, bool enforceServerSide)
			{
				if (expression == null)
					return Collection!.BuildExpression(expression, level, enforceServerSide);

				var root = Builder.GetRootObject(expression);

				//if (root == Lambda.Parameters[0])
				if (SequenceHelper.IsSameContext(root, this))
					return base.BuildExpression(expression, level, enforceServerSide);

				return Collection!.BuildExpression(expression, level, enforceServerSide);
			}

			public override void BuildQuery<T>(Query<T> query, ParameterExpression queryParameter)
			{
				if (Collection == null)
					base.BuildQuery(query, queryParameter);

				throw new NotImplementedException();
			}

			public override SqlInfo[] ConvertToIndex(Expression? expression, int level, ConvertFlags flags)
			{
				if (Collection != null)
				{
					if (expression == null)
						return Collection.ConvertToIndex(expression, level, flags);

					var root = Builder.GetRootObject(expression);

					if (root != Lambda.Parameters[0])
						return Collection.ConvertToIndex(expression, level, flags);
				}

				return base.ConvertToIndex(expression, level, flags);
			}

			public override SqlInfo[] ConvertToSql(Expression? expression, int level, ConvertFlags flags)
			{
				if (Collection != null)
				{
					expression = SequenceHelper.CorrectExpression(expression, this, Collection);

					return Collection.ConvertToSql(expression, level, flags);

					/*
					if (expression == null)
						return Collection.ConvertToSql(expression, level, flags);

					var root = Builder.GetRootObject(expression);

					if (root != Lambda.Parameters[0])
						return Collection.ConvertToSql(expression, level, flags);
				*/
				}

				return base.ConvertToSql(expression, level, flags);
			}

			public override IBuildContext? GetContext(Expression? expression, int level, BuildInfo buildInfo)
			{
				if (Collection != null)
				{
					if (expression == null)
						return Collection.GetContext(expression, level, buildInfo);

					var root = Builder.GetRootObject(expression);

					if (root != Lambda.Parameters[0])
						return Collection.GetContext(expression, level, buildInfo);
				}

				return base.GetContext(expression, level, buildInfo);
			}

			public override IsExpressionResult IsExpression(Expression? expression, int level, RequestFor requestFlag)
			{
				if (Collection != null)
				{
					if (expression == null)
						return Collection.IsExpression(expression, level, requestFlag);

					var root = Builder.GetRootObject(expression);

					if (root != Lambda.Parameters[0])
						return Collection.IsExpression(expression, level, requestFlag);
				}

				return base.IsExpression(expression, level, requestFlag);
			}

			public override void CompleteColumns()
			{
				base.CompleteColumns();

				Collection?.CompleteColumns();
			}
		}
	}
}
