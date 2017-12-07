using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	using LinqToDB.Extensions;
	using Mapping;

	[DebuggerDisplay("SQL = {SqlText}")]
	public class SelectQuery : SqlStatement, ISqlTableSource
	{
		#region Init

		public SelectQuery()
		{
			SourceID = Interlocked.Increment(ref SourceIDCounter);

			Select   = new SqlSelectClause (this);
			From     = new SqlFromClause   (this);
			Where    = new SqlWhereClause  (this);
			_groupBy = new SqlGroupByClause(this);
			_having  = new SqlWhereClause  (this);
			_orderBy = new SqlOrderByClause(this);
		}

		internal SelectQuery(int id)
		{
			SourceID = id;
		}

		internal void Init(
			SqlInsertClause    insert,
			SqlUpdateClause    update,
			SqlDeleteClause    delete,
			SqlSelectClause    select,
			SqlFromClause      from,
			SqlWhereClause     where,
			SqlGroupByClause   groupBy,
			SqlWhereClause     having,
			SqlOrderByClause   orderBy,
			List<SqlUnion>     unions,
			SelectQuery        parentSelect,
			bool               parameterDependent,
			List<SqlParameter> parameters)
		{
			_insert              = insert;
			_update              = update;
			_delete              = delete;
			Select               = select;
			From                 = from;
			Where                = where;
			_groupBy             = groupBy;
			_having              = having;
			_orderBy             = orderBy;
			_unions              = unions;
			ParentSelect         = parentSelect;
			IsParameterDependent = parameterDependent;

			Parameters.AddRange(parameters);

			foreach (var col in select.Columns)
				col.Parent = this;

			Select.  SetSqlQuery(this);
			From.    SetSqlQuery(this);
			Where.   SetSqlQuery(this);
			_groupBy.SetSqlQuery(this);
			_having. SetSqlQuery(this);
			_orderBy.SetSqlQuery(this);
		}

		private List<object> _properties;
		public  List<object>  Properties => _properties ?? (_properties = new List<object>());

		public SelectQuery ParentSelect { get; set; }

		public bool IsSimple => !Select.HasModifier && Where.IsEmpty && GroupBy.IsEmpty && Having.IsEmpty && OrderBy.IsEmpty;

		public override QueryType QueryType => _queryType;

		private QueryType _queryType = QueryType.Select;

		public void ChangeQueryType(QueryType newQueryType)
		{
			_queryType = newQueryType;
		}

		public bool IsSelect         => _queryType == QueryType.Select;
		public bool IsDelete         => _queryType == QueryType.Delete;
		public bool IsInsertOrUpdate => _queryType == QueryType.InsertOrUpdate;
		public bool IsInsert         => _queryType == QueryType.Insert || _queryType == QueryType.InsertOrUpdate;
		public bool IsUpdate         => _queryType == QueryType.Update || _queryType == QueryType.InsertOrUpdate;

		#endregion

		#region Temporary Delete

		private SqlDeleteClause _delete;
		public  SqlDeleteClause  Delete => _delete ?? (_delete = new SqlDeleteClause());

		public void ClearDelete()
		{
			_delete = null;
		}

		#endregion

		#region SelectClause

		public SqlSelectClause  Select { get; set; }

		#endregion

		#region InsertClause

		private SqlInsertClause _insert;
		public  SqlInsertClause  Insert
		{
			get { return _insert ?? (_insert = new SqlInsertClause()); }
		}

		public void ClearInsert()
		{
			_insert = null;
		}

		#endregion

		#region UpdateClause

		private SqlUpdateClause _update;
		public  SqlUpdateClause  Update
		{
			get { return _update ?? (_update = new SqlUpdateClause()); }
		}

		public void ClearUpdate()
		{
			_update = null;
		}

		#endregion

		#region FromClause

		public  SqlFromClause  From { get; set; }

		#endregion

		#region WhereClause

		public  SqlWhereClause  Where { get; set; }

		#endregion

#region GroupByClause

		private SqlGroupByClause _groupBy;
		public  SqlGroupByClause  GroupBy
		{
			get { return _groupBy; }
		}

#endregion

#region HavingClause

		private SqlWhereClause _having;
		public  SqlWhereClause  Having
		{
			get { return _having; }
		}

#endregion

#region OrderByClause

		private SqlOrderByClause _orderBy;
		public  SqlOrderByClause  OrderBy
		{
			get { return _orderBy; }
		}

#endregion

#region Union

		private List<SqlUnion> _unions;
		public  List<SqlUnion>  Unions
		{
			get { return _unions ?? (_unions = new List<SqlUnion>()); }
		}

		public bool HasUnion { get { return _unions != null && _unions.Count > 0; } }

		public void AddUnion(SelectQuery union, bool isAll)
		{
			Unions.Add(new SqlUnion(union, isAll));
		}

#endregion

#region ProcessParameters

		public override SqlStatement ProcessParameters(MappingSchema mappingSchema)
		{
			if (IsParameterDependent)
			{
				var query = new QueryVisitor().Convert(this, e =>
				{
					switch (e.ElementType)
					{
						case QueryElementType.SqlParameter :
							{
								var p = (SqlParameter)e;

								if (p.Value == null)
									return new SqlValue(null);
							}

							break;

						case QueryElementType.ExprExprPredicate :
							{
								var ee = (SqlPredicate.ExprExpr)e;

								if (ee.Operator == SqlPredicate.Operator.Equal || ee.Operator == SqlPredicate.Operator.NotEqual)
								{
									object value1;
									object value2;

									if (ee.Expr1 is SqlValue)
										value1 = ((SqlValue)ee.Expr1).Value;
									else if (ee.Expr1 is SqlParameter)
										value1 = ((SqlParameter)ee.Expr1).Value;
									else
										break;

									if (ee.Expr2 is SqlValue)
										value2 = ((SqlValue)ee.Expr2).Value;
									else if (ee.Expr2 is SqlParameter)
										value2 = ((SqlParameter)ee.Expr2).Value;
									else
										break;

									var value = Equals(value1, value2);

									if (ee.Operator == SqlPredicate.Operator.NotEqual)
										value = !value;

									return new SqlPredicate.Expr(new SqlValue(value), SqlQuery.Precedence.Comparison);
								}
							}

							break;

						case QueryElementType.InListPredicate :
							return ConvertInListPredicate(mappingSchema, (SqlPredicate.InList)e);
					}

					return null;
				});

				if (query != this)
				{
					query.Parameters.Clear();

					new QueryVisitor().VisitAll(query, expr =>
					{
						switch (expr.ElementType)
						{
							case QueryElementType.SqlParameter :
								{
									var p = (SqlParameter)expr;
									if (p.IsQueryParameter)
										query.Parameters.Add(p);

									break;
								}
						}
					});
				}

				return query;
			}

			return this;
		}

		static SqlPredicate ConvertInListPredicate(MappingSchema mappingSchema, SqlPredicate.InList p)
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
							var field  = GetUnderlayingField(keys[0]);
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
									var field = GetUnderlayingField(key);
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

		static SqlField GetUnderlayingField(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlField: return (SqlField)expr;
				case QueryElementType.Column  : return GetUnderlayingField(((SqlColumn)expr).Expression);
			}

			throw new InvalidOperationException();
		}

