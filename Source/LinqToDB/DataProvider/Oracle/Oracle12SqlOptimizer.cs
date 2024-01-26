using System;

namespace LinqToDB.DataProvider.Oracle
{
	using SqlProvider;
	using SqlQuery;

	public class Oracle12SqlOptimizer : Oracle11SqlOptimizer
	{
		public Oracle12SqlOptimizer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override SqlStatement TransformStatement(SqlStatement statement, DataOptions dataOptions)
		{
			if (statement.IsUpdate() || statement.IsInsert() || statement.IsDelete())
				statement = ReplaceTakeSkipWithRowNum(statement, false);

			switch (statement.QueryType)
			{
				case QueryType.Delete : statement = GetAlternativeDelete((SqlDeleteStatement) statement, dataOptions); break;
				case QueryType.Update : statement = GetAlternativeUpdate((SqlUpdateStatement) statement, dataOptions); break;
			}

			return statement;
		}

		protected override ISqlExpression ConvertFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func, false);

			switch (func.Name)
			{
				case PseudoFunctions.TRY_CONVERT:
					return new SqlExpression(func.SystemType, "CAST({0} AS {1} DEFAULT NULL ON CONVERSION ERROR)", Precedence.Primary, func.Parameters[2], func.Parameters[0])
					{
						CanBeNull = true
					};

				case PseudoFunctions.TRY_CONVERT_OR_DEFAULT:
					return new SqlExpression(func.SystemType, "CAST({0} AS {1} DEFAULT {2} ON CONVERSION ERROR)", Precedence.Primary, func.Parameters[2], func.Parameters[0], func.Parameters[3])
					{
						CanBeNull = func.Parameters[2].CanBeNull || func.Parameters[3].CanBeNull
					};
			}

			return base.ConvertFunction(func);
		}
	}
}
