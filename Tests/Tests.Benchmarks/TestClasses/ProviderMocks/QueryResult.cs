using System;
using System.Data;

namespace LinqToDB.Benchmarks.TestProvider
{
	public class QueryResult
	{
		public DataTable   Schema     = null!;

		public string[]    Names      = null!;
		public string[]    DbTypes    = null!;
		public Type[]      FieldTypes = null!;

		public object?[][] Data       = null!;
	}
}
