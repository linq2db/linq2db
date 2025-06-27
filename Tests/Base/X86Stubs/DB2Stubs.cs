#if DB2STUBS
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace IBM.Data.DB2
{
	public abstract class DB2Connection : DbConnection { }
}
namespace IBM.Data.Db2
{
	public abstract class DB2Connection : DbConnection { }
	public abstract class DB2Exception  : DbException  { }
}
namespace IBM.Data.DB2.Core
{
	public abstract class DB2Connection : DbConnection { }
}
namespace IBM.Data.DB2Types
{
	public struct DB2TimeStamp
	{
		public DB2TimeStamp(int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8) { }
		public DB2TimeStamp(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}

	public struct DB2Xml
	{
		public DB2Xml(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2Double
	{
		public DB2Double(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2Decimal
	{
		public DB2Decimal(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2Int64
	{
		public DB2Int64(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2Int32
	{
		public DB2Int32(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2Int16
	{
		public DB2Int16(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2DecimalFloat
	{
		public DB2DecimalFloat(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2Real
	{
		public DB2Real(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2Real370
	{
		public DB2Real370(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2String
	{
		public DB2String(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2Binary
	{
		public DB2Binary(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2Date
	{
		public DB2Date(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2Time
	{
		public DB2Time(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2RowId
	{
		public DB2RowId(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2DateTime
	{
		public DB2DateTime(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;

		public static  explicit operator DateTime(DB2DateTime _) => default;
	}
	public struct DB2Clob
	{
		public DB2Clob(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
	public struct DB2Blob
	{
		public DB2Blob(object _) { }
		public bool IsNull { get; set; } = true;
		public object? Value { get; set; } = true;
	}
}
#endif
