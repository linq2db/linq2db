using System;

namespace LinqToDB.Sql
{
	public interface IChild<T>
	{
		string Name   { get; }
		T      Parent { get; set; }
	}
}
