using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using LinqToDB.SqlQuery;

	public class SqlServer2012SqlExpressionConvertVisitor : SqlServer2008SqlExpressionConvertVisitor
	{
		public SqlServer2012SqlExpressionConvertVisitor(bool allowModify, SqlServerVersion sqlServerVersion) : base(allowModify, sqlServerVersion)
		{
		}

		public override ISqlExpression ConvertSqlFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);

			switch (func.Name)
			{
				case PseudoFunctions.TRY_CONVERT:
					return new SqlFunction(func.SystemType, "TRY_CONVERT", false, true, func.Parameters[0], func.Parameters[2]) { CanBeNull = true };

				case "CASE"     :

					if (func.Parameters.Length <= 5)
						func = ConvertCase(func.CanBeNull, func.SystemType, func.Parameters, 0);

					break;
			}

			return base.ConvertSqlFunction(func);
		}

		static SqlFunction ConvertCase(bool canBeNull, Type systemType, ISqlExpression[] parameters, int start)
		{
			var len  = parameters.Length - start;
			var name = start == 0 ? "IIF" : "CASE";
			var cond = parameters[start];

			if (start == 0 && SqlExpression.NeedsEqual(cond))
			{
				cond = new SqlSearchCondition(
					new SqlCondition(
						false,
						new SqlPredicate.ExprExpr(cond, SqlPredicate.Operator.Equal, new SqlValue(1), null)));
			}

			if (len == 3)
				return new SqlFunction(systemType, name, cond, parameters[start + 1], parameters[start + 2]) { CanBeNull = canBeNull };

			return new SqlFunction(systemType, name,
				cond,
				parameters[start                                     + 1],
				ConvertCase(canBeNull, systemType, parameters, start + 2)) { CanBeNull = canBeNull };
		}
	}
}
