using System;

namespace LinqToDB.SqlQuery
{
	public class SqlCondition : IQueryElement
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

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif
		public QueryElementType ElementType => QueryElementType.Condition;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			writer.Append('(');

			if (IsNot) writer.Append("NOT ");

			writer
				.AppendElement(Predicate)
				.Append(')').Append(IsOr ? "  OR " : " AND ");

			writer.RemoveVisited(this);

			return writer;
		}

		#endregion

		public bool Equals(SqlCondition other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return IsNot == other.IsNot
				&& IsOr  == other.IsOr
				&& Predicate.Equals(other.Predicate, comparer);
		}

		public void Deconstruct(out bool isNot, out ISqlPredicate predicate, out bool isOr)
		{
			isNot     = IsNot;
			predicate = Predicate;
			isOr      = IsOr;
		}
	}
}
