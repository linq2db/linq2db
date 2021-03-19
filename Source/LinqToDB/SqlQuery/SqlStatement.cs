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

		public abstract bool IsParameterDependent { get; set; }

		/// <summary>
		/// Used internally for SQL Builder
		/// </summary>
		public SqlStatement? ParentStatement { get; set; }

		public SqlParameter[] CollectParameters()
		{
			var parametersHash = new HashSet<SqlParameter>();

			new QueryVisitor().VisitAll(this, expr =>
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SqlParameter :
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

		static string? NormalizeParameterName(string? name)
		{
			if (string.IsNullOrEmpty(name))
				return name;

			name = name!.Replace(' ', '_');
			const string vbPrefix = "$VB$";
			if (name.StartsWith(vbPrefix))
				name = name.Substring(vbPrefix.Length, name.Length - vbPrefix.Length);

			return name;
		}

		public static void PrepareQueryAndAliases(SqlStatement statement, AliasesContext? prevAliasContext, out AliasesContext newAliasContext)
		{
			var allAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			HashSet<string>? allParameterNames = null;

			HashSet<SqlParameter>?   paramsVisited = null;
			HashSet<SqlTableSource>? tablesVisited = null;

			var newAliases = new AliasesContext();

			new QueryVisitor().VisitAll(statement, expr =>
			{
				if (prevAliasContext != null && prevAliasContext.IsAliased(expr))
				{
					// Copy aliased from previous run
					//
					newAliases.RegisterAliased(expr);

					// Remember already used aliases from previous run
					if (expr.ElementType == QueryElementType.TableSource)
					{
						var alias = ((SqlTableSource)expr).Alias;
						if (!string.IsNullOrEmpty(alias))
							allAliases.Add(alias!);
					}
					else if (expr.ElementType == QueryElementType.SqlParameter)
					{
						var alias = ((SqlParameter)expr).Name;
						if (!string.IsNullOrEmpty(alias))
						{
							allParameterNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
							allParameterNames.Add(alias!);
						}
					}

					return;
				}

				switch (expr.ElementType)
				{
					case QueryElementType.MergeSourceTable:
						{
							var source = (SqlTableLikeSource)expr;

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

							newAliases.RegisterAliased(expr);

							break;
						}
					case QueryElementType.SqlQuery:
						{
							var query = (SelectQuery)expr;

							if (query.Select.Columns.Count > 0)
							{
								Utils.MakeUniqueNames(
									query.Select.Columns.Where(c => c.Alias != "*"),
									null,
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

							newAliases.RegisterAliased(query);

							break;
						}
					case QueryElementType.SqlParameter:
						{
							var p = (SqlParameter)expr;
							paramsVisited ??= new HashSet<SqlParameter>();
							if (paramsVisited.Add(p))
							{
								p.Name = NormalizeParameterName(p.Name);
							}

							newAliases.RegisterAliased(expr);

							break;
						}
					case QueryElementType.TableSource:
						{
							var table = (SqlTableSource)expr;
							tablesVisited ??= new HashSet<SqlTableSource>();
							if (tablesVisited.Add(table))
							{
								if (table.Source is SqlTable sqlTable)
									allAliases.Add(sqlTable.PhysicalName!);
							}

							newAliases.RegisterAliased(expr);

							break;
						}
				}
			});

			if (tablesVisited != null)
			{
				Utils.MakeUniqueNames(tablesVisited,
					allAliases,
					(n, a) => !a!.Contains(n) && !ReservedWords.IsReserved(n), ts => ts.Alias, (ts, n, a) =>
					{
						ts.Alias = n;
					},
					ts =>
					{
						var a = ts.Alias;
						return a.IsNullOrEmpty() ? "t1" : a + (a!.EndsWith("_") ? string.Empty : "_") + "1";
					},
					StringComparer.OrdinalIgnoreCase);
			}

			if (paramsVisited != null)
			{
				Utils.MakeUniqueNames(
					paramsVisited,
					allParameterNames,
					(n, a) => a?.Contains(n) != true && !ReservedWords.IsReserved(n), p => p.Name, (p, n, a) =>
					{
						p.Name = n;
					},
					p => p.Name.IsNullOrEmpty() ? "p_1" :
						char.IsDigit(p.Name[p.Name.Length - 1]) ? p.Name : p.Name + "_1",
					StringComparer.OrdinalIgnoreCase);
			}

			newAliasContext = newAliases;
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
