using System;
using System.Collections.Generic;

namespace LinqToDB.DataProvider
{
	public interface IPathableSqlBuilder
	{
		public Dictionary<string,string>? TableIDs  { get; }
		string?                           TablePath { get; }
		string?                           QueryName { get; }
	}
}
