using System;

namespace LinqToDB.Data
{
	public class BulkCopyOptions
	{
		public int?         MaxBatchSize       { get; set; }
		public int?         BulkCopyTimeout    { get; set; }
		public BulkCopyType BulkCopyType       { get; set; }
		[Obsolete]
		public bool?        IgnoreSkipOnInsert { get; set; }
		public bool?        KeepIdentity       { get; set; }
		public bool?        CheckConstraints   { get; set; }

		public Action<BulkCopyRowsCopied> RowsCopiedCallback { get; set; }
	}
}
