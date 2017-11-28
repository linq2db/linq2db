using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlCondition : IQueryElement, ICloneableElement
	{
		public SqlCondition(bool isNot, ISqlPredicate predicate)
		{
			IsNot     = isNot;
			Predicate = predicate;
		}

		public SqlCondition(bool isNot, ISqlPredicate predicate, bool isOr)
		{
			IsNot     = isNot;
			Predicate = predicate;
			IsOr      = isOr;
		}

		public bool          IsNot     { get; set; }
		public ISqlPredicate Predicate { get; set; }
		public bool          IsOr      { get; set; }

		public int Precedence =>
			IsNot ? SqlQuery.Precedence.LogicalNegation :
				IsOr  ? SqlQuery.Precedence.LogicalDisjunction :
					SqlQuery.Precedence.LogicalConjunction;

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			if (!objectTree.TryGetValue(this, out var clone))
				objectTree.Add(this, clone = new SqlCondition(IsNot, (ISqlPredicate)Predicate.Clone(objectTree, doClone), IsOr));

			return clone;
		}

		public bool CanBeNull => Predicate.CanBeNull;

#if OVERRIDETOSTRING

			public override string ToString()
			{
				return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
			}

#endif

		#region IQueryElement Members

		public QueryElementType ElementType => QueryElementType.Condition;

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (dic.ContainsKey(this))
				return sb.Append("...");

			dic.Add(this, this);

			sb.Append('(');

			if (IsNot) sb.Append("NOT ");

			Predicate.ToString(sb, dic);
			sb.Append(')').Append(IsOr ? " OR " : " AND ");

			dic.Remove(this);

			return sb;
		}

		#endregion
	}
}
