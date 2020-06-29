﻿using System;

namespace LinqToDB.DataProvider.SqlCe
{
	using LinqToDB.Extensions;
	using LinqToDB.SqlQuery;
	using SqlProvider;

	class SqlCeSqlOptimizer : BasicSqlOptimizer
	{
		public SqlCeSqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement)
		{
			switch (statement.QueryType)
			{
				case QueryType.Delete :
					statement = GetAlternativeDelete((SqlDeleteStatement) statement);
					statement.SelectQuery!.From.Tables[0].Alias = "$";
					break;

				case QueryType.Update :
					statement = GetAlternativeUpdate((SqlUpdateStatement) statement);
					break;
			}

			statement = CorrectSkipAndColumns(statement);

			// call fixer after CorrectSkipAndColumns for remaining cases
			base.FixEmptySelect(statement);

			return statement;
		}

		public SqlStatement CorrectSkipAndColumns(SqlStatement statement)
		{
			new QueryVisitor().Visit(statement, e =>
			{
				switch (e.ElementType)
				{
					case QueryElementType.SqlQuery:
						{
							var q = (SelectQuery)e;

							if (q.Select.SkipValue != null && q.OrderBy.IsEmpty)
							{
								if (q.Select.Columns.Count == 0)
								{
									var source = q.Select.From.Tables[0].Source;
									var keys   = source.GetKeys(true);

									foreach (var key in keys)
									{
										q.Select.AddNew(key);
									}
								}

								for (var i = 0; i < q.Select.Columns.Count; i++)
									q.OrderBy.ExprAsc(q.Select.Columns[i].Expression);

								if (q.OrderBy.IsEmpty)
								{
									throw new LinqToDBException("Order by required for Skip operation.");
								}
							}

							// looks like SqlCE do not allow '*' for grouped records
							if (!q.GroupBy.IsEmpty && q.Select.Columns.Count == 0)
							{
								q.Select.Add(new SqlValue(1));
							}

							break;
						}
				}
			});

			return statement;
		}

		protected override void FixEmptySelect(SqlStatement statement)
		{
			// already fixed by CorrectSkipAndColumns
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			if (SqlCeConfiguration.InlineFunctionParameters && expr is SqlFunction sqlFunction)
			{
				foreach (var parameter in sqlFunction.Parameters)
				{
					if (parameter.ElementType == QueryElementType.SqlParameter && parameter is SqlParameter sqlParameter)
					{
						sqlParameter.IsQueryParameter = false;
					}
				}
			}

			expr = base.ConvertExpression(expr);

			switch (expr)
			{
				case SqlBinaryExpression be:
					switch (be.Operation)
					{
						case "%":
							return be.Expr1.SystemType!.IsIntegerType()?
								be :
								new SqlBinaryExpression(
									typeof(int),
									new SqlFunction(typeof(int), "Convert", SqlDataType.Int32, be.Expr1),
									be.Operation,
									be.Expr2,
									be.Precedence);
					}

					break;

				case SqlFunction func:
					switch (func.Name)
					{
						case "Convert" :
							switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
							{
								case TypeCode.UInt64 :
									if (func.Parameters[1].SystemType!.IsFloatType())
										return new SqlFunction(
											func.SystemType,
											func.Name,
											false,
											func.Precedence,
											func.Parameters[0],
											new SqlFunction(func.SystemType, "Floor", func.Parameters[1]));

									break;

								case TypeCode.DateTime :
									var type1 = func.Parameters[1].SystemType!.ToUnderlying();

									if (IsTimeDataType(func.Parameters[0]))
									{
										if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
											return new SqlExpression(
												func.SystemType, "Cast(Convert(NChar, {0}, 114) as DateTime)", Precedence.Primary, func.Parameters[1]);

										if (func.Parameters[1].SystemType == typeof(string))
											return func.Parameters[1];

										return new SqlExpression(
											func.SystemType, "Convert(NChar, {0}, 114)", Precedence.Primary, func.Parameters[1]);
									}

									if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
									{
										if (IsDateDataType(func.Parameters[0], "Datetime"))
											return new SqlExpression(
												func.SystemType, "Cast(Floor(Cast({0} as Float)) as DateTime)", Precedence.Primary, func.Parameters[1]);
									}

									break;
							}

							break;
					}

					break;
			}

			return expr;
		}
	}
}
