using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	using Mapping;
	using Common;
	using LinqToDB.Extensions;

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

		public SqlStatement ProcessParameters(MappingSchema mappingSchema)
		{
			if (IsParameterDependent)
			{
				var statement = new ConvertVisitor().Convert(this, e =>
				{
					switch (e.ElementType)
					{
						case QueryElementType.SqlParameter :
							{
								var p = (SqlParameter)e;

								if (p.Value == null)
									return new SqlValue(p.Type, null);
							}

							break;

						case QueryElementType.ExprExprPredicate :
							{
								var ee = (SqlPredicate.ExprExpr)e;

								if (ee.Operator == SqlPredicate.Operator.Equal || ee.Operator == SqlPredicate.Operator.NotEqual)
								{
									object? value1;
									object? value2;

									if (ee.Expr1 is SqlValue v1)
										value1 = v1.Value;
									else if (ee.Expr1 is SqlParameter p1)
										value1 = p1.Value;
									else
										break;

									if (ee.Expr2 is SqlValue v2)
										value2 = v2.Value;
									else if (ee.Expr2 is SqlParameter p2)
										value2 = p2.Value;
									else
										break;

									var value = Equals(value1, value2);

									if (ee.Operator == SqlPredicate.Operator.NotEqual)
										value = !value;

									return new SqlPredicate.Expr(new SqlValue(value), Precedence.Comparison);
								}
							}

							break;

						case QueryElementType.InListPredicate :
							return ConvertInListPredicate(mappingSchema, (SqlPredicate.InList)e)!;
					}

					return e;
				});

				return statement;
			}

			return this;
		}

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

		static SqlField GetUnderlyingField(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlField: return (SqlField)expr;
				case QueryElementType.Column  : return GetUnderlyingField(((SqlColumn)expr).Expression);
			}

			throw new InvalidOperationException();
		}

		static SqlPredicate? ConvertInListPredicate(MappingSchema mappingSchema, SqlPredicate.InList p)
		{
			if (p.Values == null || p.Values.Count == 0)
				return new SqlPredicate.Expr(new SqlValue(p.IsNot));

			if (p.Values.Count == 1 && p.Values[0] is SqlParameter)
			{
				var pr = (SqlParameter)p.Values[0];

				if (pr.Value == null)
					return new SqlPredicate.Expr(new SqlValue(p.IsNot));

				if (pr.Value is IEnumerable)
				{
					var items = (IEnumerable)pr.Value;

					if (p.Expr1 is ISqlTableSource)
					{
						var table = (ISqlTableSource)p.Expr1;
						var keys  = table.GetKeys(true);

						if (keys == null || keys.Count == 0)
							throw new SqlException("Cant create IN expression.");

						if (keys.Count == 1)
						{
							var values = new List<ISqlExpression>();
							var field  = GetUnderlyingField(keys[0]);
							var cd     = field.ColumnDescriptor;

							foreach (var item in items)
							{
								var value = cd.MemberAccessor.GetValue(item);
								values.Add(mappingSchema.GetSqlValue(cd.MemberType, value));
							}

							if (values.Count == 0)
								return new SqlPredicate.Expr(new SqlValue(p.IsNot));

							return new SqlPredicate.InList(keys[0], p.IsNot, values);
						}

						{
							var sc = new SqlSearchCondition();

							foreach (var item in items)
							{
								var itemCond = new SqlSearchCondition();

								foreach (var key in keys)
								{
									var field = GetUnderlyingField(key);
									var cd    = field.ColumnDescriptor;
									var value = cd.MemberAccessor.GetValue(item);
									var cond  = value == null ?
										new SqlCondition(false, new SqlPredicate.IsNull  (field, false)) :
										new SqlCondition(false, new SqlPredicate.ExprExpr(field, SqlPredicate.Operator.Equal, mappingSchema.GetSqlValue(value)));

									itemCond.Conditions.Add(cond);
								}

								sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.Expr(itemCond), true));
							}

							if (sc.Conditions.Count == 0)
								return new SqlPredicate.Expr(new SqlValue(p.IsNot));

							if (p.IsNot)
								return new SqlPredicate.NotExpr(sc, true, SqlQuery.Precedence.LogicalNegation);

							return new SqlPredicate.Expr(sc, SqlQuery.Precedence.LogicalDisjunction);
						}
					}

					if (p.Expr1 is ObjectSqlExpression)
					{
						var expr = (ObjectSqlExpression)p.Expr1;

						if (expr.Parameters.Length == 1)
						{
							var values = new List<ISqlExpression>();

							foreach (var item in items)
							{
								var value = expr.GetValue(item, 0);
								values.Add(new SqlValue(value));
							}

							if (values.Count == 0)
								return new SqlPredicate.Expr(new SqlValue(p.IsNot));

							return new SqlPredicate.InList(expr.Parameters[0], p.IsNot, values);
						}

						var sc = new SqlSearchCondition();

						foreach (var item in items)
						{
							var itemCond = new SqlSearchCondition();

							for (var i = 0; i < expr.Parameters.Length; i++)
							{
								var sql   = expr.Parameters[i];
								var value = expr.GetValue(item, i);
								var cond  = value == null ?
									new SqlCondition(false, new SqlPredicate.IsNull  (sql, false)) :
									new SqlCondition(false, new SqlPredicate.ExprExpr(sql, SqlPredicate.Operator.Equal, new SqlValue(value)));

								itemCond.Conditions.Add(cond);
							}

							sc.Conditions.Add(new SqlCondition(false, new SqlPredicate.Expr(itemCond), true));
						}

						if (sc.Conditions.Count == 0)
							return new SqlPredicate.Expr(new SqlValue(p.IsNot));

						if (p.IsNot)
							return new SqlPredicate.NotExpr(sc, true, SqlQuery.Precedence.LogicalNegation);

						return new SqlPredicate.Expr(sc, SqlQuery.Precedence.LogicalDisjunction);
					}
				}
			}

			return null;
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

		internal void UpdateIsParameterDepended()
		{
			if (IsParameterDependent)
				return;

			var isDepended = null != new QueryVisitor().Find(this, expr =>
			{
				if (expr.ElementType == QueryElementType.SqlParameter)
				{
					var p = (SqlParameter)expr;

					if (!p.IsQueryParameter)
					{
						return true;
					}
				}

				return false;
			});

			IsParameterDependent = isDepended;
		}

		internal void SetAliases()
		{
			_aliases = null;

			Parameters.Clear();

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
							if (query.IsParameterDependent)
								IsParameterDependent = true;

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

							if (p.IsQueryParameter)
							{
								if (paramsVisited.Add(p))
									Parameters.Add(p);
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
				Parameters,
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
