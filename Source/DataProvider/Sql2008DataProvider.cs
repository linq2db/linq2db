using System;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	public sealed class Sql2008DataProvider : SqlDataProviderBase
	{
		public override string Name
		{
			get { return LinqToDB.ProviderName.SqlServer2008; }
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new MsSql2008SqlProvider();
		}
	}
}
