using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class AliasesContext
	{
		readonly HashSet<IQueryElement> _aliasesSet = new (Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);

		// Finalized identifier names. Holding the names here (instead of mutating the AST nodes in
		// place) keeps the statement immutable across executions, so the same cached statement can be
		// aliased / rendered concurrently without a data race.
		//
		// Table aliases are keyed by the source's stable SourceID, not object identity: one logical
		// source can be wrapped by several SqlTableSource instances within a single statement (a
		// correlated reference resolves to a different wrapper than the one in the FROM clause), and
		// every wrapper must resolve to the same finalized alias.
		readonly Dictionary<int, string>               _tableAliases  = new ();
		readonly Dictionary<SqlColumn, string?>        _columnAliases = new (Utils.ObjectReferenceEqualityComparer<SqlColumn>.Default);
		// Keyed by the common field base so it covers SqlField, SqlCteTableField (both SqlFieldBase) and
		// SqlCteField (a CTE definition field) - all of which carry a finalized name in the non-mutating model.
		readonly Dictionary<SqlExpressionBase, string> _fieldNames    = new (Utils.ObjectReferenceEqualityComparer<SqlExpressionBase>.Default);

		public void RegisterAliased(IQueryElement element)
		{
			_aliasesSet.Add(element);
		}

		public void RegisterAliased(IReadOnlyCollection<IQueryElement> elements)
		{
			_aliasesSet.AddRange(elements);
		}

		public IReadOnlyCollection<IQueryElement> GetAliased()
		{
			return _aliasesSet;
		}

		#region Finalized names

		public void SetTableAlias (SqlTableSource    tableSource, string alias)  => _tableAliases [tableSource.SourceID] = alias;
		public void SetColumnAlias(SqlColumn         column,      string? alias) => _columnAliases[column]               = alias;
		public void SetFieldName  (SqlExpressionBase field,       string name)   => _fieldNames   [field]                = name;

		/// <summary>
		/// Effective table-source alias: the finalized alias recorded by the aliasing pass, otherwise
		/// derived from the node - resolving a nested table source through this context so finalized
		/// names are honoured (mirrors SqlTableSource's own derivation, but context-aware).
		/// </summary>
		public string? GetTableAlias(SqlTableSource tableSource)
		{
			if (_tableAliases.TryGetValue(tableSource.SourceID, out var alias))
				return alias;

			if (string.IsNullOrEmpty(tableSource.RawAlias))
			{
				if (tableSource.Source is SqlTableSource source)
					return GetTableAlias(source);
				if (tableSource.Source is SqlTable table)
					return table.Alias;
			}

			return tableSource.RawAlias;
		}

		/// <summary>
		/// Effective column alias: the finalized alias recorded by the aliasing pass, otherwise derived
		/// from the column's expression - resolving sub-expression aliases through this context so
		/// finalized names are honoured (mirrors SqlColumn's own derivation, but context-aware). This
		/// is the crux of non-mutating aliasing: derivation must read finalized names, not stale nodes.
		/// </summary>
		public string? GetColumnAlias(SqlColumn column)
		{
			if (_columnAliases.TryGetValue(column, out var alias))
				return alias;

			return column.RawAlias ?? DeriveAlias(column.Expression);
		}

		string? DeriveAlias(ISqlExpression? expr)
		{
			switch (expr)
			{
				case SqlFieldBase field:
					return GetFieldName(field);
				case SqlColumn column:
					return GetColumnAlias(column);
				case SelectQuery { Select.Columns: [var col] }:
				{
					var a = GetColumnAlias(col);
					return string.Equals(a, "*", StringComparison.Ordinal) ? null : a;
				}
				case SqlExpression { Expr: "{0}", Parameters: [var parameter] }:
					return DeriveAlias(parameter);
				default:
					return null;
			}
		}

		/// <summary>
		/// Effective field physical name: the finalized name recorded by the aliasing pass (for
		/// derived-table / CTE source fields), otherwise the field's own physical name -
		/// <see cref="SqlField.PhysicalName"/> for a table field, or the delegated <c>Name</c> for a
		/// CTE field (<see cref="SqlCteTableField"/> / <see cref="SqlCteField"/>).
		/// </summary>
		public string GetFieldName(SqlExpressionBase field)
			=> _fieldNames.TryGetValue(field, out var name)
				? name
				: field switch
				{
					SqlField f      => f.PhysicalName,
					SqlFieldBase fb => fb.Name,
					SqlCteField cf  => cf.Name,
					_               => throw new InvalidOperationException($"GetFieldName: unexpected field type '{field?.GetType().Name}'."),
				};

		#endregion

		public HashSet<string> GetUsedTableAliases()
		{
			return new(_aliasesSet.Where(e => e.ElementType == QueryElementType.TableSource)
					.Select(e => GetTableAlias((SqlTableSource)e)!),
				StringComparer.OrdinalIgnoreCase);

		}
	}
}
