using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.SqlQuery.Visitors
{
	sealed class ReduceIsNullExpressionVisitor : SqlQueryVisitor
	{
		public static readonly ObjectPool<ReduceIsNullExpressionVisitor> Pool = new(() => new(), v => v.Cleanup(), 100);

		readonly List<ISqlPredicate> _predicates         = [];
		         NullabilityContext  _nullabilityContext = default!;

		private ReduceIsNullExpressionVisitor()
			: base(VisitMode.ReadOnly, null)
		{
		}

		public override void Cleanup()
		{
			_predicates.Clear();
			_nullabilityContext = default!;

			base.Cleanup();
		}

		[return: NotNullIfNotNull(nameof(element))]
		public override IQueryElement? Visit(IQueryElement? element)
		{
			return element?.ElementType switch
			{
				QueryElementType.SqlNullabilityExpression or
				QueryElementType.SqlBinaryExpression or 
				QueryElementType.SqlCondition or 
				QueryElementType.SqlCast or 
				QueryElementType.SqlFunction or 
				QueryElementType.SqlExpression =>
					base.Visit(element),

				_ => element,
			};
		}

		public IQueryElement Reduce(NullabilityContext  nullabilityContext, SqlPredicate.IsNull predicate)
		{
			_nullabilityContext = nullabilityContext;

			var newExpr = (ISqlExpression)ProcessElement(predicate.Expr1);

			if (_predicates.Count == 0)
			{
				return predicate;
			}
			else if (_predicates.Count == 1)
			{
				return _predicates[0].MakeNot(predicate.IsNot);
			}

			var sc = new SqlSearchCondition(true);
			sc.AddRange(_predicates);
			return sc.MakeNot(predicate.IsNot);
		}

		void AddIsNullCheck(ISqlExpression expr)
		{
			var predicate = new SqlPredicate.IsNull(expr, false);
			_predicates.Add(predicate);
		}

		void ReduceOrAdd(ISqlExpression expr)
		{
			var cnt = _predicates.Count;

			Visit(expr);

			// not reduced, add as-is
			if (_predicates.Count == cnt && expr.CanBeNullable(_nullabilityContext))
			{
				AddIsNullCheck(expr);
			}
		}

		protected internal override IQueryElement VisitSqlConditionExpression(SqlConditionExpression element)
		{
			var trueIsNull  = element.TrueValue.IsNullValue();
			var falseIsNull = element.FalseValue.IsNullValue();

			if (trueIsNull && falseIsNull)
			{
				_predicates.Add(SqlPredicate.True);
			}
			else if (trueIsNull)
			{
				_predicates.Add(element.Condition);

				if (element.FalseValue.CanBeNullable(_nullabilityContext))
					AddIsNullCheck(element.FalseValue);
			}
			else if (falseIsNull)
			{
				if (element.TrueValue.CanBeNullable(_nullabilityContext))
					AddIsNullCheck(element.TrueValue);

				_predicates.Add(element.Condition.MakeNot());
			}

			return element;
		}

		protected internal override IQueryElement VisitSqlFunction(SqlFunction element)
		{
			if (element is { IsAggregate: false, IsPure: true })
			{
				ReduceSqlExpressionBase(element.Parameters, element.NullabilityType);
			}

			return element;
		}

		protected internal override IQueryElement VisitSqlExpression(SqlExpression element)
		{
			if (element is { IsAggregate: false, IsPure: true })
			{
				ReduceSqlExpressionBase(element.Parameters, element.NullabilityType);
			}

			return element;
		}

		void ReduceSqlExpressionBase(ISqlExpression[] parameters, ParametersNullabilityType nullabilityType)
		{
			if (nullabilityType == ParametersNullabilityType.IfAnyParameterNullable)
			{
				foreach (var p in parameters)
					ReduceOrAdd(p);
			}

			if (nullabilityType == ParametersNullabilityType.IfAllParametersNullable)
			{
				var sc = new SqlSearchCondition(false);
				sc.AddRange(parameters.Select(p => new SqlPredicate.IsNull(p, false)));
				_predicates.Add(sc);
			}

			if (nullabilityType == ParametersNullabilityType.SameAsFirstParameter)
			{
				ReduceOrAdd(parameters[0]);
			}

			if (nullabilityType == ParametersNullabilityType.SameAsSecondParameter)
			{
				ReduceOrAdd(parameters[1]);
			}

			if (nullabilityType == ParametersNullabilityType.SameAsThirdParameter)
			{
				ReduceOrAdd(parameters[2]);
			}

			if (nullabilityType == ParametersNullabilityType.SameAsFirstOrSecondParameter)
			{
				ReduceOrAdd(parameters[0]);
				ReduceOrAdd(parameters[1]);
			}

			if (nullabilityType == ParametersNullabilityType.SameAsLastParameter)
			{
				ReduceOrAdd(parameters[^1]);
			}
		}

		protected internal override IQueryElement VisitSqlUnaryExpression(SqlUnaryExpression element)
		{
			ReduceOrAdd(element.Expr);
			return element;
		}

		protected internal override IQueryElement VisitSqlBinaryExpression(SqlBinaryExpression element)
		{
			if (element.Operation is "+" or "-" or "*" or "/" or "%" or "&" or "||")
			{
				ReduceOrAdd(element.Expr1);
				ReduceOrAdd(element.Expr2);
			}

			return element;
		}

		protected internal override IQueryElement VisitSqlCastExpression(SqlCastExpression element)
		{
			ReduceOrAdd(element.Expression);

			return element;
		}

		protected internal override IQueryElement VisitSqlNullabilityExpression(SqlNullabilityExpression element)
		{
			// abort
			if (element.CanBeNullable(_nullabilityContext) != element.SqlExpression.CanBeNullable(_nullabilityContext))
				return element;

			// passthrough
			Visit(element.SqlExpression);

			return element;
		}
	}
}
