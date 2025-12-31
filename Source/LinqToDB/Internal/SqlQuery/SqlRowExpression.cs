using System;
using System.Diagnostics;
using System.Linq;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlRowExpression : SqlExpressionBase
	{
		public SqlRowExpression(ISqlExpression[] values)
		{
			Values = values;
		}

		public ISqlExpression[] Values { get; }

		public override bool CanBeNullable(NullabilityContext nullability)
		{
			// SqlRow doesn't exactly have its own type and nullability, being a collection of values.
			// But it can be null in the sense that `(1, 2) IS NULL` can be true (when all values are null).
			return QueryHelper.CalcCanBeNull(null, null, ParametersNullabilityType.IfAllParametersNullable, Values.Select(v => v.CanBeNullable(nullability)));
		}

		public override int Precedence => LinqToDB.SqlQuery.Precedence.Primary;

		public override Type? SystemType => null;

		public override QueryElementType ElementType => QueryElementType.SqlRow;

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlRowExpression otherRow)
				return false;

			if (otherRow.Values.Length != Values.Length)
				return false;

			for (var i = 0; i < Values.Length; i++)
				if (!Values[i].Equals(otherRow.Values[i], comparer))
					return false;

			return true;
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.Append("Row(");

			for (var index = 0; index < Values.Length; index++)
			{
				var value = Values[index];
				writer.AppendElement(value);
				if (index < Values.Length - 1)
					writer.Append(", ");
			}

			return writer.Append(')');
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(ElementType);

			foreach (var value in Values)
				hash.Add(value.GetElementHashCode());

			return hash.ToHashCode();
		}

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlRow(this);
	}
}
