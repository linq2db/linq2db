using System;

namespace LinqToDB.SqlBuilder
{
	public interface IChild<T>
	{
		string Name   { get; }
		T      Parent { get; set; }
	}
}
