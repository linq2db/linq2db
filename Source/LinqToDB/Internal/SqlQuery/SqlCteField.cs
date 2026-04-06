using System;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	[DebuggerDisplay("CteField({Name}, {Type})")]
	public sealed class SqlCteField : SqlFieldBase
	{
		public SqlCteField(DbDataType type, string? name, bool canBeNull)
		{
			Type      = type;
			Name      = name!;
			CanBeNull = canBeNull;
		}

		public SqlCteField(SqlCteField field)
		{
			Type      = field.Type;
			Name      = field.Name;
			CanBeNull = field.CanBeNull;
			Column    = field.Column;
		}

		/// <summary>
		/// Direct reference to the corresponding column in <see cref="CteClause.Body"/>.<see cref="SelectQuery.Select"/>.<see cref="SqlSelectClause.Columns"/>.
		/// Can be null during recursive CTE construction when the body is not yet built.
		/// </summary>
		public SqlColumn? Column { get; set; }

		public override bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public override QueryElementType ElementType => QueryElementType.SqlCteField;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.DebugAppendUniqueId(this)
				.Append("CteField(")
				.Append(Name);

			if (CanBeNull)
				writer.Append('?');

			writer.Append(')');

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);
			hash.Add(Name);
			hash.Add(CanBeNull);
			return hash.ToHashCode();
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlCteField(this);
	}
}
