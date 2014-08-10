using System;

namespace LinqToDB.Data
{
	public class BulkCopyRowsCopied
	{
		/// <summary>
		/// Gets or sets a value that indicates whether the bulk copy operation should be aborted.
		/// </summary>
		public bool Abort { get; set; }

		/// <summary>
		/// Gets a value that returns the number of rows copied during the current bulk copy operation.
		/// </summary>
		public long RowsCopied { get; internal set; }

		readonly DateTime _startTime = DateTime.Now;

		public DateTime StartTime { get { return _startTime; } }
	}
}
