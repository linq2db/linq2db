using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using Common;

	[DebuggerDisplay("SQL = {" + nameof(DebugSqlText) + "}")]
	public abstract class SqlStatement: IQueryElement, ISqlExpressionWalkable, ICloneableElement
	{
		public string SqlText =>
			((IQueryElement) this)
				.ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
				.ToString();

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected string DebugSqlText => Tools.ToDebugDisplay(SqlText);

		public abstract QueryType QueryType { get; }

		public List<SqlParameter> Parameters { get; } = new List<SqlParameter>();

		public abstract bool IsParameterDependent { get; set; }

		/// <summary>
		/// Used internally for SQL Builder
		/// </summary>
		public SqlStatement? ParentStatement { get; set; }

		public void CollectParameters()
		{
			var alreadyAdded = new HashSet<SqlParameter>();
			Parameters.Clear();

			new QueryVisitor().VisitAll(this, expr =>
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SqlParameter :
					{
						var p = (SqlParameter)expr;
						if (p.IsQueryParameter && alreadyAdded.Add(p))
							Parameters.Add(p);

						break;
					}
				}
			});
		}

		public abstract SelectQuery? SelectQuery { get; set; }


		#region IQueryElement

		public abstract QueryElementType ElementType { get; }
		public abstract StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic);

		#endregion

		#region IEquatable<ISqlExpression>

		public abstract ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func);

		#endregion

		#region ICloneableElement

		public abstract ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree,
			Predicate<ICloneableElement> doClone);

		#endregion

		public virtual IEnumerable<IQueryElement> EnumClauses()
		{
			yield break;
		}

		#region Aliases

		HashSet<string>? _aliases;

		public void RemoveAlias(string alias)
		{
			_aliases?.Remove(alias);
		}

		public string GetAlias(string desiredAlias, string defaultAlias)
		{
			if (_aliases == null)
				_aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			var alias = desiredAlias;

			if (string.IsNullOrEmpty(desiredAlias) || desiredAlias.Length > 25)
			{
				desiredAlias = defaultAlias;
				alias        = defaultAlias + "1";
			}

			for (var i = 1; ; i++)
			{
				if (!_aliases.Contains(alias) && !ReservedWords.IsReserved(alias))
				{
					_aliases.Add(alias);
					break;
				}

				alias = desiredAlias + i;
			}

			return alias;
		}

		public string[] GetTempAliases(int n, string defaultAlias)
		{
			var aliases = new string[n];

			for (var i = 0; i < aliases.Length; i++)
				aliases[i] = GetAlias(defaultAlias, defaultAlias);

			foreach (var t in aliases)
				RemoveAlias(t);

			return aliases;
		}

		static string? NormalizeParameterName(string? name)
		{
			if (string.IsNullOrEmpty(name))
				return name;

			name = name!.Replace(' ', '_');

			return name;
		}

		internal void PrepareQueryAndAliases()
		{
			_aliases = null;

			var allAliases    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var paramsVisited = new HashSet<SqlParameter>();
			var tablesVisited = new HashSet<SqlTableSource>();

			new QueryVisitor().VisitAll(this, expr =>
			{
				switch (expr.ElementType)
				{
					case QueryElementType.MergeSourceTable:
						{
							var source = (SqlMergeSourceTable)expr;

							Utils.MakeUniqueNames(
								source.SourceFields,
								null,
								(n, a) => !ReservedWords.IsReserved(n),
								f => f.PhysicalName,
								(f, n, a) => { f.PhysicalName = n; },
								f =>
								{
									var a = f.PhysicalName;
									return a.IsNullOrEmpty()
										? "c1"
										: a + (a!.EndsWith("_") ? string.Empty : "_") + "1";
								},
								StringComparer.OrdinalIgnoreCase);

							// copy aliases to source query fields
							if (source.SourceQuery != null)
								for (var i = 0; i < source.SourceFields.Count; i++)
									source.SourceQuery.Select.Columns[i].Alias = source.SourceFields[i].PhysicalName;

							break;
						}
					case QueryElementType.SqlQuery:
						{
							var query = (SelectQuery)expr;

							if (query.Select.Columns.Count > 0)
							{
								var isRootQuery = query.ParentSelect == null;

								Utils.MakeUniqueNames(
									query.Select.Columns.Where(c => c.Alias != "*"),
									isRootQuery ? allAliases : null,
									(n, a) => !ReservedWords.IsReserved(n), 
									c => c.Alias, 
									(c, n, a) =>
									{
										a?.Add(n);
										c.Alias = n;
									},
									c =>
									{
										var a = c.Alias;
										return a.IsNullOrEmpty()
											? "c1"
											: a + (a!.EndsWith("_") ? string.Empty : "_") + "1";
									},
									StringComparer.OrdinalIgnoreCase);

								if (query.HasSetOperators)
								{
									for (var i = 0; i < query.Select.Columns.Count; i++)
									{
										var col = query.Select.Columns[i];

										foreach (var t in query.SetOperators)
										{
											var union = t.SelectQuery.Select;
											union.Columns[i].Alias = col.Alias;
										}
									}
								}
							}

							break;
						}
					case QueryElementType.SqlParameter:
						{
							var p = (SqlParameter)expr;
							if (paramsVisited.Add(p))
							{
								p.Name = NormalizeParameterName(p.Name);
							}

							break;
						}
					case QueryElementType.TableSource:
						{
							var table = (SqlTableSource)expr;
							if (tablesVisited.Add(table))
							{
								if (table.Source is SqlTable sqlTable)
									allAliases.Add(sqlTable.PhysicalName!);
							}
							break;
						}
				}
			});

			Utils.MakeUniqueNames(tablesVisited,
				allAliases,
				(n, a) => !a!.Contains(n) && !ReservedWords.IsReserved(n), ts => ts.Alias, (ts, n, a) =>
				{
					a!.Add(n);
					ts.Alias = n;
				},
				ts =>
				{
					var a = ts.Alias;
					return a.IsNullOrEmpty() ? "t1" : a + (a!.EndsWith("_") ? string.Empty : "_") + "1";
				},
				StringComparer.OrdinalIgnoreCase);

			Utils.MakeUniqueNames(
				paramsVisited,
				allAliases,
				(n, a) => !a!.Contains(n) && !ReservedWords.IsReserved(n), p => p.Name, (p, n, a) =>
				{
					a!.Add(n);
					p.Name = n;
				},
				p => p.Name.IsNullOrEmpty() ? "p1" : p.Name + "_1",
				StringComparer.OrdinalIgnoreCase);

			_aliases = allAliases;
		}

		#endregion

		public abstract ISqlTableSource? GetTableSource(ISqlTableSource table);

		public abstract void WalkQueries(Func<SelectQuery, SelectQuery> func);

		internal void EnsureFindTables()
		{
			new QueryVisitor().Visit(this, e =>
			{
				if (e is SqlField f)
				{
					var ts = SelectQuery?.GetTableSource(f.Table!) ?? GetTableSource(f.Table!);

					if (ts == null && f != f.Table!.All)
						throw new SqlException("Table '{0}' not found.", f.Table);
				}
			});
		}

		/// <summary>
		/// Indicates when optimizer can not remove reference for particular table
		/// </summary>
		/// <param name="table"></param>
		/// <returns></returns>
		public virtual bool IsDependedOn(SqlTable table)
		{
			return false;
		}

	}
}