#endregion

#region Clone

		SelectQuery(SelectQuery clone, Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			objectTree.Add(clone,     this);
			objectTree.Add(clone.All, All);

			SourceID = Interlocked.Increment(ref SourceIDCounter);

			ICloneableElement parentClone;

			if (clone.ParentSelect != null)
				ParentSelect = objectTree.TryGetValue(clone.ParentSelect, out parentClone) ? (SelectQuery)parentClone : clone.ParentSelect;

			_queryType = clone._queryType;

			if (IsInsert) _insert = (SqlInsertClause)clone._insert.Clone(objectTree, doClone);
			if (IsUpdate) _update = (SqlUpdateClause)clone._update.Clone(objectTree, doClone);
			if (IsDelete) _delete = (SqlDeleteClause)clone._delete.Clone(objectTree, doClone);

			Select  = new SqlSelectClause (this, clone.Select,  objectTree, doClone);
			From    = new SqlFromClause   (this, clone.From,    objectTree, doClone);
			Where   = new SqlWhereClause  (this, clone.Where,   objectTree, doClone);
			_groupBy = new SqlGroupByClause(this, clone._groupBy, objectTree, doClone);
			_having  = new SqlWhereClause  (this, clone._having,  objectTree, doClone);
			_orderBy = new SqlOrderByClause(this, clone._orderBy, objectTree, doClone);

			Parameters.AddRange(clone.Parameters.Select(p => (SqlParameter)p.Clone(objectTree, doClone)));
			IsParameterDependent = clone.IsParameterDependent;

			new QueryVisitor().Visit(this, expr =>
			{
				var sb = expr as SelectQuery;

				if (sb != null && sb.ParentSelect == clone)
					sb.ParentSelect = this;
			});
		}

		public SelectQuery Clone()
		{
			return (SelectQuery)Clone(new Dictionary<ICloneableElement,ICloneableElement>(), _ => true);
		}

		public SelectQuery Clone(Predicate<ICloneableElement> doClone)
		{
			return (SelectQuery)Clone(new Dictionary<ICloneableElement,ICloneableElement>(), doClone);
		}

#endregion

