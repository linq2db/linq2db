using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	using SqlQuery;

	abstract class Preamble
	{
		public abstract object       Execute(IDataContext      dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles);
		public abstract Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken);

		public abstract void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values);
	}
}
