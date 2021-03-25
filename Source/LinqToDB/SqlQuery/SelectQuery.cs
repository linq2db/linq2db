using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	[DebuggerDisplay("SQL = {" + nameof(SqlText) + "}")]
	public class SelectQuery : ISqlTableSource
	{
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		protected string DebugSqlText => SqlText;

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
			List<SqlSetOperator>?   setOparators,
			List<ISqlExpression[]>? uniqueKeys,
			SelectQuery?            parentSelect,
			bool                    parameterDependent)
		{
			Select               = select;
			From                 = from;
			Where                = where;
			GroupBy              = groupBy;
			Having               = having;
			OrderBy              = orderBy;
			_setOperators        = setOparators;
			ParentSelect         = parentSelect;
			IsParameterDependent = parameterDependent;

			if (uniqueKeys != null)
				UniqueKeys.AddRange(uniqueKeys);

			foreach (var col in select.Columns)
				col.Parent = this;

			Select. SetSqlQuery(this);
			From.   SetSqlQuery(this);
			Where.  SetSqlQuery(this);
			GroupBy.SetSqlQuery(this);
			Having. SetSqlQuery(this);
			OrderBy.SetSqlQuery(this);
		}

		public SqlSelectClause  Select  { get; private set; } = null!;
		public SqlFromClause    From    { get; private set; } = null!;
		public SqlWhereClause   Where   { get; private set; } = null!;
		public SqlGroupByClause GroupBy { get; private set; } = null!;
		public SqlWhereClause   Having  { get; private set; } = null!;
		public SqlOrderByClause OrderBy { get; private set; } = null!;

		private List<object>? _properties;
		public  List<object>   Properties => _properties ??= new List<object>();

		public SelectQuery?   ParentSelect         { get; set; }
		public bool           IsSimple => !Select.HasModifier && Where.IsEmpty && GroupBy.IsEmpty && Having.IsEmpty && OrderBy.IsEmpty && !HasSetOperators;
		public bool           IsParameterDependent { get; set; }

		/// <summary>
		/// Gets or sets flag when sub-query can be removed during optimization.
		/// </summary>
		public bool               DoNotRemove         { get; set; }

		private List<ISqlExpression[]>? _uniqueKeys;

		/// <summary>
		/// Contains list of columns that build unique key for this sub-query.
		/// Used in JoinOptimizer for safely removing sub-query from resulting SQL.
		/// </summary>
		public  List<ISqlExpression[]>  UniqueKeys   => _uniqueKeys ??= new List<ISqlExpression[]>();

		public  bool                    HasUniqueKeys => _uniqueKeys != null && _uniqueKeys.Count > 0;


		#endregion

		#region Union

		private List<SqlSetOperator>? _setOperators;
		public  List<SqlSetOperator>   SetOperators => _setOperators ??= new List<SqlSetOperator>();

		public  bool            HasSetOperators    => _setOperators != null && _setOperators.Count > 0;

		public void AddUnion(SelectQuery union, bool isAll)
		{
			SetOperators.Add(new SqlSetOperator(union, isAll ? SetOperation.UnionAll : SetOperation.Union));
		}

		#endregion

		#region Clone

		SelectQuery(SelectQuery clone, Dictionary<ICloneableElement,ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			objectTree.Add(clone,     this);
			objectTree.Add(clone.All, All);

			SourceID = Interlocked.Increment(ref SourceIDCounter);

			if (clone.ParentSelect != null)
				ParentSelect = objectTree.TryGetValue(clone.ParentSelect, out var parentClone) ? (SelectQuery)parentClone : clone.ParentSelect;

			Select  = new SqlSelectClause (this, clone.Select,  objectTree, doClone);
			From    = new SqlFromClause   (this, clone.From,    objectTree, doClone);
			Where   = new SqlWhereClause  (this, clone.Where,   objectTree, doClone);
			GroupBy = new SqlGroupByClause(this, clone.GroupBy, objectTree, doClone);
			Having  = new SqlWhereClause  (this, clone.Having,  objectTree, doClone);
			OrderBy = new SqlOrderByClause(this, clone.OrderBy, objectTree, doClone);

			if (clone.HasSetOperators)
			{
				SetOperators.AddRange(
					clone.SetOperators.Select(u =>
						new SqlSetOperator((SelectQuery) u.SelectQuery.Clone(objectTree, doClone), u.Operation)));
			}

			IsParameterDependent = clone.IsParameterDependent;

			if (clone.HasUniqueKeys)
				UniqueKeys.AddRange(clone.UniqueKeys.Select(uk => uk.Select(e => (ISqlExpression)e.Clone(objectTree, doClone)).ToArray()));

			new QueryVisitor().Visit(this, expr =>
			{
				if (expr is SelectQuery sb && sb.ParentSelect == clone)
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
				if (e is SelectQuery query && e != this)
					query.ForEachTable(action, visitedQueries);
			});
		}

		public ISqlTableSource? GetTableSource(ISqlTableSource table)
		{
			var ts = From[table];

			return ts == null && ParentSelect != null ? ParentSelect.GetTableSource(table) : ts;
		}

		internal static SqlTableSource? CheckTableSource(SqlTableSource ts, ISqlTableSource table, string? alias)
		{
			if (ts.Source == table && (alias == null || ts.Alias == alias))
				return ts;

			var jt = ts[table, alias];

			if (jt != null)
				return jt;

			if (ts.Source is SelectQuery query)
			{
				var s = query.From[table, alias];

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

		public Type? SystemType
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
				clone = new SelectQuery(this, objectTree, doClone) { DoNotRemove = DoNotRemove };

			return clone;
		}

		#endregion

		#region ISqlExpressionWalkable Members

		public ISqlExpression Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			((ISqlExpressionWalkable)Select) .Walk(options, func);
			((ISqlExpressionWalkable)From)   .Walk(options, func);
			((ISqlExpressionWalkable)Where)  .Walk(options, func);
			((ISqlExpressionWalkable)GroupBy).Walk(options, func);
			((ISqlExpressionWalkable)Having) .Walk(options, func);
			((ISqlExpressionWalkable)OrderBy).Walk(options, func);

			if (HasSetOperators)
				foreach (var setOperator in SetOperators)
					((ISqlExpressionWalkable)setOperator.SelectQuery).Walk(options, func);

			if (HasUniqueKeys)
				foreach (var uk in UniqueKeys)
					foreach (var k in uk)
						k.Walk(options, func);

			return func(this);
		}

		#endregion

		#region IEquatable<ISqlExpression> Members

		bool IEquatable<ISqlExpression>.Equals(ISqlExpression? other)
		{
			return this == other;
		}

		#endregion

		#region ISqlTableSource Members

		public static int SourceIDCounter;

		public int           SourceID { get; }
		public SqlTableType  SqlTableType => SqlTableType.Table;

		private SqlField? _all;
		public  SqlField   All
		{
			get => _all ??= SqlField.All(this);

			internal set
			{
				_all = value;

				if (_all != null)
					_all.Table = this;
			}
		}

		List<ISqlExpression>? _keys;

		public IList<ISqlExpression> GetKeys(bool allIfEmpty)
		{
			if (_keys == null)
			{
				if (Select.Columns.Count > 0 && From.Tables.Count == 1 && From.Tables[0].Joins.Count == 0)
				{
					var q =
						from key in ((ISqlTableSource) From.Tables[0]).GetKeys(allIfEmpty)
						from col in Select.Columns
						where  col.Expression == key
						select col as ISqlExpression;

					_keys = q.ToList();
				}
				else
					_keys = new List<ISqlExpression>();
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
				.Append('(')
				.Append(SourceID)
				.Append(") ");

			((IQueryElement)Select). ToString(sb, dic);
			((IQueryElement)From).   ToString(sb, dic);
			((IQueryElement)Where).  ToString(sb, dic);
			((IQueryElement)GroupBy).ToString(sb, dic);
			((IQueryElement)Having). ToString(sb, dic);
			((IQueryElement)OrderBy).ToString(sb, dic);

			if (HasSetOperators)
				foreach (IQueryElement u in SetOperators)
					u.ToString(sb, dic);

			dic.Remove(this);

			return sb;
		}

		#endregion

		#region Debug

		internal void EnsureFindTables()
		{
			new QueryVisitor().Visit(this, e =>
			{
				if (e is SqlField f)
				{
					var ts = GetTableSource(f.Table!);

					if (ts == null && f != f.Table!.All)
						throw new SqlException("Table '{0}' not found.", f.Table);
				}
			});
		}

		#endregion

	}
}
