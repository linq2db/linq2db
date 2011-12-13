using System;

namespace LinqToDB.Data.Sql
{
	public interface IChild<T>
	{
		string Name   { get; }
		T      Parent { get; set; }
	}
}
