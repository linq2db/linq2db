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
			List<SqlSetOperator>?   setOperators,
			List<ISqlExpression[]>? uniqueKeys,
			SelectQuery?            parentSelect,
			bool                    parameterDependent,
			bool                    doNotSetAliases)
		{
			Select               = select;
			From                 = from;
			Where                = where;
			GroupBy              = groupBy;
			Having               = having;
			OrderBy              = orderBy;
			_setOperators        = setOperators;
			ParentSelect         = parentSelect;
			IsParameterDependent = parameterDependent;
			DoNotSetAliases      = doNotSetAliases;

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

		public SqlSelectClause  Select  { get; internal set; } = null!;
		public SqlFromClause    From    { get; internal set; } = null!;
		public SqlWhereClause   Where   { get; internal set; } = null!;
		public SqlGroupByClause GroupBy { get; internal set; } = null!;
		public SqlWhereClause   Having  { get; internal set; } = null!;
		public SqlOrderByClause OrderBy { get; internal set; } = null!;

		private List<object>? _properties;
		public  List<object>   Properties => _properties ??= new ();

		public SelectQuery?   ParentSelect         { get; set; }
		public bool           IsSimple      => IsSimpleOrSet && !HasSetOperators;
		public bool           IsSimpleOrSet => !Select.HasModifier && Where.IsEmpty && GroupBy.IsEmpty && Having.IsEmpty && OrderBy.IsEmpty;
		public bool           IsParameterDependent { get; set; }

		/// <summary>
		/// Gets or sets flag when sub-query can be removed during optimization.
		/// </summary>
		public bool               DoNotRemove         { get; set; }
		public bool            DoNotSetAliases      { get; set; }

		List<ISqlExpression[]>? _uniqueKeys;

		/// <summary>
		/// Contains list of columns that build unique key for this sub-query.
		/// Used in JoinOptimizer for safely removing sub-query from resulting SQL.
		/// </summary>
		public  List<ISqlExpression[]> UniqueKeys    => _uniqueKeys ??= new ();
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

		#region Helpers

		public void ForEachTable<TContext>(TContext context, Action<TContext, SqlTableSource> action, HashSet<SelectQuery> visitedQueries)
		{
			if (!visitedQueries.Add(this))
				return;

			foreach (var table in From.Tables)
				table.ForEach(context, action, visitedQueries);

			this.Visit((query: this, action, visitedQueries, context), static (context, e) =>
			{
				if (e is SelectQuery query && e != context.query)
					query.ForEachTable(context.context, context.action, context.visitedQueries);
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

		#region ISqlExpressionWalkable Members

		public ISqlExpression Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			((ISqlExpressionWalkable)Select) .Walk(options, context, func);
			((ISqlExpressionWalkable)From)   .Walk(options, context, func);
			((ISqlExpressionWalkable)Where)  .Walk(options, context, func);
			((ISqlExpressionWalkable)GroupBy).Walk(options, context, func);
			((ISqlExpressionWalkable)Having) .Walk(options, context, func);
			((ISqlExpressionWalkable)OrderBy).Walk(options, context, func);

			if (HasSetOperators)
				foreach (var setOperator in SetOperators)
					((ISqlExpressionWalkable)setOperator.SelectQuery).Walk(options, context, func);

			if (HasUniqueKeys)
				foreach (var uk in UniqueKeys)
					foreach (var k in uk)
						k.Walk(options, context, func);

			return func(context, this);
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
			this.Visit(this, static (query, e) =>
			{
				if (e is SqlField f)
				{
					var ts = query.GetTableSource(f.Table!);

					if (ts == null && f != f.Table!.All)
						throw new SqlException("Table '{0}' not found.", f.Table);
				}
			});
		}

		#endregion

	}
}
