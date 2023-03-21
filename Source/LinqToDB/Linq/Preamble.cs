using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Linq
{
	abstract class Preamble
	{
		public abstract object Execute(IDataContext dataContext, Expression expression, object?[]? parameters, object[]? preambles);
		public abstract Task<object> ExecuteAsync(IDataContext dataContext, Expression expression, object?[]? parameters, object[]? preambles, CancellationToken cancellationToken);
	}
}
