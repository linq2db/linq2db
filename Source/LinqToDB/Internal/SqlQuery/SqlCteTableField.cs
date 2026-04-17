using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Field in <see cref="SqlCteTable"/>. Delegates Name, Type, CanBeNullable to the referenced <see cref="SqlCteField"/>.
	/// </summary>
	[DebuggerDisplay("CteTableField({Name}, CteField={CteField?.Name})")]
	public sealed class SqlCteTableField : SqlFieldBase
	{
		public SqlCteTableField(SqlCteField? cteField)
		{
			CteField = cteField;
		}

		public SqlCteTableField(SqlCteTableField field)
		{
			CteField = field.CteField;
		}

		/// <summary>
		/// Direct reference to the corresponding <see cref="SqlCteField"/> in <see cref="CteClause.Fields"/>.
		/// All Name/Type/CanBeNullable are derived from this reference.
		/// </summary>
		public SqlCteField? CteField { get; set; }

		/// <summary>
		/// Back-reference to the owning <see cref="SqlCteTable"/>.
		/// </summary>
		public ISqlTableSource? Table { get; set; }

		public override ISqlNamedTable? NamedTable => Table as ISqlNamedTable;

		/// <summary>
		/// Name delegated to CteField.
		/// </summary>
		public override string Name
		{
			get => CteField?.Name ?? base.Name;
			set => base.Name = value;
		}

		/// <summary>
		/// Type delegated to CteField; SystemType follows automatically via the base.
		/// </summary>
		public override DbDataType Type
		{
			get => CteField?.Type ?? base.Type;
			set => base.Type = value;
		}

		public override bool CanBeNullable(NullabilityContext nullability)
			=> CteField?.CanBeNullable(nullability) ?? true;

		public override QueryElementType ElementType => QueryElementType.SqlCteTableField;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (Table != null)
				writer
					.Append('t')
					.Append(Table.SourceID)
					.Append('.');

			writer.Append(Name);
			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);
			hash.Add(Name);
			return hash.ToHashCode();
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlCteTableField(this);
	}
}
