﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace LinqToDB.SqlQuery
{
	[DebuggerDisplay("SQL = {" + nameof(SqlText) + "}")]
	public class SelectQuery : SqlExpressionBase, ISqlTableSource
	{
		#region Init

		public SelectQuery()
		{
			SourceID = Interlocked.Increment(ref SourceIDCounter);

			Select  = new(this);
			From    = new(this);
			Where   = new(this);
			GroupBy = new(this);
			Having  = new(this);
			OrderBy = new(this);
		}

		internal SelectQuery(int id)
		{
			SourceID = id;
		}

		internal void Init(SqlSelectClause select,
			SqlFromClause                  from,
			SqlWhereClause                 where,
			SqlGroupByClause               groupBy,
			SqlHavingClause                having,
			SqlOrderByClause               orderBy,
			List<SqlSetOperator>?          setOperators,
			List<ISqlExpression[]>?        uniqueKeys,
			bool                           parameterDependent,
			string?                        queryName,
			bool                           doNotSetAliases)
		{
			Select               = select;
			From                 = from;
			Where                = where;
			GroupBy              = groupBy;
			Having               = having;
			OrderBy              = orderBy;
			_setOperators        = setOperators;
			IsParameterDependent = parameterDependent;
			QueryName            = queryName;
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
		public SqlHavingClause  Having  { get; internal set; } = null!;
		public SqlOrderByClause OrderBy { get; internal set; } = null!;

		private List<object>? _properties;
		public  List<object>   Properties => _properties ??= new ();

		public bool           IsSimple         => IsSimpleOrSet && !HasSetOperators;
		public bool           IsSimpleOrSet    => !Select.HasModifier && Where.IsEmpty && GroupBy.IsEmpty && Having.IsEmpty && OrderBy.IsEmpty && From.Tables.Count == 1 && From.Tables[0].Joins.Count == 0;
		public bool           IsSimpleButWhere => !HasSetOperators && !Select.HasModifier && GroupBy.IsEmpty && Having.IsEmpty && OrderBy.IsEmpty && From.Tables.Count == 1 && From.Tables[0].Joins.Count == 0;
		public bool           IsLimited        => Select.SkipValue != null || Select.TakeValue != null;
		public bool           IsParameterDependent { get; set; }

		/// <summary>
		/// Gets or sets flag when sub-query can be removed during optimization.
		/// </summary>
		public bool                     DoNotRemove        { get; set; }
		public string?                  QueryName          { get; set; }
		public List<SqlQueryExtension>? SqlQueryExtensions { get; set; }
		public bool                     DoNotSetAliases    { get; set; }

		List<ISqlExpression[]>? _uniqueKeys;

		/// <summary>
		/// Contains list of columns that build unique key for this sub-query.
		/// Used in JoinOptimizer for safely removing sub-query from resulting SQL.
		/// </summary>
		public List<ISqlExpression[]> UniqueKeys
		{
			get => _uniqueKeys ??= new();
			internal set => _uniqueKeys = value;
		}

		public  bool                   HasUniqueKeys => _uniqueKeys?.Count > 0;

		#endregion

		#region Union

		private List<SqlSetOperator>? _setOperators;
		public  List<SqlSetOperator>  SetOperators
		{
			get => _setOperators ??= new List<SqlSetOperator>();
			internal set => _setOperators = value;
		}

		public bool HasSetOperators => _setOperators != null && _setOperators.Count > 0;

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

			return ts;
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

		public IList<ISqlExpression>? GetKeys(bool allIfEmpty)
		{
			if (Select.Columns.Count > 0 && From.Tables.Count == 1 && From.Tables[0].Joins.Count == 0)
			{
				var tableKeys = ((ISqlTableSource)From.Tables[0]).GetKeys(allIfEmpty);

				return tableKeys;
			}

			return null;
		}

		#endregion

		#region Overrides

		public override QueryElementType ElementType => QueryElementType.SqlQuery;

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			foreach(var column in Select.Columns)
				if (column.CanBeNullable(nullability))
					return true;

			var allAggregation = Select.Columns.All(c => QueryHelper.IsAggregationFunction(c.Expression));
			if (allAggregation)
				return false;

			return true;
		}

		public override int Precedence => SqlQuery.Precedence.Unknown;

		public override bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return ReferenceEquals(this, other);
		}

		public override Type? SystemType
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

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			//writer.DebugAppendUniqueId(this);

			writer
				.Append('(')
				.Append(SourceID)
				.Append(')');

			if (DoNotRemove)
				writer.Append("DNR");

			writer.Append(' ');

			if (QueryName != null)
				writer.AppendFormat("/* {0} */ ", QueryName);

			writer
				.AppendElement(Select)
				.AppendElement(From)
				.AppendElement(Where)
				.AppendElement(GroupBy)
				.AppendElement(Having)
				.AppendElement(OrderBy);

			if (HasSetOperators)
				foreach (IQueryElement u in SetOperators)
					writer.AppendElement(u);

			writer.AppendExtensions(SqlQueryExtensions);

			writer.RemoveVisited(this);

			return writer;
		}

		#endregion

		#region Debug

		public string SqlText => this.ToDebugString(this);

		#endregion

		public void Cleanup()
		{
			Select.Cleanup();
			From.Cleanup();
			Where.Cleanup();
			GroupBy.Cleanup();
			Having.Cleanup();
			OrderBy.Cleanup();
		}

		public SelectQuery CloneQuery()
		{
			return this.Clone(e => ReferenceEquals(e, this));
		}
	}
}
