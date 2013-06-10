using System;
using System.Data;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlBuilder;
	using SqlProvider;

	public class SqlServer2005SqlProvider : SqlServerSqlProvider
	{
		public SqlServer2005SqlProvider(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlFunction)
				return ConvertConvertFunction((SqlFunction)expr);

			return expr;
		}

		protected override ISqlProvider CreateSqlProvider()
		{
			return new SqlServer2005SqlProvider(SqlProviderFlags);
		}

		protected override void BuildDataType(StringBuilder sb, SqlDataType type)
		{
			switch (type.SqlDbType)
			{
				case SqlDbType.DateTimeOffset :
				case SqlDbType.DateTime2      :
				case SqlDbType.Time           :
				case SqlDbType.Date           : sb.Append("DateTime");        break;
				default                       : base.BuildDataType(sb, type); break;
			}
		}

		protected override void BuildFunction(StringBuilder sb, SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(sb, func);
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2005; }
		}
	}
}
