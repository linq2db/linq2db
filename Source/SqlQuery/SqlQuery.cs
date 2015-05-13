using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using LinqToDB.Extensions;
	using Reflection;

	public abstract class SqlQuery : IQueryElement
	{
		public abstract QueryElementType ElementType { get; }

		StringBuilder IQueryElement.ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			throw new NotImplementedException();
		}

		#region Parameters

		readonly List<SqlParameter> _parameters = new List<SqlParameter>();
		public   List<SqlParameter>  Parameters
		{
			get { return _parameters; }
		}

		public bool IsParameterDependent { get; set; }

		public SqlQuery ProcessParameters()
		{
			if (IsParameterDependent)
			{
				var query = new QueryVisitor().Convert(this, e =>
				{
					switch (e.ElementType)
					{
						case QueryElementType.SqlParameter :
							{
								var p = (SqlParameter)e;

								if (p.Value == null)
									return new SqlValue(null);
							}

							break;

						case QueryElementType.ExprExprPredicate :
							{
								var ee = (SelectQuery.Predicate.ExprExpr)e;
								
								if (ee.Operator == SelectQuery.Predicate.Operator.Equal || ee.Operator == SelectQuery.Predicate.Operator.NotEqual)
								{
									object value1;
									object value2;

									if (ee.Expr1 is SqlValue)
										value1 = ((SqlValue)ee.Expr1).Value;
									else if (ee.Expr1 is SqlParameter)
										value1 = ((SqlParameter)ee.Expr1).Value;
									else
										break;

									if (ee.Expr2 is SqlValue)
										value2 = ((SqlValue)ee.Expr2).Value;
									else if (ee.Expr2 is SqlParameter)
										value2 = ((SqlParameter)ee.Expr2).Value;
									else
										break;

									var value = Equals(value1, value2);

									if (ee.Operator == SelectQuery.Predicate.Operator.NotEqual)
										value = !value;

									return new SelectQuery.Predicate.Expr(new SqlValue(value), PrecedenceLevel.Comparison);
								}
							}

							break;

						case QueryElementType.InListPredicate :
							return ConvertInListPredicate((SelectQuery.Predicate.InList)e);
					}

					return null;
				});

				if (query != this)
				{
					query.Parameters.Clear();

					new QueryVisitor().VisitAll(query, expr =>
					{
						switch (expr.ElementType)
						{
							case QueryElementType.SqlParameter :
								{
									var p = (SqlParameter)expr;
									if (p.IsQueryParameter)
										query.Parameters.Add(p);

									break;
								}
						}
					});
				}

				return query;
			}

			return this;
		}

		static SelectQuery.Predicate ConvertInListPredicate(SelectQuery.Predicate.InList p)
		{
			if (p.Values == null || p.Values.Count == 0)
				return new SelectQuery.Predicate.Expr(new SqlValue(p.IsNot));

			if (p.Values.Count == 1 && p.Values[0] is SqlParameter)
			{
				var pr = (SqlParameter)p.Values[0];

				if (pr.Value == null)
					return new SelectQuery.Predicate.Expr(new SqlValue(p.IsNot));

				if (pr.Value is IEnumerable)
				{
					var items = (IEnumerable)pr.Value;

					if (p.Expr1 is ISqlTableSource)
					{
						var table = (ISqlTableSource)p.Expr1;
						var keys  = table.GetKeys(true);

						if (keys == null || keys.Count == 0)
							throw new SqlException("Cant create IN expression.");

						if (keys.Count == 1)
						{
							var values = new List<ISqlExpression>();
							var field  = GetUnderlayingField(keys[0]);
							var cd     = field.ColumnDescriptor;

							foreach (var item in items)
							{
								var value = cd.MemberAccessor.GetValue(item);
								values.Add(cd.MappingSchema.GetSqlValue(cd.MemberType, value));
							}

							if (values.Count == 0)
								return new SelectQuery.Predicate.Expr(new SqlValue(p.IsNot));

							return new SelectQuery.Predicate.InList(keys[0], p.IsNot, values);
						}

						{
							var sc = new SelectQuery.SearchCondition();

							foreach (var item in items)
							{
								var itemCond = new SelectQuery.SearchCondition();

								foreach (var key in keys)
								{
									var field = GetUnderlayingField(key);
									var cd    = field.ColumnDescriptor;
									var value = cd.MemberAccessor.GetValue(item);
									var cond  = value == null ?
										new SelectQuery.Condition(false, new SelectQuery.Predicate.IsNull  (field, false)) :
										new SelectQuery.Condition(false, new SelectQuery.Predicate.ExprExpr(field, SelectQuery.Predicate.Operator.Equal, cd.MappingSchema.GetSqlValue(value)));

									itemCond.Conditions.Add(cond);
								}

								sc.Conditions.Add(new SelectQuery.Condition(false, new SelectQuery.Predicate.Expr(itemCond), true));
							}

							if (sc.Conditions.Count == 0)
								return new SelectQuery.Predicate.Expr(new SqlValue(p.IsNot));

							if (p.IsNot)
								return new SelectQuery.Predicate.NotExpr(sc, true, PrecedenceLevel.LogicalNegation);

							return new SelectQuery.Predicate.Expr(sc, PrecedenceLevel.LogicalDisjunction);
						}
					}

					if (p.Expr1 is SqlExpression)
					{
						var expr = (SqlExpression)p.Expr1;

						if (expr.Expr.Length > 1 && expr.Expr[0] == '\x1')
						{
							var type  = items.GetListItemType();
							var ta    = TypeAccessor.GetAccessor(type);
							var names = expr.Expr.Substring(1).Split(',');

							if (expr.Parameters.Length == 1)
							{
								var values = new List<ISqlExpression>();

								foreach (var item in items)
								{
									var ma    = ta[names[0]];
									var value = ma.GetValue(item);
									values.Add(new SqlValue(value));
								}

								if (values.Count == 0)
									return new SelectQuery.Predicate.Expr(new SqlValue(p.IsNot));

								return new SelectQuery.Predicate.InList(expr.Parameters[0], p.IsNot, values);
							}

							{
								var sc = new SelectQuery.SearchCondition();

								foreach (var item in items)
								{
									var itemCond = new SelectQuery.SearchCondition();

									for (var i = 0; i < expr.Parameters.Length; i++)
									{
										var sql   = expr.Parameters[i];
										var value = ta[names[i]].GetValue(item);
										var cond  = value == null ?
											new SelectQuery.Condition(false, new SelectQuery.Predicate.IsNull  (sql, false)) :
											new SelectQuery.Condition(false, new SelectQuery.Predicate.ExprExpr(sql, SelectQuery.Predicate.Operator.Equal, new SqlValue(value)));

										itemCond.Conditions.Add(cond);
									}

									sc.Conditions.Add(new SelectQuery.Condition(false, new SelectQuery.Predicate.Expr(itemCond), true));
								}

								if (sc.Conditions.Count == 0)
									return new SelectQuery.Predicate.Expr(new SqlValue(p.IsNot));

								if (p.IsNot)
									return new SelectQuery.Predicate.NotExpr(sc, true, PrecedenceLevel.LogicalNegation);

								return new SelectQuery.Predicate.Expr(sc, PrecedenceLevel.LogicalDisjunction);
							}
						}
					}
				}
			}

			return null;
		}

		static SqlField GetUnderlayingField(ISqlExpression expr)
		{
			switch (expr.ElementType)
			{
				case QueryElementType.SqlField: return (SqlField)expr;
				case QueryElementType.Column  : return GetUnderlayingField(((SelectQuery.Column)expr).Expression);
			}

			throw new InvalidOperationException();
		}

		#endregion
	}
}
