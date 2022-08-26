using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	abstract class Preamble
	{
		public abstract object Execute(IDataContext dataContext, Expression expression, object?[]? parameters, object[]? preambles);
		public abstract Task<object> ExecuteAsync(IDataContext dataContext, Expression expression, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken);
	}
}
