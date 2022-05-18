using System;

namespace LinqToDB.Infrastructure
{
	interface IApplicable<in T>
	{
		void Apply(T obj);
	}
}
