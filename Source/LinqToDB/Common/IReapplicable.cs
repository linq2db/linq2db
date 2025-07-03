using System;

namespace LinqToDB.Common
{
	interface IReapplicable<T> : IOptionSet
	{
		Action? Apply(T obj, IOptionSet? previousObject);
	}
}
