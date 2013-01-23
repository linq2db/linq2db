using System;
using System.Data;

namespace LinqToDB.DataProvider
{
	using Extensions;
	using SqlBuilder;
	using SqlProvider;

	public class SqlServer2005SqlProvider : SqlServerSqlProvider
	{
		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlFunction)
			{
				var func = (SqlFunction)expr;

				switch (Type.GetTypeCode(func.SystemType.ToUnderlying()))
				{
					case TypeCode.DateTime :

						if (func.Name == "Convert")
						{
							var type1 = func.Parameters[1].SystemType.ToUnderlying();

							if (IsTimeDataType(func.Parameters[0]))
							{
								if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
									return new SqlExpression(
										func.SystemType, "Cast(Convert(Char, {0}, 114) as DateTime)", Precedence.Primary, func.Parameters[1]);

								if (func.Parameters[1].SystemType == typeof(string))
									return func.Parameters[1];

								return new SqlExpression(
									func.SystemType, "Convert(Char, {0}, 114)", Precedence.Primary, func.Parameters[1]);
							}

							if (type1 == typeof(DateTime) || type1 == typeof(DateTimeOffset))
							{
								if (IsDateDataType(func.Parameters[0], "Datetime"))
									return new SqlExpression(
										func.SystemType, "Cast(Floor(Cast({0} as Float)) as DateTime)", Precedence.Primary, func.Parameters[1]);
							}

							if (func.Parameters.Length == 2 && func.Parameters[0] is SqlDataType && func.Parameters[0] == SqlDataType.DateTime)
								return new SqlFunction(func.SystemType, func.Name, func.Precedence, func.Parameters[0], func.Parameters[1], new SqlValue(120));
						}

						break;
				}
			}

			return expr;
		}

		protected override ISqlProvider CreateSqlProvider()
		{
			return new SqlServer2005SqlProvider();
		}

		protected override void BuildDataType(System.Text.StringBuilder sb, SqlDataType type)
		{
			switch (type.SqlDbType)
			{
#if !MONO
				case SqlDbType.DateTimeOffset :
				case SqlDbType.DateTime2      :
#endif
				case SqlDbType.Time           :
				case SqlDbType.Date           : sb.Append("DateTime");        break;
				default                       : base.BuildDataType(sb, type); break;
			}
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2005; }
		}
	}
}
