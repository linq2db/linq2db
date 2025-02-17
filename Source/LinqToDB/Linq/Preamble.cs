using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Linq
{
	abstract class Preamble
	{
		public abstract object       Execute(IDataContext      dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles);
		public abstract Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken);

		public abstract void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values);
	}
}
