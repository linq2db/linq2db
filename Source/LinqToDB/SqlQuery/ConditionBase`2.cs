using System;

namespace LinqToDB.SqlQuery
{
	#region ConditionBase

	interface IConditionExpr<T>
	{
		T Expr    (ISqlExpression expr);
		T Field   (SqlField       field);
		T SubQuery(SelectQuery    selectQuery);
		T Value   (object         value);
	}

	public abstract class ConditionBase<T1,T2> : IConditionExpr<ConditionBase<T1,T2>.Expr_>
		where T1 : ConditionBase<T1,T2>
	{
		public class Expr_
		{
			internal Expr_(ConditionBase<T1,T2> condition, bool isNot, ISqlExpression expr)
			{
				_condition = condition;
				_isNot     = isNot;
				_expr      = expr;
			}

			readonly ConditionBase<T1,T2> _condition;
			readonly bool                 _isNot;
			readonly ISqlExpression       _expr;

			T2 Add(ISqlPredicate predicate)
			{
				_condition.Search.Conditions.Add(new SqlCondition(_isNot, predicate));
				return _condition.GetNext();
			}

			#region Predicate.ExprExpr

			public class Op_ : IConditionExpr<T2>
			{
				internal Op_(Expr_ expr, SqlPredicate.Operator op)
				{
					_expr = expr;
					_op   = op;
				}

				readonly Expr_              _expr;
				readonly SqlPredicate.Operator _op;

				public T2 Expr    (ISqlExpression expr)       { return _expr.Add(new SqlPredicate.ExprExpr(_expr._expr, _op, expr)); }
				public T2 Field   (SqlField      field)       { return Expr(field);               }
				public T2 SubQuery(SelectQuery   selectQuery) { return Expr(selectQuery);         }
				public T2 Value   (object        value)       { return Expr(new SqlValue(value)); }

				public T2 All     (SelectQuery   subQuery)    { return Expr(SqlFunction.CreateAll (subQuery)); }
				public T2 Some    (SelectQuery   subQuery)    { return Expr(SqlFunction.CreateSome(subQuery)); }
				public T2 Any     (SelectQuery   subQuery)    { return Expr(SqlFunction.CreateAny (subQuery)); }
			}

			public Op_ Equal          => new Op_(this, SqlPredicate.Operator.Equal);
			public Op_ NotEqual       => new Op_(this, SqlPredicate.Operator.NotEqual);
			public Op_ Greater        => new Op_(this, SqlPredicate.Operator.Greater);
			public Op_ GreaterOrEqual => new Op_(this, SqlPredicate.Operator.GreaterOrEqual);
			public Op_ NotGreater     => new Op_(this, SqlPredicate.Operator.NotGreater);
			public Op_ Less           => new Op_(this, SqlPredicate.Operator.Less);
			public Op_ LessOrEqual    => new Op_(this, SqlPredicate.Operator.LessOrEqual);
			public Op_ NotLess        => new Op_(this, SqlPredicate.Operator.NotLess);

			#endregion

			#region Predicate.Like

			public T2 Like(ISqlExpression expression, SqlValue escape) { return Add(new SqlPredicate.Like(_expr, false, expression, escape)); }
			public T2 Like(ISqlExpression expression)                  { return Like(expression, null); }
			public T2 Like(string expression,         SqlValue escape) { return Like(new SqlValue(expression), escape); }
			public T2 Like(string expression)                          { return Like(new SqlValue(expression), null);   }

			#endregion

			#region Predicate.Between

			public T2 Between   (ISqlExpression expr1, ISqlExpression expr2) { return Add(new SqlPredicate.Between(_expr, false, expr1, expr2)); }
			public T2 NotBetween(ISqlExpression expr1, ISqlExpression expr2) { return Add(new SqlPredicate.Between(_expr, true,  expr1, expr2)); }

			#endregion

			#region Predicate.IsNull

			public T2 IsNull    => Add(new SqlPredicate.IsNull(_expr, false));
			public T2 IsNotNull => Add(new SqlPredicate.IsNull(_expr, true));

			#endregion

			#region Predicate.In

			public T2 In   (SelectQuery subQuery) { return Add(new SqlPredicate.InSubQuery(_expr, false, subQuery)); }
			public T2 NotIn(SelectQuery subQuery) { return Add(new SqlPredicate.InSubQuery(_expr, true,  subQuery)); }

			SqlPredicate.InList CreateInList(bool isNot, object[] exprs)
			{
				var list = new SqlPredicate.InList(_expr, isNot, null);

				if (exprs != null && exprs.Length > 0)
				{
					foreach (var item in exprs)
					{
						if (item == null || item is SqlValue && ((SqlValue)item).Value == null)
							continue;

						if (item is ISqlExpression)
							list.Values.Add((ISqlExpression)item);
						else
							list.Values.Add(new SqlValue(item));
					}
				}

				return list;
			}

			public T2 In   (params object[] exprs) { return Add(CreateInList(false, exprs)); }
			public T2 NotIn(params object[] exprs) { return Add(CreateInList(true,  exprs)); }

			#endregion
		}

		public class Not_ : IConditionExpr<Expr_>
		{
			internal Not_(ConditionBase<T1,T2> condition)
			{
				_condition = condition;
			}

			readonly ConditionBase<T1,T2> _condition;

			public Expr_ Expr    (ISqlExpression expr)        { return new Expr_(_condition, true, expr); }
			public Expr_ Field   (SqlField       field)       { return Expr(field);               }
			public Expr_ SubQuery(SelectQuery    selectQuery) { return Expr(selectQuery);         }
			public Expr_ Value   (object         value)       { return Expr(new SqlValue(value)); }

			public T2 Exists(SelectQuery subQuery)
			{
				_condition.Search.Conditions.Add(new SqlCondition(true, new SqlPredicate.FuncLike(SqlFunction.CreateExists(subQuery))));
				return _condition.GetNext();
			}
		}

		protected abstract SqlSearchCondition Search { get; }
		protected abstract T2                 GetNext();

		protected T1 SetOr(bool value)
		{
			Search.Conditions[Search.Conditions.Count - 1].IsOr = value;
			return (T1)this;
		}

		public Not_  Not => new Not_(this);

		public Expr_ Expr    (ISqlExpression expr)        { return new Expr_(this, false, expr); }
		public Expr_ Field   (SqlField       field)       { return Expr(field);                  }
		public Expr_ SubQuery(SelectQuery    selectQuery) { return Expr(selectQuery);            }
		public Expr_ Value   (object         value)       { return Expr(new SqlValue(value));    }

		public T2 Exists(SelectQuery subQuery)
		{
			Search.Conditions.Add(new SqlCondition(false, new SqlPredicate.FuncLike(SqlFunction.CreateExists(subQuery))));
			return GetNext();
		}
	}

	#endregion
}
