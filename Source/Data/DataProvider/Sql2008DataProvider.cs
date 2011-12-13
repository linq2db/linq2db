using System;

namespace LinqToDB.Data.DataProvider
{
	using Sql.SqlProvider;

	public sealed class Sql2008DataProvider : SqlDataProviderBase
	{
		public override string Name
		{
			get { return DataProvider.ProviderName.MsSql2008; }
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new MsSql2008SqlProvider();
		}
	}
}
