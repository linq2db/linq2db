using PN = LinqToDB.ProviderName;
using System;

namespace LinqToDB
{
	public partial class Sql
	{
		public static class Types
		{
			[Property(PN.Informix,      "Boolean",        ServerSideOnly=true)]
			[Property(PN.PostgreSQL,    "Boolean",        ServerSideOnly=true)]
			[Property(PN.MySql,         "Boolean",        ServerSideOnly=true)]
			[Property(PN.SQLite,        "Boolean",        ServerSideOnly=true)]
			[Property(PN.SapHana,       "TinyInt",        ServerSideOnly=true)]
			[Property(                  "Bit",            ServerSideOnly=true)] 
			public static bool           Bit => false;

			[Property(PN.Oracle,        "Number(19)",     ServerSideOnly=true)]
			[Property(                  "BigInt",         ServerSideOnly=true)] 
			public static long           BigInt => 0;

			[Property(PN.MySql,         "Signed",         ServerSideOnly=true)]
			[Property(                  "Int",            ServerSideOnly=true)]
			public static int            Int => 0;

			[Property(PN.MySql,         "Signed",         ServerSideOnly=true)]
			[Property(                  "SmallInt",       ServerSideOnly=true)]
			public static short          SmallInt => 0;

			[Property(PN.DB2,           "SmallInt",       ServerSideOnly=true)]
			[Property(PN.Informix,      "SmallInt",       ServerSideOnly=true)]
			[Property(PN.Oracle,        "Number(3)",      ServerSideOnly=true)]
			[Property(PN.DB2,           "SmallInt",       ServerSideOnly=true)]
			[Property(PN.Firebird,      "SmallInt",       ServerSideOnly=true)]
			[Property(PN.PostgreSQL,    "SmallInt",       ServerSideOnly=true)]
			[Property(PN.MySql,         "Unsigned",       ServerSideOnly=true)]
			[Property(                  "TinyInt",        ServerSideOnly=true)]
			public static byte           TinyInt => 0;

			[Property(                  "Decimal",        ServerSideOnly=true)]
			public static decimal        DefaultDecimal => 0m;

			[Expression(PN.SapHana,     "Decimal({0},4)", ServerSideOnly=true)]
			[Function(                                    ServerSideOnly=true)]
			public static decimal        Decimal(int precision) => 0m;

			[Function(                                    ServerSideOnly=true)]
			public static decimal        Decimal(int precision, int scale) => 0m;

			[Property(PN.Oracle,        "Number(19,4)",   ServerSideOnly=true)]
			[Property(PN.Firebird,      "Decimal(18,4)",  ServerSideOnly=true)]
			[Property(PN.PostgreSQL,    "Decimal(19,4)",  ServerSideOnly=true)]
			[Property(PN.MySql,         "Decimal(19,4)",  ServerSideOnly=true)]
			[Property(PN.SapHana,       "Decimal(19,4)",  ServerSideOnly=true)]
			[Property(                  "Money",          ServerSideOnly=true)]
			public static decimal        Money => 0m;

			[Property(PN.Informix,      "Decimal(10,4)",  ServerSideOnly=true)]
			[Property(PN.Oracle,        "Number(10,4)",   ServerSideOnly=true)]
			[Property(PN.Firebird,      "Decimal(10,4)",  ServerSideOnly=true)]
			[Property(PN.PostgreSQL,    "Decimal(10,4)",  ServerSideOnly=true)]
			[Property(PN.MySql,         "Decimal(10,4)",  ServerSideOnly=true)]
			[Property(PN.SqlCe,         "Decimal(10,4)",  ServerSideOnly=true)]
			[Property(PN.SapHana,       "Decimal(10,4)",  ServerSideOnly=true)]
			[Property(                  "SmallMoney",     ServerSideOnly=true)] 
			public static decimal        SmallMoney => 0m;

			[Property(PN.MySql,         "Decimal(29,10)", ServerSideOnly=true)]
			[Property(PN.SapHana,       "Double",         ServerSideOnly=true)]
			[Property(                  "Float",          ServerSideOnly=true)]
			public static double         Float => 0.0;

			[Property(PN.MySql,         "Decimal(29,10)", ServerSideOnly=true)]
			[Property(                  "Real",           ServerSideOnly=true)]
			public static float          Real => 0f;

