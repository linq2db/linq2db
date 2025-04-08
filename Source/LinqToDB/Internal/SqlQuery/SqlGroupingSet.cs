using System;
using System.Collections.Generic;

namespace LinqToDB.Internal.SqlQuery
{
	public class SqlGroupingSet : ISqlExpression
	{
#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public QueryElementType ElementType => QueryElementType.GroupingSet;

		public SqlGroupingSet()
		{

		}

		internal SqlGroupingSet(IEnumerable<ISqlExpression> items)
		{
			Items.AddRange(items);
		}

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		public QueryElementTextWriter ToString(QueryElementTextWriter writer)
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

		public bool Equals(ISqlExpression? other)
		{
			if (this == other)
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

		public bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
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

		public bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public bool  CanBeNull  => true;
		public int   Precedence => LinqToDB.SqlQuery.Precedence.Primary;
		public Type? SystemType => typeof(object);

		public List<ISqlExpression> Items { get; } = new List<ISqlExpression>();
	}
}
