using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlQuery
{
	public static partial class QueryHelper
	{
		internal static ObjectPool<SelectQueryOptimizerVisitor> SelectOptimizer =
			new(() => new SelectQueryOptimizerVisitor(), v => v.Cleanup(), 100);

		internal static ObjectPool<AggregationCheckVisitor> AggregationCheckVisitors =
			new(() => new AggregationCheckVisitor(), v => v.Cleanup(), 100);

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
			IQueryElement                         testedRoot,
			IReadOnlyCollection<IQueryElement>?   elementsToIgnore = null,
			IReadOnlyCollection<ISqlTableSource>? currentSources   = null)
		{
			var dependedOnSources = new List<ISqlTableSource>();
			var foundSources = new List<ISqlTableSource>();

			testedRoot.VisitParentFirst((elementsToIgnore, currentSources, dependedOnSources, foundSources), static (context, e) =>
			{
				if (context.elementsToIgnore?.Contains(e) == true)
					return false;

				switch (e)
				{
					case SqlTableSource ts:
						context.foundSources.Add(ts.Source);
						break;

					case SelectQuery sc:
						context.foundSources.Add(sc);
						break;

					case SqlField field when field.Table != null:
						context.dependedOnSources.Add(field.Table);
						break;

					case SqlColumn column when column.Parent != null:
						context.dependedOnSources.Add(column.Parent);
						break;
				}

				return true;
			});

			var excepted = dependedOnSources.Except(foundSources);
			if (currentSources != null)
				excepted = excepted.Except(currentSources);

			var result = excepted.Any();
			return result;
		}

		public static bool HasTableInQuery(SelectQuery query, SqlTable table)
		{
			return EnumerateAccessibleTables(query).Any(t => t == table);
		}

		public static bool IsSingleTableInQuery(SelectQuery query, SqlTable table)
		{
			if (query.From.Tables is [{ Joins.Count: 0, Source: var s }]
				&& s == table)
			{
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
					var column = (SqlColumn)expr;
					var result = GetColumnDescriptor(column.Expression);
					if (result is not null)
						return result;

					if (column.Parent?.HasSetOperators == true)
					{
						var idx = column.Parent.Select.Columns.IndexOf(column);
						if (idx >= 0)
						{
							foreach (var setOperator in column.Parent.SetOperators)
							{
								result = GetColumnDescriptor(setOperator.SelectQuery.Select.Columns[idx].Expression);
								if (result is not null)
									return result;
							}
						}
					}

					return null;
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
				case QueryElementType.SqlCoalesce:
				{
					var coalesce = (SqlCoalesceExpression)expr;
					foreach (var expression in coalesce.Expressions)
					{
						var descriptor = GetColumnDescriptor(expression);
						if (descriptor != null)
							return descriptor;
					}

					break;
				}
				case QueryElementType.SqlCondition:
				{
					var condition = (SqlConditionExpression)expr;

					return GetColumnDescriptor(condition.TrueValue) ??
					       GetColumnDescriptor(condition.FalseValue);
				}
				case QueryElementType.SqlCase:
				{
					var caseExpression = (SqlCaseExpression)expr;

					foreach (var caseItem in caseExpression.Cases)
					{
						var descriptor = GetColumnDescriptor(caseItem.ResultExpression);
						if (descriptor != null)
							return descriptor;
					}

					return GetColumnDescriptor(caseExpression.ElseExpression);
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

		public static DbDataType GetDbDataType(ISqlExpression expr, MappingSchema mappingSchema)
		{
			var result = GetDbDataType(expr);
			if (result.DataType == DataType.Undefined)
			{
				var fromSchema = mappingSchema.GetDbDataType(expr.SystemType ?? typeof(object));

				result = fromSchema
					.WithSystemType(result.SystemType)
					.WithDbType(result.DbType ?? fromSchema.DbType)
					.WithLength(result.Length ?? fromSchema.Length)
					.WithPrecision(result.Precision ?? fromSchema.Precision)
					.WithScale(result.Scale ?? fromSchema.Scale);
			}

			return result;
		}

		public static DbDataType GetDbDataTypeWithoutSchema(ISqlExpression expr)
		{
			var result = GetDbDataType(expr);
			return result;
		}

		static DbDataType GetDbDataType(ISqlExpression? expr)
		{
			switch (expr)
			{
				case null: return DbDataType.Undefined;
				case SqlValue { ValueType: var vt }: return vt;

				case SqlParameter        { Type: var t }: return t;
				case SqlField            { Type: var t }: return t;
				case SqlDataType         { Type: var t }: return t;
				case SqlCastExpression   { Type: var t }: return t;
				case SqlBinaryExpression { Type: var t }: return t;

				case SqlParameterizedExpressionBase { Type: var t }: return t;

				case SqlColumn                { Expression:    var e }: return GetDbDataType(e);
				case SqlNullabilityExpression { SqlExpression: var e }: return GetDbDataType(e);

				case SelectQuery selectQuery:
				{
					return selectQuery is { Select.Columns: [{ Expression: var e }] }
						? GetDbDataType(e)
						: DbDataType.Undefined;
				}

				case SqlCaseExpression caseExpression          : return GetCaseExpressionType(caseExpression);
				case SqlConditionExpression conditionExpression: return GetConditionExpressionType(conditionExpression);

				case { SystemType: null } : return DbDataType.Undefined;
				case { SystemType: var t }: return new(t);
			};

			static DbDataType GetCaseExpressionType(SqlCaseExpression caseExpression)
			{
				foreach (var caseItem in caseExpression.Cases)
				{
					var caseType = GetDbDataType(caseItem.ResultExpression);
					if (caseType.DataType != DataType.Undefined)
						return caseType;
				}

				return GetDbDataType(caseExpression.ElseExpression);
			}

			static DbDataType GetConditionExpressionType(SqlConditionExpression sqlCondition)
			{
				var trueType = GetDbDataType(sqlCondition.TrueValue);
				if (trueType.DataType != DataType.Undefined)
					return trueType;

				return GetDbDataType(sqlCondition.FalseValue);
			}
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

		static bool IsTransitiveExpression(SqlExpression sqlExpression, bool checkNullability)
		{
			if (sqlExpression is { Parameters: [var p] }
				&& sqlExpression.Expr.Trim() == "{0}" 
				&& (!checkNullability || sqlExpression.CanBeNullable(NullabilityContext.NonQuery) == p.CanBeNullable(NullabilityContext.NonQuery)))
			{
				if (p is SqlExpression argExpression)
					return IsTransitiveExpression(argExpression, checkNullability);
				return true;
			}

			return false;
		}

		static bool IsTransitivePredicate(SqlExpression sqlExpression)
		{
			if (sqlExpression is { Parameters: [var p] } && sqlExpression.Expr.Trim() == "{0}")
			{
				if (p is SqlExpression argExpression)
					return IsTransitivePredicate(argExpression);
				return p is ISqlPredicate;
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

		static ISqlExpression GetUnderlyingExpressionValue(SqlExpression sqlExpression, bool checkNullability)
		{
			if (!IsTransitiveExpression(sqlExpression, checkNullability))
				return sqlExpression;

			if (sqlExpression.Parameters[0] is SqlExpression subExpr)
				return GetUnderlyingExpressionValue(subExpr, checkNullability);

			return sqlExpression.Parameters[0];
		}

		public static bool IsConstantFast(ISqlExpression expr)
		{
			if (expr.ElementType == QueryElementType.SqlValue || expr.ElementType == QueryElementType.SqlParameter)
				return true;

			if (expr.ElementType == QueryElementType.SqlBinaryExpression)
			{
				var be = (SqlBinaryExpression)expr;
				return IsConstantFast(be.Expr1) && IsConstantFast(be.Expr2);
			}

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

				case QueryElementType.SqlCast:
				{
					var sqlCast = (SqlCastExpression) expr;

					return IsConstant(sqlCast.Expression);
				}

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
				case QueryElementType.SqlFunction  :
				{
					var sqlExpr = (SqlParameterizedExpressionBase) expr;
					if (!sqlExpr.IsPure || (sqlExpr.Flags & (SqlFlags.IsAggregate | SqlFlags.IsWindowFunction)) != 0)
						return false;
					return sqlExpr.Parameters.All(static p => IsConstant(p));
				}
			}

			return false;
		}

		public static bool IsNullValue(this ISqlExpression expr)
		{
			if (expr is SqlValue { Value: null })
				return true;
			if (expr is SqlColumn { Parent: not null } column)
			{
				if (!IsNullValue(column.Expression))
					return false;

				if (column.Parent.HasSetOperators)
				{
					var idx = column.Parent.Select.Columns.IndexOf(column);
					if (idx < 0)
						return false;

					foreach (var setOperator in column.Parent.SetOperators)
					{
						var selectClause = setOperator.SelectQuery.Select;
						if (idx >= selectClause.Columns.Count || !IsNullValue(selectClause.Columns[idx].Expression))
							return false;
					}
				}

				return true;
			}

			return false;
		}

		public static void ConcatSearchCondition(this SqlWhereClause where, SqlSearchCondition search)
		{
			var sc = where.EnsureConjunction();

			if (search.IsOr)
			{
				sc.Predicates.Add(search);
			}
			else
			{
				sc.Predicates.AddRange(search.Predicates);
			}
		}

		public static void ConcatSearchCondition(this SqlHavingClause where, SqlSearchCondition search)
		{
			var sc = where.EnsureConjunction();

			if (search.IsOr)
			{
				sc.Predicates.Add(search);
			}
			else
			{
				sc.Predicates.AddRange(search.Predicates);
			}
		}

		/// <summary>
		/// Ensures that expression is not A OR B but (A OR B)
		/// Function makes all needed manipulations for that
		/// </summary>
		/// <param name="whereClause"></param>
		public static SqlSearchCondition EnsureConjunction(this SqlWhereClause whereClause)
		{
			if (whereClause.SearchCondition.IsOr)
			{
				var old = whereClause.SearchCondition;
				whereClause.SearchCondition = new SqlSearchCondition(false, canBeUnknown: null, old);
			}

			return whereClause.SearchCondition;
		}

		/// <summary>
		/// Ensures that expression is not A OR B but (A OR B)
		/// Function makes all needed manipulations for that
		/// </summary>
		/// <param name="whereClause"></param>
		static SqlSearchCondition EnsureConjunction(this SqlHavingClause whereClause)
		{
			if (whereClause.SearchCondition.IsOr)
			{
				var old = whereClause.SearchCondition;
				whereClause.SearchCondition = new SqlSearchCondition(false, canBeUnknown: null, old);
			}

			return whereClause.SearchCondition;
		}

		public static bool IsEqualTables([NotNullWhen(true)] SqlTable? table1, [NotNullWhen(true)] SqlTable? table2, bool withExtensions = true)
		{
			if (table1 == null || table2 == null)
				return false;

			// TODO: we should introduce better class hierarchy for tables
			if (table1.GetType() != typeof(SqlTable) || table2.GetType() != typeof(SqlTable))
				return false;

			var result =
				table1.ObjectType   == table2.ObjectType &&
				table1.TableName    == table2.TableName  &&
				table1.Expression   == table2.Expression;

			if (result && withExtensions)
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

		static IEnumerable<SqlTableSource> EnumerateAccessibleTableSources(SqlTableSource tableSource)
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

		static IEnumerable<SqlTableSource> EnumerateLevelSources(SqlTableSource tableSource)
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

		public static IEnumerable<SqlJoinedTable> EnumerateJoins(SelectQuery selectQuery)
		{
			return selectQuery.Select.From.Tables.SelectMany(static t => EnumerateJoins(t));
		}

		static IEnumerable<SqlJoinedTable> EnumerateJoins(SqlTableSource tableSource)
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

		public static string SuggestTableSourceAlias(SelectQuery selectQuery, string alias)
		{
			var aliases = new[] { alias };
			var currentAliases = EnumerateAccessibleTableSources(selectQuery).Where(ts => ts.Alias != null).Select(ts => ts.Alias!);
			Utils.MakeUniqueNames(aliases, currentAliases, s => s, (_, n, _) => aliases[0] = n);

			return aliases[0];
		}

		/// <summary>
		/// Unwraps SqlColumn and returns underlying expression.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>Underlying expression.</returns>
		static ISqlExpression? GetUnderlyingExpression(ISqlExpression? expression)
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
					current = column.Expression;
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
				else if (current is SqlCastExpression cast)
					current = cast.Expression;
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
			return expression switch
			{
				SqlTable t => t,

				SqlField f when f.Table is SqlTable t => t,

				SqlField f when f.Table is SelectQuery { From.Tables: [{ Source: var s }] } =>
					ExtractSqlTable(s),

				SqlColumn c => ExtractSqlTable(ExtractField(c)),

				_ => null,
			};
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
							currentQuery.OrderBy.Items.Add(new SqlOrderByItem(c, item.IsDescending, item.IsPositioned));
							prevQuery.OrderBy.Items.RemoveAt(index--);
							break;
						}
					}
				}
			}
		}

#if SUPPORTS_REGEX_GENERATORS
		[GeneratedRegex(@"(?<open>{+)(?<key>\w+)(?<format>:[^}]+)?(?<close>}+)")]
		private static partial Regex ParamsRegex();
#else
		static Regex _paramsRegex = new (@"(?<open>{+)(?<key>\w+)(?<format>:[^}]+)?(?<close>}+)", RegexOptions.Compiled);
		static Regex ParamsRegex() => _paramsRegex;
#endif

		public static string TransformExpressionIndexes<TContext>(TContext context, string expression, Func<TContext, int, int> transformFunc)
		{
			if (expression    == null) throw new ArgumentNullException(nameof(expression));
			if (transformFunc == null) throw new ArgumentNullException(nameof(transformFunc));

			var str = ParamsRegex().Replace(expression, match =>
			{
				string open   = match.Groups["open"].Value;
				string key    = match.Groups["key"].Value;

				//string close  = match.Groups["close"].Value;
				//string format = match.Groups["format"].Value;

				if (open.Length % 2 == 0)
					return match.Value;

				if (!int.TryParse(key, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var idx))
					return match.Value;

				var newIndex = transformFunc(context, idx);

				return FormattableString.Invariant($"{{{newIndex}}}");
			});

			return str;
		}

		public static ISqlExpression ConvertFormatToConcatenation(string format, IReadOnlyList<ISqlExpression> parameters)
		{
			if (format     == null) throw new ArgumentNullException(nameof(format));
			if (parameters == null) throw new ArgumentNullException(nameof(parameters));

			string StripDoubleQuotes(string str)
			{
				str = str.Replace("{{", "{");
				str = str.Replace("}}", "}");
				return str;
			}

			var matches = ParamsRegex().Matches(format);

			ISqlExpression? result = null;
			var lastMatchPosition = 0;

			foreach (Match? match in matches)
			{
				if (match == null)
					continue;

				var open = match.Groups["open"].Value;
				var key  = match.Groups["key"].Value;

				if (open.Length % 2 == 0)
					continue;

				if (!int.TryParse(key, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var idx))
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
				var value = StripDoubleQuotes(format.Substring(lastMatchPosition));
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
			return expr switch
			{
				SqlParameterizedExpressionBase p => p.IsAggregate,
				SqlExtendedFunction func         => func.IsAggregate,
				_                                => false,
			};
		}

		internal sealed class AggregationCheckVisitor : QueryElementVisitor
		{
			public bool IsAggregation          { get; set; }
			public bool IsWindow               { get; set; }
			public bool HasReference           { get; set; }
			public bool CanBeAffectedByOrderBy { get; set; }

			public AggregationCheckVisitor() : base(VisitMode.ReadOnly)
			{
			}

			public void Cleanup()
			{
				IsAggregation          = false;
				IsWindow               = false;
				HasReference           = false;
				CanBeAffectedByOrderBy = false;
			}

			[return : NotNullIfNotNull(nameof(element))]
			public override IQueryElement? Visit(IQueryElement? element)
			{
				// if we already found aggregation or window function, we can stop
				if (IsAggregation && IsWindow && HasReference)
					return element;

				return base.Visit(element);
			}

			protected override IQueryElement VisitSqlFunction(SqlFunction element)
			{
				var isAggregation = IsAggregationFunction(element);
				var isWindow      = IsWindowFunction(element);

				IsAggregation = IsAggregation || isAggregation;
				IsWindow      = IsWindow      || isWindow;

				if (isAggregation || isWindow)
				{
					return element;
				}

				return base.VisitSqlFunction(element);
			}

			protected override IQueryElement VisitSqlExtendedFunction(SqlExtendedFunction element)
			{
				var isAggregation = IsAggregationFunction(element);
				var isWindow      = IsWindowFunction(element);

				if (element.CanBeAffectedByOrderBy)
					CanBeAffectedByOrderBy = true;

				IsAggregation = IsAggregation || isAggregation;
				IsWindow      = IsWindow      || isWindow;

				if (isAggregation || isWindow)
				{
					return element;
				}

				return base.VisitSqlExtendedFunction(element);
			}

			protected override IQueryElement VisitSqlExpression(SqlExpression element)
			{
				var isAggregation = IsAggregationFunction(element);
				var isWindow      = IsWindowFunction(element);

				IsAggregation = IsAggregation || isAggregation;
				IsWindow      = IsWindow      || isWindow;

				if (isAggregation || isWindow)
				{
					return element;
				}

				return base.VisitSqlExpression(element);
			}

			protected override IQueryElement VisitSqlFieldReference(SqlField element)
			{
				HasReference = true;
				return base.VisitSqlFieldReference(element);
			}

			protected override IQueryElement VisitSqlColumnReference(SqlColumn element)
			{
				HasReference = true;
				return base.VisitSqlColumnReference(element);
			}
		}

		public static bool IsAggregationQuery(SelectQuery selectQuery)
		{
			return IsAggregationQuery(selectQuery, out _);
		}

		public static bool IsAggregationQuery(SelectQuery selectQuery, out bool needsOrderBy)
		{
			using var visitorRef = AggregationCheckVisitors.Allocate();

			var visitor        = visitorRef.Value;
			var hasAggregation = false;
			foreach (var column in selectQuery.Select.Columns)
			{
				visitor.Cleanup();
				visitor.Visit(column.Expression);

				if (visitor.HasReference)
				{
					needsOrderBy = false;
					return false;
				}

				if (visitor.IsAggregation)
				{
					hasAggregation = true;
				}
			}

			needsOrderBy = visitor.CanBeAffectedByOrderBy;
			return hasAggregation;
		}

		public static bool IsWindowFunction(IQueryElement expr)
		{
			if (expr is SqlParameterizedExpressionBase expression)
				return expression.IsWindowFunction;

			if (expr is SqlExtendedFunction { IsWindowFunction: true })
				return true;

			return false;
		}

		public static bool ContainsAggregationOrWindowFunction(IQueryElement expr)
		{
			return ContainsExpressionInSameLevel(expr, e => IsWindowFunction(e) || IsAggregationFunction(e));
		}

		public static bool ContainsWindowFunction(IQueryElement expr)
		{
			return ContainsExpressionInSameLevel(expr, IsWindowFunction);
		}

		public static bool ContainsAggregationFunction(IQueryElement expr)
		{
			return ContainsExpressionInSameLevel(expr, IsAggregationFunction);
		}

		static bool ContainsExpressionInSameLevel(IQueryElement expr, Func<ISqlExpression, bool> matchFunc)
		{
			var found = false;

			expr.VisitParentFirst(e =>
			{
				if (found)
					return false;

				if (e is SelectQuery)
					return false;

				if (e is ISqlExpression sqlExpr && matchFunc(sqlExpr))
				{
					found = true;
					return false;
				}

				return true;
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
						knownKeys.Add(selectQuery.Select.Columns.Select(c => c.Expression).ToList());

					if (!selectQuery.Select.GroupBy.IsEmpty)
					{
						knownKeys.Add(selectQuery.Select.GroupBy.Items);
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

		[return: NotNullIfNotNull(nameof(sqlExpression))]
		public static ISqlExpression? SimplifyColumnExpression(ISqlExpression? sqlExpression)
		{
			if (sqlExpression == null)
				return null;

			switch (UnwrapNullablity(sqlExpression))
			{
				case SelectQuery
				{
					Select.Columns: [{ Expression: var expr }],
					From.Tables: [],
					HasSetOperators: false
				}:
					return SimplifyColumnExpression(expr);

				default:
					return sqlExpression;
			};
		}

		/// <summary>
		/// Disables null checks for equality operations.
		/// </summary>
		public static SqlSearchCondition CorrectComparisonForJoin(SqlSearchCondition sc)
		{
			var newSc = new SqlSearchCondition(false);
			for (var index = 0; index < sc.Predicates.Count; index++)
			{
				var predicate = sc.Predicates[index];
				if (predicate is SqlPredicate.ExprExpr exprExpr)
				{
					if (exprExpr.Operator is SqlPredicate.Operator.Equal or SqlPredicate.Operator.NotEqual && exprExpr.UnknownAsValue != null)
					{
						predicate = new SqlPredicate.ExprExpr(exprExpr.Expr1, exprExpr.Operator, exprExpr.Expr2, null);
					}
				}
				else if (predicate is SqlSearchCondition { IsOr: false } subSc)
				{
					predicate = CorrectComparisonForJoin(subSc);
				}

				newSc.Predicates.Add(predicate);
			}

			return newSc;
		}

		internal static bool TypeCanBeNull(Type type)
		{
			return type.IsNullableOrReferenceType() || type is INullable;
		}

		public static bool CalcCanBeNull(Type? type, bool? canBeNull, ParametersNullabilityType isNullable, IEnumerable<bool> nullInfo)
		{
			if (canBeNull != null)
				return canBeNull.Value;

			if (isNullable == ParametersNullabilityType.Undefined)
				return type == null ? true : TypeCanBeNull(type);

			switch (isNullable)
			{
				case ParametersNullabilityType.Nullable               : return true;
				case ParametersNullabilityType.NotNullable            : return false;
			}

			var parameters = nullInfo.ToArray();

			bool? isNullableParameters = isNullable switch
			{
				ParametersNullabilityType.SameAsFirstParameter         => SameAs(0),
				ParametersNullabilityType.SameAsSecondParameter        => SameAs(1),
				ParametersNullabilityType.SameAsThirdParameter         => SameAs(2),
				ParametersNullabilityType.SameAsFirstOrSecondParameter => SameAs(0) || SameAs(1),
				ParametersNullabilityType.SameAsLastParameter          => SameAs(parameters.Length - 1),
				ParametersNullabilityType.IfAnyParameterNullable       => parameters.Any(static p => p),
				ParametersNullabilityType.IfAllParametersNullable      => parameters.All(static p => p),
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

		public static ISqlExpression CreateSqlValue(object? value, SqlBinaryExpression be, MappingSchema mappingSchema)
		{
			return CreateSqlValue(value, GetDbDataType(be, mappingSchema), be.Expr1, be.Expr2);
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

		public static ISqlExpression UnwrapNullablity(ISqlExpression expr)
		{
			while (expr is SqlNullabilityExpression nullability)
				expr = nullability.SqlExpression;

			return expr;
		}

		public static bool CanBeNullableOrUnknown(this ISqlExpression expr, NullabilityContext nullabilityContext, bool withoutUnknownErased)
		{
			if (expr is ISqlPredicate predicate)
				return predicate.CanBeUnknown(nullabilityContext, withoutUnknownErased);

			return expr.CanBeNullable(nullabilityContext);
		}

		public static bool IsPredicate(this ISqlExpression expr)
		{
			return expr is ISqlPredicate or SqlParameterizedExpressionBase { IsPredicate: true };
		}

		public static ISqlExpression UnwrapCast(ISqlExpression expr)
		{
			while (expr is SqlCastExpression { Expression: var sqlCast })
				expr = sqlCast;

			return expr;
		}

		public static bool SameWithoutNullablity(ISqlExpression expr1, ISqlExpression expr2)
		{
			if (ReferenceEquals(expr1, expr2))
				return true;

			if (ReferenceEquals(UnwrapNullablity(expr1), UnwrapNullablity(expr2)))
				return true;

			return false;
		}

		public static bool HasElement(this IQueryElement root, IQueryElement element)
		{
			return null != root.Find(element, static (tf, e) => ReferenceEquals(tf, e));
		}

		public static bool HasQueryParameter(this IQueryElement root)
		{
			return null != root.Find(static e =>
			{
				if (e.ElementType == QueryElementType.SqlParameter)
				{
					var param = (SqlParameter)e;
					return param.IsQueryParameter;
				}

				return false;
			});
		}

		public static bool HasParameter(this IQueryElement root)
		{
			return null != root.Find(static e =>
			{
				if (e.ElementType == QueryElementType.SqlParameter)
				{
					return true;
				}

				return false;
			});
		}

		public static TElement MarkAsNonQueryParameters<TElement>(TElement root)
		where TElement : class, IQueryElement
		{
			var newElement = root.Convert(1, static (_, e) =>
			{
				if (e.ElementType == QueryElementType.SqlParameter)
				{
					var param = (SqlParameter)e;
					return param.WithIsQueryParameter(false);
				}
				return e;
			});

			return (TElement)newElement;
		}

		public static bool? GetBoolValue(IQueryElement element, EvaluationContext evaluationContext)
		{
			if (element.TryEvaluateExpression(evaluationContext, out var value))
				return value as bool?;

			return null;
		}

		internal static void EnsureFindTables(this SqlStatement statement)
		{
			statement.Visit(statement, static (statement, e) =>
			{
				if (e is SqlField f)
				{
					var ts = statement.SelectQuery?.GetTableSource(f.Table!) ?? statement.GetTableSource(f.Table!, out _);

					if (ts == null && f != f.Table!.All)
						throw new LinqToDBException($"Table '{f.Table}' not found.");
				}
			});
		}

		internal static void EnsureFindTables(this SelectQuery select)
		{
			select.Visit(select, static (query, e) =>
			{
				if (e is SqlField f)
				{
					var ts = query.GetTableSource(f.Table!);

					if (ts == null && f != f.Table!.All)
						throw new LinqToDBException($"Table '{f.Table}' not found.");
				}
			});
		}

		public static void CollectParametersAndValues(IQueryElement root, ICollection<SqlParameter> parameters, ICollection<SqlValue> values)
		{
			root.VisitAll(x =>
			{
				if (x is SqlParameter { AccessorId: not null } p)
					parameters.Add(p);
				else if (x is SqlValue v)
					values.Add(v);
			});
		}

		/// <summary>
		/// Merges predicates from <paramref name="child"/> and <paramref name="parent"/> into new or <paramref name="parent"/> condition and return result.
		/// </summary>
		internal static SqlSearchCondition MergeConditions(SqlSearchCondition parent, SqlSearchCondition child)
		{
			if (parent.IsAnd)
			{
				if (child.IsAnd)
					parent.Predicates.InsertRange(0, child.Predicates);
				else
					parent.Predicates.Insert(0, new SqlSearchCondition(true, canBeUnknown: null, child.Predicates));

				return parent;
			}
			else
			{
				if (child.IsAnd)
					return new SqlSearchCondition(false, canBeUnknown: null, [..child.Predicates, parent]);
				else
					return new SqlSearchCondition(false, canBeUnknown: null, child, parent);
			}
		}

		/// <summary>
		/// Returns <c>true</c> if expression typed by predicate (returns SQL BOOLEAN-typed value).
		/// </summary>
		public static bool IsBoolean(ISqlExpression expr, bool includeFields = false)
		{
			expr = UnwrapNullablity(expr);

			if (expr is ISqlPredicate)
				return true;

			if (expr is SqlCaseExpression caseExpr)
			{
				foreach (var cs in caseExpr.Cases)
				{
					if (IsBoolean(cs.ResultExpression))
						return true;
				}

				if (caseExpr.ElseExpression != null && IsBoolean(caseExpr.ElseExpression))
					return true;
			}

			if (expr is SqlConditionExpression condExpr
				&& (IsBoolean(condExpr.TrueValue) || IsBoolean(condExpr.FalseValue)))
			{
				return true;
			}

			if (includeFields && expr is SqlField or SqlColumn)
				return true;

			return false;
		}

		public static bool HasCteClauseReference(IQueryElement element, CteClause? clause)
		{
			if (clause == null)
				return false;
			return null != element.Find(clause, static (c, e) => e.ElementType == QueryElementType.SqlCteTable && ((SqlCteTable)e).Cte == c);
		}

		/// <summary>
		/// Returns true, if type represents signed integer type.
		/// </summary>
		internal static bool IsSignedType(this DbDataType type)
		{
			return type.DataType.IsSignedType();
		}

		/// <summary>
		/// Returns true, if type represents signed integer type.
		/// </summary>
		internal static bool IsUnsignedType(this DbDataType type)
		{
			return type.DataType.IsUnsignedType();
		}

		/// <summary>
		/// Converts signed numeric type to unsigned type.
		/// </summary>
		internal static DbDataType ToUnsigned(this DbDataType type)
		{
			var newType = type.DataType switch
			{
				DataType.SByte => DataType.Byte,
				DataType.Int16 => DataType.UInt16,
				DataType.Int32 => DataType.UInt32,
				DataType.Int64 => DataType.UInt64,
				DataType.Int128 => DataType.UInt128,
				DataType.Int256 => DataType.UInt256,
				_ => throw new InvalidOperationException($"Unsigned DB type expected: {type}")
			};

			return type.WithDataType(newType);
		}

		/// <summary>
		/// Returns true, if type represents text/string database type.
		/// </summary>
		internal static bool IsTextType(this DbDataType type)
		{
			// TODO: such information should be moved to type system in future probably
			// and if needed handle type names too
			return type.DataType.IsTextType();
		}

		/// <summary>
		/// Returns true, if type represents text/string database type.
		/// </summary>
		internal static bool IsTextType(this DataType type)
		{
			return type is DataType.Char
				or DataType.VarChar
				or DataType.Text
				or DataType.NChar
				or DataType.NVarChar
				or DataType.NText
				;
		}

		/// <summary>
		/// Returns true, if type represents signed integer type.
		/// </summary>
		internal static bool IsSignedType(this DataType type)
		{
			return type is DataType.SByte
				or DataType.Int16
				or DataType.Int32
				or DataType.Int64
				or DataType.Int128
				or DataType.Int256
				;
		}

		/// <summary>
		/// Returns true, if type represents unsigned integer type.
		/// </summary>
		internal static bool IsUnsignedType(this DataType type)
		{
			return type is DataType.Byte
				or DataType.UInt16
				or DataType.UInt32
				or DataType.UInt64
				or DataType.UInt128
				or DataType.UInt256
				;
		}
	}
}
