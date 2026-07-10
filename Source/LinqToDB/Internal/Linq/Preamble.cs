using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.Linq
{
	abstract class Preamble
	{
		public abstract object       Execute(IDataContext      dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles);
		public abstract Task<object> ExecuteAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken);

		public abstract void GetUsedParametersAndValues(ICollection<SqlParameter> parameters, ICollection<SqlValue> values);

		/// <summary>
		/// When <see langword="true"/>, this preamble does not execute a separate query and should not
		/// trigger an implicit transaction. Used by CteUnion single-query mode where the
		/// preamble is a placeholder that resolves data from the main query's result set.
		/// </summary>
		public virtual bool IsInlined => false;
	}
}
