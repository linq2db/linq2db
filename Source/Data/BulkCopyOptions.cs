using System;

namespace LinqToDB.Data
{
	public class BulkCopyOptions
	{
		public int? MaxBatchSize    { get; set; }
		public int? BulkCopyTimeout { get; set; }
	}
}
