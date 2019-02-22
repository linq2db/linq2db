using System;
using System.Runtime.InteropServices;

// ReSharper disable CheckNamespace

namespace System
{
	[ComVisible(true)]
	public interface ICloneable
	{
		object Clone();
	}

#if !NETSTANDARD
	[ComVisible(true)]
	[Serializable]
	public enum TypeCode
	{
		Empty    =  0,
		Object   =  1,
		DBNull   =  2,
		Boolean  =  3,
		Char     =  4,
		SByte    =  5,
		Byte     =  6,
		Int16    =  7,
		UInt16   =  8,
		Int32    =  9,
		UInt32   = 10,
		Int64    = 11,
		UInt64   = 12,
		Single   = 13,
		Double   = 14,
		Decimal  = 15,
		DateTime = 16,
		String   = 18,
	}

	[ComVisible(true)]
	[Serializable]
	public sealed class DBNull
	{
		public static readonly DBNull Value = new DBNull();

		private DBNull()
		{
		}

		public override string ToString()
		{
			return string.Empty;
		}

		public string ToString(IFormatProvider provider)
		{
			return string.Empty;
		}

		public TypeCode GetTypeCode()
		{
			return TypeCode.DBNull;
		}
	}
#endif
}

namespace System.Threading
{
	public static class Extensions
	{
		public static void Close(this ManualResetEvent ev)
		{
			ev.Dispose();
		}
	}
}
