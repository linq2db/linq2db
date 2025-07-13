using System;
using System.Collections.Generic;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlTableSource : SqlExpressionBase, ISqlTableSource
	{
#if DEBUG
		readonly int id = System.Threading.Interlocked.Increment(ref SelectQuery.SourceIDCounter);
#endif

		public SqlTableSource(ISqlTableSource source, string? alias)
			: this(source, alias, null)
		{
		}

		public SqlTableSource(ISqlTableSource source, string? alias, params SqlJoinedTable[]? joins)
		{
			Source = source ?? throw new ArgumentNullException(nameof(source));
			_alias = alias;

			if (joins != null)
				Joins.AddRange(joins);
		}

		public SqlTableSource(ISqlTableSource source, string? alias, IEnumerable<SqlJoinedTable> joins, IEnumerable<ISqlExpression[]>? uniqueKeys)
		{
			Source = source ?? throw new ArgumentNullException(nameof(source));
			_alias = alias;

			if (joins != null)
				Joins.AddRange(joins);

			if (uniqueKeys != null)
				UniqueKeys.AddRange(uniqueKeys);
		}

		public ISqlTableSource Source       { get; set; }
		public SqlTableType    SqlTableType => Source.SqlTableType;

		private string? _alias;
		public  string?  Alias
		{
			get
			{
				if (string.IsNullOrEmpty(_alias))
				{
					if (Source is SqlTableSource sqlSource)
						return sqlSource.Alias;

					if (Source is SqlTable sqlTable)
						return sqlTable.Alias;
				}

				return _alias;
			}
			set => _alias = value;
		}

		internal string? RawAlias => _alias;

		private List<ISqlExpression[]>? _uniqueKeys;

		/// <summary>
		/// Contains list of columns that build unique key for <see cref="Source"/>.
		/// Used in JoinOptimizer for safely removing sub-query from resulting SQL.
		/// </summary>
		public  List<ISqlExpression[]>  UniqueKeys    => _uniqueKeys ??= new List<ISqlExpression[]>();

		public  bool                    HasUniqueKeys => _uniqueKeys != null && _uniqueKeys.Count > 0;

		public void Modify(ISqlTableSource source)
		{
			Source = source;
		}

		public SqlTableSource? this[ISqlTableSource table] => this[table, null];

		public SqlTableSource? this[ISqlTableSource table, string? alias]
		{
			get
			{
				foreach (var tj in Joins)
				{
					var t = SelectQuery.CheckTableSource(tj.Table, table, alias);

					if (t != null)
						return t;
				}

				return null;
			}
		}

		public List<SqlJoinedTable> Joins { get; private set; } = new();

		public void ForEach<TContext>(TContext context, Action<TContext, SqlTableSource> action, HashSet<SelectQuery> visitedQueries)
		{
			foreach (var join in Joins)
				join.Table.ForEach(context, action, visitedQueries);

			action(context, this);

			if (Source is SelectQuery query && visitedQueries.Contains(query))
				query.ForEachTable(context, action, visitedQueries);
		}

		public IEnumerable<ISqlTableSource> GetTables()
		{
			yield return Source;

			foreach (var join in Joins)
				foreach (var table in join.Table.GetTables())
					yield return table;
		}

		public int GetJoinNumber()
		{
			var n = Joins.Count;

			foreach (var join in Joins)
				n += join.Table.GetJoinNumber();

			return n;
		}

		#region IEquatable<ISqlExpression> Members

		public override bool Equals(ISqlExpression? other)
		{
			return ReferenceEquals(this, other);
		}

		#endregion

		#region ISqlTableSource Members

		public int       SourceID => Source.SourceID;
		public SqlField  All      => Source.All;

		IList<ISqlExpression>? ISqlTableSource.GetKeys(bool allIfEmpty)
		{
			return Source.GetKeys(allIfEmpty);
		}

		#endregion

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.TableSource;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			if (Source is SelectQuery)
			{
				writer.AppendLine("(");
				using (writer.IndentScope())
					writer.AppendElement(Source);
				writer.AppendLine();
				writer.Append(")");
			}
			else
				writer.AppendElement(Source);

			writer
				.Append(" as t")
				.Append(SourceID)
				.Append(" (")
				.Append(RawAlias ?? "<none>")
				.Append(")");

			using (writer.IndentScope())
			{
				foreach (var join in Joins)
				{
					writer.AppendLine();
					writer.AppendElement(join);
				}
			}

			writer.RemoveVisited(this);

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);
			hash.Add(Source.GetElementHashCode());
			hash.Add(_alias);

			foreach (var join in Joins)
				hash.Add(join.GetElementHashCode());

			if (_uniqueKeys != null)
			{
				foreach (var key in _uniqueKeys)
				{
					foreach (var expr in key)
						hash.Add(expr.GetElementHashCode());
				}
			}

			return hash.ToHashCode();
		}

		#endregion

		#region ISqlExpression Members

		public override bool CanBeNullable(NullabilityContext nullability) => false;

		public override int Precedence => Source.Precedence;
		public override Type? SystemType => Source.SystemType;

		public override bool Equals(ISqlExpression other, Func<ISqlExpression,ISqlExpression,bool> comparer)
		{
			return ReferenceEquals(this, other);
		}

		#endregion

		#region Deconstruct

		public void Deconstruct(out ISqlTableSource source)
		{
			source = Source;
		}

		#endregion
	}
}
