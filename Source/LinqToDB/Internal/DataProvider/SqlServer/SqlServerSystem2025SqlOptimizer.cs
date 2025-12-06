using System;

using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	public class SqlServerSystem2025SqlOptimizer(SqlProviderFlags sqlProviderFlags) : SqlServer2022SqlOptimizer(sqlProviderFlags, SqlServerVersion.v2025)
	{
		public override SqlStatement Finalize(MappingSchema mappingSchema, SqlStatement statement, DataOptions dataOptions)
		{
			statement.VisitAll(SetQueryParameter);

			return base.Finalize(mappingSchema, statement, dataOptions);
		}

		static void SetQueryParameter(IQueryElement element)
		{
			if (element is SqlParameter p)
			{
				if (p.Type.SystemType == typeof(float[])
#if NET8_0_OR_GREATER
					|| p.Type.SystemType == typeof(Half[])
#endif
						)
				{
					p.IsQueryParameter = false;
				}
			}
		}
	}
}
