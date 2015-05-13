using System;

namespace LinqToDB
{
	using Linq;

	public interface ITable<out T> : IExpressionQuery<T>
	{
	}
}