#region Helpers

		public void ForEachTable(Action<SqlTableSource> action, HashSet<SelectQuery> visitedQueries)
		{
			if (!visitedQueries.Add(this))
				return;

			foreach (var table in From.Tables)
				table.ForEach(action, visitedQueries);

			new QueryVisitor().Visit(this, e =>
			{
				if (e is SelectQuery && e != this)
					((SelectQuery)e).ForEachTable(action, visitedQueries);
			});
		}

		public override ISqlTableSource GetTableSource(ISqlTableSource table)
		{
			var ts = From[table];

//			if (ts == null && IsUpdate && Update.Table == table)
//				return Update.Table;

			return ts == null && ParentSelect != null? ParentSelect.GetTableSource(table) : ts;
		}

		internal static SqlTableSource CheckTableSource(SqlTableSource ts, ISqlTableSource table, string alias)
		{
			if (ts.Source == table && (alias == null || ts.Alias == alias))
				return ts;

			var jt = ts[table, alias];

			if (jt != null)
				return jt;

			if (ts.Source is SelectQuery)
			{
				var s = ((SelectQuery)ts.Source).From[table, alias];

				if (s != null)
					return s;
			}

			return null;
		}

#endregion

#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

#endregion

#region ISqlExpression Members

		public bool CanBeNull
		{
			get { return true; }
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return this == other;
		}

		public int Precedence
		{
			get { return SqlQuery.Precedence.Unknown; }
		}

		public Type SystemType
		{
			get
			{
				if (Select.Columns.Count == 1)
					return Select.Columns[0].SystemType;

				if (From.Tables.Count == 1 && From.Tables[0].Joins.Count == 0)
					return From.Tables[0].SystemType;

				return null;
			}
		}

#endregion

#region ICloneableElement Members

		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			ICloneableElement clone;

			if (!objectTree.TryGetValue(this, out clone))
				clone = new SelectQuery(this, objectTree, doClone);

			return clone;
		}

#endregion

#region ISqlExpressionWalkable Members

		public override ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			if (_insert != null) ((ISqlExpressionWalkable)_insert).Walk(skipColumns, func);
			if (_update != null) ((ISqlExpressionWalkable)_update).Walk(skipColumns, func);
			if (_delete != null) ((ISqlExpressionWalkable)_delete).Walk(skipColumns, func);

			((ISqlExpressionWalkable)Select) .Walk(skipColumns, func);
			((ISqlExpressionWalkable)From)   .Walk(skipColumns, func);
			((ISqlExpressionWalkable)Where)  .Walk(skipColumns, func);
			((ISqlExpressionWalkable)GroupBy).Walk(skipColumns, func);
			((ISqlExpressionWalkable)Having) .Walk(skipColumns, func);
			((ISqlExpressionWalkable)OrderBy).Walk(skipColumns, func);

			if (HasUnion)
				foreach (var union in Unions)
					((ISqlExpressionWalkable)union.SelectQuery).Walk(skipColumns, func);

			return func(this);
		}

#endregion

#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression other)
		{
			return this == other;
		}

#endregion

#region ISqlTableSource Members

		public static int SourceIDCounter;

		public int           SourceID     { get; private set; }
		public SqlTableType  SqlTableType { get { return SqlTableType.Table; } }

		private SqlField _all;
		public  SqlField  All
		{
			get { return _all ?? (_all = new SqlField { Name = "*", PhysicalName = "*", Table = this }); }

			internal set
			{
				_all = value;

				if (_all != null)
					_all.Table = this;
			}
		}

		List<ISqlExpression> _keys;

		public IList<ISqlExpression> GetKeys(bool allIfEmpty)
		{
			if (_keys == null && From.Tables.Count == 1 && From.Tables[0].Joins.Count == 0)
			{
				_keys = new List<ISqlExpression>();

				var q =
					from key in ((ISqlTableSource)From.Tables[0]).GetKeys(allIfEmpty)
					from col in Select.Columns
					where col.Expression == key
					select col as ISqlExpression;

				_keys = q.ToList();
			}

			return _keys;
		}

#endregion

#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.SqlQuery;

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (dic.ContainsKey(this))
				return sb.Append("...");

			dic.Add(this, this);

			sb
				.Append("(")
				.Append(SourceID)
				.Append(") ");

			((IQueryElement)Select). ToString(sb, dic);
			((IQueryElement)From).   ToString(sb, dic);
			((IQueryElement)Where).  ToString(sb, dic);
			((IQueryElement)GroupBy).ToString(sb, dic);
			((IQueryElement)Having). ToString(sb, dic);
			((IQueryElement)OrderBy).ToString(sb, dic);

			if (HasUnion)
				foreach (IQueryElement u in Unions)
					u.ToString(sb, dic);

			dic.Remove(this);

			return sb;
		}

#endregion
	}
}
