using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public abstract class SqlPredicate : ISqlPredicate
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
			NotLess         // !<    Is the operator used to test the condition of one expression not being less than the other expression.
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

			protected override void Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
			{
				Expr1 = Expr1.Walk(options, func)!;

				if (Expr1 == null)
					throw new InvalidOperationException();
			}

			public override bool CanBeNull => Expr1.CanBeNull;

			public override QueryElementType ElementType => QueryElementType.ExprPredicate;

			protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				Expr1.ToString(sb, dic);
			}
		}

		public abstract class BaseNotExpr : Expr, IInvertibleElement
		{
			public BaseNotExpr(ISqlExpression exp1, bool isNot, int precedence)
				: base(exp1, precedence)
			{
				IsNot = isNot;
			}

			public bool IsNot { get; }

			public bool CanInvert() => true;

			public abstract IQueryElement Invert();

			protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				if (IsNot) sb.Append("NOT (");
				base.ToString(sb, dic);
				if (IsNot) sb.Append(')');
			}
		}

		public class NotExpr : BaseNotExpr
		{
			public NotExpr(ISqlExpression exp1, bool isNot, int precedence)
				: base(exp1, isNot, precedence)
			{
			}

			public override IQueryElement Invert()
			{
				return new NotExpr(Expr1, !IsNot, Precedence);
			}

			public override QueryElementType ElementType => QueryElementType.NotExprPredicate;
		}

		// { expression { = | <> | != | > | >= | ! > | < | <= | !< } expression
		//
		public class ExprExpr : Expr, IInvertibleElement
		{
			public ExprExpr(ISqlExpression exp1, Operator op, ISqlExpression exp2, bool? withNull)
				: base(exp1, SqlQuery.Precedence.Comparison)
			{
				Operator = op;
				Expr2    = exp2;
				WithNull = withNull;
			}

			public new Operator   Operator { get; }
			public ISqlExpression Expr2    { get; internal set; }

			public bool? WithNull          { get; }

			protected override void Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
			{
				base.Walk(options, func);
				Expr2 = Expr2.Walk(options, func)!;
			}

			public override bool CanBeNull => base.CanBeNull || Expr2.CanBeNull;

			public override QueryElementType ElementType => QueryElementType.ExprExprPredicate;

			protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				Expr1.ToString(sb, dic);
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
					_                       => throw new InvalidOperationException(),
				};
				sb.Append(' ').Append(op).Append(' ');

				Expr2.ToString(sb, dic);
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

			public bool CanInvert()
			{
				return true;
			}

			public IQueryElement Invert()
			{
				return new ExprExpr(Expr1, InvertOperator(Operator), Expr2, !WithNull);
			}

			public ISqlPredicate Reduce(EvaluationContext context)
			{
				if (Operator == Operator.Equal || Operator == Operator.NotEqual)
				{
					if (Expr1.TryEvaluateExpression(context, out var value1))
					{
						if (value1 == null)
							return new IsNull(Expr2, Operator != Operator.Equal);

					} else if (Expr2.TryEvaluateExpression(context, out var value2))
					{
						if (value2 == null)
							return new IsNull(Expr1, Operator != Operator.Equal);
					}
				}

				if (WithNull == null)
					return this;

				var canBeNull_1 = Expr1.ShouldCheckForNull();
				var canBeNull_2 = Expr2.ShouldCheckForNull();

				var isInverted = !WithNull.Value;

				var predicate = new ExprExpr(Expr1, Operator, Expr2, null);

				if (!canBeNull_1 && !canBeNull_2)
					return predicate;

				var search = new SqlSearchCondition();

				if (Expr1.CanBeEvaluated(context))
				{
					if (!Expr2.CanBeEvaluated(context))
					{
						if (canBeNull_2)
						{
							if (isInverted)
							{
								if (Operator != Operator.Equal)
								{
									search.Conditions.Add(new SqlCondition(false, predicate, true));
									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, false), false));
								}
							}
							else if (Operator == Operator.NotEqual)
							{
								search.Conditions.Add(new SqlCondition(false, predicate, true));
								search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, false), false));
							}
						}
					}
				}
				else if (Expr2.CanBeEvaluated(context))
				{
					if (canBeNull_1)
					{
						if (isInverted)
						{
							if (Operator != Operator.Equal)
							{
								search.Conditions.Add(new SqlCondition(false, predicate, true));
								search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, false), false));
							}
						}
						else if (Operator == Operator.NotEqual)
						{
							search.Conditions.Add(new SqlCondition(false, predicate, true));
							search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, false), false));
						}
					}
				}
				else
				{
					if (canBeNull_2)
					{
						if (canBeNull_1)
						{
							if (isInverted)
							{
								if (Operator == Operator.Equal)
								{
									search.Conditions.Add(new SqlCondition(false, predicate, true));

									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, false), false));
									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, true), true));

									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, true), false));
									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, false), false));
								}
								else
								if (Operator == Operator.NotEqual)
								{
									search.Conditions.Add(new SqlCondition(false, predicate, true));

									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, false), false));
									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, true), true));

									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, true), false));
									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, false), false));
								}
								else
								if (Operator == Operator.LessOrEqual || Operator == Operator.GreaterOrEqual)
								{
									search.Conditions.Add(new SqlCondition(false, predicate, true));
									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, false), true));
									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, false), false));
								}
								else if (Operator == Operator.NotEqual)
								{
									search.Conditions.Add(new SqlCondition(false, predicate, true));
									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, false), false));
									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, false), false));
								}
								else 
								{
									search.Conditions.Add(new SqlCondition(false, predicate, true));
									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, false), false));
									search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, false), false));
								}
							}
							else if (Operator == Operator.Equal)
							{
								search.Conditions.Add(new SqlCondition(false, predicate, true));
								search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, false), false));
								search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, false), false));
							}
							else if (Operator == Operator.NotEqual)
							{
								search.Conditions.Add(new SqlCondition(false, predicate, true));

								search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, false), false));
								search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, true), true));

								search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, true), false));
								search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, false), false));
							}
						}
						else
							if (isInverted)
							{
								search.Conditions.Add(new SqlCondition(false, predicate, true));
								search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, false), false));
							}
					}
					else
					{
						if (canBeNull_1)
						{
							if (isInverted)
							{
								search.Conditions.Add(new SqlCondition(false, predicate, true));
								search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, false), false));
							}
						}
						else
						{
							search.Conditions.Add(new SqlCondition(false, predicate, true));
							search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, false), false));
							search.Conditions.Add(new SqlCondition(false, new IsNull(Expr2, false), false));
						}
					}
				}


				if (search.Conditions.Count == 0)
					return predicate;
				
				return search;
			}

			public void Deconstruct(out ISqlExpression expr1, out Operator @operator, out ISqlExpression expr2, out bool? withNull)
			{
				expr1 = Expr1;
				@operator = Operator;
				expr2 = Expr2;
				withNull = WithNull;
			}
		}

		// string_expression [ NOT ] LIKE string_expression [ ESCAPE 'escape_character' ]
		//
		public class Like : BaseNotExpr
		{
			public Like(ISqlExpression exp1, bool isNot, ISqlExpression exp2, ISqlExpression? escape, string? functionName = null)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				Expr2     = exp2;
				Escape    = escape;
				FunctionName = functionName;
			}

			public ISqlExpression  Expr2     { get; internal set; }
			public ISqlExpression? Escape    { get; internal set; }
			public string?         FunctionName { get; internal set; }

			protected override void Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
			{
				base.Walk(options, func);
				Expr2 = Expr2.Walk(options, func)!;

				Escape = Escape?.Walk(options, func);
			}

			public override IQueryElement Invert()
			{
				return new Like(Expr1, !IsNot, Expr2, Escape);
			}

			public override QueryElementType ElementType => QueryElementType.LikePredicate;

			protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				Expr1.ToString(sb, dic);

				if (IsNot) sb.Append(" NOT");

				sb.Append(' ').Append(FunctionName ?? "LIKE").Append(' ');

				Expr2.ToString(sb, dic);

				if (Escape != null)
				{
					sb.Append(" ESCAPE ");
					Escape.ToString(sb, dic);
				}
			}
		}

		// virtual predicate for simplifying string search operations
		// string_expression [ NOT ] STARTS_WITH | ENDS_WITH | CONTAINS string_expression
		//
		public class SearchString : BaseNotExpr
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
			public ISqlExpression CaseSensitive { get; }

			protected override void Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
			{
				base.Walk(options, func);
				Expr2 = Expr2.Walk(options, func)!;
			}

			public override IQueryElement Invert()
			{
				return new SearchString(Expr1, !IsNot, Expr2, Kind, CaseSensitive);
			}

			public override QueryElementType ElementType => QueryElementType.SearchStringPredicate;

			protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				Expr1.ToString(sb, dic);

				if (IsNot) sb.Append(" NOT");
				switch (Kind)
				{
					case SearchKind.StartsWith:
						sb.Append(" STARTS_WITH ");
						break;
					case SearchKind.EndsWith:
						sb.Append(" ENS_WITH ");
						break;
					case SearchKind.Contains:
						sb.Append(" CONTAINS ");
						break;
					default:
						throw new InvalidOperationException($"Unexpected search kind: {Kind}");
				}

				Expr2.ToString(sb, dic);
			}
		}

		// expression [ NOT ] BETWEEN expression AND expression
		//
		public class Between : BaseNotExpr
		{
			public Between(ISqlExpression exp1, bool isNot, ISqlExpression exp2, ISqlExpression exp3)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				Expr2 = exp2;
				Expr3 = exp3;
			}

			public ISqlExpression Expr2 { get; internal set; }
			public ISqlExpression Expr3 { get; internal set; }

			protected override void Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
			{
				base.Walk(options, func);
				Expr2 = Expr2.Walk(options, func)!;
				Expr3 = Expr3.Walk(options, func)!;
			}

			public override IQueryElement Invert()
			{
				return new Between(Expr1, !IsNot, Expr2, Expr3);
			}

			public override QueryElementType ElementType => QueryElementType.BetweenPredicate;

			protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				Expr1.ToString(sb, dic);

				if (IsNot) sb.Append(" NOT");
				sb.Append(" BETWEEN ");

				Expr2.ToString(sb, dic);
				sb.Append(" AND ");
				Expr3.ToString(sb, dic);
			}
		}

		// [NOT] expression = 1, expression = 0, expression IS NULL OR expression = 0
		//
		public class IsTrue : BaseNotExpr
		{
			public ISqlExpression TrueValue   { get; set; }
			public ISqlExpression FalseValue  { get; set; }
			public bool?          WithNull    { get; }

			public IsTrue(ISqlExpression exp1, ISqlExpression trueValue, ISqlExpression falseValue, bool? withNull, bool isNot)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				TrueValue    = trueValue;
				FalseValue   = falseValue;
				WithNull = withNull;
			}

			protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				Reduce().ToString(sb, dic);
			}

			public ISqlPredicate Reduce()
			{
				if (Expr1.ElementType == QueryElementType.SearchCondition)
				{
					if (!IsNot)
						return (ISqlPredicate)Expr1;
					return new SqlSearchCondition(new SqlCondition(true, (ISqlPredicate)Expr1));
				}

				var predicate = new ExprExpr(Expr1, Operator.Equal, IsNot ? FalseValue : TrueValue, null);
				if (WithNull == null || !Expr1.ShouldCheckForNull()) 
					return predicate;

				var search = new SqlSearchCondition();
				search.Conditions.Add(new SqlCondition(false, predicate, WithNull.Value));
				search.Conditions.Add(new SqlCondition(false, new IsNull(Expr1, !WithNull.Value), WithNull.Value));
				return search;
			}

			public override IQueryElement Invert()
			{
				return new IsTrue(Expr1, TrueValue, FalseValue, !WithNull, !IsNot);
			}

			public override QueryElementType ElementType => QueryElementType.IsTruePredicate;

		}

		// expression IS [ NOT ] NULL
		//
		public class IsNull : BaseNotExpr
		{
			public IsNull(ISqlExpression exp1, bool isNot)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
			}

			public override IQueryElement Invert()
			{
				return new IsNull(Expr1, !IsNot);
			}

			protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				Expr1.ToString(sb, dic);
				sb
					.Append(" IS ")
					.Append(IsNot ? "NOT " : "")
					.Append("NULL");
			}

			public override QueryElementType ElementType => QueryElementType.IsNullPredicate;
		}

		// expression [ NOT ] IN ( subquery | expression [ ,...n ] )
		//
		public class InSubQuery : BaseNotExpr
		{
			public InSubQuery(ISqlExpression exp1, bool isNot, SelectQuery subQuery)
				: base(exp1, isNot, SqlQuery.Precedence.Comparison)
			{
				SubQuery = subQuery;
			}

			public SelectQuery SubQuery { get; private set; }

			protected override void Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
			{
				base.Walk(options, func);
				SubQuery = (SelectQuery)((ISqlExpression)SubQuery).Walk(options, func)!;
			}

			public override IQueryElement Invert()
			{
				return new InSubQuery(Expr1, !IsNot, SubQuery);
			}

			public override QueryElementType ElementType => QueryElementType.InSubQueryPredicate;

			protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				Expr1.ToString(sb, dic);

				if (IsNot) sb.Append(" NOT");
				sb.Append(" IN (");

				((IQueryElement)SubQuery).ToString(sb, dic);
				sb.Append(')');
			}
		}

		public class InList : BaseNotExpr
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

			public   List<ISqlExpression>  Values { get; } = new List<ISqlExpression>();

			protected override void Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
			{
				base.Walk(options, func);
				for (var i = 0; i < Values.Count; i++)
					Values[i] = Values[i].Walk(options, func)!;
			}

			public override IQueryElement Invert()
			{
				return new InList(Expr1, !WithNull, !IsNot, Values);
			}

			public override QueryElementType ElementType => QueryElementType.InListPredicate;

			protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				Expr1.ToString(sb, dic);

				if (IsNot) sb.Append(" NOT");
				sb.Append(" IN (");

				foreach (var value in Values)
				{
					value.ToString(sb, dic);
					sb.Append(',');
				}

				if (Values.Count > 0)
					sb.Length--;

				sb.Append(')');
			}
		}

		// CONTAINS ( { column | * } , '< contains_search_condition >' )
		// FREETEXT ( { column | * } , 'freetext_string' )
		// expression { = | <> | != | > | >= | !> | < | <= | !< } { ALL | SOME | ANY } ( subquery )
		// EXISTS ( subquery )

		public class FuncLike : SqlPredicate
		{
			public FuncLike(SqlFunction func)
				: base(func.Precedence)
			{
				Function = func;
			}

			public SqlFunction Function { get; private set; }

			protected override void Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
			{
				Function = (SqlFunction)((ISqlExpression)Function).Walk(options, func)!;
			}

			public override bool CanBeNull => Function.CanBeNull;

			public override QueryElementType ElementType => QueryElementType.FuncLikePredicate;

			protected override void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
			{
				((IQueryElement)Function).ToString(sb, dic);
			}
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return ((IQueryElement)this).ToString(new StringBuilder(), new Dictionary<IQueryElement,IQueryElement>()).ToString();
		}

#endif

		#endregion

		protected SqlPredicate(int precedence)
		{
			Precedence = precedence;
		}

		#region IPredicate Members

		public             int               Precedence { get; }

		public    abstract bool              CanBeNull  { get; }
		protected abstract void              Walk     (WalkOptions options, Func<ISqlExpression,ISqlExpression> func);

		ISqlExpression? ISqlExpressionWalkable.Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			Walk(options, func);
			return null;
		}

		#endregion

		#region IQueryElement Members

		public abstract QueryElementType ElementType { get; }

		protected abstract void ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic);

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			if (dic.ContainsKey(this))
				return sb.Append("...");

			dic.Add(this, this);
			ToString(sb, dic);
			dic.Remove(this);

			return sb;
		}

		#endregion
	}
}
