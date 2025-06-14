using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Common.Internal;
using LinqToDB.SqlQuery.Visitors;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlSearchCondition : SqlExpressionBase, ISqlPredicate
	{
		public SqlSearchCondition(bool isOr = false, bool? canBeUnknown = null)
		{
			IsOr = isOr;
			CanReturnUnknown = canBeUnknown;
		}

		public SqlSearchCondition(bool isOr, bool? canBeUnknown, ISqlPredicate predicate) : this(isOr, canBeUnknown)
		{
			Predicates.Add(predicate);
		}

		public SqlSearchCondition(bool isOr, bool? canBeUnknown, ISqlPredicate predicate1, ISqlPredicate predicate2) : this(isOr, canBeUnknown)
		{
			Predicates.Add(predicate1);
			Predicates.Add(predicate2);
		}

		public SqlSearchCondition(bool isOr, bool? canBeUnknown, IEnumerable<ISqlPredicate> predicates) : this(isOr, canBeUnknown)
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

		public bool? CanReturnUnknown { get; }

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

		public bool CanInvert(NullabilityContext nullability)
		{
			// TODO: review logic of this method
			var maxCount = Math.Max(Predicates.Count / 2, 2);
			if (Predicates.Count > maxCount)
				return false;

			if (Predicates.Count > 1 && IsAnd)
				return false;

			return Predicates.All(p =>
			{
				if (p is not SqlSearchCondition)
					return false;

				// commented, as check disabled by previous condition
				//if (p is SqlPredicate.ExprExpr exprExpr && (exprExpr.UnknownAsValue != null || exprExpr.UnknownAsValue == true))
				//{
				//	return false;
				//}

				return p.CanInvert(nullability);
			});
		}

		public ISqlPredicate Invert(NullabilityContext nullability)
		{
			if (Predicates.Count == 0)
			{
				return new SqlSearchCondition(!IsOr);
			}

			var newPredicates = Predicates.Select(p => new SqlPredicate.Not(p));

			return new SqlSearchCondition(!IsOr, CanReturnUnknown, newPredicates);
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

		public bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased)
		{
			if (CanReturnUnknown != null)
				return CanReturnUnknown.Value;

			using var visitor = _notNullVisitorPool.Allocate();
			visitor.Value.Collect(this);

			if (visitor.Value.NotNullOverrides?.Count > 0)
			{
				nullability = new NullabilityContext(nullability, visitor.Value.NotNullOverrides);
			}

			return Predicates.Any(predicate => predicate.CanBeUnknown(nullability, withoutUnknownErased));
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

		static readonly ObjectPool<CollectNotNullExpressionsVisitor> _notNullVisitorPool = new(() => new CollectNotNullExpressionsVisitor(), v => v.Cleanup(), 100);

		// AND: when AND contains "expr is not null" predicate, this EXPR will not contribute NULL to result
		// OR: when OR contains "expr is null" predicate, this EXPR will not contribute NULL to result
		sealed class CollectNotNullExpressionsVisitor() : SqlQueryVisitor(VisitMode.ReadOnly, null)
		{
			private bool _isOr;
			public Dictionary<ISqlExpression, bool>? NotNullOverrides;

			public override void Cleanup()
			{
				NotNullOverrides?.Clear();

				base.Cleanup();
			}

			public void Collect(SqlSearchCondition search)
			{
				_isOr = search.IsOr;
				Visit(search);
			}

			[return: NotNullIfNotNull(nameof(element))]
			public override IQueryElement? Visit(IQueryElement? element)
			{
				if (element is not ISqlPredicate)
					return element;

				return base.Visit(element);
			}

			protected override IQueryElement VisitIsNullPredicate(SqlPredicate.IsNull predicate)
			{
				if (predicate.IsNot != _isOr)
#if NET8_0_OR_GREATER
					(NotNullOverrides ??= new(ISqlExpressionEqualityComparer.Instance)).TryAdd(predicate.Expr1, false);
#else
					if (NotNullOverrides?.ContainsKey(predicate.Expr1) != true)
						(NotNullOverrides ??= new(ISqlExpressionEqualityComparer.Instance)).Add(predicate.Expr1, false);
#endif

				return predicate;
			}

			protected override IQueryElement VisitSqlSearchCondition(SqlSearchCondition element)
			{
				if (element.IsOr != _isOr)
					return element;

				return base.VisitSqlSearchCondition(element);
			}
		}
	}
}
