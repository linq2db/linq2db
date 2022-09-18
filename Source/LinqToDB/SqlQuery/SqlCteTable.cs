using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using System.Diagnostics.CodeAnalysis;
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
			MappingSchema mappingSchema,
			CteClause     cte)
			: base(mappingSchema, cte.ObjectType, cte.Name)
		{
			Cte = cte;

			// CTE has it's own names even there is mapping
			foreach (var field in Fields)
				field.PhysicalName = field.Name;
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

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			Cte?.ToString(sb, dic);
			return sb;
		}

		#region IQueryElement Members

		public string SqlText =>
			((IQueryElement) this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
			.ToString();


		#endregion
	}
}
