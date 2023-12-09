using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public class SqlSearchCondition : SqlExpressionBase, ISqlPredicate, IInvertibleElement
	{
		public SqlSearchCondition(bool isOr)
		{
			IsOr = isOr;
		}

		public SqlSearchCondition(bool isOr, ISqlPredicate predicate) : this(isOr)
		{
			Predicates.Add(predicate);
		}

		public SqlSearchCondition(bool isOr, ISqlPredicate predicate1, ISqlPredicate predicate2) : this(isOr)
		{
			Predicates.Add(predicate1);
			Predicates.Add(predicate2);
		}

		public SqlSearchCondition(bool isOr, IEnumerable<ISqlPredicate> predicates) : this(isOr)
		{
			Predicates.AddRange(predicates);
		}

		public List<ISqlPredicate> Predicates { get; } = new();

		public SqlSearchCondition Add(ISqlPredicate predicate)
		{
			Predicates.Add(predicate);
			return this;
		}

		public bool IsOr { get; set; }

		#region Overrides

		public override QueryElementType ElementType => QueryElementType.SearchCondition;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			foreach (IQueryElement c in Predicates)
				writer.AppendElement(c);

			if (Predicates.Count > 0)
				writer.Length -= 5;

			writer.RemoveVisited(this);

			return writer;
		}

		#endregion

		#region IPredicate Members

		public override int Precedence
		{
			get
			{
				if (Predicates.Count == 0) return SqlQuery.Precedence.Unknown;

				return IsOr ? SqlQuery.Precedence.LogicalDisjunction : SqlQuery.Precedence.LogicalConjunction;
			}
		}

		public override Type SystemType => typeof(bool);

		#endregion

		#region IInvertibleElement Members

		public bool CanInvert()
		{
			return Predicates.Count == 1;
		}

		public IQueryElement Invert()
		{
			if (Predicates.Count == 0)
			{
				return new SqlSearchCondition(!IsOr);
			}

			var newPredicates = Predicates.Select(p => new SqlPredicate.Not(p));

			return new SqlSearchCondition(!IsOr, newPredicates);
		}

		#endregion

		#region ISqlExpression Members

		public override bool CanBeNullable(NullabilityContext nullability) => CanBeNull;

		public bool CanBeNull => false;

		public override bool Equals(ISqlExpression? other)
		{
			return other != null && Equals(other, (e1, e2) => e1.Equals(e2));
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return other is ISqlPredicate otherPredicate
				&& Equals(otherPredicate, comparer);
		}

		public bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			if (other is not SqlSearchCondition otherCondition
				|| Predicates.Count != otherCondition.Predicates.Count || IsOr != otherCondition.IsOr)
			{
				return false;
			}

			for (var i = 0; i < Predicates.Count; i++)
				if (!Predicates[i].Equals(otherCondition.Predicates[i], comparer))
					return false;

			return true;
		}

		#endregion

		public void Deconstruct(out List<ISqlPredicate> predicates)
		{
			predicates = Predicates;
		}
	}
}
