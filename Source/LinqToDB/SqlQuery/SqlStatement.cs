using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using Common;

	using Remote;

	[DebuggerDisplay("SQL = {" + nameof(DebugSqlText) + "}")]
	public abstract class SqlStatement : IQueryElement, ISqlExpressionWalkable, IQueryExtendible
	{
		public string SqlText => this.ToDebugString(SelectQuery);

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected string DebugSqlText => Tools.ToDebugDisplay(SqlText);

		public abstract QueryType QueryType { get; }

		public abstract bool IsParameterDependent { get; set; }

		/// <summary>
		/// Used internally for SQL Builder
		/// </summary>
		public SqlStatement? ParentStatement { get; set; }

		// TODO: V6: used by tests only -> move to test helpers
		[Obsolete("API will be removed in future versions")]
		public SqlParameter[] CollectParameters()
		{
			var parametersHash = new HashSet<SqlParameter>();

			this.VisitAll(parametersHash, static (parametersHash, expr) =>
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SqlParameter:
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

		public SqlComment?              Tag                { get; internal set; }
		public List<SqlQueryExtension>? SqlQueryExtensions { get; set; }

		#region IQueryElement

#if DEBUG
		public virtual string DebugText => this.ToDebugString();
#endif

		public abstract QueryElementType       ElementType { get; }
		public abstract QueryElementTextWriter ToString(QueryElementTextWriter writer);

		#endregion

		#region IEquatable<ISqlExpression>

		public virtual ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			if (SqlQueryExtensions != null)
				foreach (var e in SqlQueryExtensions)
					e.Walk(options, context, func);
			return null;
		}

		#endregion

		#region Aliases

		private sealed class PrepareQueryAndAliasesContext
		{
			public PrepareQueryAndAliasesContext(AliasesContext? prevAliasContext)
			{
				PrevAliasContext = prevAliasContext;
			}

			public HashSet<SqlTableSource>? TablesVisited;

			public readonly AliasesContext? PrevAliasContext;
			public readonly AliasesContext  NewAliases = new ();
			public readonly HashSet<string> AllAliases = new (StringComparer.OrdinalIgnoreCase);
		}

		public static void PrepareQueryAndAliases(SqlStatement statement, AliasesContext? prevAliasContext, out AliasesContext newAliasContext)
		{
			var ctx = new PrepareQueryAndAliasesContext(prevAliasContext);

			statement.VisitAll(ctx, static (context, expr) =>
			{
				if (context.PrevAliasContext != null && context.PrevAliasContext.IsAliased(expr))
				{
					// Copy aliased from previous run
					//
					context.NewAliases.RegisterAliased(expr);

					// Remember already used aliases from previous run
					if (expr.ElementType == QueryElementType.TableSource)
					{
						var alias = ((SqlTableSource)expr).Alias;
						if (!string.IsNullOrEmpty(alias))
							context.AllAliases.Add(alias!);
					}

					return;
				}

				switch (expr.ElementType)
				{
					case QueryElementType.SqlTableLikeSource:
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
									return string.IsNullOrEmpty(a)
										? "c1"
										: a + (a!.EndsWith("_") ? string.Empty : "_") + "1";
								},
								StringComparer.OrdinalIgnoreCase);

							// copy aliases to source query fields
							if (source.SourceQuery != null)
								for (var i = 0; i < source.SourceFields.Count; i++)
									source.SourceQuery.Select.Columns[i].Alias = source.SourceFields[i].PhysicalName;

							context.NewAliases.RegisterAliased(expr);

							break;
						}
					case QueryElementType.SqlQuery:
						{
							var query = (SelectQuery)expr;

							if (query.DoNotSetAliases == false && query.Select.Columns.Count > 0)
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
										return string.IsNullOrEmpty(a)
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

							context.NewAliases.RegisterAliased(query);

							break;
						}
					case QueryElementType.TableSource:
						{
							var table = (SqlTableSource)expr;
							if ((context.TablesVisited ??= new(Utils.ObjectReferenceEqualityComparer<IQueryElement>.Default)).Add(table))
							{
								if (table.Source is SqlTable sqlTable)
									context.AllAliases.Add(sqlTable.TableName.Name);
							}

							context.NewAliases.RegisterAliased(expr);

							break;
						}
				}
			});

			if (ctx.TablesVisited != null)
			{
				Utils.MakeUniqueNames(ctx.TablesVisited,
					ctx.AllAliases,
					(n, a) => !a!.Contains(n) && !ReservedWords.IsReserved(n), ts => ts.Alias, (ts, n, a) =>
					{
						ts.Alias = n;
					},
					ts =>
					{
						var a = ts.Alias;
						return string.IsNullOrEmpty(a) ? "t1" : a + (a!.EndsWith("_") ? string.Empty : "_") + "1";
					},
					StringComparer.OrdinalIgnoreCase);
			}

			newAliasContext = ctx.NewAliases;

			if (statement is SqlUpdateStatement updateStatement)
				updateStatement.AfterSetAliases();
		}

		#endregion

		public abstract ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias);

		internal void EnsureFindTables()
		{
			this.Visit(this, static (statement, e) =>
			{
				if (e is SqlField f)
				{
					var ts = statement.SelectQuery?.GetTableSource(f.Table!) ?? statement.GetTableSource(f.Table!, out _);

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

#if OVERRIDETOSTRING
		public override string ToString()
		{
			return this.ToDebugString(SelectQuery);
		}
#endif

	}
}
