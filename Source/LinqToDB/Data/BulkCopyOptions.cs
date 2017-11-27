using System;

namespace LinqToDB.Data
{
	public class BulkCopyOptions
	{
		/// <summary>Number of rows in each batch. At the end of each batch, the rows in the batch are sent to the server.</summary>
		/// <returns>The integer value of the <see cref="P:MaxBatchSize"></see> property, or zero if no value has been set.</returns>
		public int?         MaxBatchSize           { get; set; }
		public int?         BulkCopyTimeout        { get; set; }
		public BulkCopyType BulkCopyType           { get; set; }
		[Obsolete("Use the Tools.RetrieveIdentity method instead.")]
		public bool         RetrieveSequence       { get; set; }
		[Obsolete]
		public bool?        IgnoreSkipOnInsert     { get; set; }
		public bool?        CheckConstraints       { get; set; }
		public bool?        KeepIdentity           { get; set; }
		public bool?        TableLock              { get; set; }
		public bool?        KeepNulls              { get; set; }
		public bool?        FireTriggers           { get; set; }
		public bool?        UseInternalTransaction { get; set; }

		public string       DatabaseName           { get; set; }
		public string       SchemaName             { get; set; }
		public string       TableName              { get; set; }

		public int          NotifyAfter            { get; set; }

		public Action<BulkCopyRowsCopied> RowsCopiedCallback { get; set; }
	}
}
