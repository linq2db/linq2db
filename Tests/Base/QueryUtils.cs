using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB;
using LinqToDB.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.SqlQuery;

namespace Tests
{
	public static class QueryUtils
	{
		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static SqlInsertClause RequireInsertClause(this SqlStatement statement)
		{
			var result = statement.InsertClause;
			if (result == null)
				throw new LinqToDBException($"Insert clause not found in {statement.GetType().Name}");
			return result;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		public static SqlUpdateClause RequireUpdateClause(this SqlStatement statement)
		{
			var result = statement.GetUpdateClause();
			if (result == null)
				throw new LinqToDBException($"Update clause not found in {statement.GetType().Name}");
			return result;
		}

		/// <summary>
		/// This is internal API and is not intended for use by Linq To DB applications.
		/// It may change or be removed without further notice.
		/// </summary>
		static SqlUpdateClause? GetUpdateClause(this SqlStatement statement)
		{
			return statement switch
			{
				SqlUpdateStatement update => update.Update,
				SqlInsertOrUpdateStatement insertOrUpdate => insertOrUpdate.Update,
				_ => null,
			};
		}

		public static SqlStatement GetStatement<T>(this IQueryable<T> query)
		{
			var eq          = (IExpressionQuery)query;
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(eq.Expression);
			var info        = Query<T>.GetQuery(eq.DataContext, ref expressions, out _);

			InitParameters(eq, info, expressions);

			return info.GetQueries().Single().Statement;
		}

		public static SqlStatement GetStatement<T, TResult>(this IQueryable<T> query, Expression<Func<IQueryable<T>, TResult>> executeBody)
		{
			var queryExpression = executeBody.GetBody(query.Expression);

			var eq          = (IExpressionQuery)query;
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(queryExpression);
			var info        = Query<T>.GetQuery(eq.DataContext, ref expressions, out _);

			InitParameters(eq, info, expressions);

			return info.GetQueries().Single().Statement;
		}

		private static void InitParameters<T>(IExpressionQuery eq, Query<T> info, IQueryExpressions expressions)
		{
			eq.DataContext.GetQueryRunner(info, eq.DataContext, 0, expressions, null, null).GetSqlText();
		}

		public static SelectQuery GetSelectQuery<T>(this IQueryable<T> query)
		{
			return query.GetStatement().SelectQuery!;
		}

		public static SelectQuery GetSelectQuery<T, TResult>(this IQueryable<T> query, Expression<Func<IQueryable<T>, TResult>> executeBody)
		{
			return query.GetStatement(executeBody).SelectQuery!;
		}

		public static IEnumerable<SelectQuery> EnumQueries<T>([NoEnumeration] this IQueryable<T> query)
		{
			var selectQuery = query.GetSelectQuery();
			var information = new QueryInformation(selectQuery);
			return information.GetQueriesParentFirst();
		}

		public static IEnumerable<SqlJoinedTable> EnumJoins(this SelectQuery query)
		{
			return query.From.Tables.SelectMany(t => t.Joins);
		}

		public static SqlSearchCondition GetWhere<T>(this IQueryable<T> query)
		{
			return GetSelectQuery(query).Where.SearchCondition;
		}

		public static SqlSearchCondition GetWhere(this SelectQuery selectQuery)
		{
			return selectQuery.Where.SearchCondition;
		}

		public static SqlTableSource GetTableSource(this SelectQuery selectQuery)
		{
			return selectQuery.From.Tables.Single();
		}

		public static SqlTableSource GetTableSource<T>(this IQueryable<T> query)
		{
			return GetSelectQuery(query).From.Tables.Single();
		}

		public static long GetCacheMissCount<T>(this IQueryable<T> _)
		{
			return Query<T>.CacheMissCount;
		}

		public static void ClearCache<T>(this IQueryable<T> _)
		{
			Query<T>.ClearCache();
		}

		public static SqlParameter[] CollectParameters(this SqlStatement statement)
		{
			var parametersHash = new HashSet<SqlParameter>();

			statement.VisitAll(parametersHash, static (parametersHash, expr) =>
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SqlParameter:
					{
						var p = (SqlParameter)expr;
						if (p.IsQueryParameter)
							parametersHash.Add(p);

						break;
					}
				}
			});

			return parametersHash.ToArray();
		}

		public static void CollectParameters(this IQueryElement root, ICollection<SqlParameter> parameters)
		{
			root.VisitAll(x =>
			{
				if (x is SqlParameter { AccessorId: not null } p)
					parameters.Add(p);
			});
		}

		sealed class QueryInformation
		{
			SelectQuery RootQuery { get; }

			Dictionary<SelectQuery, List<SelectQuery>>? _tree;

			public QueryInformation(SelectQuery rootQuery)
			{
				RootQuery = rootQuery ?? throw new ArgumentNullException(nameof(rootQuery));
			}

			private void CheckInitialized()
			{
				if (_tree == null)
				{
					_tree = new Dictionary<SelectQuery, List<SelectQuery>>();
					BuildParentHierarchy(RootQuery);
				}
			}

			public IEnumerable<SelectQuery> GetQueriesParentFirst()
			{
				return GetQueriesParentFirst(RootQuery);
			}

			IEnumerable<SelectQuery> GetQueriesParentFirst(SelectQuery root)
			{
				yield return root;

				CheckInitialized();

				if (_tree!.TryGetValue(root, out var list))
				{
					// assuming that list at this stage is immutable
					foreach (var item in list)
						foreach (var subItem in GetQueriesParentFirst(item))
						{
							yield return subItem;
						}
				}
			}

			void RegisterHierarchry(SelectQuery parent, SelectQuery child)
			{
				if (!_tree!.TryGetValue(parent, out var list))
				{
					list = new List<SelectQuery>();
					_tree.Add(parent, list);
				}

				list.Add(child);
			}

			void BuildParentHierarchy(SelectQuery selectQuery)
			{
				foreach (var table in selectQuery.From.Tables)
				{
					if (table.Source is SelectQuery s)
					{
						RegisterHierarchry(selectQuery, s);

						foreach (var setOperator in s.SetOperators)
						{
							RegisterHierarchry(selectQuery, setOperator.SelectQuery);
							BuildParentHierarchy(setOperator.SelectQuery);
						}

						BuildParentHierarchy(s);
					}

					foreach (var joinedTable in table.Joins)
					{
						if (joinedTable.Table.Source is SelectQuery joinQuery)
						{
							RegisterHierarchry(selectQuery, joinQuery);
							BuildParentHierarchy(joinQuery);
						}
					}

				}

				var items = new List<IQueryElement>
			{
				selectQuery.GroupBy,
				selectQuery.Having,
				selectQuery.Where,
				selectQuery.OrderBy,
			};

				items.AddRange(selectQuery.Select.Columns);
				if (!selectQuery.Where.IsEmpty)
					items.Add(selectQuery.Where);

				var ctx = new BuildParentHierarchyContext(this, selectQuery);
				foreach (var item in items)
				{
					item.VisitParentFirst(ctx, static (context, e) =>
					{
						if (e is SelectQuery q)
						{
							context.Info.RegisterHierarchry(context.SelectQuery, q);
							context.Info.BuildParentHierarchy(q);
							return false;
						}

						return true;
					});
				}
			}

			private sealed class BuildParentHierarchyContext
			{
				public BuildParentHierarchyContext(QueryInformation qi, SelectQuery selectQuery)
				{
					Info = qi;
					SelectQuery = selectQuery;
				}

				public readonly QueryInformation Info;
				public readonly SelectQuery      SelectQuery;
			}
		}

	}
}
