using System;
using System.Data;

namespace LinqToDB.Data
{
	public class DataReader : IDisposable
	{
		public   DataConnection Connection { get; set; }
		public   IDataReader    Reader     { get; set; }
		internal int            ReadNumber { get; set; }

		public void Dispose()
		{
			if (Reader != null)
				Reader.Dispose();
		}
	}
}
