﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public class SqlRowExpression : ISqlExpression
	{
		public SqlRowExpression(ISqlExpression[] values)
		{
			Values = values;
		}

		public ISqlExpression[] Values { get; }

		public bool CanBeNullable(NullabilityContext nullability)
		{
			// SqlRow doesn't exactly have its own type and nullability, being a collection of values.
			// But it can be null in the sense that `(1, 2) IS NULL` can be true (when all values are null).
			return QueryHelper.CalcCanBeNull(null, ParametersNullabilityType.IfAllParametersNullable, Values.Select(v => v.CanBeNullable(nullability)));
		}

		public int Precedence => SqlQuery.Precedence.Primary;

		public Type? SystemType => null;

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public QueryElementType ElementType => QueryElementType.SqlRow;

		public bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			=> other is SqlRowExpression row && Values.Zip(row.Values, comparer).All(x => x);

		public bool Equals([AllowNull] ISqlExpression other)
			=> other is SqlRowExpression row && Values.SequenceEqual(row.Values);

#if OVERRIDETOSTRING
		public override string ToString()
		{
			return this.ToDebugString();
		}
#endif

		public QueryElementTextWriter ToString(QueryElementTextWriter writer)
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
	}
}
