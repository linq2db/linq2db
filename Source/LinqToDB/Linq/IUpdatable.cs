using System.Linq;

namespace LinqToDB.Linq
{
	public interface IUpdatable<T>
	{
		internal IQueryable<T> Query { get; }
	}
}
