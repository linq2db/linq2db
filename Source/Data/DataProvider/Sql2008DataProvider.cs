using System;

using LinqToDB.SqlProvider;

namespace LinqToDB.Data.DataProvider
{
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
