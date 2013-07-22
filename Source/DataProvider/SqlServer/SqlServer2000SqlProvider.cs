using System;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlBuilder;
	using SqlProvider;

	public class SqlServer2000SqlProvider : SqlServerSqlProvider
	{
		public SqlServer2000SqlProvider(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
		{
		}

		protected override string FirstFormat { get { return "TOP {0}"; } }

		public override ISqlExpression ConvertExpression(ISqlExpression expr)
		{
			expr = base.ConvertExpression(expr);

			if (expr is SqlFunction)
				return ConvertConvertFunction((SqlFunction)expr);

			return expr;
		}

		protected override ISqlProvider CreateSqlProvider()
		{
			return new SqlServer2000SqlProvider(SqlProviderFlags);
		}

		protected override void BuildDataType(StringBuilder sb, SqlDataType type)
		{
			switch (type.DataType)
			{
				case DataType.DateTimeOffset :
				case DataType.DateTime2      :
				case DataType.Time           :
				case DataType.Date           : sb.Append("DateTime");        break;
				default                      : base.BuildDataType(sb, type); break;
			}
		}

		protected override void BuildFunction(StringBuilder sb, SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(sb, func);
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2000; }
		}
	}
}
