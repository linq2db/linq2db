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

	[DebuggerDisplay("SQL = {" + nameof(SqlText) + "}")]
	public class SelectQuery : ISqlTableSource
	{
		#region Init

		public SelectQuery()
		{
			SourceID = Interlocked.Increment(ref SourceIDCounter);

			Select  = new SqlSelectClause (this);
			From    = new SqlFromClause   (this);
			Where   = new SqlWhereClause  (this);
			GroupBy = new SqlGroupByClause(this);
			Having  = new SqlWhereClause  (this);
			OrderBy = new SqlOrderByClause(this);
		}

		internal SelectQuery(int id)
		{
			SourceID = id;
		}

		internal void Init(
			SqlSelectClause         select,
			SqlFromClause           from,
			SqlWhereClause          where,
			SqlGroupByClause        groupBy,
			SqlWhereClause          having,
			SqlOrderByClause        orderBy,
			List<SqlUnion>          unions,
			SelectQuery          parentSelect,
			bool                 parameterDependent)
		{
			Select               = select;
			From                 = from;
			Where                = where;
			GroupBy              = groupBy;
			Having               = having;
			OrderBy              = orderBy;
			_unions              = unions;
			ParentSelect         = parentSelect;
			IsParameterDependent = parameterDependent;

			foreach (var col in select.Columns)
				col.Parent = this;

			Select. SetSqlQuery(this);
			From.   SetSqlQuery(this);
			Where.  SetSqlQuery(this);
			GroupBy.SetSqlQuery(this);
			Having. SetSqlQuery(this);
			OrderBy.SetSqlQuery(this);
		}

		public SqlSelectClause  Select  { get; private set; }
		public SqlFromClause    From    { get; private set; }
		public SqlWhereClause   Where   { get; private set; }
		public SqlGroupByClause GroupBy { get; private set; }
		public SqlWhereClause   Having  { get; private set; }
		public SqlOrderByClause OrderBy { get; private set; }

		private List<object> _properties;
		public  List<object>  Properties => _properties ?? (_properties = new List<object>());

		public SelectQuery ParentSelect         { get; set; }

		public bool IsSimple => !Select.HasModifier && Where.IsEmpty && GroupBy.IsEmpty && Having.IsEmpty && OrderBy.IsEmpty;

		public bool               IsParameterDependent { get; set; }

		#endregion

		#region Union

		private List<SqlUnion> _unions;
		public  List<SqlUnion>  Unions   => _unions ?? (_unions = new List<SqlUnion>());

		public  bool            HasUnion => _unions != null && _unions.Count > 0;

		public void AddUnion(SelectQuery union, bool isAll)
		{
			Unions.Add(new SqlUnion(union, isAll));
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

			Select  = new SqlSelectClause (this, clone.Select,  objectTree, doClone);
			From    = new SqlFromClause   (this, clone.From,    objectTree, doClone);
			Where   = new SqlWhereClause  (this, clone.Where,   objectTree, doClone);
			GroupBy = new SqlGroupByClause(this, clone.GroupBy, objectTree, doClone);
			Having  = new SqlWhereClause  (this, clone.Having,  objectTree, doClone);
			OrderBy = new SqlOrderByClause(this, clone.OrderBy, objectTree, doClone);

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

		public ISqlTableSource GetTableSource(ISqlTableSource table)
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

		public bool CanBeNull => true;
		public int Precedence => SqlQuery.Precedence.Unknown;


		public bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return this == other;
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

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			if (!objectTree.TryGetValue(this, out var clone))
				clone = new SelectQuery(this, objectTree, doClone);

			return clone;
		}

		#endregion

		#region ISqlExpressionWalkable Members

		public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
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

		public int           SourceID { get; }
		public SqlTableType  SqlTableType => SqlTableType.Table;

		private SqlField _all;
		public  SqlField  All
		{
			get => _all ?? (_all = new SqlField { Name = "*", PhysicalName = "*", Table = this });

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

		public QueryElementType ElementType => QueryElementType.SqlQuery;

		public string SqlText =>
			((IQueryElement) this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
			.ToString();

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
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
