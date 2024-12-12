using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlSearchCondition : SqlExpressionBase, ISqlPredicate
	{
		public SqlSearchCondition(bool isOr = false)
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

		public SqlSearchCondition AddRange(IEnumerable<ISqlPredicate> predicates)
		{
			Predicates.AddRange(predicates);
			return this;
		}

		public bool IsOr  { get; set; }
		public bool IsAnd { get => !IsOr; set => IsOr = !value; }

		#region Overrides

		public override QueryElementType ElementType => QueryElementType.SearchCondition;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			/*writer
			   .Append("sc=")
			   .DebugAppendUniqueId(this);*/

			writer.Append('(');

			var isFirst = true;
			foreach (IQueryElement c in Predicates)
			{
				if (!isFirst)
				{
					if (IsOr)
						writer.Append(" OR ");
					else
						writer.Append(" AND ");
				}
				else
				{
					isFirst = false;
				}

				writer.AppendElement(c);
			}

			writer.RemoveVisited(this);

			writer.Append(')');

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

		public bool InvertIsSimple()
		{
			var moreComplex = Predicates.Count(static p => !p.InvertIsSimple());

			// don't invert if half or more of containing predicates will be more complex
			return moreComplex <= Predicates.Count / 2;
		}

		public ISqlPredicate Invert()
		{
			if (Predicates.Count == 0)
			{
				return new SqlSearchCondition(!IsOr);
			}

			var newPredicates = Predicates.Select(static p => p.Invert());

			return new SqlSearchCondition(!IsOr, newPredicates);
		}

		public bool IsTrue()
		{
			if (Predicates.Count == 0)
				return true;

			if (Predicates is [{ ElementType: QueryElementType.TruePredicate }])
				return true;

			return false;
		}

		public bool IsFalse()
		{
			if (Predicates.Count == 0)
				return false;

			if (Predicates is [{ ElementType: QueryElementType.FalsePredicate }])
				return true;

			return false;
		}

		#endregion

		#region ISqlExpression Members

		public override bool CanBeNullable(NullabilityContext nullability) => false;

		public bool CanBeUnknown(NullabilityContext nullability)
		{
			return Predicates.Any(predicate => predicate.CanBeUnknown(nullability));
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
