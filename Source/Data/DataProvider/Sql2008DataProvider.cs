using System;

namespace LinqToDB.Data.DataProvider
{
	using SqlProvider;

	public sealed class Sql2008DataProvider : SqlDataProviderBase
	{
		public override string Name
		{
			get { return LinqToDB.ProviderName.MsSql2008; }
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new MsSql2008SqlProvider();
		}
	}
}
