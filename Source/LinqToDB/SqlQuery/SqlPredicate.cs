using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Common;

namespace LinqToDB.SqlQuery
{
	public abstract class SqlPredicate : QueryElement, ISqlPredicate
	{
		public enum Operator
		{
			Equal,          // =     Is the operator used to test the equality between two expressions.
			NotEqual,       // <> != Is the operator used to test the condition of two expressions not being equal to each other.
			Greater,        // >     Is the operator used to test the condition of one expression being greater than the other.
			GreaterOrEqual, // >=    Is the operator used to test the condition of one expression being greater than or equal to the other expression.
			NotGreater,     // !>    Is the operator used to test the condition of one expression not being greater than the other expression.
			Less,           // <     Is the operator used to test the condition of one expression being less than the other.
			LessOrEqual,    // <=    Is the operator used to test the condition of one expression being less than or equal to the other expression.
			NotLess,        // !<    Is the operator used to test the condition of one expression not being less than the other expression.
			Overlaps,       // x OVERLAPS y Is the operator used to test Overlaps operator.
		}

#if DEBUG
		static readonly TruePredicate  _trueInstance  = new();
		static readonly FalsePredicate _falseInstance = new();

		public static TruePredicate True
		{
			get
			{
				return _trueInstance;
			}
		}

		public static FalsePredicate False
		{
			get
			{
				return _falseInstance;
			}
		}
#else
		public static readonly TruePredicate True   = new();
		public static readonly FalsePredicate False = new();
#endif

		public abstract bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased);

		public static ISqlPredicate MakeBool(bool isTrue)
		{
			return isTrue ? SqlPredicate.True : SqlPredicate.False;
		}

		public sealed class Not : SqlPredicate
		{
			public Not(ISqlPredicate predicate) : base(SqlQuery.Precedence.LogicalNegation)
			{
				Predicate = predicate;
			}

			public ISqlPredicate Predicate { get; private set; }

			public override QueryElementType ElementType => QueryElementType.NotPredicate;

			public override bool          CanInvert(NullabilityContext    nullability) => true;
			public override ISqlPredicate Invert(NullabilityContext       nullability) => Predicate;
			public override bool          CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased) => Predicate.CanBeUnknown(nullability, withoutUnknownErased);

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				if (other is not Not notPredicate)
					return false;

