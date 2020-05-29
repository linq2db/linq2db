using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlGroupingSet : ISqlExpression
	{
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
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			sb.Append("(");
			for (int i = 0; i < Items.Count; i++)
			{
				Items[i].ToString(sb, dic);
				if (i < Items.Count - 1)
					sb.Append(", ");
			}
			sb.Append(")");
			return sb;
		}

		public bool Equals(ISqlExpression other)
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

		public ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			for (var i = 0; i < Items.Count; i++)
				Items[i] = Items[i].Walk(options, func)!;

			return func(this);
		}

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			if (!objectTree.TryGetValue(this, out var clone))
			{
				clone = new SqlGroupingSet();
				((SqlGroupingSet)clone).Items.AddRange(Items.Select(i => (ISqlExpression)i.Clone(objectTree, doClone)));
			}

			return clone;
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

		public bool  CanBeNull  => true;
		public int   Precedence => SqlQuery.Precedence.Primary;
		public Type? SystemType => typeof(object);

		public List<ISqlExpression> Items { get; } = new List<ISqlExpression>();
	}
}
