using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlGroupingSet : SqlExpressionBase
	{
		public override QueryElementType ElementType => QueryElementType.GroupingSet;

		public SqlGroupingSet()
		{

		}

		internal SqlGroupingSet(IEnumerable<ISqlExpression> items)
		{
			Items.AddRange(items);
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.Append('(');
			for (int i = 0; i < Items.Count; i++)
			{
				Items[i].ToString(writer);
				if (i < Items.Count - 1)
					writer.Append(", ");
			}

			writer.Append(')');
			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			foreach (var item in Items)
			{
				hash.Add(item.GetElementHashCode());
			}

			return hash.ToHashCode();
		}

		public override bool Equals(ISqlExpression? other)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (!(other is SqlGroupingSet otherSet))
				return false;

			if (Items.Count != otherSet.Items.Count)
				return false;

			for (int i = 0; i < Items.Count; i++)
			{
				if (!Items[i].Equals(otherSet.Items[i]))
					return false;
			}

			return true;
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (this == other)
				return true;

			if (!(other is SqlGroupingSet otherSet))
				return false;

			if (Items.Count != otherSet.Items.Count)
				return false;

			for (int i = 0; i < Items.Count; i++)
			{
				if (!Items[i].Equals(otherSet.Items[i], comparer))
					return false;
			}

			return true;
		}

		public override	bool CanBeNullable(NullabilityContext nullability) => true;

		public override int   Precedence => SqlQuery.Precedence.Primary;
		public override Type? SystemType => typeof(object);

		public List<ISqlExpression> Items { get; } = new List<ISqlExpression>();
	}
}
