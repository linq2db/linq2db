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

		public static bool IsDependsOn(IQueryElement testedRoot, IQueryElement onElement, HashSet<IQueryElement>? elementsToIgnore = null)
		{
			var dependencyFound = false;

			new QueryVisitor().VisitParentFirst(testedRoot, e =>
			{
				if (elementsToIgnore != null && elementsToIgnore.Contains(e))
					return false;

				if (e == onElement)
					dependencyFound = true;

				return !dependencyFound;
			});

			return dependencyFound;
		}


		public static int DependencyCount(IQueryElement testedRoot, IQueryElement onElement, HashSet<IQueryElement>? elementsToIgnore = null)
		{
			var dependencyCount = 0;

			new QueryVisitor().VisitParentFirst(testedRoot, e =>
			{
				if (elementsToIgnore != null && elementsToIgnore.Contains(e))
					return false;

				if (e == onElement)
					++dependencyCount;

				return true;
			});

			return dependencyCount;
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
				case QueryElementType.SqlBinaryExpression:
				{
					var binary = (SqlBinaryExpression)expr;
					return GetColumnDescriptor(binary.Expr1) ?? GetColumnDescriptor(binary.Expr2);
				}
			}
			return null;
		}

		public static DbDataType GetDbDataType(ISqlExpression? expr)
		{
			if (expr == null)
				return new DbDataType(typeof(object), DataType.Undefined);

			var descriptor = GetColumnDescriptor(expr);
			if (descriptor == null)
			{
				return new DbDataType(expr.SystemType ?? typeof(object), DataType.Undefined);
			}

			return descriptor.GetDbDataType(true);
		}
		
		public static void CollectDependencies(IQueryElement root, IEnumerable<ISqlTableSource> sources, HashSet<ISqlExpression> found, IEnumerable<IQueryElement>? ignore = null)
		{
			var hash       = new HashSet<ISqlTableSource>(sources);
			var hashIgnore = new HashSet<IQueryElement>(ignore ?? Enumerable.Empty<IQueryElement>());

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

		public static void CollectUsedSources(IQueryElement root, HashSet<ISqlTableSource> found, IEnumerable<IQueryElement>? ignore = null)
		{
			var hashIgnore = new HashSet<IQueryElement>(ignore ?? Enumerable.Empty<IQueryElement>());

			new QueryVisitor().VisitParentFirst(root, e =>
			{
				if (e is SqlTableSource source)
				{
					if (hashIgnore.Contains(e))
						return false;
					found.Add(source.Source);
				}

				switch (e.ElementType)
				{
					case QueryElementType.Column :
					{
						var c = (SqlColumn) e;
						found.Add(c.Parent!);
						return false;
					}
					case QueryElementType.SqlField :
					{
						var f = (SqlField) e;
						found.Add(f.Table!);
						return false;
					}
				}
				return true;
			});
		}

		public static bool IsTransitiveExpression(SqlExpression sqlExpression)
		{
			if (sqlExpression.Parameters.Length == 1 && sqlExpression.Expr.Trim() == "{0}")
			{
				if (sqlExpression.Parameters[0] is SqlExpression argExpression)
					return IsTransitiveExpression(argExpression);
				return true;
			}

			return false;
		}

		public static ISqlExpression UnwrapExpression(ISqlExpression expr)
		{
			if (expr.ElementType == QueryElementType.SqlExpression)
			{
				var underlying = GetUnderlyingExpressionValue((SqlExpression)expr);
				if (!ReferenceEquals(expr, underlying))
					return UnwrapExpression(underlying);
			}

			return expr;
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
		/// Unwraps SqlColumn and returns underlying expression.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>Underlying expression.</returns>
		public static ISqlExpression? GetUnderlyingExpression(ISqlExpression? expression)
		{
			var current = expression;
			HashSet<ISqlExpression>? visited = null;
			while (current?.ElementType == QueryElementType.Column)
			{
				visited ??= new HashSet<ISqlExpression>();
				if (!visited.Add(current))
					return null;
				current = ((SqlColumn)current).Expression;
			}

			return current;
		}

		/// <summary>
		/// Returns SqlField from specific expression. Usually from SqlColumn.
		/// Complex expressions ignored.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>Field instance associated with expression</returns>
		public static SqlField? GetUnderlyingField(ISqlExpression expression)
		{
			return GetUnderlyingExpression(expression) as SqlField;
		}

		public static SqlCondition GenerateEquality(ISqlExpression field1, ISqlExpression field2)
		{
			var compare = new SqlCondition(false,
				new SqlPredicate.ExprExpr(field1, SqlPredicate.Operator.Equal, field2,
					Configuration.Linq.CompareNullsAsValues ? true : (bool?)null));

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

					var toMap = levelTables.SelectMany(t => t.Fields);

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
		/// Removes Join from query based on <paramref name="joinFunc"/> result.
		/// </summary>
		/// <param name="statement">Source statement.</param>
		/// <param name="joinFunc"></param>
		/// <returns>Same or new statement with removed joins.</returns>
		public static SqlStatement JoinRemoval(SqlStatement statement, Func<SqlStatement, SqlJoinedTable, bool> joinFunc)
		{
			var newStatement = ConvertVisitor.ConvertAll(statement, (visitor, e) =>
			{
				if (e.ElementType == QueryElementType.TableSource)
				{
					var tableSource = (SqlTableSource)e;
					if (tableSource.Joins.Count > 0)
					{
						List<SqlJoinedTable>? joins = null;
						for (var i = 0; i < tableSource.Joins.Count; i++)
						{
							var joinedTable = tableSource.Joins[i];
							if (joinFunc(statement, joinedTable))
							{
								joins ??= new List<SqlJoinedTable>(tableSource.Joins.Take(i));
							}
							else
							{
								joins?.Add(joinedTable);
							}
						}

						if (joins != null)
						{
							var newTableSource = new SqlTableSource(
								tableSource.Source,
								tableSource._alias,
								joins,
								tableSource.HasUniqueKeys ? tableSource.UniqueKeys : null);
							return newTableSource;
						}
					}
				}

				return e;
			});

			return newStatement;
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

		static Regex _paramsRegex = new Regex(@"(?<open>{+)(?<key>\w+)(?<format>:[^}]+)?(?<close>}+)", RegexOptions.Compiled);

		public static string TransformExpressionIndexes(string expression, Func<int, int> transformFunc)
		{
			if (expression    == null) throw new ArgumentNullException(nameof(expression));
			if (transformFunc == null) throw new ArgumentNullException(nameof(transformFunc));

			var str = _paramsRegex.Replace(expression, match =>
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

		public static ISqlExpression ConvertFormatToConcatenation(string format, IList<ISqlExpression> parameters)
		{
			if (format     == null) throw new ArgumentNullException(nameof(format));
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			string StripDoubleQuotes(string str)
			{
				str = str.Replace("{{", "{");
				str = str.Replace("}}", "}");
				return str;
			}

			var matches = _paramsRegex.Matches(format);

			ISqlExpression? result = null;
			var lastMatchPosition = 0;

			foreach (Match? match in matches)
			{
				if (match == null)
					continue;

				string open = match.Groups["open"].Value;
				string key  = match.Groups["key"].Value;

				if (open.Length % 2 == 0)
					continue;

				if (!int.TryParse(key, out var idx))
					continue;

				var current = parameters[idx];

				var brackets = open.Length / 2;
				if (match.Index > lastMatchPosition)
				{

					current = new SqlBinaryExpression(typeof(string),
						new SqlValue(typeof(string),
							StripDoubleQuotes(format.Substring(lastMatchPosition, match.Index - lastMatchPosition + brackets))),
						"+", current,
						Precedence.Additive);
				}

				result = result == null ? current : new SqlBinaryExpression(typeof(string), result, "+", current);

				lastMatchPosition = match.Index + match.Length - brackets;
			}

			if (result != null && lastMatchPosition < format.Length)
			{
				result = new SqlBinaryExpression(typeof(string),
					result, "+", new SqlValue(typeof(string),
						StripDoubleQuotes(format.Substring(lastMatchPosition, format.Length - lastMatchPosition))), Precedence.Additive);
			}

			result ??= new SqlValue(typeof(string), format);

			return result;
		}

		public static bool IsAggregationFunction(IQueryElement expr)
		{
			if (expr is SqlFunction func)
				return func.IsAggregate;

			if (expr is SqlExpression expression)
				return expression.IsAggregate;

			return false;
		}

		/// <summary>
		/// Collects unique keys from different sources.
		/// </summary>
		/// <param name="tableSource"></param>
		/// <param name="knownKeys">List with found keys.</param>
		public static void CollectUniqueKeys(SqlTableSource tableSource, List<IList<ISqlExpression>> knownKeys)
		{
			if (tableSource.HasUniqueKeys)
				knownKeys.AddRange(tableSource.UniqueKeys);

			CollectUniqueKeys(tableSource.Source, true, knownKeys);
		}


		/// <summary>
		/// Collects unique keys from different sources.
		/// </summary>
		/// <param name="tableSource"></param>
		/// <param name="includeDistinct">Flag to include Distinct as unique key.</param>
		/// <param name="knownKeys">List with found keys.</param>
		public static void CollectUniqueKeys(ISqlTableSource tableSource, bool includeDistinct, List<IList<ISqlExpression>> knownKeys)
		{
			switch (tableSource)
			{
				case SqlTable table:
				{
					var keys = table.GetKeys(false);
					if (keys != null && keys.Count > 0)
						knownKeys.Add(keys);

					break;
				}
				case SelectQuery selectQuery:
				{
					if (selectQuery.HasUniqueKeys)
						knownKeys.AddRange(selectQuery.UniqueKeys);

					if (includeDistinct && selectQuery.Select.IsDistinct)
						knownKeys.Add(selectQuery.Select.Columns.OfType<ISqlExpression>().ToList());

					if (!selectQuery.Select.GroupBy.IsEmpty)
					{
						var columns = selectQuery.Select.GroupBy.Items
							.Select(i => selectQuery.Select.Columns.Find(c => c.Expression.Equals(i))).Where(c => c != null)
							.ToArray();
						if (columns.Length == selectQuery.Select.GroupBy.Items.Count)
							knownKeys.Add(columns.OfType<ISqlExpression>().ToList());
					}

					if (selectQuery.From.Tables.Count == 1)
					{
						var table = selectQuery.From.Tables[0];
						if (table.HasUniqueKeys && table.Joins.Count == 0)
						{
							knownKeys.AddRange(table.UniqueKeys);
						}
					}

					break;
				}
			}
		}

		#region Expression Evaluation

		public static SqlParameterValue GetParameterValue(this SqlParameter parameter, IReadOnlyParameterValues? parameterValues)
		{
			if (parameterValues != null && parameterValues.TryGetValue(parameter, out var value))
			{
				return value;
			}
			return new SqlParameterValue(parameter.Value, parameter.Type);
		}

		public static bool TryEvaluateExpression(this IQueryElement expr, IReadOnlyParameterValues? parameterValues, out object? result)
		{
			return expr.TryEvaluateExpression(parameterValues, out result, out _);
		}

		public static bool CanBeEvaluated(this IQueryElement expr, bool withParameters)
		{
			return expr.TryEvaluateExpression(withParameters ? SqlParameterValues.Empty : null, out _, out _);
		}

		public static bool CanBeEvaluated(this IQueryElement expr, IReadOnlyParameterValues? parameterValues)
		{
			return expr.TryEvaluateExpression(parameterValues, out _, out _);
		}

		public static bool TryEvaluateExpression(this IQueryElement expr, IReadOnlyParameterValues? parameterValues, out object? result, out string? errorMessage)
		{
			result = null;
			errorMessage = null;
			switch (expr.ElementType)
			{
				case QueryElementType.SqlValue           : result = ((SqlValue)expr).Value; return true;
				case QueryElementType.SqlParameter       :
				{
					var sqlParameter = (SqlParameter)expr;

					if (parameterValues == null) 
						return false;

					result = sqlParameter.GetParameterValue(parameterValues).Value;
					return true;
				}
				case QueryElementType.IsNullPredicate:
				{
					var isNullPredicate = (SqlPredicate.IsNull)expr;
					if (!isNullPredicate.Expr1.TryEvaluateExpression(parameterValues, out var value))
						return false;
					result = isNullPredicate.IsNot == (value != null);
					return true;
				}
				case QueryElementType.ExprExprPredicate:
				{
					var exprExpr = (SqlPredicate.ExprExpr)expr;
					var reduced = exprExpr.Reduce(parameterValues);
					if (!ReferenceEquals(reduced, expr))
						return TryEvaluateExpression(reduced, parameterValues, out result, out errorMessage);

					if (!exprExpr.Expr1.TryEvaluateExpression(parameterValues, out var value1) ||
					    !exprExpr.Expr2.TryEvaluateExpression(parameterValues, out var value2))
						return false;

					switch (exprExpr.Operator)
					{
						case SqlPredicate.Operator.Equal:
						{
							if (value1 == null)
							{
								result = value2 == null;
							}
							else
							{
								result = (value2 != null) && value1.Equals(value2);
							}
							break;
						}
						case SqlPredicate.Operator.NotEqual:
						{
							if (value1 == null)
							{
								result = value2 != null;
							}
							else
							{
								result = value2 == null || !value1.Equals(value2);
							}
							break;
						}
						default:
						{
							if (!(value1 is IComparable comp1) || !(value2 is IComparable comp2))
							{
								result = false;
								return true;
							}

							switch (exprExpr.Operator)
							{
								case SqlPredicate.Operator.Greater:
									result = comp1.CompareTo(comp2) > 0;
									break;
								case SqlPredicate.Operator.GreaterOrEqual:
									result = comp1.CompareTo(comp2) >= 0;
									break;
								case SqlPredicate.Operator.NotGreater:
									result = !(comp1.CompareTo(comp2) > 0);
									break;
								case SqlPredicate.Operator.Less:
									result = comp1.CompareTo(comp2) < 0;
									break;
								case SqlPredicate.Operator.LessOrEqual:
									result = comp1.CompareTo(comp2) <= 0;
									break;
								case SqlPredicate.Operator.NotLess:
									result = !(comp1.CompareTo(comp2) < 0);
									break;

								default:
									return false;

							}
							break;
						}
					}

					return true;
				}
				case QueryElementType.IsTruePredicate:
				{
					var isTruePredicate = (SqlPredicate.IsTrue)expr;
					if (!isTruePredicate.Expr1.TryEvaluateExpression(parameterValues, out var value))
						return false;

					if (value == null)
					{
						throw new NotImplementedException();
					}
					else if (value is bool boolValue)
					{
						result = boolValue != isTruePredicate.IsNot;
						return true;
					}	
					return false;
				}
				case QueryElementType.SqlBinaryExpression:
				{
					var binary = (SqlBinaryExpression)expr;
					if (!binary.Expr1.TryEvaluateExpression(parameterValues, out var leftEvaluated, out errorMessage))
						return false;
					if (!binary.Expr2.TryEvaluateExpression(parameterValues, out var rightEvaluated, out errorMessage))
						return false;
					dynamic? left  = leftEvaluated;
					dynamic? right = rightEvaluated;
					if (left == null || right == null)
						return true;
					switch (binary.Operation)
					{
						case "+" : result = left + right; break;
						case "-" : result = left - right; break;
						case "*" : result = left * right; break;
						case "/" : result = left / right; break;
						case "%" : result = left % right; break;
						case "^" : result = left ^ right; break;
						case "&" : result = left & right; break;
						case "<" : result = left < right; break;
						case ">" : result = left > right; break;
						case "<=": result = left <= right; break;
						case ">=": result = left >= right; break;
						default:
							errorMessage = $"Unknown binary operation '{binary.Operation}'.";
							return false;
					}

					return true;
				}
				case QueryElementType.SqlFunction        :
				{
					var function = (SqlFunction)expr;

					switch (function.Name)
					{
						case "CASE":

							if (function.Parameters.Length != 3)
							{
								errorMessage = "CASE function expected to have 3 parameters.";
								return false;
							}

							if (!function.Parameters[0].TryEvaluateExpression(parameterValues, out var cond, out errorMessage))
								return false;

							if (!(cond is bool))
							{
								errorMessage =
									$"CASE function expected to have boolean condition (was: {cond?.GetType()}).";
								return false;
							}

							if ((bool)cond!)
								return function.Parameters[1].TryEvaluateExpression(parameterValues, out result, out errorMessage);
							else
								return function.Parameters[2].TryEvaluateExpression(parameterValues, out result, out errorMessage);

						default:
							errorMessage = $"Unknown function '{function.Name}'.";
							return false;
					}
				}

				default:
				{
					return false;
				}
			}
		}

		public static object? EvaluateExpression(this IQueryElement expr, IReadOnlyParameterValues? parameterValues)
		{
			if (!expr.TryEvaluateExpression(parameterValues, out var result, out var message))
			{
				message ??= GetEvaluationError(expr);

				throw new LinqToDBException(message);
			}

			return result;
		}

		private static string GetEvaluationError(IQueryElement expr)
		{
			return $"Not implemented evaluation of '{expr.ElementType}': '{expr.ToDebugString()}'.";
		}

		#endregion


		public static bool? GetBoolValue(ISqlExpression expression, IReadOnlyParameterValues? parameterValues)
		{
			if (expression.TryEvaluateExpression(parameterValues, out var value))
			{
				if (value is bool b)
					return b;
			}
			else if (expression is SqlSearchCondition searchCondition)
			{
				if (searchCondition.Conditions.Count == 0)
					return true;
				if (searchCondition.Conditions.Count == 1)
				{
					var cond = searchCondition.Conditions[0];
					if (cond.Predicate.ElementType == QueryElementType.ExprPredicate)
					{
						var boolValue = GetBoolValue(((SqlPredicate.Expr)cond.Predicate).Expr1, parameterValues);
						if (boolValue.HasValue)
							return cond.IsNot ? !boolValue : boolValue;
					}
				}
			}

			return null;
		}

		public static string ToDebugString(this IQueryElement expr)
		{
			try
			{
				var str = expr.ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
					.ToString();
				return str;
			}
			catch
			{
				return $"FAIL ToDebugString('{expr.GetType().Name}').";
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

		public static bool HasQueryParameters(ISqlExpression expression)
		{
			return null != new QueryVisitor().Find(expression, e => (e.ElementType == QueryElementType.SqlParameter) && ((SqlParameter)e).IsQueryParameter);
		}

		public static bool NeedParameterInlining(ISqlExpression expression)
		{
			bool hasParameter     = false;
			bool isQueryParameter = false;
			new QueryVisitor().Visit(expression, e =>
			{
				if (e.ElementType == QueryElementType.SqlParameter)
				{
					hasParameter = true;
					isQueryParameter = isQueryParameter || ((SqlParameter)e).IsQueryParameter;
				}
			});

			if (hasParameter && isQueryParameter)
				return false;

			return hasParameter;
		}

		public static bool ShouldCheckForNull(this ISqlExpression expr)
		{
			if (!expr.CanBeNull)
				return false;

			if (expr.ElementType == QueryElementType.SqlBinaryExpression)
				return false;

			if (expr.ElementType.In(QueryElementType.SqlField, QueryElementType.Column, QueryElementType.SqlValue, QueryElementType.SqlParameter))
				return true;

			if (null != new QueryVisitor().Find(expr, e => e.ElementType.In(QueryElementType.SqlQuery)))
				return false;

			return true;
		}


	}
}
