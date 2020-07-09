using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LinqToDB.SqlQuery
{
	using SqlProvider;
	using Tools;
	using Common;
	using Mapping;

	public static class QueryHelper
	{

		public static bool ContainsElement(IQueryElement testedRoot, IQueryElement element)
		{
			return null != new QueryVisitor().Find(testedRoot, e => e == element);
		}

		public static bool IsDependsOn(IQueryElement testedRoot, HashSet<ISqlTableSource> onSources, HashSet<IQueryElement>? elementsToIgnore = null)
		{
			var dependencyFound = false;

			new QueryVisitor().VisitParentFirst(testedRoot, e =>
			{
				if (dependencyFound)
					return false;

				if (elementsToIgnore != null && elementsToIgnore.Contains(e))
					return false;

				if (e is ISqlTableSource source && onSources.Contains(source))
				{
					dependencyFound = true;
					return false;
				}

				switch (e.ElementType)
				{
					case QueryElementType.Column :
						{
							var c = (SqlColumn) e;
							if (onSources.Contains(c.Parent!))
								dependencyFound = true;
							break;
						}
					case QueryElementType.SqlField :
						{
							var f = (SqlField) e;
							if (onSources.Contains(f.Table!))
								dependencyFound = true;
							break;
						}
				}

				return !dependencyFound;
			});

			return dependencyFound;
		}

		/// <summary>
		/// Returns <see cref="IValueConverter"/> for <paramref name="expr"/>.
		/// </summary>
		/// <param name="expr">Tested SQL Expression.</param>
		/// <returns>Associated converter or <c>null</c>.</returns>
		public static IValueConverter? GetValueConverter(ISqlExpression? expr)
		{
			return GetColumnDescriptor(expr)?.ValueConverter;
		}
		
		/// <summary>
		/// Returns <see cref="ColumnDescriptor"/> for <paramref name="expr"/>.
		/// </summary>
		/// <param name="expr">Tested SQL Expression.</param>
		/// <returns>Associated column descriptor or <c>null</c>.</returns>
		public static ColumnDescriptor? GetColumnDescriptor(ISqlExpression? expr)
		{
			if (expr == null)
				return null;
			
			switch (expr.ElementType)
			{
				case QueryElementType.Column:
					{
						return GetColumnDescriptor(((SqlColumn)expr).Expression);
					}
				case QueryElementType.SqlField:
					{
						return ((SqlField)expr).ColumnDescriptor;
					}
				case QueryElementType.SqlExpression:
					{
						var sqlExpr = (SqlExpression)expr;
						if (sqlExpr.Parameters.Length == 1 && sqlExpr.Expr == "{0}")
							return GetColumnDescriptor(sqlExpr.Parameters[0]);
						break;
					}
				case QueryElementType.SqlQuery:
					{
						var query = (SelectQuery)expr;
						if (query.Select.Columns.Count == 1)
							return GetColumnDescriptor(query.Select.Columns[0]);
						break;
					}
			}
			return null;
		}
		
		public static void CollectDependencies(IQueryElement root, IEnumerable<ISqlTableSource> sources, HashSet<ISqlExpression> found, IEnumerable<IQueryElement>? ignore = null)
		{
			var hash       = new HashSet<ISqlTableSource>(sources);
			var hashIgnore = new HashSet<IQueryElement  >(ignore ?? Enumerable.Empty<IQueryElement>());

			new QueryVisitor().VisitParentFirst(root, e =>
			{
				if (e is ISqlTableSource source && hash.Contains(source) || hashIgnore.Contains(e))
					return false;

				switch (e.ElementType)
				{
					case QueryElementType.Column :
						{
							var c = (SqlColumn) e;
							if (hash.Contains(c.Parent!))
								found.Add(c);
							break;
						}
					case QueryElementType.SqlField :
						{
							var f = (SqlField) e;
							if (hash.Contains(f.Table!))
								found.Add(f);
							break;
						}
				}
				return true;
			});
		}

		public static bool IsTransitiveExpression(SqlExpression sqlExpression)
		{
			if (sqlExpression.Parameters.Length == 1 && sqlExpression.Expr.Trim() == "{0}")
			{
				var argExpression = sqlExpression.Parameters[0] as SqlExpression;
				if (argExpression != null)
					return IsTransitiveExpression(argExpression);
				return true;
			}

			return false;
		}

		public static ISqlExpression GetUnderlyingExpressionValue(SqlExpression sqlExpression)
		{
			if (!IsTransitiveExpression(sqlExpression))
				return sqlExpression;

			if (sqlExpression.Parameters[0] is SqlExpression subExpr)
				return GetUnderlyingExpressionValue(subExpr);

			return sqlExpression.Parameters[0];
		}
	
		/// <summary>
		/// Returns true if it is anything except Field or Column.
		/// </summary>
		/// <param name="expr">Tested expression</param>
		/// <returns>true if tested expression is not a Field or Column</returns>
		public static bool IsExpression(ISqlExpression expr)
		{
			if (expr.ElementType == QueryElementType.SqlExpression)
			{
				var sqlExpression = (SqlExpression) expr;
				expr = GetUnderlyingExpressionValue(sqlExpression);
			}
			return expr.ElementType.NotIn(QueryElementType.Column, QueryElementType.SqlField);
		}
			
		/// <summary>
		/// Returns <c>true</c> if tested expression can be constant or immutable value based on parameters.
		/// </summary>
		/// <param name="expr">Tested expression.</param>
		/// <returns></returns>
		public static bool IsImmutable(ISqlExpression expr)
		{
			var result = null == new QueryVisitor()
				.Find(expr, e =>
				{
					// Constants and Parameters do not changes during query execution 
					if (e.ElementType.In(QueryElementType.SqlValue, QueryElementType.SqlParameter))
						return false;

					if (e.ElementType == QueryElementType.Column)
					{
						var sqlColumn = (SqlColumn) e;
						
						// We can not guarantee order here
						if (sqlColumn.Parent != null && sqlColumn.Parent.SetOperators.Count > 0)
							return true;
						
						// column can be generated from subquery which can reference to Immutable expression
						return !IsImmutable(sqlColumn.Expression);
					}

					if (e.ElementType == QueryElementType.SqlExpression)
					{
						var sqlExpr = (SqlExpression) e;
						return !sqlExpr.IsPure || sqlExpr.IsAggregate;
					}
					
					if (e.ElementType == QueryElementType.SqlFunction)
					{
						var sqlFunc = (SqlFunction) e;
						return !sqlFunc.IsPure || sqlFunc.IsAggregate;
					}

					return e.ElementType.In(QueryElementType.SqlField,
						QueryElementType.SelectClause);
				});

			return result;
		}

		public static SelectQuery RootQuery(this SelectQuery query)
		{
			while (query.ParentSelect != null)
			{
				query = query.ParentSelect;
			}
			return query;
		}

		public static SqlJoinedTable? FindJoin(this SelectQuery query,
			Func<SqlJoinedTable, bool> match)
		{
			return new QueryVisitor().Find(query, e =>
			{
				if (e.ElementType == QueryElementType.JoinedTable)
				{
					if (match((SqlJoinedTable) e))
						return true;
				}
				return false;
			}) as SqlJoinedTable;
		}

		public static void ConcatSearchCondition(this SqlWhereClause where, SqlSearchCondition search)
		{
			if (where.IsEmpty)
			{
				where.SearchCondition.Conditions.AddRange(search.Conditions);
			}
			else
			{
				if (where.SearchCondition.Precedence < Precedence.LogicalConjunction)
				{
					var sc1 = new SqlSearchCondition();

					sc1.Conditions.AddRange(where.SearchCondition.Conditions);

					where.SearchCondition.Conditions.Clear();
					where.SearchCondition.Conditions.Add(new SqlCondition(false, sc1));
				}

				if (search.Precedence < Precedence.LogicalConjunction)
				{
					var sc2 = new SqlSearchCondition();

					sc2.Conditions.AddRange(search.Conditions);

					where.SearchCondition.Conditions.Add(new SqlCondition(false, sc2));
				}
				else
					where.SearchCondition.Conditions.AddRange(search.Conditions);
			}
		}

		/// <summary>
		/// Ensures that expression is not A OR B but (A OR B)
		/// Function makes all needed manipulations for that
		/// </summary>
		/// <param name="searchCondition"></param>
		public static SqlSearchCondition EnsureConjunction(this SqlSearchCondition searchCondition)
		{
			if (searchCondition.Conditions.Count > 0 && searchCondition.Precedence < Precedence.LogicalConjunction)
			{
				var sc1 = new SqlSearchCondition();

				sc1.Conditions.AddRange(searchCondition.Conditions);

				searchCondition.Conditions.Clear();
				searchCondition.Conditions.Add(new SqlCondition(false, sc1));
			}

			return searchCondition;
		}

		/// <summary>
		/// Ensures that expression is not A OR B but (A OR B)
		/// Function makes all needed manipulations for that
		/// </summary>
		/// <param name="whereClause"></param>
		public static SqlWhereClause EnsureConjunction(this SqlWhereClause whereClause)
		{
			whereClause.SearchCondition.EnsureConjunction();
			return whereClause;
		}

		public static bool IsEqualTables(SqlTable? table1, SqlTable? table2)
		{
			var result =
				table1                 != null
				&& table2              != null
				&& table1.ObjectType   == table2.ObjectType
				&& table1.Database     == table2.Database
				&& table1.Server       == table2.Server
				&& table1.Schema       == table2.Schema
				&& table1.Name         == table2.Name
				&& table1.PhysicalName == table2.PhysicalName;

			return result;
		}

		public static IEnumerable<ISqlTableSource> EnumerateAccessibleSources(SqlTableSource tableSource)
		{
			if (tableSource.Source is SelectQuery q)
				{
					foreach (var ts in EnumerateAccessibleSources(q))
						yield return ts;
				}
			else 
				yield return tableSource.Source;

			foreach (var join in tableSource.Joins)
			{
				foreach (var source in EnumerateAccessibleSources(join.Table))
					yield return source;
			}

		}

		/// <summary>
		/// Enumerates table sources recursively based on joins
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <returns></returns>
		public static IEnumerable<ISqlTableSource> EnumerateAccessibleSources(SelectQuery selectQuery)
		{
			yield return selectQuery;

			foreach (var tableSource in selectQuery.Select.From.Tables)
			{
				foreach (var source in EnumerateAccessibleSources(tableSource))
					yield return source;
			}
		}

		public static IEnumerable<SqlTable> EnumerateAccessibleTables(SelectQuery selectQuery)
		{
			return EnumerateAccessibleSources(selectQuery)
				.OfType<SqlTableSource>()
				.Select(ts => ts.Source)
				.OfType<SqlTable>();
		}

		public static IEnumerable<ISqlTableSource> EnumerateLevelSources(SqlTableSource tableSource)
		{
			foreach (var j in tableSource.Joins)
			{
				yield return j.Table;

				foreach (var js in EnumerateLevelSources(j.Table))
				{
					yield return js;
				}
			}
		}

		public static IEnumerable<ISqlTableSource> EnumerateLevelSources(SelectQuery selectQuery)
		{
			foreach (var tableSource in selectQuery.Select.From.Tables)
			{
				yield return tableSource;

				foreach (var js in EnumerateLevelSources(tableSource))
				{
					yield return js;
				}
			}
		}

		public static IEnumerable<SqlTable> EnumerateLevelTables(SelectQuery selectQuery)
		{
			return EnumerateLevelSources(selectQuery)
				.OfType<SqlTableSource>()
				.Select(ts => ts.Source)
				.OfType<SqlTable>();
		}

		public static IEnumerable<SqlJoinedTable> EnumerateJoins(SelectQuery selectQuery)
		{
			return selectQuery.Select.From.Tables.SelectMany(t => EnumerateJoins(t));
		}

		public static IEnumerable<SqlJoinedTable> EnumerateJoins(SqlTableSource tableSource)
		{
			foreach (var tableSourceJoin in tableSource.Joins)
			{
				yield return tableSourceJoin;	
			}

			foreach (var tableSourceJoin in tableSource.Joins)
			{
				foreach (var subJoin in EnumerateJoins(tableSourceJoin.Table))
				{
					yield return subJoin;
				}
			}
		}


		/// <summary>
		/// Converts ORDER BY DISTINCT to GROUP BY equivalent
		/// </summary>
		/// <param name="select"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static bool TryConvertOrderedDistinctToGroupBy(SelectQuery select, SqlProviderFlags flags)
		{
			if (!select.Select.IsDistinct || select.OrderBy.IsEmpty)
				return false;

			var nonProjecting = select.Select.OrderBy.Items.Select(i => i.Expression)
				.Except(select.Select.Columns.Select(c => c.Expression))
				.ToList();

			if (nonProjecting.Count > 0)
			{
				if (!flags.IsOrderByAggregateFunctionsSupported)
					throw new LinqToDBException("Can not convert sequence to SQL. DISTINCT with ORDER BY not supported.");

				// converting to Group By

				var newOrderItems = select.Select.OrderBy.Items
					.Select(oi =>
						!nonProjecting.Contains(oi.Expression)
							? oi
							: new SqlOrderByItem(
								new SqlFunction(oi.Expression.SystemType!, oi.IsDescending ? "Min" : "Max", true, oi.Expression),
								oi.IsDescending))
					.ToList();

				select.Select.OrderBy.Items.Clear();
				select.Select.OrderBy.Items.AddRange(newOrderItems);

				// add only missing group items
				var currentGroupItems = new HashSet<ISqlExpression>(select.Select.GroupBy.Items);
				select.Select.GroupBy.Items.AddRange(
					select.Select.Columns.Select(c => c.Expression)
						.Where(e => !currentGroupItems.Contains(e)));

				select.Select.IsDistinct = false;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Detects when we can remove order
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <param name="flags"></param>
		/// <param name="information"></param>
		/// <returns></returns>
		public static bool CanRemoveOrderBy(SelectQuery selectQuery, SqlProviderFlags flags, QueryInformation information)
		{
			if (selectQuery == null) throw new ArgumentNullException(nameof(selectQuery));

			if (selectQuery.OrderBy.IsEmpty || selectQuery.ParentSelect == null)
				return false;

			var current = selectQuery;
			do
			{
				if (current.Select.SkipValue != null || current.Select.TakeValue != null)
				{
					return false;
				}

				if (current != selectQuery)
				{
					if (!current.OrderBy.IsEmpty || current.Select.IsDistinct)
						return true;
				}

				var info = information.GetHierarchyInfo(current);
				if (info == null)
					break;

				switch (info.HierarchyType)
				{
					case QueryInformation.HierarchyType.From:
						if (!flags.IsSubQueryOrderBySupported)
							return true;
						current = info.MasterQuery;
						break;
					case QueryInformation.HierarchyType.Join:
						return true;
					case QueryInformation.HierarchyType.SetOperator:
						// currently removing ordering for all UNION
						return true;
					case QueryInformation.HierarchyType.InnerQuery:
						return true;
					default:
						throw new ArgumentOutOfRangeException();
				}

			} while (current != null);

			return false;
		}

		/// <summary>
		/// Detects when we can remove order
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <param name="information"></param>
		/// <returns></returns>
		public static bool TryRemoveDistinct(SelectQuery selectQuery, QueryInformation information)
		{
			if (selectQuery == null) throw new ArgumentNullException(nameof(selectQuery));

			if (!selectQuery.Select.IsDistinct)
				return false;

			var info = information.GetHierarchyInfo(selectQuery);
			switch (info?.HierarchyType)
			{
				case QueryInformation.HierarchyType.InnerQuery:
					{
						if (info.ParentElement is SqlFunction func && func.Name == "EXISTS")
						{
							// ORDER BY not needed for EXISTS function, even when Take and Skip specified
							selectQuery.Select.OrderBy.Items.Clear();

							if (selectQuery.Select.SkipValue == null && selectQuery.Select.TakeValue == null)
							{
								// we can safely remove DISTINCT
								selectQuery.Select.IsDistinct = false;
								selectQuery.Select.Columns.Clear();
								return true;
							}
						}
					}
					break;
			}

			return false;
		}
		/// <summary>
		/// Transforms
		///   SELECT * FROM A
		///     INNER JOIN B ON A.ID = B.ID
		/// to
		///   SELECT * FROM A, B
		///   WHERE A.ID = B.ID
		/// </summary>
		/// <param name="selectQuery">Input SelectQuery.</param>
		/// <returns>The same query instance.</returns>
		public static SelectQuery TransformInnerJoinsToWhere(this SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count > 0 && selectQuery.From.Tables[0] is SqlTableSource tableSource)
			{
				if (tableSource.Joins.All(j => j.JoinType == JoinType.Inner))
				{
					while (tableSource.Joins.Count > 0)
					{
						// consider to remove join and simplify query
						var join = tableSource.Joins[0];
						selectQuery.Where.ConcatSearchCondition(@join.Condition);
						selectQuery.From.Tables.Add(@join.Table);
						tableSource.Joins.RemoveAt(0);
					}
				}
			}

			return selectQuery;
		}

		/// <summary>
		/// Returns SqlField from specific expression. Usually from SqlColumn.
		/// Complex expressions ignored.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>Field instance associated with expression</returns>
		public static SqlField? GetUnderlyingField(ISqlExpression expression)
		{
			switch (expression)
			{
				case SqlField field:
					return field;
				case SqlColumn column:
					return GetUnderlyingField(column.Expression, new HashSet<ISqlExpression>());
			}

			return null;
		}

		static SqlField? GetUnderlyingField(ISqlExpression expression, HashSet<ISqlExpression> visited)
		{
			switch (expression)
			{
				case SqlField field:
					return field;
				case SqlColumn column:
				{
					if (visited.Contains(column))
						return null;
					visited.Add(column);
					return GetUnderlyingField(column.Expression, visited);
				}
			}
			return null;
		}

		public static SqlCondition GenerateEquality(ISqlExpression field1, ISqlExpression field2)
		{
			var compare = new SqlCondition(false, new SqlPredicate.ExprExpr(field1, SqlPredicate.Operator.Equal, field2));

			if (field1.CanBeNull && field2.CanBeNull)
			{
				var isNull1 = new SqlCondition(false, new SqlPredicate.IsNull(field1, false));
				var isNull2 = new SqlCondition(false, new SqlPredicate.IsNull(field2, false));
				var nulls = new SqlSearchCondition(isNull1, isNull2);
				compare.IsOr = true;
				compare = new SqlCondition(false, new SqlSearchCondition(compare, new SqlCondition(false, nulls)));
			}

			return compare;
		}

		/// <summary>
		/// Retrieves which sources are used in the <paramref name="root"/>expression
		/// </summary>
		/// <param name="root">Expression to analyze.</param>
		/// <param name="foundSources">Output container for detected sources/</param>
		public static void GetUsedSources(ISqlExpression root, HashSet<ISqlTableSource> foundSources)
		{
			if (foundSources == null) throw new ArgumentNullException(nameof(foundSources));

			new QueryVisitor().Visit(root, e =>
			{
				if (e is ISqlTableSource source)
					foundSources.Add(source);
				else
					switch (e.ElementType)
					{
						case QueryElementType.Column:
						{
							var c = (SqlColumn) e;
							foundSources.Add(c.Parent!);
							break;
						}
						case QueryElementType.SqlField:
						{
							var f = (SqlField) e;
							foundSources.Add(f.Table!);
							break;
						}
					}
			});
		}

		/// <summary>
		/// Returns correct column or field according to nesting.
		/// </summary>
		/// <param name="selectQuery">Analyzed query.</param>
		/// <param name="forExpression">Expression that has to be enveloped by column.</param>
		/// <param name="inProjection">If 'true', function ensures that column is created. If 'false' it may return Field if it fits to nesting level.</param>
		/// <returns>Returns Column of Field according to its nesting level. May return null if expression is not valid for <paramref name="selectQuery"/></returns>
		public static ISqlExpression? NeedColumnForExpression(SelectQuery selectQuery, ISqlExpression forExpression, bool inProjection)
		{
			var field = GetUnderlyingField(forExpression);

			SqlColumn? column = null;

			if (inProjection)
			{
				column = selectQuery.Select.Columns.Find(c =>
					{
						if (c.Expression.Equals(forExpression))
							return true;
						if (field != null && field.Equals(GetUnderlyingField(c.Expression)))
							return true;
						return false;
					}
				);
			}

			if (column != null)
				return column;

			var tableToCompare = field?.Table;

			var tableSources = EnumerateLevelSources(selectQuery).OfType<SqlTableSource>().Select(s => s.Source).ToArray();

			// enumerate tables first

			foreach (var table in tableSources.OfType<SqlTable>())
			{
				if (tableToCompare != null && tableToCompare == table)
				{
					if (inProjection)
						return selectQuery.Select.AddNewColumn(field!);
					return field;
				}
			}

			foreach (var subQuery in tableSources.OfType<SelectQuery>())
			{
				column = NeedColumnForExpression(subQuery, forExpression, true) as SqlColumn;
				if (column != null && inProjection)
				{
					column = selectQuery.Select.AddNewColumn(column);
				}

				if (column != null)
					break;
			}

			return column;
		}

		public static bool ValidateTable(SelectQuery selectQuery,  ISqlTableSource table)
		{
			var compared = new HashSet<SqlTable>();

			foreach (var t in EnumerateAccessibleTables(selectQuery))
			{
				// infinite recursion can be here. Usually it indicates that query malformed.
				if (compared.Contains(t))
					return false;

				if (t == table)
					return true;

				compared.Add(t);
			}

			return false;
		}

		/// <summary>
		/// Removes not needed subqueries nesting. Very simple algorithm.
		/// </summary>
		/// <typeparam name="TStatement"></typeparam>
		/// <param name="statement">Statement which may contain queries that needs optimization</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when optimization has been performed.</returns>
		public static TStatement OptimizeSubqueries<TStatement>(TStatement statement)
			where TStatement : SqlStatement
		{
			if (statement == null) throw new ArgumentNullException(nameof(statement));

			statement = ConvertVisitor.Convert(statement, (visitor, element) =>
			{
				if (!(element is SelectQuery q))
					return element;

				if (!q.IsSimple || q.HasSetOperators)
					return q;
					
				if (q.Select.From.Tables.Count != 1)
					return q;

				var tableSource = q.Select.From.Tables[0];
				if (tableSource.Joins.Count > 0 || !(q.Select.From.Tables[0].Source is SelectQuery subQuery))
					return q;

				// column list should be equal
				if (subQuery.HasSetOperators || q.Select.Columns.Count != subQuery.Select.Columns.Count)
					return q;

				for (var index = 0; index < q.Select.Columns.Count; index++)
				{
					var column = q.Select.Columns[index];
					var idx = subQuery.Select.Columns.FindIndex(c => c.Equals(column.Expression));
					if (idx < 0)
						return q;
				}

				// correct column order
				for (var index = 0; index < q.Select.Columns.Count; index++)
				{
					var column = q.Select.Columns[index];
					var idx = subQuery.Select.Columns.FindIndex(c => c.Equals(column.Expression));
					var subColumn = subQuery.Select.Columns[idx];
					if (idx != index)
					{
						subQuery.Select.Columns.RemoveAt(idx);
						subQuery.Select.Columns.Insert(index, subColumn);
					}

					// replace
					visitor.VisitedElements[column] = subColumn;
				}

				return subQuery;

			});

			return statement;
		}

		/// <summary>
		/// Wraps tested query in subquery(s).
		/// Keeps columns count the same. After modification statement is equivalent semantically.
		/// <code>
		/// --before
		/// SELECT c1, c2           -- QA
		/// FROM A
		/// -- after (with 2 subqueries)
		/// SELECT C.c1, C.c2       -- QC
		/// FROM (
		///   SELECT B.c1, B.c2     -- QB
		///   FROM (
		///     SELECT c1, c2       -- QA
		///     FROM A
		///        ) B
		///   FROM 
		///      ) C
		/// </code>
		/// </summary>
		/// <typeparam name="TStatement"></typeparam>
		/// <param name="statement">Statement which may contain tested query</param>
		/// <param name="wrapTest">Delegate for testing which query needs to be enveloped.
		/// Result of delegate call tells how many subqueries needed.
		/// 0 - no changes
		/// 1 - one subquery
		/// N - N subqueries
		/// </param>
		/// <param name="onWrap">
		/// After wrapping query this function called for prcess needed optimizations. Array of queries contains [QC, QB, QA]
		/// </param>
		/// <returns>The same <paramref name="statement"/> or modified statement when wrapping has been performed.</returns>
		public static TStatement WrapQuery<TStatement>(
			TStatement             statement,
			Func<SelectQuery, int> wrapTest,
			Action<IReadOnlyList<SelectQuery>> onWrap)
			where TStatement : SqlStatement
		{
			if (statement == null) throw new ArgumentNullException(nameof(statement));
			if (wrapTest  == null) throw new ArgumentNullException(nameof(wrapTest));
			if (onWrap    == null) throw new ArgumentNullException(nameof(onWrap));

			var correctedTables = new Dictionary<ISqlTableSource, SelectQuery>();
			var newStatement = ConvertVisitor.Convert(statement, (visitor, element) =>
			{
				if (element is SelectQuery query)
				{
					var ec = wrapTest(query);
					if (ec <= 0)
						return element;

					var queries = new List<SelectQuery>();
					for (int i = 0; i < ec; i++)
					{
						var newQuery = new SelectQuery
						{
							IsParameterDependent = query.IsParameterDependent,
							ParentSelect         = query.ParentSelect
						};
						queries.Add(newQuery);
					}

					queries.Add(query);

					for (int i = queries.Count - 2; i >= 0; i--)
					{
						queries[i].From.Table(queries[i + 1]);
					}

					foreach (var prevColumn in query.Select.Columns)
					{
						var newColumn = prevColumn;
						for (int ic = ec - 1; ic >= 0; ic--)
						{
							newColumn = queries[ic].Select.AddNewColumn(newColumn);
						}

						// correct mapping
						visitor.VisitedElements[prevColumn] = newColumn;
					}

					onWrap(queries);

					var levelTables = EnumerateLevelTables(query).ToArray();
					var resultQuery = queries[0];
					foreach (var table in levelTables)
					{
						correctedTables.Add(table, resultQuery);
					}

					var toMap = levelTables.SelectMany(t => t.Fields.Values);

					foreach (var field in toMap)
						visitor.VisitedElements.Remove(field);

					return resultQuery;
				} 
				
				if (element is SqlField f && f.Table != null && correctedTables.TryGetValue(f.Table, out var levelQuery))
				{
					return NeedColumnForExpression(levelQuery, f, false)!;
				} 

				return element;
			});

			return newStatement;
		}

		/// <summary>
		/// Wraps <paramref name="queryToWrap"/> by another select.
		/// Keeps columns count the same. After modification statement is equivalent symantically.
		/// <code>
		/// --before
		/// SELECT c1, c2
		/// FROM A
		/// -- after
		/// SELECT B.c1, B.c2 
		/// FROM (
		///   SELECT c1, c2
		///   FROM A
		///      ) B
		/// </code>
		/// </summary>
		/// <typeparam name="TStatement"></typeparam>
		/// <param name="statement">Statement which may contain tested query</param>
		/// <param name="queryToWrap">Tells which select query needs enveloping</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when wrapping has been performed.</returns>
		public static TStatement WrapQuery<TStatement>(TStatement statement, SelectQuery queryToWrap)
			where TStatement : SqlStatement
		{
			if (statement == null) throw new ArgumentNullException(nameof(statement));

			return WrapQuery(statement, q => q == queryToWrap, (q1, q2) => { });
		}

		/// <summary>
		/// Wraps queries by another select.
		/// Keeps columns count the same. After modification statement is equivalent symantically.
		/// </summary>
		/// <typeparam name="TStatement"></typeparam>
		/// <param name="statement"></param>
		/// <param name="wrapTest">Delegate for testing when query needs to be wrapped.</param>
		/// <param name="onWrap">After enveloping query this function called for prcess needed optimizations.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when wrapping has been performed.</returns>
		public static TStatement WrapQuery<TStatement>(
			TStatement                       statement,
			Func<SelectQuery, bool>          wrapTest,
			Action<SelectQuery, SelectQuery> onWrap)
			where TStatement : SqlStatement
		{
			if (statement == null) throw new ArgumentNullException(nameof(statement));
			if (wrapTest == null)  throw new ArgumentNullException(nameof(wrapTest));
			if (onWrap == null)    throw new ArgumentNullException(nameof(onWrap));

			return WrapQuery(statement, q => wrapTest(q) ? 1 : 0, queries => onWrap(queries[0], queries[1]));
		}

		/// <summary>
		/// Helper function for moving Ordering up in select tree.
		/// </summary>
		/// <param name="queries">Array of queries</param>
		public static void MoveOrderByUp(params SelectQuery[] queries)
		{
			// move order up if possible
			for (int qi = queries.Length - 2; qi >= 0; qi--)
			{
				var prevQuery = queries[qi + 1];
				if (prevQuery.Select.OrderBy.IsEmpty || prevQuery.Select.TakeValue != null || prevQuery.Select.SkipValue != null)
					continue;

				var currentQuery = queries[qi];

				for (var index = 0; index < prevQuery.Select.OrderBy.Items.Count; index++)
				{
					var item = prevQuery.Select.OrderBy.Items[index];
					var foundColumn = prevQuery.Select.Columns.Find(c => c.Expression.Equals(item.Expression));
					if (foundColumn != null)
					{
						currentQuery.OrderBy.Items.Add(new SqlOrderByItem(foundColumn, item.IsDescending));
						prevQuery.OrderBy.Items.RemoveAt(index--);
					}
				}
			}
		}

		public static string TransformExpressionIndexes(string expression, Func<int, int> transformFunc)
		{
			if (expression    == null) throw new ArgumentNullException(nameof(expression));
			if (transformFunc == null) throw new ArgumentNullException(nameof(transformFunc));

			const string pattern = @"(?<open>{+)(?<key>\w+)(?<format>:[^}]+)?(?<close>}+)";

			var str = Regex.Replace(expression, pattern, match =>
			{
				string open   = match.Groups["open"].Value;
				string key    = match.Groups["key"].Value;

				//string close  = match.Groups["close"].Value;
				//string format = match.Groups["format"].Value;

				if (open.Length % 2 == 0)
					return match.Value;

				if (!int.TryParse(key, out var idx))
					return match.Value;

				var newIndex = transformFunc(idx);

				return $"{{{newIndex}}}";
			});

			return str;
		}

		public static bool IsAggregationFunction(IQueryElement expr)
		{
			if (expr is SqlFunction func)
				return func.IsAggregate;

			if (expr is SqlExpression expression)
				return expression.IsAggregate;

			return false;
		}

		public static object? EvaluateExpression(this ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlValue           : return ((SqlValue)expr).Value;
				case QueryElementType.SqlParameter       : return ((SqlParameter)expr).Value;
				case QueryElementType.SqlBinaryExpression:
					{
						var binary = (SqlBinaryExpression)expr;
						dynamic? left  = binary.Expr1.EvaluateExpression();
						dynamic? right = binary.Expr2.EvaluateExpression();
						if (left == null || right == null)
							return null;
						switch (binary.Operation)
						{
							case "+" : return left +  right;
							case "-" : return left -  right;
							case "*" : return left *  right;
							case "/" : return left /  right;
							case "%" : return left %  right;
							case "^" : return left ^  right;
							case "&" : return left &  right;
							case "<" : return left <  right;
							case ">" : return left >  right;
							case "<=": return left <= right;
							case ">=": return left >= right;
							default:
								throw new LinqToDBException($"Unknown binary operation '{binary.Operation}'.");
						}
					}
				case QueryElementType.SqlFunction        :
					{
						var function = (SqlFunction)expr;

						switch (function.Name)
						{
							case "CASE":

								if (function.Parameters.Length != 3)
									throw new LinqToDBException($"CASE function expected to have 3 parameters.");

								var cond = function.Parameters[0].EvaluateExpression();

								if (!(cond is bool))
									throw new LinqToDBException($"CASE function expected to have boolean condition (was: {cond?.GetType()}).");

								if ((bool)cond!)
									return function.Parameters[1].EvaluateExpression();
								else
									return function.Parameters[2].EvaluateExpression();

							default:
								throw new LinqToDBException($"Unknown function '{function.Name}'.");
						}
					}

				default:
					{
						var str = expr.ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
							.ToString();
						throw new NotImplementedException(
							$"Not implemented evaluation of '{expr.ElementType}': '{str}'.");
					}
			}
		}


		public static SqlCondition CorrectSearchConditionNesting(SelectQuery sql, SqlCondition condition, HashSet<ISqlTableSource> forTableSources)
		{
			var newCondition = ConvertVisitor.Convert(condition, (v, e) =>
			{
				if (   e is SqlColumn column && column.Parent != null && forTableSources.Contains(column.Parent) 
				    || e is SqlField field   && field.Table   != null && forTableSources.Contains(field.Table))
				{
					e = sql.Select.AddColumn((ISqlExpression)e);
				}

				return e;
			});

			return newCondition;
		}

		public static void MoveSearchConditionsToJoin(SelectQuery sql, SqlJoinedTable joinedTable, List<SqlCondition>? movedConditions)
		{
			var usedTableSources = new HashSet<ISqlTableSource>(sql.Select.From.Tables.Select(t => t.Source));

			var tableSources = new HashSet<ISqlTableSource>();

			((ISqlExpressionWalkable)sql.Where.SearchCondition).Walk(new WalkOptions(), e =>
			{
				if (e is ISqlTableSource ts && usedTableSources.Contains(ts))
					tableSources.Add(ts);
				return e;
			});

			bool ContainsTable(ISqlTableSource tbl, IQueryElement qe)
			{
				return null != new QueryVisitor().Find(qe, e =>
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
						var corrected = CorrectSearchConditionNesting(sql, condition, usedTableSources);
						joinedTable.Condition.Conditions.Insert(0, corrected);
						conditions.RemoveAt(i);
						movedConditions?.Insert(0, condition);
					}
				}
			}
		}

	}
}
