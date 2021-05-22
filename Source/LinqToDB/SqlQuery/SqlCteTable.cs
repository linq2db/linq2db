using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using Mapping;

	public class SqlCteTable : SqlTable
	{
		public          CteClause? Cte  { get; private set; }

		public override string?    Name
		{
			get => Cte?.Name ?? base.Name;
			set => base.Name = value;
		}

		public override string?    PhysicalName
		{
			get => Cte?.Name ?? base.PhysicalName;
			set => base.PhysicalName = value;
		}

		// required by Clone :-/
		internal string? BaseName         => base.Name;
		internal string? BasePhysicalName => base.PhysicalName;

		public SqlCteTable(
			MappingSchema mappingSchema,
			CteClause     cte)
			: base(mappingSchema, cte.ObjectType, cte.Name)
		{
			Cte = cte ?? throw new ArgumentNullException(nameof(cte));

			// CTE has it's own names even there is mapping
			foreach (var field in Fields)
				field.PhysicalName = field.Name;
		}

		internal SqlCteTable(int id, string alias, SqlField[] fields, CteClause cte)
			: base(id, cte.Name, alias, string.Empty, string.Empty, string.Empty, cte.Name, cte.ObjectType, null, fields, SqlTableType.Cte, null, TableOptions.NotSet)
		{
			Cte = cte ?? throw new ArgumentNullException(nameof(cte));
		}

		internal SqlCteTable(int id, string alias, SqlField[] fields)
			: base(id, null, alias, string.Empty, string.Empty, string.Empty, null, null, null, fields, SqlTableType.Cte, null, TableOptions.NotSet)
		{
		}

		internal void SetDelayedCteObject(CteClause cte)
		{
			Cte          = cte ?? throw new ArgumentNullException(nameof(cte));
			Name         = cte.Name;
			PhysicalName = cte.Name;
			ObjectType   = cte.ObjectType;
		}

		public SqlCteTable(SqlCteTable table, IEnumerable<SqlField> fields, CteClause cte)
		{
			Alias              = table.Alias;
			Server             = table.Server;
			Database           = table.Database;
			Schema             = table.Schema;

			PhysicalName       = table.PhysicalName;
			ObjectType         = table.ObjectType;
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
