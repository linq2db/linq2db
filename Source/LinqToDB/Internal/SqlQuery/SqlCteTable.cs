using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlCteTable : SqlExpressionBase, ISqlNamedTable
	{
		public CteClause? Cte { get; set; }

		public SqlObjectName TableName => new SqlObjectName(Cte?.Name ?? string.Empty);

		public int     SourceID   { get; }
		public string? Alias      { get; set; }
		public Type    ObjectType { get; set; }

		public List<SqlCteTableField>   Fields             { get; }
		public List<SqlQueryExtension>? SqlQueryExtensions { get; set; }

		public SqlField All
		{
			get => field ??= SqlField.All(this);
			set;
		}

		public SqlTableType SqlTableType => SqlTableType.Cte;

		public SqlCteTable(
			CteClause cte,
			Type      entityType)
		{
			SourceID   = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
			Cte        = cte;
			ObjectType = entityType;
			Fields     = new();
		}

		internal SqlCteTable(int id, string alias, SqlCteTableField[] fields, CteClause cte)
		{
			SourceID   = id;
			Alias      = alias;
			Cte        = cte;
			ObjectType = cte.ObjectType;
			Fields     = new(fields.Length);

			foreach (var field in fields)
				Add(field);
		}

		internal SqlCteTable(int id, string alias, SqlCteTableField[] fields)
		{
			SourceID   = id;
			Alias      = alias;
			ObjectType = null!;
			Fields     = new(fields.Length);

			foreach (var field in fields)
				Add(field);
		}

		internal SqlCteTable(int id, string alias, SqlField all, SqlCteTableField[] fields, CteClause? cte)
		{
			SourceID   = id;
			Alias      = alias;
			Cte        = cte;
			ObjectType = cte?.ObjectType ?? null!;
			Fields     = new(fields.Length);
			All        = all;
			All.Table  = this;

			foreach (var field in fields)
				Add(field);
		}

		public SqlCteTable(SqlCteTable table, IEnumerable<SqlCteTableField> fields, CteClause? cte)
		{
			SourceID   = Interlocked.Increment(ref SelectQuery.SourceIDCounter);
			Alias      = table.Alias;
			Cte        = cte;
			ObjectType = cte?.ObjectType ?? table.ObjectType;
			Fields     = new();

			foreach (var field in fields)
				Add(field);
		}

		public void Add(SqlCteTableField field)
		{
			if (field.Table != null) throw new InvalidOperationException("Invalid parent table.");

			field.Table = this;
			Fields.Add(field);
		}

		public IList<ISqlExpression>? GetKeys(bool allIfEmpty)
		{
			if (Cte?.Body == null)
				return null;

			var cteKeys = Cte.Body.GetKeys(allIfEmpty);

			if (!(cteKeys?.Count > 0))
				return cteKeys;

			var hasInvalid = false;
			IList<ISqlExpression> projected = Cte.Body.Select.Columns.Select((c, idx) =>
			{
				var found = cteKeys.FirstOrDefault(k => ReferenceEquals(c, k));
				if (found != null)
				{
					var cteField = Cte.Fields[idx];

					// Direct reference lookup via SqlCteTableField.CteField
					var foundField = Fields.Find(f => ReferenceEquals(f.CteField, cteField));

					// Fallback to name-based lookup
					foundField ??= Fields.Find(f => string.Equals(f.Name, cteField.Name, StringComparison.Ordinal));

					if (foundField == null)
						hasInvalid = true;
					return (foundField as ISqlExpression)!;
				}

				hasInvalid = true;
				return null!;
			}).ToList();

			if (hasInvalid)
				return null;

			return projected;
		}

		internal void SetDelayedCteObject(CteClause cte)
		{
			Cte        = cte;
			ObjectType = cte.ObjectType;
		}

		#region ISqlExpression Members

		public override Type?  SystemType => ObjectType;
		public override int    Precedence => LinqToDB.SqlQuery.Precedence.Unknown;

		public override bool CanBeNullable(NullabilityContext nullability) => false;

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return ReferenceEquals(this, other);
		}

		#endregion

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.SqlCteTable;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append("CteTable(")
				.AppendElement(Cte)
				.Append('[').Append(SourceID).Append(']')
				.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			return RuntimeHelpers.GetHashCode(this);
		}

		public string SqlText => this.ToDebugString();

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlCteTable(this);

		#endregion
	}
}
