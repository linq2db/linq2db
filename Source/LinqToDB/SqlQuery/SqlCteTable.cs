using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery
{
	using Mapping;

	public class SqlCteTable : SqlTable
	{
		[DisallowNull]
		public CteClause? Cte { get; set; }

		public override SqlObjectName TableName
		{
			get => new SqlObjectName(Cte?.Name ?? string.Empty);
			set { }
		}

		public SqlCteTable(
			CteClause           cte, 
            EntityDescriptor    entityDescriptor)
			: base(entityDescriptor, cte.Name)
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

		internal void SetDelayedCteObject(CteClause cte)
		{
			Cte        = cte;
			ObjectType = cte.ObjectType;
		}

		public SqlCteTable(SqlCteTable table, IEnumerable<SqlField> fields, CteClause cte)
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
				.AppendElement(Cte)
				.Append('[').Append(SourceID).Append(']');
			return writer;
		}

		#region IQueryElement Members

		public string SqlText => this.ToDebugString();

		#endregion
	}
}
