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
#pragma warning disable IDE0060 // Remove unused parameter
			public static decimal  Decimal(int precision) => 0m;
			public static decimal  Decimal([SqlQueryDependent] int precision, [SqlQueryDependent] int scale) => 0m;
#pragma warning restore IDE0060 // Remove unused parameter
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
#pragma warning disable IDE0060 // Remove unused parameter
			public static string  Char(int length) => "";
#pragma warning restore IDE0060 // Remove unused parameter
			public static string  DefaultChar => "";
#pragma warning disable IDE0060 // Remove unused parameter
			public static string  VarChar(int length) => "";
#pragma warning restore IDE0060 // Remove unused parameter
			public static string  DefaultVarChar => "";
#pragma warning disable IDE0060 // Remove unused parameter
			public static string  NChar(int length) => "";
#pragma warning restore IDE0060 // Remove unused parameter
			public static string  DefaultNChar => "";
#pragma warning disable IDE0060 // Remove unused parameter
			public static string  NVarChar(int length) => "";
#pragma warning restore IDE0060 // Remove unused parameter
			public static string  DefaultNVarChar => "";
		}
	}
}
