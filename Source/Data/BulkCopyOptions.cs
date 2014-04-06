using System;

namespace LinqToDB.Data
{
	public class BulkCopyOptions
	{
		public int?         MaxBatchSize       { get; set; }
		public int?         BulkCopyTimeout    { get; set; }
		public BulkCopyType BulkCopyType       { get; set; }
		public bool?        IgnoreSkipOnInsert { get; set; }
	}
}
