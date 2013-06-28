using System;

namespace LinqToDB
{
	using Linq;

	public interface ITable<
#if !SL4
		out
#endif
		T> : IExpressionQuery<T>
	{
	}
}
