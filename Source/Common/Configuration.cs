using System;

namespace LinqToDB.Common
{
	using Mapping;

	public static class Configuration
	{
		public enum NullEquivalent { DBNull, Null, Value }

		/// <summary>
		/// Controls global trimming behaviour of mapper. Specifies whether trailing spaces
		/// should be trimmed when mapping from one entity to another. Default is: false. 
		/// To specify trimming behaviour other than global, please user <see cref="TrimmableAttribute"/>.
		/// </summary>
		public static bool TrimOnMapping { get; set; }

		/// <summary>
		/// Controls whether attributes specified on base types should be always added to list of attributes
		/// when scanning hierarchy tree or they should be compared to attributes found on derived classes
		/// and added only when not present already. Default value: false;
		/// WARNING: setting this flag to "true" can significantly affect initial object generation/access performance
		/// use only when side effects are noticed with attribute being present on derived and base classes. 
		/// For builder attributes use provided attribute compatibility mechanism.
		/// </summary>
		public static bool FilterOutBaseEqualAttributes { get; set; }

		public static class Linq
		{
			public static bool PreloadGroups      { get; set; }
			public static bool IgnoreEmptyUpdate  { get; set; }
			public static bool AllowMultipleQuery { get; set; }
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
