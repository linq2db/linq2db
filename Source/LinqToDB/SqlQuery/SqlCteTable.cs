using System;
using System.Collections.Generic;
using System.Text;
using LinqToDB.Mapping;

namespace LinqToDB.SqlQuery
{
	public class SqlCteTable : SqlTable
	{
		[JetBrains.Annotations.NotNull]
		public          CteClause Cte  { get; private set; }

		public override string    Name
		{
			get => Cte?.Name ?? base.Name;
			set => base.Name = value;
		}

		public override string    PhysicalName
		{
			get => Cte?.Name ?? base.PhysicalName;
			set => base.PhysicalName = value;
		}

		public SqlCteTable(
			[JetBrains.Annotations.NotNull] MappingSchema mappingSchema,
			[JetBrains.Annotations.NotNull] CteClause cte) : base(mappingSchema, cte.ObjectType, cte.Name)
		{
			Cte = cte ?? throw new ArgumentNullException(nameof(cte));

			// CTE has it's own names even there is mapping
			foreach (var field in Fields.Values)
				field.PhysicalName = field.Name;
		}

		internal SqlCteTable(int id, string alias, SqlField[] fields, [JetBrains.Annotations.NotNull] CteClause cte)
			: base(id, cte.Name, alias, string.Empty, string.Empty, cte.Name, cte.ObjectType, null, fields, SqlTableType.Cte, null)
		{
			Cte = cte ?? throw new ArgumentNullException(nameof(cte));
		}

		internal SqlCteTable(int id, string alias, SqlField[] fields)
			: base(id, null, alias, string.Empty, string.Empty, null, null, null, fields, SqlTableType.Cte, null)
		{
		}

		internal void SetDelayedCteObject([JetBrains.Annotations.NotNull] CteClause cte)
		{
			Cte          = cte ?? throw new ArgumentNullException(nameof(cte));
			Name         = cte.Name;
			PhysicalName = cte.Name;
			ObjectType   = cte.ObjectType;
		}

		public SqlCteTable(SqlCteTable table, IEnumerable<SqlField> fields, CteClause cte)
		{
			Alias              = table.Alias;
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

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb.Append(Name);
		}

		#region IQueryElement Members

		public string SqlText =>
			((IQueryElement) this).ToString(new StringBuilder(), new Dictionary<IQueryElement, IQueryElement>())
			.ToString();


		#endregion
	}
}
