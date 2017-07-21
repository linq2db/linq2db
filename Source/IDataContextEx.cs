using System;
using System.Linq.Expressions;

namespace LinqToDB
{
	using Linq;

	interface IDataContextEx : IDataContext
	{
		IQueryRunner GetQueryRunner(Query query, int queryNumber, Expression expression, object[] parameters);
		IQueryRunner1 GetQueryRun(Query query, int queryNumber, Expression expression, object[] parameters);
	}
}
