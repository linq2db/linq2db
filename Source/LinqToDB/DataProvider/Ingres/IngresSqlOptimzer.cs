using System.Collections.Generic;

namespace LinqToDB.DataProvider.Ingres
{
	using Extensions;
	using SqlProvider;
	using SqlQuery;
	
	class IngresSqlOptimzer : BasicSqlOptimizer
    {
        public IngresSqlOptimzer(SqlProviderFlags sqlProviderFlags) : base(sqlProviderFlags)
        {
        }
    }
}