				return notPredicate.Predicate.Equals(notPredicate.Predicate, comparer);
			}

			public void Modify(ISqlPredicate predicate)
			{
				Predicate = predicate;
			}

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				writer.Append("NOT (");
				writer.Append(Predicate);
				writer.Append(')');
			}
		}

		public sealed class TruePredicate : SqlPredicate
		{
			public TruePredicate() : base(SqlQuery.Precedence.Primary)
			{
			}

			public override QueryElementType ElementType => QueryElementType.TruePredicate;

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				if (other is not TruePredicate)
					return false;

				return true;
			}

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				writer.Append("True");
			}

			public override bool          CanInvert(NullabilityContext    nullability) => true;
			public override ISqlPredicate Invert(NullabilityContext       nullability) => False;
			public override bool          CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased) => false;
		}

		public sealed class FalsePredicate : SqlPredicate
		{
			public FalsePredicate() : base(SqlQuery.Precedence.Primary)
			{
			}

			public override QueryElementType ElementType => QueryElementType.FalsePredicate;

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				if (other is not TruePredicate)
					return false;

				return true;
			}

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				writer.Append("False");
			}

			public override bool          CanInvert(NullabilityContext    nullability) => true;
			public override ISqlPredicate Invert(NullabilityContext       nullability) => True;
			public override bool          CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased) => false;
		}

		public class Expr : SqlPredicate
		{
			public Expr(ISqlExpression exp1, int precedence)
				: base(precedence)
			{
				Expr1 = exp1 ?? throw new ArgumentNullException(nameof(exp1));
			}

			public Expr(ISqlExpression exp1)
				: base(exp1.Precedence)
			{
				Expr1 = exp1 ?? throw new ArgumentNullException(nameof(exp1));
			}

			public ISqlExpression Expr1 { get; set; }

			public override bool          CanInvert(NullabilityContext    nullability) => false;
			public override ISqlPredicate Invert(NullabilityContext       nullability) => throw new InvalidOperationException();
			public override bool          CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased) => Expr1.CanBeNullableOrUnknown(nullability, withoutUnknownErased);

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				return other is Expr expr
					&& Precedence == expr.Precedence
					&& Expr1.Equals(expr.Expr1, comparer);
			}

			public override QueryElementType ElementType => QueryElementType.ExprPredicate;

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				writer.AppendElement(Expr1);
			}
		}

		public abstract class BaseNotExpr : Expr
		{
			protected BaseNotExpr(ISqlExpression exp1, bool isNot, int precedence)
				: base(exp1, precedence)
			{
				IsNot = isNot;
			}

			public bool IsNot { get; }

			public override bool CanInvert(NullabilityContext nullability) => true;

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				return other is BaseNotExpr expr
					&& IsNot == expr.IsNot
					&& base.Equals(other, comparer);
			}

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				if (IsNot) writer.Append("NOT (");
				base.WritePredicate(writer);
				if (IsNot) writer.Append(')');
			}
		}

		// { expression { = | <> | != | > | >= | ! > | < | <= | !< } expression
		//
		public sealed class ExprExpr : Expr
		{
			public ExprExpr(ISqlExpression exp1, Operator op, ISqlExpression exp2, bool? unknownAsValue)
				: base(exp1, SqlQuery.Precedence.Comparison)
			{
				Operator       = op;
				Expr2          = exp2;
				UnknownAsValue = unknownAsValue;
			}

			public new Operator       Operator { get; }
			public     ISqlExpression Expr2    { get; internal set; }

			public override bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased)
			{
				if (!withoutUnknownErased)
				{
					return UnknownAsValue == null && (Expr1.CanBeNullableOrUnknown(nullability, withoutUnknownErased) || Expr2.CanBeNullableOrUnknown(nullability, withoutUnknownErased));
				}

				if (Operator == Operator.Equal)
					return Expr1.CanBeNullableOrUnknown(nullability, withoutUnknownErased) || Expr2.CanBeNullableOrUnknown(nullability, withoutUnknownErased);

				if (Operator == Operator.Equal)
					return Expr1.CanBeNullableOrUnknown(nullability, withoutUnknownErased) && Expr2.CanBeNullableOrUnknown(nullability, withoutUnknownErased);

				// comparison
				return UnknownAsValue != true && Expr1.CanBeNullableOrUnknown(nullability, withoutUnknownErased) || Expr2.CanBeNullableOrUnknown(nullability, withoutUnknownErased);
			}

			/// <summary>
			/// Specify value, used as UNKNOWN value replacement on reduced predicate with UNKNOWN value erased.
			/// Replacement only applyed when this property is not null.
			/// </summary>
			public bool? UnknownAsValue { get; }

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				return other is ExprExpr expr
					&& UnknownAsValue == expr.UnknownAsValue
					&& Operator == expr.Operator
					&& Expr2.Equals(expr.Expr2, comparer)
					&& base.Equals(other, comparer);
			}

			public override QueryElementType ElementType => QueryElementType.ExprExprPredicate;

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				//writer.DebugAppendUniqueId(this);
				writer.AppendElement(Expr1);
				var op = Operator switch
				{
					Operator.Equal          => "=",
					Operator.NotEqual       => "<>",
					Operator.Greater        => ">",
					Operator.GreaterOrEqual => ">=",
					Operator.NotGreater     => "!>",
					Operator.Less           => "<",
					Operator.LessOrEqual    => "<=",
					Operator.NotLess        => "!<",
					Operator.Overlaps       => "OVERLAPS",
					_                       => throw new InvalidOperationException(),
				};
				writer.Append(' ').Append(op).Append(' ')
					.AppendElement(Expr2);
			}

			static Operator InvertOperator(Operator op)
			{
				switch (op)
				{
					case Operator.Equal          : return Operator.NotEqual;
					case Operator.NotEqual       : return Operator.Equal;
					case Operator.Greater        : return Operator.LessOrEqual;
					case Operator.NotLess        :
					case Operator.GreaterOrEqual : return Operator.Less;
					case Operator.Less           : return Operator.GreaterOrEqual;
					case Operator.NotGreater     :
					case Operator.LessOrEqual    : return Operator.Greater;
					default: throw new InvalidOperationException();
				}
			}

			public static Operator SwapOperator(Operator op)
			{
				switch (op)
				{
					case Operator.Equal:          return Operator.Equal;
					case Operator.NotEqual:       return Operator.NotEqual;
					case Operator.Greater:        return Operator.Less;
					case Operator.NotLess:        return Operator.NotGreater;
					case Operator.GreaterOrEqual: return Operator.LessOrEqual;
					case Operator.Less:           return Operator.Greater;
					case Operator.NotGreater:     return Operator.NotLess;
					case Operator.LessOrEqual:    return Operator.GreaterOrEqual;
					case Operator.Overlaps:       return Operator.Overlaps;
					default:                      throw new InvalidOperationException();
				}
			}

			public override bool CanInvert(NullabilityContext nullability) => true;

			public override ISqlPredicate Invert(NullabilityContext nullability)
			{
				return new ExprExpr(Expr1, InvertOperator(Operator), Expr2, !UnknownAsValue);
			}

			/// <summary>
			/// Converts predicate to final form based on null comparison options.
			/// </summary>
			/// <param name="isInsidePredicate">Enables generation of addtional conversion of UNKNOWN to FALSE for nested predicates when non-nullable result.</param>
			public ISqlPredicate Reduce(NullabilityContext nullability, EvaluationContext context, bool isInsidePredicate, LinqOptions options)
			{
				if (options.CompareNulls == CompareNulls.LikeSql)
					return this;

				ISqlPredicate MakeWithoutNulls()
				{
					return new ExprExpr(Expr1, Operator, Expr2, null);
				}

				// CompareNulls.LikeSqlExceptParameters and CompareNulls.LikeClr
				// always sniffs parameters to == and != (for backward compatibility).
				if (Operator == Operator.Equal || Operator == Operator.NotEqual)
				{
					if (this.TryEvaluateExpression(context, out var value))
					{
						if (value is null)
						{
							return new Expr(new SqlValue(typeof(bool?), null));
						}

						return value is true ? True : False;
					}
					else if (Expr1.TryEvaluateExpression(context, out value) && value == null)
					{
						return new IsNull(Expr2, Operator != Operator.Equal);
					}
					else if (Expr2.TryEvaluateExpression(context, out value) && value == null)
					{
						return new IsNull(Expr1, Operator != Operator.Equal);
					}

					if (UnknownAsValue == null && Operator == Operator.NotEqual)
					{
						if (Expr1 is SqlValue { Value: bool } sqlValue1)
						{
							return new ExprExpr(Expr2, Operator.Equal, new SqlValue(sqlValue1.ValueType, !(bool)sqlValue1.Value), null);
						}
						else if (Expr2 is SqlValue { Value: bool } sqlValue2)
						{
							return new ExprExpr(Expr1, Operator.Equal, new SqlValue(sqlValue2.ValueType, !(bool)sqlValue2.Value), null);
						}
					}
				}
				else
				{
					if (UnknownAsValue != null
						&& ((Expr1.TryEvaluateExpression(context, out var value) && value == null)
						|| (Expr2.TryEvaluateExpression(context, out value) && value == null)))
					{
						return new Expr(new SqlValue(typeof(bool), UnknownAsValue.Value));
					}
				}

				if (UnknownAsValue == null || nullability.IsEmpty)
					return this;

				var expr1CanBeUnknown = Expr1.CanBeNullableOrUnknown(nullability, false);
				var expr2CanBeUnknown = Expr2.CanBeNullableOrUnknown(nullability, false);
				if (!expr1CanBeUnknown && !expr2CanBeUnknown)
					return MakeWithoutNulls();

				switch (Operator)
				{
					case Operator.NotEqual:
					{
						var search = new SqlSearchCondition(true, canBeUnknown: expr1CanBeUnknown && expr2CanBeUnknown)
							.Add(MakeWithoutNulls());

						if (expr1CanBeUnknown && expr2CanBeUnknown)
						{
							search
								.AddAnd(sc => sc
									.Add(new IsNull(Expr1, false))
									.Add(new IsNull(Expr2, true)))
								.AddAnd(sc => sc
									.Add(new IsNull(Expr1, true))
									.Add(new IsNull(Expr2, false)));
						}
						else
						{
							search.Add(new IsNull(expr1CanBeUnknown ? Expr1 : Expr2, false));
						}

						// eliminate UNKNOWN for nested conditions
						if (isInsidePredicate && search.CanReturnUnknown == true)
						{
							search = new SqlSearchCondition(false, canBeUnknown: false)
								.Add(search)
								.Add(
									new Not(
										new SqlSearchCondition(false)
											.Add(new IsNull(Expr1, false))
											.Add(new IsNull(Expr2, false))));
						}

						return search;
					}
					case Operator.Equal:
					{
						var search = MakeWithoutNulls();

						if (expr1CanBeUnknown && expr2CanBeUnknown)
						{
							search = new SqlSearchCondition(true, canBeUnknown: true)
								.Add(search)
								.AddAnd(sc => sc
									.Add(new IsNull(Expr1, false))
									.Add(new IsNull(Expr2, false)));
						}

						// eliminate UNKNOWN for nested conditions
						if (isInsidePredicate)
						{
							if (expr1CanBeUnknown && expr2CanBeUnknown)
							{
								search = new SqlSearchCondition(false, canBeUnknown: false).Add(search);

								((SqlSearchCondition)search)
									.Add(
										new Not(
											new SqlSearchCondition(false)
												.Add(new IsNull(Expr1, false))
												.Add(new IsNull(Expr2, true))))
									.Add(
										new Not(
											new SqlSearchCondition(false)
												.Add(new IsNull(Expr1, true))
												.Add(new IsNull(Expr2, false))));
							}
							else
							{
								search = new SqlSearchCondition(false, canBeUnknown: false)
									.Add(search)
									.Add(new IsNull(expr1CanBeUnknown ? Expr1 : Expr2, true));
							}
						}

						return search;
					}
					default:
					{
						if (!isInsidePredicate && UnknownAsValue != true)
							return MakeWithoutNulls();

						// eliminate UNKNOWN for nested conditions
						// in C# >, >=, <, <= evaluate to FALSE if any (one or both) operands are NULL
						return new SqlSearchCondition(UnknownAsValue.Value, canBeUnknown: false)
							.Add(MakeWithoutNulls())
							.Add(new IsNull(Expr1, !UnknownAsValue.Value))
							.Add(new IsNull(Expr2, !UnknownAsValue.Value));
					}
				}
			}

			public void Deconstruct(out ISqlExpression expr1, out Operator @operator, out ISqlExpression expr2, out bool? unknownAsValue)
			{
				expr1          = Expr1;
				@operator      = Operator;
				expr2          = Expr2;
				unknownAsValue = UnknownAsValue;
			}
		}

		// string_expression [ NOT ] LIKE string_expression [ ESCAPE 'escape_character' ]
		//
		public sealed class Like : BaseNotExpr
		{
			public Like(ISqlExpression exp1, bool isNot, ISqlExpression exp2, ISqlExpression? escape, string? functionName = null)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				Expr2     = exp2;
				Escape    = escape;
				FunctionName = functionName;
			}

			public ISqlExpression  Expr2        { get; internal set; }
			public ISqlExpression? Escape       { get; internal set; }
			public string?         FunctionName { get; internal set; }

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				return other is Like expr
					&& FunctionName == expr.FunctionName
					&& Expr2.Equals(expr.Expr2, comparer)
					&& (   (Escape != null && expr.Escape != null && Escape.Equals(expr.Escape, comparer))
						|| (Escape == null && expr.Escape == null))
					&& base.Equals(other, comparer);
			}

			public override ISqlPredicate Invert(NullabilityContext nullability)
			{
				return new Like(Expr1, !IsNot, Expr2, Escape);
			}

			public override bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased)
			{
				return Expr1.CanBeNullable(nullability) || Expr2.CanBeNullable(nullability);
			}

			public override QueryElementType ElementType => QueryElementType.LikePredicate;

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				writer.AppendElement(Expr1);

				if (IsNot) writer.Append(" NOT");

				writer.Append(' ').Append(FunctionName ?? "LIKE").Append(' ');

				writer.AppendElement(Expr2);

				if (Escape != null)
				{
					writer.Append(" ESCAPE ");
					writer.AppendElement(Escape);
				}
			}
		}

		// virtual predicate for simplifying string search operations
		// string_expression [ NOT ] STARTS_WITH | ENDS_WITH | CONTAINS string_expression
		//
		public sealed class SearchString : BaseNotExpr
		{
			public enum SearchKind
			{
				StartsWith,
				EndsWith,
				Contains
			}

			public SearchString(ISqlExpression exp1, bool isNot, ISqlExpression exp2, SearchKind searchKind, ISqlExpression caseSensitive)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				Expr2         = exp2;
				Kind          = searchKind;
				CaseSensitive = caseSensitive;
			}

			public ISqlExpression Expr2         { get; internal set; }
			public SearchKind     Kind          { get; }
			public ISqlExpression CaseSensitive { get; private set; }

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				return other is SearchString expr
					&& Kind == expr.Kind
					&& Expr2.Equals(expr.Expr2, comparer)
					&& CaseSensitive.Equals(expr.CaseSensitive, comparer)
					&& base.Equals(other, comparer);
			}

			public override ISqlPredicate Invert(NullabilityContext nullability)
			{
				return new SearchString(Expr1, !IsNot, Expr2, Kind, CaseSensitive);
			}

			public override bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased)
			{
				return Expr1.CanBeNullable(nullability) || Expr2.CanBeNullable(nullability);
			}

			public override QueryElementType ElementType => QueryElementType.SearchStringPredicate;

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				writer.AppendElement(Expr1);

				if (IsNot) writer.Append(" NOT");
				switch (Kind)
				{
					case SearchKind.StartsWith:
						writer.Append(" STARTS_WITH ");
						break;
					case SearchKind.EndsWith:
						writer.Append(" ENDS_WITH ");
						break;
					case SearchKind.Contains:
						writer.Append(" CONTAINS ");
						break;
					default:
						throw new InvalidOperationException($"Unexpected search kind: {Kind}");
				}

				writer.AppendElement(Expr2);
			}

			public void Modify(ISqlExpression expr1, ISqlExpression expr2, ISqlExpression caseSensitive)
			{
				Expr1 = expr1;
				Expr2 = expr2;
				CaseSensitive = caseSensitive;
			}
		}

		// expression IS [ NOT ] DISTINCT FROM expression
		//
		public sealed class IsDistinct : BaseNotExpr
		{
			public IsDistinct(ISqlExpression exp1, bool isNot, ISqlExpression exp2)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				Expr2 = exp2;
			}

			public ISqlExpression Expr2 { get; internal set; }

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				return other is IsDistinct expr
					&& Expr2.Equals(expr.Expr2, comparer)
					&& base.Equals(other, comparer);
			}

			public override ISqlPredicate Invert(NullabilityContext nullability) => new IsDistinct(Expr1, !IsNot, Expr2);

			public override bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased) => false;

			public override QueryElementType ElementType => QueryElementType.IsDistinctPredicate;

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				writer.AppendElement(Expr1);
				writer.Append(IsNot ? " IS NOT DISTINCT FROM " : " IS DISTINCT FROM ");
				writer.AppendElement(Expr2);
			}
		}

		// expression [ NOT ] BETWEEN expression AND expression
		//
		public sealed class Between : BaseNotExpr
		{
			public Between(ISqlExpression exp1, bool isNot, ISqlExpression exp2, ISqlExpression exp3)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				Expr2 = exp2;
				Expr3 = exp3;
			}

			public ISqlExpression Expr2 { get; internal set; }
			public ISqlExpression Expr3 { get; internal set; }

			public override bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased)
			{
				return Expr1.CanBeNullable(nullability) || Expr2.CanBeNullable(nullability) || Expr3.CanBeNullable(nullability);
			}

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				return other is Between expr
					&& Expr2.Equals(expr.Expr2, comparer)
					&& Expr3.Equals(expr.Expr3, comparer)
					&& base.Equals(other, comparer);
			}

			public override ISqlPredicate Invert(NullabilityContext nullability)
			{
				return new Between(Expr1, !IsNot, Expr2, Expr3);
			}

			public override QueryElementType ElementType => QueryElementType.BetweenPredicate;

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				writer.AppendElement(Expr1);

				if (IsNot) writer.Append(" NOT");

				writer.Append(" BETWEEN ")
					.AppendElement(Expr2)
					.Append(" AND ")
					.AppendElement(Expr3);
			}
		}

		// [NOT] expression = 1, expression = 0, expression IS NULL OR expression = 0
		//
		/// <summary>
		/// '[NOT] Expr1 IS TRUE' predicate.
		/// </summary>
		public sealed class IsTrue : BaseNotExpr
		{
			public ISqlExpression TrueValue   { get; set; }
			public ISqlExpression FalseValue  { get; set; }
			/// <summary>
			/// <list type="bullet">
			/// <item><c>null</c> : evaluate predicate as is and preserve UNKNOWN (null) values if they produced</item>
			/// <item><c>false</c> : UNKNOWN values should be converted to FALSE</item>
			/// <item><c>true</c> : UNKNOWN values should be converted to TRUE</item>
			/// </list>
			/// </summary>
			public bool?          WithNull    { get; }

			public IsTrue(ISqlExpression exp1, ISqlExpression trueValue, ISqlExpression falseValue, bool? withNull, bool isNot)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				TrueValue  = trueValue;
				FalseValue = falseValue;
				WithNull   = withNull;
			}

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				return other is IsTrue expr
					&& WithNull == expr.WithNull
					&& TrueValue.Equals(expr.TrueValue, comparer)
					&& FalseValue.Equals(expr.FalseValue, comparer)
					&& base.Equals(other, comparer);
			}

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				writer.AppendElement(Reduce(writer.Nullability, true));
			}

			/// <param name="isInsidePredicate">Enables generation of addtional conversion of UNKNOWN to FALSE for nested predicates when non-nullable result.</param>
			public ISqlPredicate Reduce(NullabilityContext nullability, bool isInsidePredicate)
			{
				if (Expr1.ElementType == QueryElementType.SearchCondition)
				{
					return ((ISqlPredicate)Expr1).MakeNot(IsNot);
				}

				var predicate = new ExprExpr(Expr1, Operator.Equal, IsNot ? FalseValue : TrueValue, null);

				// IS [NOT] NULL check needed for nullable predicate when it:
				// - part of logic - evaluates predicate to true (WithNull == true)
				// - when predicate is nested and expected to not return UNKNOWN (WithNull != null && isInsidePredicate)
				if (WithNull == null || !Expr1.CanBeNullableOrUnknown(nullability, false) || (!isInsidePredicate && WithNull == false))
					return predicate;

				var search = new SqlSearchCondition(WithNull == true, false)
					.Add(predicate)
					.Add(new IsNull(Expr1, WithNull != true));

				return search;
			}

			public override ISqlPredicate Invert(NullabilityContext nullability)
			{
				return new IsTrue(Expr1, TrueValue, FalseValue, !WithNull, !IsNot);
			}

			public override QueryElementType ElementType => QueryElementType.IsTruePredicate;

			public override bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased)
			{
				return (withoutUnknownErased || WithNull == null) && Expr1.CanBeNullableOrUnknown(nullability, withoutUnknownErased);
			}
		}

		// expression IS [ NOT ] NULL
		//
		public sealed class IsNull : BaseNotExpr
		{
			public IsNull(ISqlExpression exp1, bool isNot)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
			}

			public override ISqlPredicate Invert(NullabilityContext nullability)
			{
				return new IsNull(Expr1, !IsNot);
			}

			public override bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased) => false;

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				writer
					//.DebugAppendUniqueId(this)
					.AppendElement(Expr1)
					.Append(" IS ")
					.Append(IsNot ? "NOT " : "")
					.Append("NULL");
			}

			public override QueryElementType ElementType => QueryElementType.IsNullPredicate;
		}

		// expression [ NOT ] IN ( subquery | expression [ ,...n ] )
		//
		public sealed class InSubQuery : BaseNotExpr
		{
			public InSubQuery(ISqlExpression exp1, bool isNot, SelectQuery subQuery, bool doNotConvert)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				SubQuery     = subQuery;
				DoNotConvert = doNotConvert;
			}

			public bool        DoNotConvert { get; }
			public SelectQuery SubQuery     { get; private set; }

			public void Modify(ISqlExpression exp1, SelectQuery subQuery)
			{
				Expr1    = exp1;
				SubQuery = subQuery;
			}

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				return other is InSubQuery expr
					&& SubQuery.Equals(expr.SubQuery, comparer)
					&& base.Equals(other, comparer);
			}

			public override bool CanInvert(NullabilityContext nullability)
			{
				return true;
			}

			public override ISqlPredicate Invert(NullabilityContext nullability)
			{
				return new InSubQuery(Expr1, !IsNot, SubQuery, DoNotConvert);
			}

			public override bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased) => base.CanBeUnknown(nullability, withoutUnknownErased) || SubQuery.CanBeNullable(nullability);

			public override QueryElementType ElementType => QueryElementType.InSubQueryPredicate;

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				//writer.DebugAppendUniqueId(this);
				writer.AppendElement(Expr1);

				if (IsNot) writer.Append(" NOT");

				writer.Append(" IN");
				writer.AppendLine();
				using (writer.IndentScope())
				{
					writer.AppendLine('(');
					using (writer.IndentScope())
					{
						writer.AppendElement(SubQuery);
					}

					writer.AppendLine();
					writer.Append(')');
				}
	
			}

			public void Deconstruct(out ISqlExpression exp1, out bool isNot, out SelectQuery subQuery)
			{
				exp1     = Expr1;
				isNot    = IsNot;
				subQuery = SubQuery;
			}
		}

		public sealed class InList : BaseNotExpr
		{
			public bool?          WithNull    { get; }

			public InList(ISqlExpression exp1, bool? withNull, bool isNot)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				WithNull = withNull;
			}

			public InList(ISqlExpression exp1, bool? withNull, bool isNot, ISqlExpression value)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				WithNull = withNull;
				Values.Add(value);
			}

			public InList(ISqlExpression exp1, bool? withNull, bool isNot, IEnumerable<ISqlExpression>? values)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				WithNull = withNull;
				if (values != null)
					Values.AddRange(values);
			}

			public List<ISqlExpression> Values { get; private set; } = new();

			public void Modify(ISqlExpression expr1)
			{
				Expr1  = expr1;
			}

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				if (other is not InList expr
					|| WithNull != expr.WithNull
					|| Values.Count != expr.Values.Count
					|| !base.Equals(other, comparer))
					return false;

				for (var i = 0; i < Values.Count; i++)
					if (!Values[i].Equals(expr.Values[i], comparer))
						return false;

				return true;
			}

			public override bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased)
			{
				if (base.CanBeUnknown(nullability, withoutUnknownErased))
					return true;

				return Values.Any(e => e.CanBeNullable(nullability));
			}

			public override ISqlPredicate Invert(NullabilityContext nullability)
			{
				return new InList(Expr1, !WithNull, !IsNot, Values);
			}

			public override QueryElementType ElementType => QueryElementType.InListPredicate;

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				writer.AppendElement(Expr1);

				if (IsNot) writer.Append(" NOT");
				writer.Append(" IN (");

				foreach (var value in Values)
				{
					writer
						.AppendElement(value)
						.Append(',');
				}

				if (Values.Count > 0)
					writer.Length--;

				writer.Append(')');
			}
		}

		public sealed class Exists : SqlPredicate
		{
			public Exists(bool isNot, SelectQuery subQuery)
				: base(isNot ? SqlQuery.Precedence.LogicalNegation : SqlQuery.Precedence.Primary)
			{
				IsNot    = isNot;
				SubQuery = subQuery;
			}

			public bool        IsNot    { get; }
			public SelectQuery SubQuery { get; private set; }

			public void Modify(SelectQuery subQuery)
			{
				SubQuery = subQuery;
			}

			public override bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer)
			{
				return other is Exists expr
					&& IsNot == expr.IsNot
					&& SubQuery.Equals(expr.SubQuery, comparer);
			}

			public override bool CanInvert(NullabilityContext nullability) => true;

			public override ISqlPredicate Invert(NullabilityContext nullability)
			{
				return new Exists(!IsNot, SubQuery);
			}

			public override bool CanBeUnknown(NullabilityContext nullability, bool withoutUnknownErased) => false;

			public override QueryElementType ElementType => QueryElementType.ExistsPredicate;

			protected override void WritePredicate(QueryElementTextWriter writer)
			{
				if (IsNot) writer.Append(" NOT");

				writer.Append(" EXISTS");
				writer.AppendLine();
				using (writer.IndentScope())
				{
					writer.AppendLine('(');
					using (writer.IndentScope())
					{
						writer.AppendElement(SubQuery);
					}

					writer.AppendLine();
					writer.Append(')');
				}
			}

			public void Deconstruct(out bool isNot, out SelectQuery subQuery)
			{
				isNot    = IsNot;
				subQuery = SubQuery;
			}
		}

		protected SqlPredicate(int precedence)
		{
			Precedence = precedence;
		}

		#region IPredicate Members

		public int  Precedence { get; }

		public abstract bool           CanInvert    (NullabilityContext nullability);
		public abstract ISqlPredicate  Invert       (NullabilityContext nullability);

		public abstract bool Equals(ISqlPredicate other, Func<ISqlExpression, ISqlExpression, bool> comparer);

		#endregion

		#region IQueryElement Members

		protected abstract void WritePredicate(QueryElementTextWriter writer);

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			if (!writer.AddVisited(this))
				return writer.Append("...");

			WritePredicate(writer);

			writer.RemoveVisited(this);

			return writer;
		}

		#endregion
	}
}
