using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.DataProvider
{
	using Common.Internal;
	using Common;
	using SqlQuery;
	using SqlQuery.Visitors;

	public static class AliasesHelper
	{
		static readonly ObjectPool<AliasesVisitor> _aliasesVisitorPool = new(() => new AliasesVisitor(), v => v.Cleanup(), 100);

		#region Aliases

		public static void PrepareQueryAndAliases(IIdentifierService identifierService, SqlStatement statement, AliasesContext? prevAliasContext, out AliasesContext newAliasContext)
		{
			using var visitor = _aliasesVisitorPool.Allocate();

			newAliasContext = visitor.Value.SetAliases(identifierService, statement, prevAliasContext);
		}

		class AliasesVisitor : SqlQueryVisitor
		{
			IIdentifierService _identifierService = default!;
			AliasesContext?    _prevAliases;

			AliasesContext           _newAliases = default!;
			HashSet<string>          _allAliases = default!;
			HashSet<SqlTableSource>? _tablesVisited;

			public AliasesVisitor() : base(VisitMode.ReadOnly, null)
			{
			}

			public AliasesContext SetAliases(IIdentifierService identifierService, SqlStatement statement, AliasesContext? prevAliases)
			{
				_identifierService = identifierService;
				_newAliases        = new();
				_allAliases        = new(StringComparer.OrdinalIgnoreCase);
				_tablesVisited     = default;
				_prevAliases       = prevAliases;

				Visit(statement);

				if (_tablesVisited != null)
				{
					Utils.MakeUniqueNames(_tablesVisited,
						_allAliases,
						(n, a) => !a!.Contains(n) && IsValidAlias(n), 
						GetCurrentAlias, 
						(ts, n, a) =>
						{
							ts.Alias = n;
						},
						ts =>
						{
							var a = GetCurrentAlias(ts);
							return string.IsNullOrEmpty(a) ? "t1" : a + (a!.EndsWith("_") ? string.Empty : "_") + "1";
						},
						StringComparer.OrdinalIgnoreCase);
				}

				string GetCurrentAlias(SqlTableSource tableSource)
				{
					if (tableSource.Alias is ("$F" or "$")) 
						return tableSource.Alias;

					return TruncateAlias(tableSource.Alias ?? string.Empty);
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
						var alias = ((SqlTableSource)element).Alias;
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
				if (corrected != identifier)
					return true;

				if (ReservedWords.IsReserved(identifier))
					return false;

				if (!_identifierService.IsFit(IdentifierKind.Alias, identifier, out _))
					return false;

				return true;
			}

			protected override IQueryElement VisitSqlTableLikeSource(SqlTableLikeSource element)
			{
				base.VisitSqlTableLikeSource(element);

				Utils.MakeUniqueNames(
					element.SourceFields,
					null,
					(n, a) => IsValidAlias(n),
					f => TruncateAlias(f.PhysicalName),
					(f, n, a) => { f.PhysicalName = n; },
					f =>
					{
						var a = TruncateAlias(f.PhysicalName);
						return string.IsNullOrEmpty(a)
							? "c1"
							: a + (a!.EndsWith("_") ? string.Empty : "_") + "1";
					},
					StringComparer.OrdinalIgnoreCase);

				// copy aliases to source query fields
				if (element.SourceQuery != null)
				{
					for (var i = 0; i < element.SourceFields.Count; i++)
						element.SourceQuery.Select.Columns[i].Alias = element.SourceFields[i].PhysicalName;

					_newAliases.RegisterAliased(element.SourceQuery);
				}
				_newAliases.RegisterAliased(element);

				return element;
			}

			protected override IQueryElement VisitCteClause(CteClause element)
			{
				Utils.MakeUniqueNames(
					element.Fields,
					null,
					(n, a) => IsValidAlias(n),
					f => TruncateAlias(f.PhysicalName),
					(f, n, a) =>
					{
						f.PhysicalName = n;
						// do not touch name
					},
					f =>
					{
						var a = TruncateAlias(f.PhysicalName);
						return string.IsNullOrEmpty(a)
							? "f1"
							: a + (a!.EndsWith("_") ? string.Empty : "_") + "1";
					},
					StringComparer.OrdinalIgnoreCase);

				base.VisitCteClause(element);

				// copy aliases to source query fields
				if (element.Body != null)
				{
					for (var i = 0; i < element.Fields.Count; i++)
					{
						var field = element.Fields[i];

						element.Body.Select.Columns[i].Alias = field.PhysicalName;

						if (element.Body.HasSetOperators)
						{
							foreach (var setOperator in element.Body.SetOperators)
							{
								setOperator.SelectQuery.Select.Columns[i].Alias = field.PhysicalName;
							}
						}
					}

					_newAliases.RegisterAliased(element.Body);
				}
				
				_newAliases.RegisterAliased(element);

				return element;
			}

			protected override IQueryElement VisitSqlCteTable(SqlCteTable element)
			{
				base.VisitSqlCteTable(element);

				if (element.Cte != null)
				{
					for (int i = 0; i < element.Fields.Count; i++)
					{
						var field    = element.Fields[i];
						var cteField = element.Cte.Fields.FirstOrDefault(f => f.Name == field.PhysicalName);
						if (cteField != null)
						{
							if (field.PhysicalName != cteField.PhysicalName)
								field.PhysicalName = cteField.PhysicalName;
						}
						else
						{

						}
					}
				}

				_newAliases.RegisterAliased(element);

				return element;
			}

			protected override IQueryElement VisitSqlQuery(SelectQuery selectQuery)
			{
				base.VisitSqlQuery(selectQuery);

				if (selectQuery.DoNotSetAliases == false && selectQuery.Select.Columns.Count > 0)
				{
					Utils.MakeUniqueNames(
						selectQuery.Select.Columns.Where(c => c.Alias != "*"),
						null,
						(n, a) => IsValidAlias(n),
						c => TruncateAlias(c.Alias ?? string.Empty),
						(c, n, a) =>
						{
							a?.Add(n);
							c.Alias = n;
						},
						c =>
						{
							var a = TruncateAlias(c.Alias ?? string.Empty);
							return string.IsNullOrEmpty(a)
								? "c1"
								: a + (a!.EndsWith("_") ? string.Empty : "_") + "1";
						},
						StringComparer.OrdinalIgnoreCase);

					if (selectQuery.HasSetOperators)
					{
						for (var i = 0; i < selectQuery.Select.Columns.Count; i++)
						{
							var col = selectQuery.Select.Columns[i];

							foreach (var t in selectQuery.SetOperators)
							{
								var union = t.SelectQuery.Select;
								union.Columns[i].Alias = col.Alias;
							}
						}
					}
				}

				_newAliases.RegisterAliased(selectQuery);

				return selectQuery;
			}

			protected override IQueryElement VisitSqlTableSource(SqlTableSource element)
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
