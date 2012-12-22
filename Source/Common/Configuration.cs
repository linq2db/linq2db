using System;

namespace LinqToDB.Common
{
	public static class Configuration
	{
		public static bool IsStructIsScalarType = true;

		public static class Linq
		{
			public static bool PreloadGroups          { get; set; }
			public static bool IgnoreEmptyUpdate      { get; set; }
			public static bool AllowMultipleQuery     { get; set; }
			public static bool GenerateExpressionTest { get; set; }
		}

		public static class NullableValues
		{
			public static Int32          Int32          = 0;
			public static Double         Double         = 0;
			public static Int16          Int16          = 0;
			public static Boolean        Boolean        = false;
			[CLSCompliant(false)]
			public static SByte          SByte          = 0;
			public static Int64          Int64          = 0;
			public static Byte           Byte           = 0;
			[CLSCompliant(false)]
			public static UInt16         UInt16         = 0;
			[CLSCompliant(false)]
			public static UInt32         UInt32         = 0;
			[CLSCompliant(false)]
			public static UInt64         UInt64         = 0;
			public static Single         Single         = 0;
			public static Char           Char           = '\x0';
			public static DateTime       DateTime       = DateTime.MinValue;
			public static TimeSpan       TimeSpan       = TimeSpan.MinValue;
			public static DateTimeOffset DateTimeOffset = DateTimeOffset.MinValue;
			public static Decimal        Decimal        = 0m;
			public static Guid           Guid           = Guid.Empty;
			public static String         String         = null;
		}
	}
}
