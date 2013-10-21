using System;
using System.Diagnostics;

namespace LinqToDB.Data
{
	using System.Data;

	public class TraceInfo
	{
		public bool           BeforeExecute   { get; set; }
		public TraceLevel     TraceLevel      { get; set; }
		public DataConnection DataConnection  { get; set; }
		public IDbCommand     Command         { get; set; }
		public TimeSpan?      ExecutionTime   { get; set; }
		public int?           RecordsAffected { get; set; }
		public Exception      Exception       { get; set; }
	}
}
