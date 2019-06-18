using System;

namespace LinqToDB.DataProvider.SqlServer
{
	using SqlQuery;
	using SqlProvider;

	class SqlServer2005SqlBuilder : SqlServerSqlBuilder
	{
		public SqlServer2005SqlBuilder(ISqlOptimizer sqlOptimizer, SqlProviderFlags sqlProviderFlags, ValueToSqlConverter valueToSqlConverter)
			: base(sqlOptimizer, sqlProviderFlags, valueToSqlConverter)
		{
		}

		protected override ISqlBuilder CreateSqlBuilder()
		{
			return new SqlServer2005SqlBuilder(SqlOptimizer, SqlProviderFlags, ValueToSqlConverter);
		}

		protected override void BuildDataTypeFromDataType(SqlDataType type, bool forCreateTable)
		{
			switch (type.DataType)
			{
				case DataType.DateTimeOffset :
				case DataType.DateTime2      :
				case DataType.Time           :
				case DataType.Date           : StringBuilder.Append("DateTime");                     break;
				default                      : base.BuildDataTypeFromDataType(type, forCreateTable); break;
			}
		}

		protected override void BuildFunction(SqlFunction func)
		{
			func = ConvertFunctionParameters(func);
			base.BuildFunction(func);
		}

		public override string  Name
		{
			get { return ProviderName.SqlServer2005; }
		}
	}
}
