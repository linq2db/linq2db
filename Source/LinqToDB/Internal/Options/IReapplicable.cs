using System;

namespace LinqToDB.Internal.Options
{
	interface IReapplicable<T> : IOptionSet
	{
		Action? Apply(T obj, IOptionSet? previousObject);
	}
}
