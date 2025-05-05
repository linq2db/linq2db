﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public class SqlCteTable : SqlTable
	{
		public CteClause? Cte { get; set; }

		public override SqlObjectName TableName
		{
			get => new SqlObjectName(Cte?.Name ?? string.Empty);
			set { }
		}

		public SqlCteTable(
			CteClause cte,
			Type      entityType)
			: base(entityType, null, new SqlObjectName(cte.Name ?? string.Empty))
		{
			Cte = cte;
		}

		internal SqlCteTable(int id, string alias, SqlField[] fields, CteClause cte)
			: base(id, null, alias, new(string.Empty), cte.ObjectType, null, fields, SqlTableType.Cte, null, TableOptions.NotSet, null)
		{
			Cte = cte;
		}

		internal SqlCteTable(int id, string alias, SqlField[] fields)
			: base(id, null, alias, new(string.Empty), null!, null, fields, SqlTableType.Cte, null, TableOptions.NotSet, null)
		{
		}

		public override IList<ISqlExpression>? GetKeys(bool allIfEmpty)
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
					var field = Cte.Fields[idx];

					var foundField = Fields.FirstOrDefault(f => f.Name == field.Name);
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

		public SqlCteTable(SqlCteTable table, IEnumerable<SqlField> fields, CteClause? cte)
			: base(table.ObjectType, null, table.TableName)
		{
			Alias              = table.Alias;
			SequenceAttributes = table.SequenceAttributes;
			Cte                = cte;

			AddRange(fields);
		}

		public override QueryElementType ElementType  => QueryElementType.SqlCteTable;
		public override SqlTableType     SqlTableType => SqlTableType.Cte;

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

		#region IQueryElement Members

		public string SqlText => this.ToDebugString();

		#endregion
	}
}