			[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
			[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
			[Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true)]
			[Property(                  "DateTime",       ServerSideOnly=true)]
			public static DateTime       DateTime => DateTime.Now;

			[Property(PN.SqlServer2005, "DateTime",       ServerSideOnly=true)]
			[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
			[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
			[Property(PN.MySql,         "DateTime",       ServerSideOnly=true)]
			[Property(PN.SqlCe,         "DateTime",       ServerSideOnly=true)]
			[Property(PN.Sybase,        "DateTime",       ServerSideOnly=true)]
			[Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true)]
			[Property(                  "DateTime2",      ServerSideOnly=true)]
			public static DateTime       DateTime2 => DateTime.Now;

			[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
			[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
			[Property(PN.MySql,         "DateTime",       ServerSideOnly=true)]
			[Property(PN.SqlCe,         "DateTime",       ServerSideOnly=true)]
			[Property(PN.SapHana,       "SecondDate",     ServerSideOnly=true)]
			[Property(                  "SmallDateTime",  ServerSideOnly=true)]
			public static DateTime       SmallDateTime => DateTime.Now;

			[Property(PN.SqlServer2005, "Datetime",       ServerSideOnly=true)]
			[Property(PN.SqlCe,         "Datetime",       ServerSideOnly=true)]
			[Property(                  "Date",           ServerSideOnly=true)]
			public static DateTime       Date => DateTime.Now;

			[Property(                  "Time",           ServerSideOnly=true)]
			public static DateTime       Time => DateTime.Now;

			[Property(PN.PostgreSQL,    "TimeStamp",      ServerSideOnly=true)]
			[Property(PN.Firebird,      "TimeStamp",      ServerSideOnly=true)]
			[Property(PN.SqlServer2019, "DateTimeOffset", ServerSideOnly=true)]
			[Property(PN.SqlServer2017, "DateTimeOffset", ServerSideOnly=true)]
			[Property(PN.SqlServer2016, "DateTimeOffset", ServerSideOnly=true)]
			[Property(PN.SqlServer2014, "DateTimeOffset", ServerSideOnly=true)]
			[Property(PN.SqlServer2012, "DateTimeOffset", ServerSideOnly=true)]
			[Property(PN.SqlServer2008, "DateTimeOffset", ServerSideOnly=true)]
			[Property(PN.SapHana,       "TimeStamp",      ServerSideOnly=true)]
			[Property(                  "DateTime",       ServerSideOnly=true)]
			public static DateTimeOffset DateTimeOffset => DateTimeOffset.Now;

			[Function(PN.SqlCe,         "NChar",          ServerSideOnly=true)]
			[Function(                                    ServerSideOnly=true)]
			public static string  Char(int length) => "";

			[Property(PN.SqlCe,         "NChar",          ServerSideOnly=true)]
			[Property(                  "Char",           ServerSideOnly=true)]
			public static string  DefaultChar => "";

			[Function(PN.MySql,         "Char",           ServerSideOnly=true)]
			[Function(PN.SqlCe,         "NVarChar",       ServerSideOnly=true)]
			[Function(                                    ServerSideOnly=true)]
			public static string  VarChar(int length) => "";

			[Property(PN.MySql,         "Char",           ServerSideOnly=true)]
			[Property(PN.SqlCe,         "NVarChar",       ServerSideOnly=true)]
			[Property(                  "VarChar",        ServerSideOnly=true)]
			public static string  DefaultVarChar => "";

			[Function(PN.DB2,           "Char",           ServerSideOnly=true)]
			[Function(                                    ServerSideOnly=true)]
			public static string  NChar(int length) => "";

			[Property(PN.DB2,           "Char",           ServerSideOnly=true)]
			[Property(                  "NChar",          ServerSideOnly=true)]
			public static string  DefaultNChar => "";

			[Function(PN.DB2,           "Char",           ServerSideOnly=true)]
			[Function(PN.Oracle,        "VarChar2",       ServerSideOnly=true)]
			[Function(PN.Firebird,      "VarChar",        ServerSideOnly=true)]
			[Function(PN.PostgreSQL,    "VarChar",        ServerSideOnly=true)]
			[Function(PN.MySql,         "Char",           ServerSideOnly=true)]
			[Function(                                    ServerSideOnly=true)]
			public static string  NVarChar(int length) => "";

			[Property(PN.DB2,           "Char",           ServerSideOnly=true)]
			[Property(PN.Oracle,        "VarChar2",       ServerSideOnly=true)]
			[Property(PN.Firebird,      "VarChar",        ServerSideOnly=true)]
			[Property(PN.PostgreSQL,    "VarChar",        ServerSideOnly=true)]
			[Property(PN.MySql,         "Char",           ServerSideOnly=true)]
			[Property(                  "NVarChar",       ServerSideOnly=true)]
			public static string  DefaultNVarChar => "";
		}
	}
}
