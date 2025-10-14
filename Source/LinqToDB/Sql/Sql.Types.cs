#pragma warning disable IDE0060 // Remove unused parameter
using System;

using LinqToDB.Mapping;

namespace LinqToDB
{
	public partial class Sql
	{
		public static class Types
		{
			public static bool     Bit => false;
			public static long     BigInt => 0;
			public static int      Int => 0;
			public static short    SmallInt => 0;
			public static byte     TinyInt => 0;
			public static decimal  DefaultDecimal => 0m;
			public static decimal  Decimal(int precision) => 0m;
			public static decimal  Decimal([SqlQueryDependent] int precision, [SqlQueryDependent] int scale) => 0m;
			public static decimal  Money => 0m;
			public static decimal  SmallMoney => 0m;
			public static double   Float => 0.0;
			public static float    Real => 0f;
			public static DateTime DateTime => DateTime.Now;
			public static DateTime DateTime2 => DateTime.Now;
			public static DateTime SmallDateTime => DateTime.Now;
			public static DateTime Date => DateTime.Now;
#if SUPPORTS_DATEONLY
			public static DateOnly DateOnly => DateOnly.FromDateTime(DateTime.Now);
#endif
			public static DateTime Time => DateTime.Now;
			public static DateTimeOffset DateTimeOffset => DateTimeOffset.Now;
			public static string  Char(int length) => "";
			public static string  DefaultChar => "";
			public static string  VarChar(int length) => "";
			public static string  DefaultVarChar => "";
			public static string  NChar(int length) => "";
			public static string  DefaultNChar => "";
			public static string  NVarChar(int length) => "";
			public static string  DefaultNVarChar => "";
		}
	}
}
