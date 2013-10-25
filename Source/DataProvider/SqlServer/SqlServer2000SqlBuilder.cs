using System;
using System.Text;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;

	class SqlServer2000SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2000SqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags)
			: base(sqlOptimizer, sqlProviderFlags)
		{
		}

		protected override string FirstFormat { get { return "TOP {0}"; } }

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2000SqlBuilder(SqlOptimizer, SqlProviderFlags);
		}

		protected override void BuildDataType(SqlDataType type, bool createDbType = false)
		{
			switch (type.DataType)
			{
				case DataType.DateTimeOffset :
				case DataType.DateTime2      :
				case DataType.Time           :
				case DataType.Date           : StringBuilder.Append("DateTime"); break;
				default                      : base.BuildDataType(type);         break;
			}
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2000; }
		}
	}
}
