using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LinqToDB.SqlQuery
{
	using SqlProvider;
	using Common;
	using Mapping;

	public static partial class QueryHelper
	{
		public static bool ContainsElement(IQueryElement testedRoot, IQueryElement element)
		{
			return null != testedRoot.Find(element, static (element, e) => e == element);
		}

		private class IsDependsOnSourcesContext
		{
			public IsDependsOnSourcesContext(HashSet<ISqlTableSource> onSources, HashSet<IQueryElement>? elementsToIgnore)
			{
				OnSources = onSources;
				ElementsToIgnore = elementsToIgnore;
			}

			public readonly HashSet<ISqlTableSource> OnSources;
			public readonly HashSet<IQueryElement>?  ElementsToIgnore;

			public          bool                     DependencyFound;
		}

		public static bool IsDependsOn(IQueryElement testedRoot, HashSet<ISqlTableSource> onSources, HashSet<IQueryElement>? elementsToIgnore = null)
		{
			var ctx = new IsDependsOnSourcesContext(onSources, elementsToIgnore);

			testedRoot.VisitParentFirst(ctx, static (context, e) =>
			{
				if (context.DependencyFound)
					return false;

				if (context.ElementsToIgnore != null && context.ElementsToIgnore.Contains(e))
					return false;

				if (e is ISqlTableSource source && context.OnSources.Contains(source))
				{
					context.DependencyFound = true;
					return false;
				}

				switch (e.ElementType)
				{
					case QueryElementType.Column:
					{
						var c = (SqlColumn) e;
						if (context.OnSources.Contains(c.Parent!))
							context.DependencyFound = true;
						break;
					}
					case QueryElementType.SqlField:
					{
						var f = (SqlField) e;
						if (context.OnSources.Contains(f.Table!))
							context.DependencyFound = true;
						break;
					}
				}

				return !context.DependencyFound;
			});

			return ctx.DependencyFound;
		}

		private class IsDependsOnElementContext
		{
			public IsDependsOnElementContext(IQueryElement onElement, HashSet<IQueryElement>? elementsToIgnore)
			{
				OnElement = onElement;
				ElementsToIgnore = elementsToIgnore;
			}

			public readonly IQueryElement           OnElement;
			public readonly HashSet<IQueryElement>? ElementsToIgnore;

			public          bool                    DependencyFound;
		}

		public static bool IsDependsOn(IQueryElement testedRoot, IQueryElement onElement, HashSet<IQueryElement>? elementsToIgnore = null)
		{
			var ctx = new IsDependsOnElementContext(onElement, elementsToIgnore);

			testedRoot.VisitParentFirst(ctx, static (context, e) =>
			{
				if (context.ElementsToIgnore != null && context.ElementsToIgnore.Contains(e))
					return false;

				if (e == context.OnElement)
					context.DependencyFound = true;

				return !context.DependencyFound;
			});

			return ctx.DependencyFound;
		}

		private class DependencyCountContext
		{
			public DependencyCountContext(IQueryElement onElement, HashSet<IQueryElement>? elementsToIgnore)
			{
				OnElement = onElement;
				ElementsToIgnore = elementsToIgnore;
			}

			public readonly IQueryElement           OnElement;
			public readonly HashSet<IQueryElement>? ElementsToIgnore;

			public          int                     DependencyCount;
		}

		public static int DependencyCount(IQueryElement testedRoot, IQueryElement onElement, HashSet<IQueryElement>? elementsToIgnore = null)
		{
			var ctx = new DependencyCountContext(onElement, elementsToIgnore);

			testedRoot.VisitParentFirstAll(ctx, static (context, e) =>
			{
				if (context.ElementsToIgnore != null && context.ElementsToIgnore.Contains(e))
					return false;

				if (e == context.OnElement)
					++context.DependencyCount;

				return true;
			});

			return ctx.DependencyCount;
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

		public static DbDataType? SuggestDbDataType(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.Column:
				{
					var column = (SqlColumn)expr;

					var suggested = SuggestDbDataType(column.Expression);
					if (suggested != null)
						return suggested;

					if (column.Parent?.HasSetOperators == true)
					{
						var idx = column.Parent.Select.Columns.IndexOf(column);
						if (idx >= 0)
						{
							foreach (var setOperator in column.Parent.SetOperators)
							{
								suggested = SuggestDbDataType(setOperator.SelectQuery.Select.Columns[idx].Expression);
								if (suggested != null)
									return suggested;
							}
						}
					}

					break;
				}
				case QueryElementType.SqlField:
				{
					return ((SqlField)expr).ColumnDescriptor.GetDbDataType(true);
				}
				case QueryElementType.SqlExpression:
				{
					var sqlExpr = (SqlExpression)expr;
					if (sqlExpr.Parameters.Length == 1 && sqlExpr.Expr == "{0}")
						return SuggestDbDataType(sqlExpr.Parameters[0]);
					break;
				}
				case QueryElementType.SqlQuery:
				{
					var query = (SelectQuery)expr;
					if (query.Select.Columns.Count == 1)
						return SuggestDbDataType(query.Select.Columns[0]);
					break;
				}
				case QueryElementType.SqlBinaryExpression:
				{
					var binary = (SqlBinaryExpression)expr;
					return SuggestDbDataType(binary.Expr1) ?? SuggestDbDataType(binary.Expr2);
				}
				case QueryElementType.SqlValue:
				{
					var sqlValue = (SqlValue)expr;
					if (sqlValue.ValueType.DbType != null || sqlValue.ValueType.DataType != DataType.Undefined)
						return sqlValue.ValueType;
					break;
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

			root.VisitParentFirst((hash, hashIgnore, found), static (context, e) =>
			{
				if (e is ISqlTableSource source && context.hash.Contains(source) || context.hashIgnore.Contains(e))
					return false;

				switch (e.ElementType)
				{
					case QueryElementType.Column:
					{
						var c = (SqlColumn) e;
						if (context.hash.Contains(c.Parent!))
							context.found.Add(c);
						break;
					}
					case QueryElementType.SqlField:
					{
						var f = (SqlField) e;
						if (context.hash.Contains(f.Table!))
							context.found.Add(f);
						break;
					}
				}
				return true;
			});
		}

		public static void CollectUsedSources(IQueryElement root, HashSet<ISqlTableSource> found, IEnumerable<IQueryElement>? ignore = null)
		{
			var hashIgnore = new HashSet<IQueryElement>(ignore ?? Enumerable.Empty<IQueryElement>());

			root.VisitParentFirst((hashIgnore, found), static (context, e) =>
			{
				if (e is SqlTableSource source)
				{
					if (context.hashIgnore.Contains(e))
						return false;
					context.found.Add(source.Source);
				}

				switch (e.ElementType)
				{
					case QueryElementType.Column:
					{
						var c = (SqlColumn) e;
						context.found.Add(c.Parent!);
						return false;
					}
					case QueryElementType.SqlField:
					{
						var f = (SqlField) e;
						context.found.Add(f.Table!);
						return false;
					}
				}
				return true;
			});
		}

		public static bool IsTransitiveExpression(SqlExpression sqlExpression)
		{
			if (sqlExpression.Parameters.Length == 1 && sqlExpression.Expr.Trim() == "{0}" && sqlExpression.CanBeNull == sqlExpression.Parameters[0].CanBeNull)
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
			return expr.ElementType != QueryElementType.Column && expr.ElementType != QueryElementType.SqlField;
		}

		public static bool IsConstantFast(ISqlExpression expr)
		{
			return expr.ElementType == QueryElementType.SqlValue || expr.ElementType == QueryElementType.SqlParameter;
		}

		/// <summary>
		/// Returns <c>true</c> if tested expression is constant during query execution (e.g. value or parameter).
		/// </summary>
		/// <param name="expr">Tested expression.</param>
		/// <returns></returns>
		public static bool IsConstant(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlValue:
				case QueryElementType.SqlParameter:
					return true;

				case QueryElementType.Column:
				{
					var sqlColumn = (SqlColumn) expr;

					// we can not guarantee order here
					// set operation contains at least two expressions for column
					// (in theory we can test that they are equal, but it is not worth it)
					if (sqlColumn.Parent != null && sqlColumn.Parent.HasSetOperators)
						return false;

					// column can be generated from subquery which can reference to constant expression
					return IsConstant(sqlColumn.Expression);
				}

				case QueryElementType.SqlExpression:
				{
					var sqlExpr = (SqlExpression) expr;
					if (!sqlExpr.IsPure || (sqlExpr.Flags & (SqlFlags.IsAggregate | SqlFlags.IsWindowFunction)) != 0)
						return false;
					return sqlExpr.Parameters.All(static p => IsConstant(p));
				}

				case QueryElementType.SqlFunction:
				{
					var sqlFunc = (SqlFunction) expr;
					if (!sqlFunc.IsPure || sqlFunc.IsAggregate)
						return false;
					return sqlFunc.Parameters.All(static p => IsConstant(p));
				}
			}

			return false;
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
			return query.Find(match, static (match, e) =>
			{
				if (e.ElementType == QueryElementType.JoinedTable)
				{
					if (match((SqlJoinedTable)e))
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
				.Select(static ts => ts.Source)
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
				.Select(static ts => ts.Source)
				.OfType<SqlTable>();
		}

		public static IEnumerable<SqlJoinedTable> EnumerateJoins(SelectQuery selectQuery)
		{
			return selectQuery.Select.From.Tables.SelectMany(static t => EnumerateJoins(t));
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

		public static IEnumerable<SqlTableSource> EnumerateInnerJoined(SqlTableSource tableSource)
		{
			yield return tableSource;

			foreach (var tableSourceJoin in tableSource.Joins)
			{
				if (tableSourceJoin.JoinType == JoinType.Inner)
					yield return tableSourceJoin.Table;
			}

			foreach (var tableSourceJoin in tableSource.Joins)
			{
				if (tableSourceJoin.JoinType == JoinType.Inner)
				{
					foreach (var subJoined in EnumerateInnerJoined(tableSourceJoin.Table))
					{
						yield return subJoined;
					}
				}
			}
		}

		public static IEnumerable<SqlTableSource> EnumerateInnerJoined(SelectQuery selectQuery)
		{
			return selectQuery.Select.From.Tables.SelectMany(static t => EnumerateInnerJoined(t));
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

			var nonProjecting = select.Select.OrderBy.Items.Select(static i => i.Expression)
				.Except(select.Select.Columns.Select(static c => c.Expression))
				.ToList();

			if (nonProjecting.Count > 0)
			{
				if (!flags.IsOrderByAggregateFunctionsSupported)
					throw new LinqToDBException("Can not convert sequence to SQL. DISTINCT with ORDER BY not supported.");

				// converting to Group By

				var newOrderItems = new SqlOrderByItem[select.Select.OrderBy.Items.Count];
				for (var i = 0; i < newOrderItems.Length; i++)
				{
					var oi = select.Select.OrderBy.Items[i];
					newOrderItems[i] = !nonProjecting.Contains(oi.Expression)
							? oi
							: new SqlOrderByItem(
								new SqlFunction(oi.Expression.SystemType!, !oi.IsDescending ? "Min" : "Max", true, oi.Expression),
								oi.IsDescending);
				}

				select.Select.OrderBy.Items.Clear();
				select.Select.OrderBy.Items.AddRange(newOrderItems);

				// add only missing group items
				var currentGroupItems = new HashSet<ISqlExpression>(select.Select.GroupBy.Items);
				foreach (var c in select.Select.Columns)
					if (!currentGroupItems.Contains(c.Expression))
						select.Select.GroupBy.Items.Add(c.Expression);

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
						throw new InvalidOperationException($"Unexpected hierarchy type: {info.HierarchyType}");
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
				if (tableSource.Joins.All(static j => j.JoinType == JoinType.Inner))
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

				var column = (SqlColumn)current;
				if (column.Parent != null && !column.Parent.HasSetOperators)
					current = ((SqlColumn)current).Expression;
				else
					return null;
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

		/// <summary>
		/// Returns SqlField from specific expression. Usually from SqlColumn.
		/// Conversion is ignored.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>Field instance associated with expression</returns>
		public static SqlField? ExtractField(ISqlExpression expression)
		{
			var                      current = expression;
			HashSet<ISqlExpression>? visited = null;
			while (true)
			{
				visited ??= new HashSet<ISqlExpression>();
				if (!visited.Add(current))
					return null;

				if (current is SqlColumn column)
					current = column.Expression;
				else if (current is SqlFunction func)
				{
					if (func.Name == "$Convert$")
						current = func.Parameters[2];
					else
						break;
				}
				else if (current is SqlExpression expr)
				{
					if (IsTransitiveExpression(expr))
						current = expr.Parameters[0];
					else
						break;
				}
				else
					break;
			}

			return current as SqlField;
		}

		public static SqlCondition GenerateEquality(ISqlExpression field1, ISqlExpression field2)
		{
			var compare = new SqlCondition(false,
				new SqlPredicate.ExprExpr(field1, SqlPredicate.Operator.Equal, field2,
					Configuration.Linq.CompareNullsAsValues ? true : null));

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

			root.Visit(foundSources, static (foundSources, e) =>
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
				foreach (var c in selectQuery.Select.Columns)
				{
					if (c.Expression.Equals(forExpression) || (field != null && field.Equals(GetUnderlyingField(c.Expression))))
					{
						column = c;
						break;
					}
				}
			}

			if (column != null)
				return column;

			var tableToCompare = field?.Table;

			var tableSources = EnumerateLevelSources(selectQuery).OfType<SqlTableSource>().Select(static s => s.Source).ToArray();

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

		public static bool ValidateTable(SelectQuery selectQuery, ISqlTableSource table)
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
		/// <typeparam name="TContext">Type of <paramref name="onWrap"/> and <paramref name="wrapTest"/> context object.</typeparam>
		/// <param name="context"><paramref name="onWrap"/> and <paramref name="wrapTest"/> context object.</param>
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
		/// <param name="allowMutation">Wrapped query can be not recreated for performance considerations.</param>
		/// <param name="withStack">Must be set to <c>true</c>, if <paramref name="wrapTest"/> function use 3rd parameter (containing parent element) otherwise it will be always null.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when wrapping has been performed.</returns>
		public static TStatement WrapQuery<TStatement, TContext>(
			TContext                                         context,
			TStatement                                       statement,
			Func<TContext, SelectQuery, IQueryElement?, int> wrapTest,
			Action<TContext, IReadOnlyList<SelectQuery>>     onWrap,
			bool                                             allowMutation,
			bool                                             withStack)
			where TStatement : SqlStatement
		{
			if (statement == null) throw new ArgumentNullException(nameof(statement));
			if (wrapTest  == null) throw new ArgumentNullException(nameof(wrapTest));
			if (onWrap    == null) throw new ArgumentNullException(nameof(onWrap));

			var correctedTables = new Dictionary<ISqlTableSource, SelectQuery>();
			var newStatement = statement.Convert(
				(context, wrapTest, onWrap, correctedTables),
				allowMutation,
				static (visitor, element) =>
				{
					if (element is SelectQuery query)
					{
						var ec = visitor.Context.wrapTest(visitor.Context.context, query, visitor.HasStack ? visitor.ParentElement : null);
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

						var clonedQuery = query.Clone(
							query,
							static (query, e) =>
							{
								if (e == query || e is SqlColumn c && c.Parent == query)
									return true;

								if (e is SqlSetOperator setOperator)
								{
									if (!query.HasSetOperators)
										return true;

									var all = true;
									foreach (var so in query.SetOperators)
									{
										if (so != setOperator)
										{
											all = false;
											break;
										}
									}

									return all;
								}

								return false;
							});

						queries.Add(clonedQuery);

						if (clonedQuery.HasSetOperators)
						{
							queries[0].SetOperators.AddRange(clonedQuery.SetOperators);
							clonedQuery.SetOperators.Clear();
						}

						for (int i = queries.Count - 2; i >= 0; i--)
						{
							queries[i].From.Table(queries[i + 1]);
						}

						for (var index = 0; index < clonedQuery.Select.Columns.Count; index++)
						{
							var prevColumn = clonedQuery.Select.Columns[index];
							var newColumn = prevColumn;
							for (int ic = ec - 1; ic >= 0; ic--)
							{
								newColumn = queries[ic].Select.AddNewColumn(newColumn);
							}

							// correct mapping
							visitor.AddVisited(prevColumn, newColumn);
							visitor.AddVisited(query.Select.Columns[index], newColumn);
						}

						visitor.Context.onWrap(visitor.Context.context, queries);

						var levelTables = EnumerateLevelTables(query).ToArray();
						var resultQuery = queries[0];
						foreach (var table in levelTables)
						{
							visitor.Context.correctedTables.Add(table, resultQuery);
						}

						var toMap = levelTables.SelectMany(static t => t.Fields);

						foreach (var field in toMap)
							visitor.RemoveVisited(field);

						return resultQuery;
					}

					if (element is SqlField f && f.Table != null && visitor.Context.correctedTables.TryGetValue(f.Table, out var levelQuery))
					{
						return NeedColumnForExpression(levelQuery, f, false)!;
					}

					return element;
				}, withStack: withStack);

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
		/// <param name="allowMutation">Wrapped query can be not recreated for performance considerations.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when wrapping has been performed.</returns>
		public static TStatement WrapQuery<TStatement>(
			TStatement  statement,
			SelectQuery queryToWrap,
			bool        allowMutation)
			where TStatement : SqlStatement
		{
			if (statement == null) throw new ArgumentNullException(nameof(statement));

			return WrapQuery(queryToWrap, statement, static (queryToWrap, q, _) => q == queryToWrap, null, allowMutation, false);
		}

		/// <summary>
		/// Wraps queries by another select.
		/// Keeps columns count the same. After modification statement is equivalent symantically.
		/// </summary>
		/// <typeparam name="TStatement"></typeparam>
		/// <typeparam name="TContext">Type of <paramref name="onWrap"/> and <paramref name="wrapTest"/> context object.</typeparam>
		/// <param name="context"><paramref name="onWrap"/> and <paramref name="wrapTest"/> context object.</param>
		/// <param name="statement"></param>
		/// <param name="wrapTest">Delegate for testing when query needs to be wrapped.</param>
		/// <param name="onWrap">After enveloping query this function called for prcess needed optimizations.</param>
		/// <param name="allowMutation">Wrapped query can be not recreated for performance considerations.</param>
		/// <param name="withStack">Must be set to <c>true</c>, if <paramref name="wrapTest"/> function use 3rd parameter (containing parent element) otherwise it will be always null.</param>
		/// <returns>The same <paramref name="statement"/> or modified statement when wrapping has been performed.</returns>
		public static TStatement WrapQuery<TStatement, TContext>(
			TContext                                          context,
			TStatement                                        statement,
			Func<TContext, SelectQuery, IQueryElement?, bool> wrapTest,
			Action<TContext, SelectQuery, SelectQuery>?       onWrap,
			bool                                              allowMutation,
			bool                                              withStack)
			where TStatement : SqlStatement
		{
			if (statement == null) throw new ArgumentNullException(nameof(statement));
			if (wrapTest == null)  throw new ArgumentNullException(nameof(wrapTest));

			return WrapQuery(
				(context, wrapTest, onWrap),
				statement,
				static (context, q, pe  ) => context.wrapTest(context.context, q, pe) ? 1 : 0,
				static (context, queries) => context.onWrap?.Invoke(context.context, queries[0], queries[1]),
				allowMutation,
				withStack);
		}

		/// <summary>
		/// Removes Join from query based on <paramref name="joinFunc"/> result.
		/// </summary>
		/// <param name="context"><paramref name="joinFunc"/> context object.</param>
		/// <param name="statement">Source statement.</param>
		/// <param name="joinFunc"></param>
		/// <returns>Same or new statement with removed joins.</returns>
		public static SqlStatement JoinRemoval<TContext>(TContext context, SqlStatement statement, Func<TContext, SqlStatement, SqlJoinedTable, bool> joinFunc)
		{
			var newStatement = statement.ConvertAll((joinFunc, statement, context), static (visitor, e) =>
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
							if (visitor.Context.joinFunc(visitor.Context.context, visitor.Context.statement, joinedTable))
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
					foreach (var c in prevQuery.Select.Columns)
					{
						if (c.Expression.Equals(item.Expression))
						{
							currentQuery.OrderBy.Items.Add(new SqlOrderByItem(c, item.IsDescending));
							prevQuery.OrderBy.Items.RemoveAt(index--);
							break;
						}
					}
				}
			}
		}

		static Regex _paramsRegex = new (@"(?<open>{+)(?<key>\w+)(?<format>:[^}]+)?(?<close>}+)", RegexOptions.Compiled);

		public static string TransformExpressionIndexes<TContext>(TContext context, string expression, Func<TContext, int, int> transformFunc)
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

				var newIndex = transformFunc(context, idx);

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

		public static bool IsAggregationOrWindowFunction(IQueryElement expr)
		{
			if (expr is SqlFunction func)
				return func.IsAggregate;

			if (expr is SqlExpression expression)
				return (expression.Flags & (SqlFlags.IsAggregate | SqlFlags.IsWindowFunction)) != 0;

			return false;
		}

		// TODO: IsAggregationOrWindowFunction use needs review - maybe we should call ContainsAggregationOrWindowFunction there
		public static bool ContainsAggregationOrWindowFunction(IQueryElement expr) => null != expr.Find(IsAggregationOrWindowFunction);

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
						var columns = new List<ISqlExpression>();
						foreach (var i in selectQuery.Select.GroupBy.Items)
						{
							SqlColumn? c = null;
							foreach (var col in selectQuery.Select.Columns)
							{
								if (col.Expression.Equals(i))
								{
									c = col;
									break;
								}
							}

							if (c != null)
								columns.Add(c);
						}

						if (columns.Count == selectQuery.Select.GroupBy.Items.Count)
							knownKeys.Add(columns);
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

		public static bool? GetBoolValue(ISqlExpression expression, EvaluationContext context)
		{
			if (expression.TryEvaluateExpression(context, out var value))
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
						var boolValue = GetBoolValue(((SqlPredicate.Expr)cond.Predicate).Expr1, context);
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
			var newCondition = condition.Convert((sql, forTableSources), static (v, e) =>
			{
				if (   e is SqlColumn column && column.Parent != null && v.Context.forTableSources.Contains(column.Parent)
				    || e is SqlField field   && field.Table   != null && v.Context.forTableSources.Contains(field.Table))
				{
					e = v.Context.sql.Select.AddColumn((ISqlExpression)e);
				}

				return e;
			});

			return newCondition;
		}

		public static void MoveSearchConditionsToJoin(SelectQuery sql, SqlJoinedTable joinedTable, List<SqlCondition>? movedConditions)
		{
			var usedTableSources = new HashSet<ISqlTableSource>(sql.Select.From.Tables.Select(static t => t.Source));

			var tableSources = new HashSet<ISqlTableSource>();

			((ISqlExpressionWalkable)sql.Where.SearchCondition).Walk(WalkOptions.Default, (usedTableSources, tableSources), static (ctx, e) =>
			{
				if (e is ISqlTableSource ts && ctx.usedTableSources.Contains(ts))
					ctx.tableSources.Add(ts);
				return e;
			});

			bool ContainsTable(ISqlTableSource tbl, IQueryElement qe)
			{
				return null != qe.Find(tbl, static (tbl, e) =>
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

					var found = false;
					foreach (var ts in tableSources)
					{
						if (ContainsTable(ts, condition))
						{
							found = true;
							break;
						}
					}

					if (!found)
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
			return null != expression.Find(static e => (e.ElementType == QueryElementType.SqlParameter) && ((SqlParameter)e).IsQueryParameter);
		}

		private class NeedParameterInliningContext
		{
			public bool HasParameter;
			public bool IsQueryParameter;
		}

		public static bool NeedParameterInlining(ISqlExpression expression)
		{
			var ctx = new NeedParameterInliningContext();

			expression.Visit(ctx, static (context, e) =>
			{
				if (e.ElementType == QueryElementType.SqlParameter)
				{
					context.HasParameter     = true;
					context.IsQueryParameter = context.IsQueryParameter || ((SqlParameter)e).IsQueryParameter;
				}
			});

			if (ctx.HasParameter && ctx.IsQueryParameter)
				return false;

			return ctx.HasParameter;
		}

		public static IDictionary<QueryElementType, int> CountElements(ISqlExpression expr)
		{
			var result = new Dictionary<QueryElementType, int>();
			expr.VisitAll(result, static (result, e) =>
			{
				if (!result.TryGetValue(e.ElementType, out var cnt))
				{
					result[e.ElementType] = 1;
				}
				else
				{
					result[e.ElementType] = cnt + 1;
				}
			});

			return result;
		}

		public static bool IsComplexExpression(this ISqlExpression expr)
		{
			var counts = CountElements(expr);

			if (counts.ContainsKey(QueryElementType.SqlQuery) ||
			    counts.ContainsKey(QueryElementType.LikePredicate) ||
			    counts.ContainsKey(QueryElementType.SearchStringPredicate) ||
			    counts.ContainsKey(QueryElementType.SearchCondition)
			)
			{
				return true;
			}

			int count;
			if (counts.TryGetValue(QueryElementType.SqlBinaryExpression, out count) && count > 1)
			{
				return true;
			}

			if (counts.TryGetValue(QueryElementType.SqlFunction, out count) && count > 1)
			{
				return true;
			}

			return false;
		}

		public static bool ShouldCheckForNull(this ISqlExpression expr)
		{
			if (!expr.CanBeNull)
				return false;

			if (expr.ElementType == QueryElementType.SqlBinaryExpression)
				return false;

			if (expr.ElementType == QueryElementType.SqlField ||
				expr.ElementType == QueryElementType.Column   ||
				expr.ElementType == QueryElementType.SqlValue ||
				expr.ElementType == QueryElementType.SqlParameter)
				return true;

			if ((expr.ElementType == QueryElementType.SqlFunction) && ((SqlFunction)expr).Parameters.Length == 1)
				return true;

			if (null != expr.Find(QueryElementType.SqlQuery))
				return false;

			return true;
		}

		public static DbDataType GetExpressionType(this ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlParameter: return ((SqlParameter)expr).Type;
				case QueryElementType.SqlValue    : return ((SqlValue)expr).ValueType;
			}

			var descriptor = GetColumnDescriptor(expr);
			if (descriptor != null)
				return descriptor.GetDbDataType(true);

			return new DbDataType(expr.SystemType!);
		}

		public static bool HasOuterReferences(ISet<ISqlTableSource> sources, ISqlExpression expr)
		{
			var outerElementFound = null != expr.Find(sources, static (sources, e) =>
			{
				if (e.ElementType == QueryElementType.Column)
				{
					var parent = ((SqlColumn)e).Parent;
					if (parent != null && !sources.Contains(parent))
						return true;
				}
				else if (e.ElementType == QueryElementType.SqlField)
				{
					var table = ((SqlField)e).Table;
					if (table != null && !sources.Contains(table))
						return true;
				}

				return false;
			});

			return outerElementFound;
		}

		public static SqlTable? GetUpdateTable(this SqlUpdateStatement updateStatement)
		{
			var tableToUpdate = updateStatement.Update.Table;

			if (tableToUpdate == null)
			{
				tableToUpdate = EnumerateAccessibleSources(updateStatement.SelectQuery)
					.OfType<SqlTable>()
					.FirstOrDefault();
			}

			return tableToUpdate;
		}

		public static SqlTable? GetDeleteTable(this SqlDeleteStatement deleteStatement)
		{
			var tableToDelete = deleteStatement.Table;

			if (tableToDelete == null)
			{
				tableToDelete = EnumerateAccessibleSources(deleteStatement.SelectQuery)
					.OfType<SqlTable>()
					.FirstOrDefault();
			}

			return tableToDelete;
		}
	}
}
