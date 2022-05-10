using PN = LinqToDB.ProviderName;

namespace LinqToDB;

public partial class Sql
{
	public static class Types
	{
		[Property(PN.Informix,      "Boolean",        ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.PostgreSQL,    "Boolean",        ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.MySql,         "Boolean",        ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SQLite,        "Boolean",        ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SapHana,       "TinyInt",        ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "Bit",            ServerSideOnly=true, CanBeNull = false)]
		public static bool           Bit => false;

		[Property(PN.Oracle,        "Number(19)",     ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "BigInt",         ServerSideOnly=true, CanBeNull = false)]
		public static long           BigInt => 0;

		[Property(PN.MySql,         "Signed",         ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "Int",            ServerSideOnly=true, CanBeNull = false)]
		public static int            Int => 0;

		[Property(PN.MySql,         "Signed",         ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "SmallInt",       ServerSideOnly=true, CanBeNull = false)]
		public static short          SmallInt => 0;

		[Property(PN.DB2,           "SmallInt",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Informix,      "SmallInt",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Oracle,        "Number(3)",      ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.DB2,           "SmallInt",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Firebird,      "SmallInt",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.PostgreSQL,    "SmallInt",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.MySql,         "Unsigned",       ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "TinyInt",        ServerSideOnly=true, CanBeNull = false)]
		public static byte           TinyInt => 0;

		[Property(                  "Decimal",        ServerSideOnly=true, CanBeNull = false)]
		public static decimal        DefaultDecimal => 0m;

		[Expression(PN.SapHana,     "Decimal({0},4)", ServerSideOnly=true, CanBeNull = false)]
		[Function(                                    ServerSideOnly=true, CanBeNull = false)]
		public static decimal        Decimal(int precision) => 0m;

		[Function(                                    ServerSideOnly=true, CanBeNull = false)]
		public static decimal        Decimal(int precision, int scale) => 0m;

		[Property(PN.Oracle,        "Number(19,4)",   ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Firebird,      "Decimal(18,4)",  ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.PostgreSQL,    "Decimal(19,4)",  ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.MySql,         "Decimal(19,4)",  ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SapHana,       "Decimal(19,4)",  ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "Money",          ServerSideOnly=true, CanBeNull = false)]
		public static decimal        Money => 0m;

		[Property(PN.Informix,      "Decimal(10,4)",  ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Oracle,        "Number(10,4)",   ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Firebird,      "Decimal(10,4)",  ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.PostgreSQL,    "Decimal(10,4)",  ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.MySql,         "Decimal(10,4)",  ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlCe,         "Decimal(10,4)",  ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SapHana,       "Decimal(10,4)",  ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "SmallMoney",     ServerSideOnly=true, CanBeNull = false)]
		public static decimal        SmallMoney => 0m;

		[Property(PN.MySql,         "Decimal(29,10)", ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SapHana,       "Double",         ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "Float",          ServerSideOnly=true, CanBeNull = false)]
		public static double         Float => 0.0;

		[Property(PN.MySql,         "Decimal(29,10)", ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "Real",           ServerSideOnly=true, CanBeNull = false)]
		public static float          Real => 0f;

		[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "DateTime",       ServerSideOnly=true, CanBeNull = false)]
		public static DateTime       DateTime => DateTime.Now;

		[Property(PN.SqlServer2005, "DateTime",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.MySql,         "DateTime",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlCe,         "DateTime",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Sybase,        "DateTime",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "DateTime2",      ServerSideOnly=true, CanBeNull = false)]
		public static DateTime       DateTime2 => DateTime.Now;

		[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.MySql,         "DateTime",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlCe,         "DateTime",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SapHana,       "SecondDate",     ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "SmallDateTime",  ServerSideOnly=true, CanBeNull = false)]
		public static DateTime       SmallDateTime => DateTime.Now;

		[Property(PN.SqlServer2005, "Datetime",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlCe,         "Datetime",       ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "Date",           ServerSideOnly=true, CanBeNull = false)]
		public static DateTime       Date => DateTime.Now;
#if NET6_0_OR_GREATER
		[Property(PN.SqlServer2005, "Datetime",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlCe,         "Datetime",       ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "Date",           ServerSideOnly=true, CanBeNull = false)]
		public static DateOnly       DateOnly => DateOnly.FromDateTime(DateTime.Now);
#endif

		[Property(                  "Time",           ServerSideOnly=true, CanBeNull = false)]
		public static DateTime       Time => DateTime.Now;

		[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlServer2019, "DateTimeOffset", ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlServer2017, "DateTimeOffset", ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlServer2016, "DateTimeOffset", ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlServer2014, "DateTimeOffset", ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlServer2012, "DateTimeOffset", ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlServer2008, "DateTimeOffset", ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "DateTime",       ServerSideOnly=true, CanBeNull = false)]
		public static DateTimeOffset DateTimeOffset => DateTimeOffset.Now;

		[Function(PN.SqlCe,         "NChar",          ServerSideOnly=true, CanBeNull = false)]
		[Function(                                    ServerSideOnly=true, CanBeNull = false)]
		public static string  Char(int length) => "";

		[Property(PN.SqlCe,         "NChar",          ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "Char",           ServerSideOnly=true, CanBeNull = false)]
		public static string  DefaultChar => "";

		[Function(PN.MySql,         "Char",           ServerSideOnly=true, CanBeNull = false)]
		[Function(PN.SqlCe,         "NVarChar",       ServerSideOnly=true, CanBeNull = false)]
		[Function(                                    ServerSideOnly=true, CanBeNull = false)]
		public static string  VarChar(int length) => "";

		[Property(PN.MySql,         "Char",           ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.SqlCe,         "NVarChar",       ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "VarChar",        ServerSideOnly=true, CanBeNull = false)]
		public static string  DefaultVarChar => "";

		[Function(PN.DB2,           "Char",           ServerSideOnly=true, CanBeNull = false)]
		[Function(                                    ServerSideOnly=true, CanBeNull = false)]
		public static string  NChar(int length) => "";

		[Property(PN.DB2,           "Char",           ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "NChar",          ServerSideOnly=true, CanBeNull = false)]
		public static string  DefaultNChar => "";

		[Function(PN.DB2,           "Char",           ServerSideOnly=true, CanBeNull = false)]
		[Function(PN.Oracle,        "VarChar2",       ServerSideOnly=true, CanBeNull = false)]
		[Function(PN.Firebird,      "VarChar",        ServerSideOnly=true, CanBeNull = false)]
		[Function(PN.PostgreSQL,    "VarChar",        ServerSideOnly=true, CanBeNull = false)]
		[Function(PN.MySql,         "Char",           ServerSideOnly=true, CanBeNull = false)]
		[Function(                                    ServerSideOnly=true, CanBeNull = false)]
		public static string  NVarChar(int length) => "";

		[Property(PN.DB2,           "Char",           ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Oracle,        "VarChar2",       ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.Firebird,      "VarChar",        ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.PostgreSQL,    "VarChar",        ServerSideOnly=true, CanBeNull = false)]
		[Property(PN.MySql,         "Char",           ServerSideOnly=true, CanBeNull = false)]
		[Property(                  "NVarChar",       ServerSideOnly=true, CanBeNull = false)]
		public static string  DefaultNVarChar => "";
	}
}
