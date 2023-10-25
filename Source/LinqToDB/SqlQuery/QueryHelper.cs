using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace LinqToDB.SqlQuery
{
	using SqlProvider;
	using Common;
	using Mapping;
	using Common.Internal;

	public static partial class QueryHelper
	{
		internal static ObjectPool<SelectQueryOptimizerVisitor> SelectOptimizer =
			new(() => new SelectQueryOptimizerVisitor(), v => v.Cleanup(), 100);

		public static bool ContainsElement(IQueryElement testedRoot, IQueryElement element)
		{
			return null != testedRoot.Find(element, static (element, e) => e == element);
		}

        sealed class IsDependsOnSourcesContext
		{
			public IsDependsOnSourcesContext(IReadOnlyCollection<ISqlTableSource> onSources, IReadOnlyCollection<IQueryElement>? elementsToIgnore)
			{
				OnSources = onSources;
				ElementsToIgnore = elementsToIgnore;
			}

			public readonly IReadOnlyCollection<ISqlTableSource> OnSources;
			public readonly IReadOnlyCollection<IQueryElement>?  ElementsToIgnore;

			public          bool                     DependencyFound;
		}

        public static bool IsDependsOnSource(IQueryElement testedRoot, ISqlTableSource onSource, IReadOnlyCollection<IQueryElement>? elementsToIgnore = null)
        {
	        return IsDependsOnSources(testedRoot, new [] { onSource }, elementsToIgnore);
        }

		public static bool IsDependsOnSources(IQueryElement testedRoot, IReadOnlyCollection<ISqlTableSource> onSources, IReadOnlyCollection<IQueryElement>? elementsToIgnore = null)
		{
			var ctx = new IsDependsOnSourcesContext(onSources, elementsToIgnore);

			testedRoot.VisitParentFirst(ctx, static (context, e) =>
			{
				if (context.DependencyFound)
					return false;

				if (context.ElementsToIgnore != null && context.ElementsToIgnore.Contains(e, QueryElement.ReferenceComparer))
					return false;

				if (e is ISqlTableSource source && context.OnSources.Contains(source, QueryElement.ReferenceComparer))
				{
					context.DependencyFound = true;
					return false;
				}

				switch (e.ElementType)
				{
					case QueryElementType.Column:
					{
						var c = (SqlColumn) e;
						if (context.OnSources.Contains(c.Parent!, QueryElement.ReferenceComparer))
							context.DependencyFound = true;
						break;
					}
					case QueryElementType.SqlField:
					{
						var f = (SqlField) e;
						if (context.OnSources.Contains(f.Table!, QueryElement.ReferenceComparer))
							context.DependencyFound = true;
						break;
					}
				}

				return !context.DependencyFound;
			});

			return ctx.DependencyFound;
		}

		public static bool IsDependsOnOuterSources(
			IQueryElement           testedRoot,
			HashSet<IQueryElement>? elementsToIgnore = null)
		{
			var dependedOnSources = new HashSet<ISqlTableSource>();
			var foundSources = new HashSet<ISqlTableSource>();

			testedRoot.VisitParentFirst((elementsToIgnore, dependedOnSources, foundSources), static (context, e) =>
			{
				if (e is SqlTableSource ts)
					context.foundSources.Add(ts.Source);

				if (e is SelectQuery sc)
					context.foundSources.Add(sc);

				if (e is SqlField field && field.Table != null)
					context.dependedOnSources.Add(field.Table);
				else if (e is SqlColumn column && column.Parent != null)
					context.dependedOnSources.Add(column.Parent);

				return true;
			});

			var result = dependedOnSources.Except(foundSources).Any();
			return result;
		}


		public static bool HasTableInQuery(SelectQuery query, SqlTable table)
		{
			return EnumerateAccessibleTables(query).Any(t => t == table);
		}

		public static bool IsSingleTableInQuery(SelectQuery query, SqlTable table)
		{
			if (query.From.Tables.Count == 1)
			{
				var ts = query.From.Tables[0];
				if (ts.Source == table && ts.Joins.Count == 0)
					return true;
			}
			return false;
		}

        sealed class IsDependsOnElementContext
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

        sealed class DependencyCountContext
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
					var found = GetColumnDescriptor(binary.Expr1) ?? GetColumnDescriptor(binary.Expr2);
					if (found?.GetDbDataType(true).SystemType != binary.SystemType)
						return null;
					return found;
				}
				case QueryElementType.SqlNullabilityExpression:
				{
					var nullability = (SqlNullabilityExpression)expr;
					return GetColumnDescriptor(nullability.SqlExpression);
				}
				case QueryElementType.SqlFunction:
				{
					var function = (SqlFunction)expr;

					//TODO: unify function names and put in common constant storage
					//For example it should be "$COALESCE$" and "$CASE$" do do not mix with user defined extension

					if (function.Name is "Coalesce" or PseudoFunctions.COALESCE && function.Parameters.Length == 2)
					{
						return GetColumnDescriptor(function.Parameters[0]);
					}
					else if (function.Name == "CASE" && function.Parameters.Length == 3)
					{
						return GetColumnDescriptor(function.Parameters[1]) ??
						       GetColumnDescriptor(function.Parameters[2]);
					}
					break;
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
					return ((SqlField)expr).ColumnDescriptor?.GetDbDataType(true);
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
				var field = GetUnderlyingField(expr);
				if (field != null)
					return field.Type;

				return new DbDataType(expr.SystemType ?? typeof(object), DataType.Undefined);
			}

			return descriptor.GetDbDataType(true);
		}

		public static void CollectDependencies(IQueryElement root, IEnumerable<ISqlTableSource> sources, HashSet<ISqlExpression> found, IEnumerable<IQueryElement>? ignore = null, bool singleColumnLevel = false)
		{
			var hash       = new HashSet<ISqlTableSource>(sources);
			var hashIgnore = new HashSet<IQueryElement>(ignore ?? Enumerable.Empty<IQueryElement>());

			root.VisitParentFirst((hash, hashIgnore, found, singleColumnLevel), static (context, e) =>
			{
				if (e is ISqlTableSource source && context.hash.Contains(source) || context.hashIgnore.Contains(e))
					return false;

				switch (e.ElementType)
				{
					case QueryElementType.Column:
					{
						var c = (SqlColumn) e;
						if (c.Parent != null && context.hash.Contains(c.Parent))
							context.found.Add(c);
						if (context.singleColumnLevel)
							return false;
						break;
					}
					case QueryElementType.SqlField:
					{
						var f = (SqlField) e;
						if (f.Table != null && context.hash.Contains(f.Table))
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

		public static bool IsTransitiveExpression(SqlExpression sqlExpression, bool checkNullability)
		{
			if (sqlExpression.Parameters.Length == 1 && sqlExpression.Expr.Trim() == "{0}" && (!checkNullability || sqlExpression.CanBeNull == sqlExpression.Parameters[0].CanBeNullable(NullabilityContext.NonQuery)))
			{
				if (sqlExpression.Parameters[0] is SqlExpression argExpression)
					return IsTransitiveExpression(argExpression, checkNullability);
				return true;
			}

			return false;
		}

		public static bool IsTransitivePredicate(SqlExpression sqlExpression)
		{
			if (sqlExpression.Parameters.Length == 1 && sqlExpression.Expr.Trim() == "{0}")
			{
				if (sqlExpression.Parameters[0] is SqlExpression argExpression)
					return IsTransitivePredicate(argExpression);
				return sqlExpression.Parameters[0] is ISqlPredicate;
			}

			return false;
		}

		public static ISqlExpression UnwrapExpression(ISqlExpression expr, bool checkNullability)
		{
			if (expr.ElementType == QueryElementType.SqlExpression)
			{
				var underlying = GetUnderlyingExpressionValue((SqlExpression)expr, checkNullability);
				if (!ReferenceEquals(expr, underlying))
					return UnwrapExpression(underlying, checkNullability);
			}
			else if (!checkNullability && expr.ElementType == QueryElementType.SqlNullabilityExpression)
				return UnwrapExpression(((SqlNullabilityExpression)expr).SqlExpression, checkNullability);

			return expr;
		}

		public static ISqlExpression GetUnderlyingExpressionValue(SqlExpression sqlExpression, bool checkNullability)
		{
			if (!IsTransitiveExpression(sqlExpression, checkNullability))
				return sqlExpression;

			if (sqlExpression.Parameters[0] is SqlExpression subExpr)
				return GetUnderlyingExpressionValue(subExpr, checkNullability);

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
				expr = GetUnderlyingExpressionValue(sqlExpression, true);
			}
			return expr.ElementType != QueryElementType.Column && expr.ElementType != QueryElementType.SqlField;
		}

		public static bool IsConstantFast(ISqlExpression expr)
		{
			if (expr.ElementType == QueryElementType.SqlValue || expr.ElementType == QueryElementType.SqlParameter)
				return true;

			if (expr.ElementType == QueryElementType.SqlNullabilityExpression)
				return IsConstantFast(((SqlNullabilityExpression)expr).SqlExpression);

			return false;
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

		public static bool IsNullValue(this ISqlExpression expr)
		{
			if (expr is SqlValue { Value: null })
				return true;
			return false;
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
			if (search.Conditions.Count == 0)
				return;

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
			if (table1 == null || table2 == null)
				return false;

			var result =
				table1.ObjectType   == table2.ObjectType &&
				table1.TableName    == table2.TableName  &&
				table1.Expression   == table2.Expression;

			if (result)
			{
				result =
					(table1.SqlQueryExtensions == null || table1.SqlQueryExtensions.Count == 0) &&
					(table2.SqlQueryExtensions == null || table2.SqlQueryExtensions.Count == 0);
			}

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
		public static IEnumerable<SqlTableSource> EnumerateAccessibleTableSources(SelectQuery selectQuery)
		{
			foreach (var tableSource in selectQuery.Select.From.Tables)
			{
				foreach (var source in EnumerateAccessibleTableSources(tableSource))
					yield return source;
			}
		}

		public static IEnumerable<SqlTableSource> EnumerateAccessibleTableSources(SqlTableSource tableSource)
		{
			yield return tableSource;

			if (tableSource.Source is SelectQuery q)
			{
				foreach (var ts in EnumerateAccessibleTableSources(q))
					yield return ts;
			}

			foreach (var join in tableSource.Joins)
			{
				foreach (var source in EnumerateAccessibleTableSources(join.Table))
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
				.OfType<SqlTable>();
		}

		public static IEnumerable<SqlTableSource> EnumerateLevelSources(SqlTableSource tableSource)
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

		public static IEnumerable<SqlTableSource> EnumerateLevelSources(SelectQuery selectQuery)
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

		public static string SuggestTableSourceAlias(SelectQuery selectQuery, string alias)
		{
			var aliases = new[] { alias };
			var currentAliases = EnumerateAccessibleTableSources(selectQuery).Where(ts => ts.Alias != null).Select(ts => ts.Alias!);
			Utils.MakeUniqueNames(aliases, currentAliases, s => s, (_, n, _) => aliases[0] = n);

			return aliases[0];
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

			if (selectQuery.OrderBy.IsEmpty)
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
					if (func.Name == PseudoFunctions.CONVERT)
						current = func.Parameters[2];
					else
						break;
				}
				else if (current is SqlExpression expr)
				{
					if (IsTransitiveExpression(expr, true))
						current = expr.Parameters[0];
					else
						break;
				}
				else
					break;
			}

			return current as SqlField;
		}

		/// <summary>
		/// Returns SqlTable from specific expression. Usually from SqlColumn.
		/// Conversion is ignored.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>SqlTable instance associated with expression</returns>
		public static SqlTable? ExtractSqlTable(ISqlExpression? expression)
		{
			if (expression == null)
				return null;

			switch (expression)
			{
				case SqlTable t:
					return t;
				case SqlField field:
					if (field.Table is SqlTable table)
						return table;
					if (field.Table is SelectQuery sq && sq.From.Tables.Count == 1)
						return ExtractSqlTable(sq.From.Tables[0].Source);
					break;
				case SqlColumn column:
					return ExtractSqlTable(ExtractField(column));
			}

			return null;
		}



		public static SqlCondition GenerateEquality(ISqlExpression field1, ISqlExpression field2, bool compareNullsAsValues)
		{
			var compare = new SqlCondition(false,
				new SqlPredicate.ExprExpr(field1, SqlPredicate.Operator.Equal, field2,
					compareNullsAsValues ? true : null));

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

		public static void MoveDuplicateUsageToSubQuery(SelectQuery query)
		{
			var sources = new HashSet<ISqlTableSource>(EnumerateAccessibleSources(query));
			var uniqueColumns = new HashSet<ISqlExpression>(query.Select.Columns.SelectMany(c =>
			{
				var found   = new HashSet<ISqlExpression>();
				CollectDependencies(c.Expression, sources, found, singleColumnLevel: true);
				return found;
			}));

			var subQuery = new SelectQuery();

			subQuery.DoNotRemove = true;

			var columnsMap = new Dictionary<IQueryElement, SqlColumn>();
			foreach (var expr in uniqueColumns)
			{
				columnsMap.Add(expr, subQuery.Select.AddNewColumn(expr));
			}

			foreach (var column in query.Select.Columns)
			{
				var convertedExpression = column.Expression.ConvertAll(columnsMap, (v, e) =>
				{
					if (v.Context.TryGetValue(e, out var newColumn))
						return newColumn;
					return e;
				});

				column.Expression = convertedExpression;
			}

			if (!query.Where.IsEmpty)
			{
				query.Where = query.Where.ConvertAll(columnsMap, (v, e) =>
				{
					if (v.Context.TryGetValue(e, out var newColumn))
						return newColumn;
					return e;
				});
			}

			if (!query.Having.IsEmpty)
			{
				query.Having = query.Having.ConvertAll(columnsMap, (v, e) =>
				{
					if (v.Context.TryGetValue(e, out var newColumn))
						return newColumn;
					return e;
				});
			}

		
			if (!query.GroupBy.IsEmpty)
			{
				query.GroupBy = query.GroupBy.ConvertAll(columnsMap, (v, e) =>
				{
					if (v.Context.TryGetValue(e, out var newColumn))
						return newColumn;
					return e;
				});
			}

			if (!query.OrderBy.IsEmpty)
			{
				query.OrderBy = query.OrderBy.ConvertAll(columnsMap, (v, e) =>
				{
					if (v.Context.TryGetValue(e, out var newColumn))
						return newColumn;
					return e;
				});
			}

			/*
			subQuery.Select.IsDistinct = query.Select.IsDistinct;
			query.Select.IsDistinct    = false;

			subQuery.Select.SkipValue = query.Select.SkipValue;
			query.Select.SkipValue    = null;
			subQuery.Select.TakeValue = query.Select.TakeValue;
			query.Select.TakeValue    = null;
			*/

			subQuery.From.Tables.AddRange(query.From.Tables);

			query.Select.From.Tables.Clear();
			_ = query.Select.From.Table(subQuery);

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
								tableSource.RawAlias,
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
					var value = StripDoubleQuotes(format.Substring(lastMatchPosition, match.Index - lastMatchPosition + brackets));
					current = new SqlBinaryExpression(typeof(string),
						new SqlValue(typeof(string), value),
						"+", current,
						Precedence.Additive);
				}

				result = result == null ? current : new SqlBinaryExpression(typeof(string), result, "+", current);

				lastMatchPosition = match.Index + match.Length - brackets;
			}

			if (result != null && lastMatchPosition < format.Length)
			{
				var value = StripDoubleQuotes(format.Substring(lastMatchPosition, format.Length - lastMatchPosition));
				result = new SqlBinaryExpression(typeof(string),
					result, "+", new SqlValue(typeof(string), value), Precedence.Additive);
			}

			result ??= new SqlValue(typeof(string), format);

			return result;
		}

		public static bool IsAggregationOrWindowFunction(IQueryElement expr)
		{
			return IsAggregationFunction(expr) || IsWindowFunction(expr);
		}

		public static bool IsAggregationFunction(IQueryElement expr)
		{
			if (expr is SqlFunction func)
				return func.IsAggregate;

			if (expr is SqlExpression expression)
				return (expression.Flags & SqlFlags.IsAggregate) != 0;

			return false;
		}

		public static bool IsWindowFunction(IQueryElement expr)
		{
			if (expr is SqlExpression expression)
				return (expression.Flags & SqlFlags.IsWindowFunction) != 0;

			return false;
		}

		public static bool ContainsAggregationOrWindowFunction(IQueryElement expr)
		{
			if (expr is SqlColumn)
				return false;

			return ContainsAggregationOrWindowFunctionDeep(expr);
		}

		public static bool ContainsAggregationOrWindowFunctionDeep(IQueryElement expr)
		{
			return null != expr.Find(e => IsAggregationFunction(e) || IsWindowFunction(e));
		}

		public static bool ContainsAggregationOrWindowFunctionOneLevel(IQueryElement expr)
		{
			var found = false;
			expr.VisitParentFirst(expr, (_, e) =>
			{
				if (found)
					return true;
				if (e is SqlColumn)
					return false;
				found = IsAggregationFunction(e) || IsWindowFunction(e);
				return !found;
			});

			return found;
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

        sealed class NeedParameterInliningContext
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

		public static bool ShouldCheckForNull(this ISqlExpression expr, NullabilityContext nullability)
		{
			if (!expr.CanBeNullable(nullability))
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

			tableToUpdate ??= EnumerateAccessibleSources(updateStatement.SelectQuery)
				.OfType<SqlTable>()
				.FirstOrDefault();

			return tableToUpdate;
		}

		public static SqlTable? GetDeleteTable(this SqlDeleteStatement deleteStatement)
		{
			var tableToDelete = deleteStatement.Table;

			tableToDelete ??= EnumerateAccessibleSources(deleteStatement.SelectQuery)
				.OfType<SqlTable>()
				.FirstOrDefault();

			return tableToDelete;
		}

		static void RemoveNotUnusedColumnsInternal(SelectQuery selectQuery, SelectQuery parentQuery)
		{
			for (int i = 0; i < selectQuery.From.Tables.Count; i++)
			{
				var table = selectQuery.From.Tables[i];
				if (table.Source is SelectQuery sc)
				{
					for (int c = 0; c < sc.Select.Columns.Count; )
					{
						var column = sc.Select.Columns[c];

						if (IsDependsOn(selectQuery, column, new HashSet<IQueryElement> { table }))
							c++;
						else
						{
							sc.Select.Columns.RemoveAt(c);
						}
					}

					RemoveNotUnusedColumnsInternal(sc, parentQuery);
				}
			}
		}

		public static void RemoveNotUnusedColumns(this SelectQuery selectQuery)
		{
			RemoveNotUnusedColumnsInternal(selectQuery, selectQuery);
		}

		public static SelectQuery GetInnerQuery(this SelectQuery selectQuery)
		{
			if (selectQuery.IsSimple)
			{
				if (selectQuery.From.Tables[0].Source is SelectQuery sub)
				{
					var inner = sub.GetInnerQuery();
					if (inner.From.Tables.Count == 0)
					{
						return inner;
					}
				}
			}

			return selectQuery;
		}

		public static void OptimizeSelectQuery(this SelectQuery selectQuery, IQueryElement root, SqlProviderFlags providerFlags, DataOptions dataOptions)
		{
			var visitor = SelectOptimizer.Allocate();

			var evaluationContext = new EvaluationContext(null);
			visitor.Value.OptimizeQueries(root, providerFlags, dataOptions, evaluationContext, selectQuery, 0);
		}

		[return: NotNullIfNotNull(nameof(sqlExpression))]
		public static ISqlExpression? SimplifyColumnExpression(ISqlExpression? sqlExpression)
		{
			if (sqlExpression is SelectQuery selectQuery && selectQuery.Select.Columns.Count == 1 && selectQuery.From.Tables.Count == 0)
			{
				sqlExpression = SimplifyColumnExpression(selectQuery.Select.Columns[0].Expression);
			}

			return sqlExpression;
		}

		public static SqlSearchCondition CorrectComparisonForJoin(SqlSearchCondition sc)
		{
			var newSc = new SqlSearchCondition();
			for (var index = 0; index < sc.Conditions.Count; index++)
			{
				var condition = sc.Conditions[index];
				if (condition.Predicate is SqlPredicate.ExprExpr exprExpr)
				{
					if ((exprExpr.Operator == SqlPredicate.Operator.Equal ||
					     exprExpr.Operator == SqlPredicate.Operator.NotEqual)
					    && exprExpr.WithNull != null)
					{
						condition = new SqlCondition(condition.IsNot,
							new SqlPredicate.ExprExpr(exprExpr.Expr1, exprExpr.Operator, exprExpr.Expr2, null),
							condition.IsOr);
					}
				}
				else if (condition.Predicate is SqlSearchCondition subSc)
				{
					condition = new SqlCondition(condition.IsNot,
						CorrectComparisonForJoin(subSc),
						condition.IsOr);
				}

				newSc.Conditions.Add(condition);
			}

			return newSc;
		}

		public static bool CalcCanBeNull(bool? canBeNull, ParametersNullabilityType isNullable, IEnumerable<bool> nullInfo)
		{
			if (canBeNull != null)
				return canBeNull.Value;

			switch (isNullable)
			{
				case ParametersNullabilityType.Undefined              : return true;
				case ParametersNullabilityType.Nullable               : return true;
				case ParametersNullabilityType.NotNullable            : return false;
			}

			var parameters = nullInfo.ToArray();

			bool? isNullableParameters = isNullable switch
			{
				ParametersNullabilityType.SameAsFirstParameter     => SameAs(0),
				ParametersNullabilityType.SameAsSecondParameter    => SameAs(1),
				ParametersNullabilityType.SameAsThirdParameter     => SameAs(2),
				ParametersNullabilityType.SameAsLastParameter      => SameAs(parameters.Length - 1),
				ParametersNullabilityType.IfAnyParameterNullable   => parameters.Any(static p => p),
				ParametersNullabilityType.IfAllParametersNullable  => parameters.All(static p => p),
				_ => null
			};

			bool SameAs(int parameterNumber)
			{
				if (parameterNumber >= 0 && parameters.Length > parameterNumber)
					return parameters[parameterNumber];
				return true;
			}

			return isNullableParameters ?? true;
		}


		public static ISqlExpression CreateSqlValue(object? value, SqlBinaryExpression be)
		{
			return CreateSqlValue(value, be.GetExpressionType(), be.Expr1, be.Expr2);
		}

		public static ISqlExpression CreateSqlValue(object? value, DbDataType dbDataType, params ISqlExpression[] basedOn)
		{
			SqlParameter? foundParam = null;

			foreach (var element in basedOn)
			{
				if (element.ElementType == QueryElementType.SqlParameter)
				{
					var param = (SqlParameter)element;
					if (param.IsQueryParameter)
					{
						foundParam = param;
					}
					else
						foundParam ??= param;
				}
			}

			if (foundParam != null)
			{
				var newParam = new SqlParameter(dbDataType, foundParam.Name, value)
				{
					IsQueryParameter = foundParam.IsQueryParameter,
					NeedsCast = foundParam.NeedsCast
				};

				return newParam;
			}

			return new SqlValue(dbDataType, value);
		}

		public static List<IQueryElement> CollectElements(IQueryElement root, Func<IQueryElement, bool> filter)
		{
			var result = new List<IQueryElement>();
			root.VisitAll((list: result, filter), static (ctx, e) =>
			{
				if (ctx.filter(e))
					ctx.list.Add(e);
			});

			return result;
		}

		public static ISqlExpression UnwrapNullablity(ISqlExpression expr)
		{
			while (expr is SqlNullabilityExpression nullability)
				expr = nullability.SqlExpression;

			return expr;
		}

	}
}
