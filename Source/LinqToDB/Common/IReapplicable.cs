using System;

namespace LinqToDB.Common
{
	interface IReapplicable<T>
	{
		Action? Apply(T obj, object? previousObject);
	}
}
