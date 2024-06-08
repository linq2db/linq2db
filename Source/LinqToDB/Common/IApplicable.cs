using System;

namespace LinqToDB.Common
{
	interface IApplicable<in T>
	{
		void Apply(T obj);
	}
}
