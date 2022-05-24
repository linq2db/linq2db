using System;

namespace LinqToDB
{
	public class DataOptions<T>
	{
		public DataOptions(DataOptions options)
		{
			Options = options;
		}

		public DataOptions Options { get; set; }
	}
}
