using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Internal.Common;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.DataProvider
{
	public static class AliasesHelper
	{
		static readonly ObjectPool<AliasesVisitor> _aliasesVisitorPool = new(() => new AliasesVisitor(), v => v.Cleanup(), 100);

		#region Aliases

		public static void PrepareQueryAndAliases(IIdentifierService identifierService, SqlStatement statement, AliasesContext? prevAliasContext, out AliasesContext newAliasContext)
		{
			using var visitor = _aliasesVisitorPool.Allocate();

			newAliasContext = visitor.Value.SetAliases(identifierService, statement, prevAliasContext);
		}

		sealed class AliasesVisitor : SqlQueryVisitor
		{
			IIdentifierService _identifierService = default!;
			AliasesContext?    _prevAliases;

			AliasesContext           _newAliases = default!;
			HashSet<string>          _allAliases = default!;
			HashSet<SqlTableSource>? _tablesVisited;
			SelectQuery?             _rootSelectQuery;

			public AliasesVisitor() : base(VisitMode.ReadOnly, null)
			{
			}

			public AliasesContext SetAliases(IIdentifierService identifierService, SqlStatement statement, AliasesContext? prevAliases)
			{
				_identifierService = identifierService;
				_newAliases        = new();
				if (prevAliases != null)
					_newAliases.InheritNamesFrom(prevAliases);
				_allAliases        = new(StringComparer.OrdinalIgnoreCase);
				_tablesVisited     = default;
				_prevAliases       = prevAliases;
				_rootSelectQuery   = statement.SelectQuery;

				Visit(statement);

				if (_tablesVisited != null)
				{
					Utils.MakeUniqueNames(_tablesVisited,
						_allAliases,
						(n, a) => !a!.Contains(n) && IsValidAlias(n),
						GetCurrentAlias,
						(ts, n, a) =>
						{
							_newAliases.SetTableAlias(ts, n);
						},
						ts =>
						{
							var a = GetCurrentAlias(ts);
							return string.IsNullOrEmpty(a) ? "t1" : a + (a!.EndsWith('_') ? string.Empty : "_") + "1";
						},
						StringComparer.OrdinalIgnoreCase);
				}

				string GetCurrentAlias(SqlTableSource tableSource)
				{
					// Read the current alias through the context so already-finalized nested table
					// sources are honoured (non-mutating: names live in the context, not the node).
					var current = _newAliases.GetTableAlias(tableSource);
					return current switch
					{
						"$F" or "$" => current,
						_ => TruncateAlias(current ?? string.Empty),
					};
				}

				return _newAliases;
			}

			public override void Cleanup()
			{
				_identifierService = default!;
				_newAliases        = default!;
				_allAliases        = default!;
				_tablesVisited     = default;
				_prevAliases       = null;
				_rootSelectQuery   = null;

				base.Cleanup();
			}

			public override IQueryElement? Visit(IQueryElement? element)
			{
				if (element != null && _prevAliases != null && _prevAliases.IsAliased(element))
				{
					// Copy aliased from previous run
					//
					_newAliases.RegisterAliased(element);

					// Remember already used aliases from previous run
					if (element.ElementType == QueryElementType.TableSource)
					{
						var alias = _newAliases.GetTableAlias((SqlTableSource)element);
						if (!string.IsNullOrEmpty(alias))
							_allAliases.Add(alias!);
					}

					return element;
				}

				return base.Visit(element);
			}

			string TruncateAlias(string identifier)
			{
				identifier = _identifierService.CorrectAlias(identifier);

				return IdentifiersHelper.TruncateIdentifier(_identifierService, IdentifierKind.Alias, identifier);
			}

			bool IsValidAlias(string identifier)
			{
				var corrected = _identifierService.CorrectAlias(identifier);
				if (!string.Equals(corrected, identifier, StringComparison.Ordinal))
					return true;

				if (ReservedWords.IsReserved(identifier))
					return false;

				if (!_identifierService.IsFit(IdentifierKind.Alias, identifier, out _))
					return false;

				return true;
			}

			// Seed used to uniquify a select column's alias. For a bare entity-field column in the ROOT
			// select, seed from the field's physical column name instead of its member-name alias: the
			// result-set column then keeps its physical name, which is what raw-SQL by-name entity mapping
			// resolves on (providers that force root aliases - SqlCe / YDB - otherwise rename it to the
			// member name and break the mapping). The uniquifier still suffixes genuine duplicates, so the
			// "no duplicate root column names" rule those providers enforce stays satisfied (#5599).
			string? GetColumnAliasSeed(SelectQuery selectQuery, SqlColumn column)
			{
				if (ReferenceEquals(selectQuery, _rootSelectQuery) && column.Expression is SqlField field)
					return field.PhysicalName;

				return _newAliases.GetColumnAlias(column);
			}

			protected internal override IQueryElement VisitSqlTableLikeSource(SqlTableLikeSource element)
			{
				base.VisitSqlTableLikeSource(element);

				Utils.MakeUniqueNames(
					element.SourceFields,
					null,
					(n, a) => IsValidAlias(n),
					f => TruncateAlias(f.PhysicalName),
					(f, n, a) => { _newAliases.SetFieldName(f, n); },
					f =>
					{
						var a = TruncateAlias(f.PhysicalName);
						return string.IsNullOrEmpty(a)
							? "c1"
							: a + (a!.EndsWith('_') ? string.Empty : "_") + "1";
					},
					StringComparer.OrdinalIgnoreCase);

				// copy aliases to source query fields
				if (element.SourceQuery != null)
				{
					for (var i = 0; i < element.SourceFields.Count; i++)
						_newAliases.SetColumnAlias(element.SourceQuery.Select.Columns[i], _newAliases.GetFieldName(element.SourceFields[i]));

					_newAliases.RegisterAliased(element.SourceQuery);
				}

				_newAliases.RegisterAliased(element);

				return element;
			}

			protected internal override IQueryElement VisitCteClause(CteClause element)
			{
				Utils.MakeUniqueNames(
					element.Fields,
					null,
					(n, a) => IsValidAlias(n),
					f => TruncateAlias(f.PhysicalName),
					(f, n, a) =>
					{
						_newAliases.SetFieldName(f, n);
						// do not touch name
					},
					f =>
					{
						var a = TruncateAlias(f.PhysicalName);
						return string.IsNullOrEmpty(a)
							? "f1"
							: a + (a!.EndsWith('_') ? string.Empty : "_") + "1";
					},
					StringComparer.OrdinalIgnoreCase);

				base.VisitCteClause(element);

				// copy aliases to source query fields
				if (element.Body != null)
				{
					for (var i = 0; i < element.Fields.Count; i++)
					{
						var field     = element.Fields[i];
						var fieldName = _newAliases.GetFieldName(field);

						_newAliases.SetColumnAlias(element.Body.Select.Columns[i], fieldName);

						if (element.Body.HasSetOperators)
						{
							foreach (var setOperator in element.Body.SetOperators)
							{
								_newAliases.SetColumnAlias(setOperator.SelectQuery.Select.Columns[i], fieldName);
							}
						}
					}

					_newAliases.RegisterAliased(element.Body);
				}
				
				_newAliases.RegisterAliased(element);

				return element;
			}

			protected internal override IQueryElement VisitSqlCteTable(SqlCteTable element)
			{
				base.VisitSqlCteTable(element);

				if (element.Cte != null)
				{
					for (int i = 0; i < element.Fields.Count; i++)
					{
						var field    = element.Fields[i];
						var cteField = element.Cte.Fields.Find(f => string.Equals(f.Name, field.PhysicalName, StringComparison.Ordinal));
						if (cteField != null)
						{
							var cteName = _newAliases.GetFieldName(cteField);
							if (!string.Equals(_newAliases.GetFieldName(field), cteName, StringComparison.Ordinal))
								_newAliases.SetFieldName(field, cteName);
						}
					}
				}

				_newAliases.RegisterAliased(element);

				return element;
			}

			protected internal override IQueryElement VisitSqlTable(SqlTable element)
			{
				base.VisitSqlTable(element);

				_allAliases.Add(element.TableName.Name);

				return element;
			}

			protected internal override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
			{
				base.VisitSqlQuery(selectQuery);

				if (selectQuery is { DoNotSetAliases: false, Select.Columns.Count: > 0 })
				{
					Utils.MakeUniqueNames(
						selectQuery.Select.Columns.Where(c => !string.Equals(c.Alias, "*", StringComparison.Ordinal)),
						null,
						(n, a) => IsValidAlias(n),
						c => TruncateAlias(GetColumnAliasSeed(selectQuery, c) ?? string.Empty),
						(c, n, a) =>
						{
							a?.Add(n);
							_newAliases.SetColumnAlias(c, n);
						},
						c =>
						{
							var a = TruncateAlias(_newAliases.GetColumnAlias(c) ?? string.Empty);
							return string.IsNullOrEmpty(a)
								? "c1"
								: a + (a!.EndsWith('_') ? string.Empty : "_") + "1";
						},
						StringComparer.OrdinalIgnoreCase);

					if (selectQuery.HasSetOperators)
					{
						for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
						{
							var col      = selectQuery.Select.Columns[i];
							var colAlias = _newAliases.GetColumnAlias(col);

							foreach (var t in selectQuery.SetOperators)
							{
								var union = t.SelectQuery.Select;
								_newAliases.SetColumnAlias(union.Columns[i], colAlias);
							}
						}
					}
				}

				_newAliases.RegisterAliased(selectQuery);

				return selectQuery;
			}

			protected internal override IQueryElement VisitSqlTableSource(SqlTableSource element)
			{
				base.VisitSqlTableSource(element);

				_tablesVisited ??= new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default);
				if (_tablesVisited.Add(element))
				{
					if (element.Source is SqlTable sqlTable)
						_allAliases.Add(sqlTable.TableName.Name);
				}

				return element;
			}
		}

		#endregion
	}
}
