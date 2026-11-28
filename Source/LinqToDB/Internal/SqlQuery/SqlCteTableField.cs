using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// Field in <see cref="SqlCteTable"/>. Delegates Name and Type to the referenced <see cref="SqlCteField"/>;
	/// nullability also depends on how the owning table is joined (see <see cref="NullabilityContext.CanBeNull"/>).
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
		/// Name and Type are derived from this reference, and so is the column's own nullability.
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

		// The field's own nullability comes from the CTE column, but a reference to it is also null when its CTE
		// table is the outer-joined side of a join: NullabilityContext checks both, as it does for SqlField.
		public override bool CanBeNullable(NullabilityContext nullability)
		{
			return nullability.CanBeNull(this);
		}

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
			// Identity-based hash to match reference-equality semantics in Equals (via SqlFieldBase).
			// Name delegates to CteField.Name which is mutable (aliasing passes update it), so it
			// cannot participate in the hash without violating the hash contract for instances kept
			// in hash-based collections via ISqlExpressionEqualityComparer.
			return RuntimeHelpers.GetHashCode(this);
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlCteTableField(this);
	}
}
